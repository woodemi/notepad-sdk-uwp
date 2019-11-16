using System.Diagnostics;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Foundation;

namespace NotepadKit
{
    public class NotepadScanner
    {
        private readonly BluetoothLEAdvertisementWatcher _watcher = new BluetoothLEAdvertisementWatcher();

        public NotepadScanner()
        {
            _watcher.Received += OnAdvertisementReceived;
        }

        public event TypedEventHandler<NotepadScanner, NotepadScanResult> Found;

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
            var notepadScanResult = new NotepadScanResult(eventArgs);
            if (NotepadHelper.Support(notepadScanResult))
                Found?.Invoke(this, notepadScanResult);
        }
    }
}