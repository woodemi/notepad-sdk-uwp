using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

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
            _notepadType = new NotepadType(_notepadClient);
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
            if (device.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                await device.DiscoverServices();
                await _notepadType.ConfigCharacteristics();
            }
        }
    }

    internal static class BluetoothLEDeviceExtension
    {
        internal static async Task DiscoverServices(this BluetoothLEDevice device)
        {
            var serviceResult = await device.GetGattServicesAsync();
            if (serviceResult.Status != GattCommunicationStatus.Success) return;

            await Task.WhenAll(serviceResult.Services.Select(ConfirmCharacteristics));
        }

        private static async Task ConfirmCharacteristics(GattDeviceService service)
        {
            var result = await service.GetCharacteristicsAsync();
            if (result.Status != GattCommunicationStatus.Success)
                throw new Exception($"GetCharacteristicsAsync {result}");

            Debug.WriteLine($"service {service.Uuid}");
            foreach (var characteristic in result.Characteristics)
                Debug.WriteLine($"  characteristic {characteristic.Uuid}");
        }
    }
}