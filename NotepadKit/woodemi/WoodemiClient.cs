using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static readonly byte[] DEFAULT_AUTH_TOKEN = {0x00, 0x00, 0x00, 0x01};

        private static readonly int A1_WIDTH = 14800;
        private static readonly int A1_HEIGHT = 21000;

        public override (string, string) CommandRequestCharacteristic => (SERV__COMMAND, CHAR__COMMAND_REQUEST);

        public override (string, string) CommandResponseCharacteristic => (SERV__COMMAND, CHAR__COMMAND_RESPONSE);

        public override (string, string) SyncInputCharacteristic => (SERV__SYNC, CHAR__SYNC_INPUT);

        public override (string, string) FileInputControlRequestCharacteristic =>
            (SERV__FILE_INPUT, CHAR__FILE_INPUT_CONTROL_REQUEST);

        public override (string, string) FileInputControlResponseCharacteristic =>
            (SERV__FILE_INPUT, CHAR__FILE_INPUT_CONTROL_RESPONSE);

        public override IReadOnlyList<(string, string)> InputIndicationCharacteristics => new List<(string, string)>
        {
            CommandResponseCharacteristic,
            FileInputControlResponseCharacteristic
        };

        public override IReadOnlyList<(string, string)> InputNotificationCharacteristics => new List<(string, string)>
        {
            SyncInputCharacteristic
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
                    var chars = reader.ReadBytes(FileInfo.Item2.Length).Select(b => (char) b).ToArray();
                    var createdAt = Convert.ToInt32(new string(chars));
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


            var request = new byte[] {0x02}.Concat(FileInfo.Item1).Concat(FileInfo.Item2).ToArray();
            return await _notepadType.ExecuteFileInputControl(new WoodemiCommand<MemoInfo>
            {
                request = request,
                intercept = bytes => bytes.First() == 0x03,
                handle = Handle
            });
        }

        private readonly (byte[], byte[]) FileInfo = (
            // imageId
            new byte[] {0x00, 0x01},
            // imageVersion
            new byte[]
            {
                0x01, 0x00, 0x00, // Build Version
                0x41, // Stack Version
                0x11, 0x11, 0x11, // Hardware Id
                0x01 // Manufacturer Id
            }
        );
    }
}