using System;

namespace NotepadKit
{
    internal struct WoodemiCommand<Response>
    {
        public byte[] request;
        public Func<byte[], bool> intercept;
        public Func<byte[], Response> handle;
    }
}