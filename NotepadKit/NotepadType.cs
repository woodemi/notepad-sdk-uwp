using System;
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

        private async Task<byte[]> ReceiveValue((string, string) serviceCharacteristic)
        {
            var (characteristicId, value) = await _bleType.InputChannelReader.ReadAsync();
            if (characteristicId != serviceCharacteristic.Item2)
                throw new Exception("Unknown response");

            return value;
        }

        public async Task<byte[]> ReceiveResponseAsync(string messageHead, (string, string) serviceCharacteristic,
            Func<byte[], bool> predict)
        {
            var value = await ReceiveValue(serviceCharacteristic);
            Debug.WriteLine($"on{messageHead}Receive: {value.ToHexString()}");
            return value;
        }

        public async Task<Response> ExecuteCommand<Response>(WoodemiCommand<Response> command)
        {
            await SendRequestAsync("Command", _notepadClient.CommandRequestCharacteristic, command.request);
            var response = await ReceiveResponseAsync("Command", _notepadClient.CommandResponseCharacteristic,
                command.intercept);
            return command.handle(response);
        }
    }
}