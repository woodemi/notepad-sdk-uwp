using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace NotepadKit
{
    public class NotepadConnector
    {
        private BluetoothLEDevice _bluetoothDevice;
        private NotepadClient _notepadClient;
        private NotepadType _notepadType;

        public event TypedEventHandler<NotepadClient, ConnectionState> ConnectionChanged;

        public async void Connect(NotepadScanResult scanResult)
        {
            Debug.WriteLine("NotepadConnector::Connect");
            Connect_(scanResult);
            ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Connecting);
        }

        private async Task Connect_(NotepadScanResult scanResult)
        {
            var bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(scanResult.BluetoothAddress);
            var gattDeviceServicesResult = await bluetoothDevice.GetGattServicesAsync();
            Debug.WriteLine($"Connect_ GetGattServicesAsync {gattDeviceServicesResult.Status}");
            if (gattDeviceServicesResult.Status != GattCommunicationStatus.Success)
            {
                ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Disconnected);
                return;
            }

            _bluetoothDevice = bluetoothDevice;
            _bluetoothDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            _notepadClient = NotepadHelper.Create(scanResult);
            _notepadType = new NotepadType(_notepadClient, new BleType(_bluetoothDevice));

            await _notepadType.ConfigCharacteristics();
            await _notepadClient.CompleteConnection(awaitConfirm =>
                ConnectionChanged?.Invoke(_notepadClient, ConnectionState.AwaitConfirm));

            ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Connected);
        }

        public void Disconnect()
        {
            Debug.WriteLine("NotepadConnector::Disconnect");
            if (_bluetoothDevice != null)
                _bluetoothDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
            _bluetoothDevice?.Dispose();
            _bluetoothDevice = null;
        }

        private async void OnConnectionStatusChanged(BluetoothLEDevice device, object args)
        {
            Debug.WriteLine(
                $"OnConnectionStatusChanged {device.BluetoothAddress}, {device.ConnectionStatus.ToString()}");
            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
                ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Disconnected);
        }
    }
}