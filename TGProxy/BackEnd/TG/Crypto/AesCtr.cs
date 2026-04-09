namespace TGProxy.BackEnd.TG.Crypto
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
            int i = 0;
            while (i < buffer.Length)
            {
                if (_bytesInBlock == 0)
                {
                    _encryptor.TransformBlock(_counter, 0, 16, _counterOut, 0);
                    IncrementCounter();
                }
                int availableInBlock = 16 - _bytesInBlock;
                int bytesToProcess = Math.Min(buffer.Length - i, availableInBlock);

                for (int j = 0; j < bytesToProcess; j++)
                {
                    buffer[i + j] ^= _counterOut[_bytesInBlock + j];
                }

                i += bytesToProcess;
                _bytesInBlock = (_bytesInBlock + bytesToProcess) % 16;
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
