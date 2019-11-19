using System;
using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using Windows.Foundation;

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

        public NotepadType(NotepadClient notepadClient, BleType bleType)
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

        private void OnInputReceived(string characteristic, byte[] value)
        {
            Debug.WriteLine("OnInputReceived ");
        }

        private async Task<byte[]> ReadValue((string, string) serviceCharacteristic)
        {
            var channel = Channel.CreateUnbounded<(string, byte[])>(new UnboundedChannelOptions());
            TypedEventHandler<string, byte[]> OnInputReceived =
                (sender, args) => channel.Writer.WriteAsync((sender, args));
            _bleType.InputReceived += OnInputReceived;
            try
            {
                while (true)
                {
                    var (characteristic, value) = await channel.Reader.ReadAsync();
                    if (characteristic == serviceCharacteristic.Item2)
                        return value;
                }
            }
            finally
            {
                _bleType.InputReceived -= OnInputReceived;
            }
        }

        public async Task<byte[]> ReceiveResponseAsync(string messageHead, (string, string) serviceCharacteristic,
            Func<byte[], bool> predict)
        {
            // TODO Timeout
            while (true)
            {
                var value = await ReadValue(serviceCharacteristic);
                if (!predict(value)) continue;
                Debug.WriteLine($"on{messageHead}Receive: {value.ToHexString()}");
                return value;
            }
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