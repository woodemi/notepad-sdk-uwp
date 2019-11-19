using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace NotepadKit
{
    internal class BleType
    {
        private readonly BluetoothLEDevice _bluetoothDevice;

        public BleType(BluetoothLEDevice bluetoothDevice)
        {
            _bluetoothDevice = bluetoothDevice;
        }

        private async Task<GattCharacteristic> GetCharacteristic((string, string) serviceCharacteristic)
        {
            var (serviceId, characteristicId) = serviceCharacteristic;
            var servicesResult = await _bluetoothDevice.GetGattServicesAsync();
            var service = servicesResult.Services.First(s => s.Uuid.ToString().ToUpper() == serviceId);
            var characteristicsResult = await service.GetCharacteristicsAsync();
            return characteristicsResult.Characteristics.First(c => c.Uuid.ToString().ToUpper() == characteristicId);
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
                characteristic.ValueChanged += OnCharacteristicValueChanged;
            else
                characteristic.ValueChanged -= OnCharacteristicValueChanged;
        }

        public event TypedEventHandler<string, byte[]> InputReceived;

        private void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
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