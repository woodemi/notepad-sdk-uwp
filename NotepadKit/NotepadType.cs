using System.Diagnostics;
using System.Threading.Tasks;

namespace NotepadKit
{
    internal enum BleInputProperty
    {
        Disabled,
        Notification,
        Indication
    }

    internal class NotepadType
    {
        private readonly BleType _bleType;
        private readonly NotepadClient _notepadClient;

        internal NotepadType(NotepadClient notepadClient, BleType bleType)
        {
            _bleType = bleType;
            _notepadClient = notepadClient;
            _notepadClient._notepadType = this;
        }

        public async Task ConfigCharacteristics()
        {
            foreach (var characteristic in _notepadClient.InputIndicationCharacteristics)
                await ConfigInputCharacteristic(characteristic, BleInputProperty.Indication);
        }

        private async Task ConfigInputCharacteristic((string, string) serviceCharacteristic,
            BleInputProperty inputProperty)
        {
            await _bleType.SetNotifiable(serviceCharacteristic, inputProperty);
        }

        private async Task SendValue((string, string) serviceCharacteristic, byte[] request)
        {
            await _bleType.WriteValue(serviceCharacteristic, request);
        }

        private async Task SendRequestAsync(string messageHead, (string, string) serviceCharacteristic, byte[] request)
        {
            await SendValue(serviceCharacteristic, request);
            Debug.WriteLine($"on{messageHead}Send: {request.ToHexString()}");
        }

        public async Task ExecuteCommand<Response>(WoodemiCommand<Response> command)
        {
            await SendRequestAsync("Command", _notepadClient.CommandRequestCharacteristic, command.request);
        }
    }
}