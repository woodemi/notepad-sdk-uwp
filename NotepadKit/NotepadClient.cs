using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace NotepadKit
{
    public abstract class NotepadClient
    {
        internal NotepadType _notepadType;
        public abstract (string, string) CommandRequestCharacteristic { get; }

        public abstract (string, string) CommandResponseCharacteristic { get; }

        public abstract (string, string) SyncInputCharacteristic { get; }

        public abstract (string, string) FileInputControlRequestCharacteristic { get; }

        public abstract (string, string) FileInputControlResponseCharacteristic { get; }

        public abstract (string, string) FileInputCharacteristic { get; }

        public abstract IReadOnlyList<(string, string)> InputIndicationCharacteristics { get; }

        public abstract IReadOnlyList<(string, string)> InputNotificationCharacteristics { get; }

        internal virtual async Task CompleteConnection(Action<bool> awaitConfirm)
        {
            _notepadType.ReceiveSyncInput().Subscribe(value => SyncPointerReceived?.Invoke(this, ParseSyncData(value)));
        }

        public event TypedEventHandler<NotepadClient, List<NotePenPointer>> SyncPointerReceived;

        public abstract Task SetMode(NotepadMode mode);

        protected abstract List<NotePenPointer> ParseSyncData(byte[] value);

        public abstract Task<MemoSummary> GetMemoSummary();

        public abstract Task<MemoInfo> GetMemoInfo();

        public abstract Task<MemoData> ImportMemo(Action<int> progress);
    }
}