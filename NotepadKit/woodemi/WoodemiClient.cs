using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace NotepadKit
{
    internal class WoodemiClient : NotepadClient
    {
        private static readonly string SUFFIX = "BA5E-F4EE-5CA1-EB1E5E4B1CE0";

        private static readonly string SERV__COMMAND = $"57444D01-{SUFFIX}";
        private static readonly string CHAR__COMMAND_REQUEST = $"57444E02-{SUFFIX}";
        private static readonly string CHAR__COMMAND_RESPONSE = CHAR__COMMAND_REQUEST;

        private static readonly string SERV__SYNC = $"57444D06-{SUFFIX}";
        private static readonly string CHAR__SYNC_INPUT = $"57444D07-{SUFFIX}";

        private static readonly string SERV__FILE_INPUT = $"57444D03-{SUFFIX}";
        private static readonly string CHAR__FILE_INPUT_CONTROL_REQUEST = $"57444D04-{SUFFIX}";
        private static readonly string CHAR__FILE_INPUT_CONTROL_RESPONSE = CHAR__FILE_INPUT_CONTROL_REQUEST;
        private static readonly string CHAR__FILE_INPUT = $"57444D05-{SUFFIX}";

        private static readonly byte[] DEFAULT_AUTH_TOKEN = {0x00, 0x00, 0x00, 0x01};

        private static readonly int A1_WIDTH = 14800;
        private static readonly int A1_HEIGHT = 21000;

        private static readonly int SAMPLE_INTERVAL_MS = 5;

        public override (string, string) CommandRequestCharacteristic => (SERV__COMMAND, CHAR__COMMAND_REQUEST);

        public override (string, string) CommandResponseCharacteristic => (SERV__COMMAND, CHAR__COMMAND_RESPONSE);

        public override (string, string) SyncInputCharacteristic => (SERV__SYNC, CHAR__SYNC_INPUT);

        public override (string, string) FileInputControlRequestCharacteristic =>
            (SERV__FILE_INPUT, CHAR__FILE_INPUT_CONTROL_REQUEST);

        public override (string, string) FileInputControlResponseCharacteristic =>
            (SERV__FILE_INPUT, CHAR__FILE_INPUT_CONTROL_RESPONSE);

        public override (string, string) FileInputCharacteristic => (SERV__FILE_INPUT, CHAR__FILE_INPUT);

        public override IReadOnlyList<(string, string)> InputIndicationCharacteristics => new List<(string, string)>
        {
            CommandResponseCharacteristic,
            FileInputControlResponseCharacteristic
        };

        public override IReadOnlyList<(string, string)> InputNotificationCharacteristics => new List<(string, string)>
        {
            SyncInputCharacteristic,
            FileInputCharacteristic
        };

        internal override async Task CompleteConnection(Action<bool> awaitConfirm)
        {
            var accessResult = await CheckAccess(DEFAULT_AUTH_TOKEN, 10, awaitConfirm);
            switch (accessResult)
            {
                case AccessResult.Denied:
                    throw AccessException.Denied;
                case AccessResult.Unconfirmed:
                    throw AccessException.Unconfirmed;
            }

            await base.CompleteConnection(awaitConfirm);
        }

        private async Task<AccessResult> CheckAccess(byte[] authToken, int seconds, Action<bool> awaitConfirm)
        {
            var command = new WoodemiCommand<byte>
            {
                request = new byte[] {0x01, (byte) seconds}.Concat(authToken).ToArray(),
                intercept = bytes => bytes.First() == 0x02,
                handle = bytes => bytes[1]
            };
            switch (await _notepadType.ExecuteCommand(command))
            {
                case 0x00:
                    return AccessResult.Denied;
                case 0x01:
                    awaitConfirm(true);
                    var confirm = await _notepadType.ReceiveResponseAsync("Confirm", CommandResponseCharacteristic,
                        bytes => bytes.First() == 0x03);
                    return confirm[1] == 0 ? AccessResult.Confirmed : AccessResult.Unconfirmed;
                case 0x02:
                    return AccessResult.Approved;
                default:
                    throw new Exception("Unknown error");
            }
        }

        #region SyncInput

        public override async Task SetMode(NotepadMode mode)
        {
            var command = new WoodemiCommand<byte[]>
            {
                request = new byte[] {0x05, 0x00},
                intercept = bytes => bytes.First() == 0x07 && bytes[1] == 0x05,
                handle = bytes => bytes
            };
            var response = await _notepadType.ExecuteCommand(command);
            if (response[4] != 0)
                throw new Exception($"WOODEMI_COMMAND fail: response {response.ToHexString()}");
        }

        protected override List<NotePenPointer> ParseSyncData(byte[] value)
        {
            return NotePenPointer.Create(value)
                .Where(p => 0 <= p.x && p.x <= A1_WIDTH && 0 <= p.y && p.y <= A1_HEIGHT).ToList();
        }

        #endregion

        #region ImportMemo

        public override async Task<MemoSummary> GetMemoSummary()
        {
            MemoSummary Handle(byte[] bytes)
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    reader.ReadBytes(1); // Skip response tag
                    return new MemoSummary
                    {
                        totalCapacity = reader.ReadUInt32(),
                        freeCapacity = reader.ReadUInt32(),
                        usedCapacity = reader.ReadUInt32(),
                        memoCount = reader.ReadUInt16()
                    };
                }
            }

            return await _notepadType.ExecuteCommand(new WoodemiCommand<MemoSummary>
            {
                request = new byte[] {0x08, 0x02},
                intercept = bytes => bytes.First() == 0x0D,
                handle = Handle
            });
        }

        public override async Task<MemoInfo> GetMemoInfo()
        {
            var largeDataInfo = await GetLargeDataInfo();
            return new MemoInfo
            {
                sizeInByte = largeDataInfo.sizeInByte - ImageTransmission.EMPTY_LENGTH,
                createdAt = largeDataInfo.createdAt,
                partIndex = largeDataInfo.partIndex,
                restCount = largeDataInfo.restCount
            };
        }

        private async Task<MemoInfo> GetLargeDataInfo()
        {
            MemoInfo Handle(byte[] bytes)
            {
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    reader.ReadBytes(1); // Skip response tag
                    int partIndex = reader.ReadByte();
                    int restCount = reader.ReadByte();
                    var chars = reader.ReadBytes(FileInfo.imageVersion.Length).Select(b => (char) b).ToArray();
                    var createdAt = Convert.ToInt32(new string(chars), 16);
                    var sizeInByte = reader.ReadUInt32();
                    return new MemoInfo
                    {
                        sizeInByte = sizeInByte,
                        createdAt = createdAt,
                        partIndex = partIndex,
                        restCount = restCount
                    };
                }
            }


            var request = new byte[] {0x02}.Concat(FileInfo.imageId).Concat(FileInfo.imageVersion).ToArray();
            return await _notepadType.ExecuteFileInputControl(new WoodemiCommand<MemoInfo>
            {
                request = request,
                intercept = bytes => bytes.First() == 0x03,
                handle = Handle
            });
        }

        private readonly (byte[] imageId, byte[] imageVersion) FileInfo = (
            new byte[] {0x00, 0x01},
            new byte[]
            {
                0x01, 0x00, 0x00, // Build Version
                0x41, // Stack Version
                0x11, 0x11, 0x11, // Hardware Id
                0x01 // Manufacturer Id
            }
        );

        /**
         * Memo is kind of LargeData, transferred in data structure [ImageTransmission]
         * +------------------------------------------------------------+
         * |                            LargeData                       |
         * +------------------------+----------+------------------------+
         * | [ImageTransmission]    |   ...    |  [ImageTransmission]   |
         * +------------------------+----------+------------------------+
         */
        public override async Task<MemoData> ImportMemo(Action<int> progress)
        {
            var info = await GetLargeDataInfo();
            if (info.sizeInByte <= ImageTransmission.EMPTY_LENGTH) throw new Exception("No memo");

            var imageData = await RequestTransmission(info.sizeInByte, progress);
            return new MemoData {memoInfo = info, pointers = ParseMemo(imageData, info.createdAt).ToList()};
        }

        private IEnumerable<NotePenPointer> ParseMemo(byte[] bytes, long createdAt)
        {
            var byteGroups = bytes.Select((b, index) => (b, index)).GroupBy(g => g.index / 6, e => e.b);
            var byteParts = byteGroups.Select(g => g.AsEnumerable().ToArray());
            var start = createdAt;
            foreach (var byteList in byteParts)
            {
                if (byteList[4] == 0xFF && byteList[5] == 0xFF)
                    start = BitConverter.ToUInt32(byteList, 0);
                else
                    yield return new NotePenPointer(
                        BitConverter.ToUInt16(byteList, 0),
                        BitConverter.ToUInt16(byteList, 2),
                        start += SAMPLE_INTERVAL_MS,
                        BitConverter.ToUInt16(byteList, 2));
            }
        }

        /**
         * +--------------------------------+
         * |       [ImageTransmission]      |
         * +----------+----------+----------+
         * |  block   |    ...   |   block  |
         * +----------+----------+----------+
         */
        private async Task<byte[]> RequestTransmission(long totalSize, Action<int> progress)
        {
            var data = new byte[] { };
            while (data.Length < totalSize)
            {
                var currentPos = data.Length;
                var blockProgress = 0;
                var blockChunkDictionary = await (await RequestForNextBlock(currentPos, totalSize))
                    .Aggregate(new Dictionary<int, byte[]>(),
                        (Dictionary<int, byte[]> acc, (int index, byte[] value) chunk) =>
                        {
                            blockProgress += chunk.value.Length;
                            progress((int) ((currentPos + blockProgress) * 100 / totalSize));
                            acc[chunk.index] = chunk.value;
                            return acc;
                        });
                var block = blockChunkDictionary.ToImmutableSortedDictionary().Select(pair => pair.Value)
                    .Aggregate((acc, value) => acc.Concat(value).ToArray());
                Debug.WriteLine($"receiveBlock size({block.Length})");
                data = data.Concat(block).ToArray();
            }

            return ImageTransmission.forInput(data).ImageData;
        }

        /**
         * Request in file input control pipe
         * +------------+--------------------------------------------------------------------------------------------+
         * | requestTag |                                     requestData                                            |
         * +------------+----------+-------------+------------+---------------+-----------------+--------------------+
         * |            |  imageId |  currentPos |  BlockSize |  maxChunkSize |  transferMethod |  l2capChannelOrPsm |
         * |            |          |             |            |               |                 |                    |
         * | 1 byte     |  2 bytes |  4 bytes    |  4 bytes   |  2bytes       |  1 byte         |  2 bytes           |
         * +------------+----------+-------------+------------+---------------+-----------------+--------------------+
         *
         * [maxChunkSize] not larger than (0xFFFF + 1)
         *
         * Response in file input data pipe
         * +--------------------------------+
         * |             block              |
         * +----------+----------+----------+
         * | chunk    |   ...    |  chunk   |
         * +----------+----------+----------+
         */
        private async Task<IObservable<(int, byte[])>> RequestForNextBlock(int currentPos, long totalSize)
        {
            var maxChunkSize = _notepadType.mtu - 3 /*GATT_HEADER_LENGTH*/ - 1 /*responseTag*/ - 1;
            var maxBlockSize = maxChunkSize * (0xFF + 1); // chunkSeqId(1 byte) -> maxChunkPerBlock
            var blockSize = Math.Min(totalSize - currentPos, maxBlockSize);
            var transferMethod = (byte) 0x00;
            var l2capChannelOrPsm = (short) 0x0004;

            Debug.WriteLine(
                $"requestForNextBlock currentPos {currentPos}, totalSize {totalSize}, blockSize {blockSize}, maxChunkSize {maxChunkSize}");

            byte[] request;
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write((byte) 0x04);
                    writer.Write(FileInfo.imageId);
                    writer.Write(currentPos);
                    writer.Write((int) blockSize);
                    writer.Write((short) maxChunkSize);
                    writer.Write(transferMethod);
                    writer.Write(l2capChannelOrPsm);
                }

                request = stream.ToArray();
            }

            var chunkCountCeil = (int) Math.Ceiling(blockSize * 1.0 / maxChunkSize);
            var indexedChunkObservable = ReceiveChunks(chunkCountCeil);

            _notepadType.SendRequestAsync("FileInputControl", FileInputControlRequestCharacteristic, request);

            return indexedChunkObservable;
        }

        /**
         * +-------------+--------------------------+
         * | responseTag |       responseData       |
         * +-------------+-------------+------------+
         * |             |  chunkSeqId |  chunkData |
         * |             |             |            |
         * | 1 byte      |  1 byte     |  ...       |
         * +-------------+-------------+------------+
         */
        private IObservable<(int, byte[])> ReceiveChunks(int count) =>
            _notepadType.ReceiveFileInput()
                .Where(value => value.First() == 0x05)
                .Take(count)
                .Select(value => ((int) value[1], value.Skip(2).ToArray()));

        public override async Task DeleteMemo()
        {
            await _notepadType.SendRequestAsync("FileInputControl", FileInputControlRequestCharacteristic,
                new byte[] {0x06, 0x00, 0x00, 0x00});
            // FIXME Deal with 0x01 as notification instead of response
            await Task.Delay(200);
        }

        #endregion
    }
}