using System;
using System.Collections.Generic;
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

    public enum NotepadMode
    {
        Sync,
        Common
    }

    public struct NotePenPointer
    {
        public int x;
        public int y;
        public long t;
        public int p;

        public static NotePenPointer[] Create(byte[] bytes)
        {
            // TODO BitConverter.IsLittleEndian
            return Enumerable.Range(0, bytes.Length / 6).Select(i => new NotePenPointer
            {
                x = BitConverter.ToInt16(bytes, i * 6),
                y = BitConverter.ToInt16(bytes, i * 6 + 2),
                t = -1,
                p = BitConverter.ToInt16(bytes, i * 6 + 4)
            }).ToArray();
        }

        public override string ToString()
        {
            return $"x: {x}, y: {y}, t: {t}, p: {p}";
        }
    }

    public struct MemoSummary
    {
        public long totalCapacity;
        public long freeCapacity;
        public long usedCapacity;
        public int memoCount;

        public override string ToString()
        {
            return
                $"memoCount: {memoCount}, totalCapacity: {totalCapacity}, freeCapacity: {freeCapacity}, usedCapacity: {usedCapacity}";
        }
    }

    public struct MemoInfo
    {
        public long sizeInByte;
        public long createdAt;

        public long partIndex;

        // Rest part count in current transportation
        public long restCount;

        public override string ToString()
        {
            return $"sizeInByte: {sizeInByte}, createdAt: {createdAt}, partIndex: {partIndex}, restCount: {restCount}";
        }
    }

    public struct MemoData
    {
        public MemoInfo memoInfo;
        public List<NotePenPointer> pointers;

        public override string ToString() => $"memoInfo: {memoInfo}, pointers: {pointers.Count}";
    }
}