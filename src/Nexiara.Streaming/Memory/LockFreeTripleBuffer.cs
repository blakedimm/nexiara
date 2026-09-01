using System.Threading;

namespace Nexiara.Streaming.Memory
{
    /// <summary>
    /// Неблокирующий тройной буфер кадрового конвейера (Zero-Lock, Lock-Free Exchange).
    /// </summary>
    public class LockFreeTripleBuffer<T> where T : class
    {
        private int _readIdx = 0;
        private int _writeIdx = 1;
        private int _cleanIdx = 2;

        private int _isNewDataAvailable = 0;
        private readonly T[] _buffers;

        public LockFreeTripleBuffer(T buffer0, T buffer1, T buffer2)
        {
            _buffers = new T[] { buffer0, buffer1, buffer2 };
        }

        /// <summary>
        /// Вызывается потоком захвата для получения активного буфера записи.
        /// </summary>
        public T GetWriteBuffer() => _buffers[_writeIdx];

        /// <summary>
        /// Фиксирует готовность нового кадра и публикует его для чтения.
        /// </summary>
        public void CommitWrite()
        {
            int lastCleanIdx = Interlocked.Exchange(ref _cleanIdx, _writeIdx);
            _writeIdx = lastCleanIdx;
            Interlocked.Exchange(ref _isNewDataAvailable, 1);
        }

        /// <summary>
        /// Вызывается кодировщиком для забора самого актуального кадра. Возвращает null, если новых кадров нет.
        /// </summary>
        public T? GetLatestReadBuffer()
        {
            if (Interlocked.CompareExchange(ref _isNewDataAvailable, 0, 1) == 0)
            {
                return null;
            }

            int lastCleanIdx = Interlocked.Exchange(ref _cleanIdx, _readIdx);
            _readIdx = lastCleanIdx;

            return _buffers[_readIdx];
        }
    }
}