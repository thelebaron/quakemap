using System;
using System.IO;

namespace Junk.Sludge.Formats
{
    /// <summary>
    /// A stream that represents a sub-range within another stream.
    /// </summary>
    public class SubStream : Stream
    {
        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => true;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Position { get; set; }

        /// <inheritdoc />
        public override long Length => _length;

        private readonly Stream _stream;
        private readonly long _offset;
        private readonly long _length;
        private readonly bool _keepOpen;

        /// <summary>
        /// Create a new substream
        /// </summary>
        /// <param name="stream">The parent stream</param>
        /// <param name="offset">The start of the substream</param>
        /// <param name="length">The length of the substream</param>
        /// <param name="keepOpen">True to keep the parent stream open after closing</param>
        public SubStream(Stream stream, long offset, long length, bool keepOpen = true)
        {
            _stream = stream;
            _offset = offset;
            _length = length;
            _keepOpen = keepOpen;
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _length + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }
            return Position;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (_stream)
            {
                var pos = _stream.Position;
                _stream.Position =  _offset + Position;
                count            =  (int) System.Math.Min(count, _length - Position);
                count            =  _stream.Read(buffer, offset, count);
                Position         += count;
                _stream.Position =  pos;
                return count;
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_keepOpen)
            {
                lock (_stream)
                {
                    _stream.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}