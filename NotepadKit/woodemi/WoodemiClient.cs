using System.Collections.Generic;
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

        public override (string, string) CommandRequestCharacteristic => (SERV__COMMAND, CHAR__COMMAND_REQUEST);

        public override (string, string) CommandResponseCharacteristic => (SERV__COMMAND, CHAR__COMMAND_RESPONSE);

        public override IReadOnlyList<(string, string)> InputIndicationCharacteristics => new List<(string, string)>
        {
            CommandResponseCharacteristic
        };

        internal override async Task CompleteConnection()
        {
            await CheckAccess(new byte[] {0x00, 0x00, 0x00, 0x01}, 10);
        }

        private async Task CheckAccess(byte[] authToken, int seconds)
        {
            var command = new WoodemiCommand<object>
            {
                request = new byte[] {0x01, (byte) seconds}.Concat(authToken).ToArray(),
                intercept = bytes => bytes.First() == 0x02,
                handle = null
            };
            await _notepadType.ExecuteCommand(command);
        }
    }
}