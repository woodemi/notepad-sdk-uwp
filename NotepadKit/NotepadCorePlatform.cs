using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace NotepadKit
{
    internal class NotepadCorePlatform
    {
        private static readonly Lazy<NotepadCorePlatform> _lazy =
            new Lazy<NotepadCorePlatform>(() => new NotepadCorePlatform());

        private readonly Dictionary<string, GattCharacteristic> _gattCharacteristics =
            new Dictionary<string, GattCharacteristic>();

        private BluetoothLEDevice _bluetoothLEDevice;

        public TypedEventHandler<object, BluetoothConnectionStatus> ConnectionStatusChanged;

        private NotepadCorePlatform()
        {
        }

        public static NotepadCorePlatform Instance => _lazy.Value;

        public async void ConnectAsync(NotepadScanResult scanResult)
        {
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(scanResult.BluetoothAddress);
            var gattDeviceServicesResult = await device.GetGattServicesAsync();
            Debug.WriteLine($"ConnectAsync GetGattServicesAsync {gattDeviceServicesResult.Status}");
            if (gattDeviceServicesResult.Status != GattCommunicationStatus.Success)
            {
                ConnectionStatusChanged?.Invoke(null, BluetoothConnectionStatus.Disconnected);
                return;
            }

            _bluetoothLEDevice = device;
            _bluetoothLEDevice.ConnectionStatusChanged += BluetoothLEDevice_ConnectionStatusChanged;

            ConnectionStatusChanged?.Invoke(null, BluetoothConnectionStatus.Connected);
        }

        public void Disconnect()
        {
            Clean();
        }

        private void BluetoothLEDevice_ConnectionStatusChanged(BluetoothLEDevice device, object args)
        {
            Debug.WriteLine(
                $"OnConnectionStatusChanged {device.BluetoothAddress}, {device.ConnectionStatus.ToString()}");
            if (_bluetoothLEDevice != device)
            {
                Debug.WriteLine("Probably MEMORY LEAK!");
                return;
            }

            if (device.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                ConnectionStatusChanged?.Invoke(null, BluetoothConnectionStatus.Disconnected);
                Clean();
            }
        }

        private async Task<GattCharacteristic> GetCharacteristic((string, string) serviceCharacteristic)
        {
            var (serviceId, characteristicId) = serviceCharacteristic;
            if (!_gattCharacteristics.ContainsKey(characteristicId))
            {
                var servicesResult = await _bluetoothLEDevice.GetGattServicesAsync();
                var service = servicesResult.Services.First(s => s.Uuid.ToString().ToUpper() == serviceId);
                var characteristicsResult = await service.GetCharacteristicsAsync();
                _gattCharacteristics[characteristicId] =
                    characteristicsResult.Characteristics.First(c => c.Uuid.ToString().ToUpper() == characteristicId);
            }

            return _gattCharacteristics[characteristicId];
        }

        private void Clean()
        {
            foreach (var characteristicPair in _gattCharacteristics)
                characteristicPair.Value.ValueChanged -= GattCharacteristic_ValueChanged;
            _gattCharacteristics.Clear();

            if (_bluetoothLEDevice != null)
                _bluetoothLEDevice.ConnectionStatusChanged -= BluetoothLEDevice_ConnectionStatusChanged;
            _bluetoothLEDevice?.Dispose();
            _bluetoothLEDevice = null;
        }

        public async Task SetNotifiable((string, string) serviceCharacteristic, BleInputProperty inputProperty)
        {
            var characteristic = await GetCharacteristic(serviceCharacteristic);

            var descriptorValue = inputProperty == BleInputProperty.Notification
                ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                : inputProperty == BleInputProperty.Indication
                    ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                    : GattClientCharacteristicConfigurationDescriptorValue.None;

            var descriptorStatus =
                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue);
            if (descriptorStatus != GattCommunicationStatus.Success)
                throw new Exception($"{characteristic.Service.Uuid} {characteristic.Uuid} setNotifiable fail");

            if (inputProperty != BleInputProperty.Disabled)
                characteristic.ValueChanged += GattCharacteristic_ValueChanged;
            else
                characteristic.ValueChanged -= GattCharacteristic_ValueChanged;
        }

        public event TypedEventHandler<string, byte[]> InputReceived;

        private void GattCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var value = args.CharacteristicValue.ToByteArray();
            Debug.WriteLine($"OnCharacteristicValueChanged {sender.Uuid}, {value.ToHexString()}");
            InputReceived?.Invoke(sender.Uuid.ToString().ToUpper(), value);
        }

        public async Task WriteValue((string, string) serviceCharacteristic, byte[] request)
        {
            var characteristic = await GetCharacteristic(serviceCharacteristic);
            var result = await characteristic.WriteValueAsync(request.ToBuffer());
            if (result != GattCommunicationStatus.Success)
                throw new Exception($"{characteristic.Uuid} WriteValueAsync fail");
        }
    }
}