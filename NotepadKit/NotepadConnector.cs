using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth;

namespace NotepadKit
{
    public class NotepadConnector
    {
        private BluetoothLEDevice _bluetoothDevice;
        private NotepadClient _notepadClient;

        public async void Connect(NotepadScanResult scanResult)
        {
            Debug.WriteLine("NotepadConnector::Connect");
            _bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(scanResult.BluetoothAddress);
            _notepadClient = NotepadHelper.Create(scanResult);
        }

        public void Disconnect()
        {
            Debug.WriteLine("NotepadConnector::Disconnect");
            _bluetoothDevice?.Dispose();
            _bluetoothDevice = null;
        }
    }
}