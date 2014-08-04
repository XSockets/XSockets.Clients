using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace XSockets.Client40.Wrapper
{
    sealed class SslStreamWrapper : Stream
    {
        readonly object _locker = new Object();
        readonly Stream _stream;

        AutoResetEvent _asyncRead;
        AutoResetEvent _asyncWrite;


        public SslStreamWrapper(Stream stream)
        {
            _stream = stream;
        }

        public override bool CanRead
        {
            get
            {
                return _stream.CanRead;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return _stream.CanSeek;
            }
        }

        public override bool CanTimeout
        {
            get
            {
                return base.CanTimeout;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return _stream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return _stream.Length;
            }
        }

        public override long Position
        {
            get { return _stream.Position; }
            set { _stream.Position = value; }
        }
        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read([In, Out] byte[] buffer, int offset, int count)
        {
            try
            {
                lock (_locker)
                    _stream.Read(buffer, offset, count);
            }
            finally
            {
                throw new NotSupportedException("This stream does not support reading");
            }

        }
        public override long Seek(long offset, SeekOrigin origin)
        {

            try
            {
                lock (_locker)
                    _stream.Seek(offset, origin);
            }
            finally
            {
                throw new NotSupportedException("This stream does not support seek");
            }
        }
        public override void SetLength(long value)
        {

            lock (_locker)
                _stream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (_locker)
                _stream.Write(buffer, offset, count);
        }


        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (!CanRead)
                throw new NotSupportedException("This stream does not support reading");

            if (_asyncRead == null)
            {
                lock (this)
                {
                    if (_asyncRead == null)
                        _asyncRead = new AutoResetEvent(true);
                }
            }

            _asyncRead.WaitOne();
            return _stream.BeginRead(buffer, offset, count, callback, state);
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (_asyncWrite == null)
            {
                lock (this)
                {
                    if (_asyncWrite == null)
                        _asyncWrite = new AutoResetEvent(true);
                }
            }
            _asyncWrite.WaitOne(); //Only allow one thread through
            return _stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult result)
        {
            try
            {
                _stream.EndWrite(result);
            }
            finally
            {
                _asyncWrite.Set(); //Signal so the next thread can pass through
            }
        }

        public override int EndRead(IAsyncResult result)
        {
            try
            {
                return _stream.EndRead(result);
            }
            finally
            {
                _asyncRead.Set(); //Signal so the next thread can pass through
            }
        }
    }
}