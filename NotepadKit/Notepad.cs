using System;

namespace NotepadKit
{
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        AwaitConfirm,
        Connected
    }

    internal enum AccessResult
    {
        Denied, // Device claimed by other user
        Confirmed, // Access confirmed, indicating device not claimed by anyone
        Unconfirmed, // Access unconfirmed, as user doesn't confirm before timeout
        Approved // Device claimed by this user
    }

    internal class AccessException : Exception
    {
        public static readonly AccessException Denied = new AccessException();
        public static readonly AccessException Unconfirmed = new AccessException();

        private AccessException()
        {
        }
    }
}