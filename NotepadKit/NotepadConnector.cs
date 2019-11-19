using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth;

namespace NotepadKit
{
    public class NotepadConnector
    {
        private BluetoothLEDevice _bluetoothDevice;
        private NotepadClient _notepadClient;
        private NotepadType _notepadType;

        public async void Connect(NotepadScanResult scanResult)
        {
            Debug.WriteLine("NotepadConnector::Connect");
            _bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(scanResult.BluetoothAddress);
            _bluetoothDevice.ConnectionStatusChanged += OnConnectionStatusChanged;
            _bluetoothDevice.GetGattServicesAsync();

            _notepadClient = NotepadHelper.Create(scanResult);
            _notepadType = new NotepadType(_notepadClient, new BleType(_bluetoothDevice));
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
            if (device.ConnectionStatus != BluetoothConnectionStatus.Connected) return;
            await _notepadType.ConfigCharacteristics();
            await _notepadClient.CompleteConnection();
        }
    }
}