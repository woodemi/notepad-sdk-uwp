using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;

namespace NotepadKit
{
    public class NotepadScanner
    {
        private readonly BluetoothLEAdvertisementWatcher _watcher = new BluetoothLEAdvertisementWatcher();

        public NotepadScanner()
        {
            _watcher.Received += OnAdvertisementReceived;
        }

        public void StartScan()
        {
            Debug.WriteLine("StartScan");
            _watcher.Start();
        }

        public void StopScan()
        {
            Debug.WriteLine("StopScan");
            _watcher.Stop();
        }

        private void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher watcher,
            BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            Debug.WriteLine($"OnAdvertisementReceived {eventArgs.BluetoothAddress}");
        }
    }
}