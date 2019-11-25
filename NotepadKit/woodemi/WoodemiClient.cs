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

        private static readonly byte[] DEFAULT_AUTH_TOKEN = {0x00, 0x00, 0x00, 0x01};

        private static readonly int A1_WIDTH = 14800;
        private static readonly int A1_HEIGHT = 21000;

        public override (string, string) CommandRequestCharacteristic => (SERV__COMMAND, CHAR__COMMAND_REQUEST);

        public override (string, string) CommandResponseCharacteristic => (SERV__COMMAND, CHAR__COMMAND_RESPONSE);

        public override (string, string) SyncInputCharacteristic => (SERV__SYNC, CHAR__SYNC_INPUT);

        public override IReadOnlyList<(string, string)> InputIndicationCharacteristics => new List<(string, string)>
        {
            CommandResponseCharacteristic
        };

        public override IReadOnlyList<(string, string)> InputNotificationCharacteristics => new List<(string, string)>
        {
            SyncInputCharacteristic
        };

        internal override async Task CompleteConnection(Action<bool> awaitConfirm)
        {
            await base.CompleteConnection(awaitConfirm);
            await CheckAccess(DEFAULT_AUTH_TOKEN, 10, awaitConfirm);
        }

        private enum AccessResult
        {
            Denied, // Device claimed by other user
            Confirmed, // Access confirmed, indicating device not claimed by anyone
            Unconfirmed, // Access unconfirmed, as user doesn't confirm before timeout
            Approved // Device claimed by this user
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
    }
}