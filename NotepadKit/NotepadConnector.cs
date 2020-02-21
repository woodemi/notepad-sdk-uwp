using System.Diagnostics;
using Windows.Devices.Bluetooth;
using Windows.Foundation;

namespace NotepadKit
{
    public class NotepadConnector
    {
        private NotepadClient _notepadClient;
        private NotepadType _notepadType;

        public NotepadConnector()
        {
            NotepadCorePlatform.Instance.ConnectionStatusChanged += NotepadCorePlatform_ConnectionStatusChanged;
        }

        public event TypedEventHandler<NotepadClient, ConnectionState> ConnectionChanged;

        public void Connect(NotepadScanResult scanResult)
        {
            Debug.WriteLine("NotepadConnector::Connect");
            _notepadClient = NotepadHelper.Create(scanResult);
            _notepadType = new NotepadType(_notepadClient);
            NotepadCorePlatform.Instance.ConnectAsync(scanResult);
            ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Connecting);
        }

        public void Disconnect()
        {
            Debug.WriteLine("NotepadConnector::Disconnect");
            Clean();
            NotepadCorePlatform.Instance.Disconnect();
        }

        private async void NotepadCorePlatform_ConnectionStatusChanged(object sender, BluetoothConnectionStatus args)
        {
            if (args == BluetoothConnectionStatus.Connected)
            {
                await _notepadType.ConfigCharacteristics();
                await _notepadClient.CompleteConnection(awaitConfirm =>
                    ConnectionChanged?.Invoke(_notepadClient, ConnectionState.AwaitConfirm));
                ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Connected);
            }
            else
            {
                Clean();
                ConnectionChanged?.Invoke(_notepadClient, ConnectionState.Disconnected);
            }
        }

        private void Clean()
        {
            _notepadClient = null;
            _notepadType = null;
        }
    }
}