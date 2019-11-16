using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth;
using NotepadKit;

namespace NotepadKitSample
{
    public class NotepadConnector
    {
        private BluetoothLEDevice _bluetoothDevice;

        public async void Connect(NotepadScanResult scanResult)
        {
            Debug.WriteLine("NotepadConnector::Connect");
            _bluetoothDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(scanResult.BluetoothAddress);
        }

        public void Disconnect()
        {
            Debug.WriteLine("NotepadConnector::Disconnect");
            _bluetoothDevice?.Dispose();
            _bluetoothDevice = null;
        }
    }
}