using System;
using System.Diagnostics;
using System.Reactive.Linq;
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
            foreach (var characteristic in _notepadClient.InputNotificationCharacteristics)
                await ConfigInputCharacteristic(characteristic, BleInputProperty.Notification);
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

        private IObservable<byte[]> ReceiveValue((string, string) serviceCharacteristic)
        {
            var observable = Observable.FromEvent<TypedEventHandler<string, byte[]>, (string, byte[])>(
                rxHandler => (sender, args) => rxHandler((sender, args)),
                handler => _bleType.InputReceived += handler,
                handler => _bleType.InputReceived -= handler);
            return observable.Where(e => e.Item1 == serviceCharacteristic.Item2).Select(e => e.Item2);
        }

        public async Task<byte[]> ReceiveResponseAsync(string messageHead, (string, string) serviceCharacteristic,
            Func<byte[], bool> predict)
        {
            var value = await ReceiveValue(serviceCharacteristic).FirstAsync(predict);
            Debug.WriteLine($"on{messageHead}Receive: {value.ToHexString()}");
            return value;
        }

        public async Task<Response> ExecuteCommand<Response>(WoodemiCommand<Response> command)
        {
            var receiveResponse = ReceiveResponseAsync("Command", _notepadClient.CommandResponseCharacteristic,
                command.intercept);
            await SendRequestAsync("Command", _notepadClient.CommandRequestCharacteristic, command.request);
            return command.handle(await receiveResponse);
        }

        public IObservable<byte[]> ReceiveSyncInput()
        {
            return ReceiveValue(_notepadClient.SyncInputCharacteristic).Select(
                value =>
                {
                    Debug.WriteLine($"OnSyncInputReceive: {value.ToHexString()}");
                    return value;
                });
        }

        public async Task<Response> ExecuteFileInputControl<Response>(WoodemiCommand<Response> command)
        {
            var receiveResponse = ReceiveResponseAsync("FileInputControl", _notepadClient.FileInputControlResponseCharacteristic,
                command.intercept);
            await SendRequestAsync("FileInputControl", _notepadClient.FileInputControlRequestCharacteristic, command.request);
            return command.handle(await receiveResponse);
        }
    }
}