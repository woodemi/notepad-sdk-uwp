using System;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;

namespace NotepadKit
{
    public class NotepadScanResult
    {
        public readonly ulong BluetoothAddress;
        public readonly byte[] ManufacturerData;

        public NotepadScanResult(BluetoothLEAdvertisementReceivedEventArgs eventArgs)
        {
            BluetoothAddress = eventArgs.BluetoothAddress;
            ManufacturerData = eventArgs.Advertisement.ManufacturerByteArray();
        }
    }

    internal static class AdvertisementExtension
    {
        internal static byte[] ManufacturerByteArray(this BluetoothLEAdvertisement advertisement)
        {
            if (advertisement.ManufacturerData.Count == 0) return null;
            var manufacturerData = advertisement.ManufacturerData[0];
            return BitConverter.GetBytes(manufacturerData.CompanyId).Concat(manufacturerData.Data.ToByteArray())
                .ToArray();
        }
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        AwaitConfirm,
        Connected
    }
}