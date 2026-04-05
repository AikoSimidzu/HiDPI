namespace HiDPI.BackEnd.TG.Crypto
{
    using System.Security.Cryptography;

    public class AesCtr : IDisposable
    {
        private readonly ICryptoTransform _encryptor;
        private readonly byte[] _counter;
        private readonly byte[] _counterOut;
        private int _bytesInBlock;
        private readonly Aes _aes;

        public AesCtr(byte[] key, byte[] iv)
        {
            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
            _aes.Key = key;

            _encryptor = _aes.CreateEncryptor();
            _counter = (byte[])iv.Clone();
            _counterOut = new byte[16];
            _bytesInBlock = 0;
        }

        public void Transform(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                if (_bytesInBlock == 0)
                {
                    _encryptor.TransformBlock(_counter, 0, 16, _counterOut, 0);
                    IncrementCounter();
                }
                buffer[i] ^= _counterOut[_bytesInBlock];
                _bytesInBlock = (_bytesInBlock + 1) % 16;
            }
        }

        private void IncrementCounter()
        {
            for (int i = 15; i >= 0; i--)
            {
                if (++_counter[i] != 0) break;
            }
        }

        public void Dispose()
        {
            _encryptor?.Dispose();
            _aes?.Dispose();
        }
    }
}
