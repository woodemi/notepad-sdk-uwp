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
        private readonly NotepadClient _notepadClient;

        public NotepadType(NotepadClient notepadClient)
        {
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
            await NotepadCorePlatform.Instance.SetNotifiable(serviceCharacteristic, inputProperty);
        }

        // FIXME Windows lacks BLE-MTU API
        public int mtu = 247;

        private async Task SendValue((string, string) serviceCharacteristic, byte[] request)
        {
            await NotepadCorePlatform.Instance.WriteValue(serviceCharacteristic, request);
        }

        public async Task SendRequestAsync(string messageHead, (string, string) serviceCharacteristic, byte[] request)
        {
            await SendValue(serviceCharacteristic, request);
            Debug.WriteLine($"on{messageHead}Send: {request.ToHexString()}");
        }

        private IObservable<byte[]> ReceiveValue((string service, string characteristic) tuple)
        {
            var observable =
                Observable.FromEvent<TypedEventHandler<string, byte[]>, (string characteristic, byte[] value)>(
                    rxHandler => (sender, args) => rxHandler((sender, args)),
                    handler => NotepadCorePlatform.Instance.InputReceived += handler,
                    handler => NotepadCorePlatform.Instance.InputReceived -= handler);
            return observable.Where(e => e.characteristic == tuple.characteristic).Select(e => e.value);
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

        public IObservable<byte[]> ReceiveSyncInput() =>
            ReceiveValue(_notepadClient.SyncInputCharacteristic).Select(value =>
            {
                Debug.WriteLine($"OnSyncInputReceive: {value.ToHexString()}");
                return value;
            });

        public async Task<Response> ExecuteFileInputControl<Response>(WoodemiCommand<Response> command)
        {
            var receiveResponse = ReceiveResponseAsync("FileInputControl",
                _notepadClient.FileInputControlResponseCharacteristic,
                command.intercept);
            await SendRequestAsync("FileInputControl", _notepadClient.FileInputControlRequestCharacteristic,
                command.request);
            return command.handle(await receiveResponse);
        }

        public IObservable<byte[]> ReceiveFileInput() =>
            ReceiveValue(_notepadClient.FileInputCharacteristic).Select(value =>
            {
                Debug.WriteLine($"onFileInputReceive: {value.ToHexString()}");
                return value;
            });
    }
}