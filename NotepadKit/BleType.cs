using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace NotepadKit
{
    internal class BleType
    {
        private readonly BluetoothLEDevice _bluetoothDevice;

        public BleType(BluetoothLEDevice bluetoothDevice)
        {
            _bluetoothDevice = bluetoothDevice;
        }

        public async Task SetNotifiable((string, string) serviceCharacteristic, BleInputProperty inputProperty)
        {
            var (serviceId, characteristicId) = serviceCharacteristic;
            var servicesResult = await _bluetoothDevice.GetGattServicesAsync();
            var service = servicesResult.Services.First(s => s.Uuid.ToString().ToUpper() == serviceId);
            var characteristicsResult = await service.GetCharacteristicsAsync();
            var characteristic = characteristicsResult.Characteristics.First(c => c.Uuid.ToString().ToUpper() == characteristicId);
            
            var descriptorValue = inputProperty == BleInputProperty.Notification ? GattClientCharacteristicConfigurationDescriptorValue.Notify
                : inputProperty == BleInputProperty.Indication ? GattClientCharacteristicConfigurationDescriptorValue.Indicate
                : GattClientCharacteristicConfigurationDescriptorValue.None;

            var descriptorStatus = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue);
            if (descriptorStatus != GattCommunicationStatus.Success)
                throw new Exception($"{characteristic.Service.Uuid} {characteristic.Uuid} setNotifiable fail");
        }
    }
}