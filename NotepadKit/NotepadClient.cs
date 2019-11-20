using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NotepadKit
{
    public abstract class NotepadClient
    {
        internal NotepadType _notepadType;
        public abstract (string, string) CommandRequestCharacteristic { get; }

        public abstract (string, string) CommandResponseCharacteristic { get; }

        public abstract (string, string) SyncInputCharacteristic { get; }

        public abstract IReadOnlyList<(string, string)> InputIndicationCharacteristics { get; }

        public abstract IReadOnlyList<(string, string)> InputNotificationCharacteristics { get; }

        internal abstract Task CompleteConnection(Action<bool> awaitConfirm);
    }
}