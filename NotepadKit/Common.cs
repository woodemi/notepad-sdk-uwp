using System;
using System.Linq;
using Windows.Storage.Streams;

namespace NotepadKit
{
    internal static class NotepadHelper
    {
        private static readonly byte[] WOODEMI_PREFIX = {0x57, 0x44, 0x4D};

        internal static bool Support(NotepadScanResult scanResult)
        {
            return scanResult.ManufacturerData?.StartWith(WOODEMI_PREFIX) == true;
        }

        internal static NotepadClient Create(NotepadScanResult scanResult)
        {
            if (scanResult.ManufacturerData?.StartWith(WOODEMI_PREFIX) == true)
                return new WoodemiClient();

            throw new Exception("Unsupported BLE device");
        }
    }

    internal static class Extensions
    {
        public static byte[] ToByteArray(this IBuffer value)
        {
            var bytes = new byte[value.Length];
            using (var reader = DataReader.FromBuffer(value))
            {
                reader.ReadBytes(bytes);
            }

            return bytes;
        }

        public static bool StartWith(this byte[] value, byte[] prefix)
        {
            return value.Length >= prefix.Length && Enumerable.Range(0, prefix.Length).All(i => value[i] == prefix[i]);
        }
    }
}