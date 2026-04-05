namespace HiDPI.BackEnd.TG.Crypto
{
    using System.Buffers.Binary;

    public class MtProtoSplitter
    {
        private readonly AesCtr _dec;
        private readonly uint _proto;
        private readonly List<byte> _cipherBuf = new();
        private readonly List<byte> _plainBuf = new();
        private bool _disabled = false;

        public MtProtoSplitter(byte[] relayInit, uint protoInt)
        {
            byte[] key = relayInit.AsSpan(8, 32).ToArray();
            byte[] iv = relayInit.AsSpan(40, 16).ToArray();
            _dec = new AesCtr(key, iv);

            _dec.Transform(new byte[64]);
            _proto = protoInt;
        }

        public List<byte[]> Split(byte[] chunk)
        {
            if (chunk == null || chunk.Length == 0) return new List<byte[]>();
            if (_disabled) return new List<byte[]> { chunk };

            _cipherBuf.AddRange(chunk);

            byte[] plainChunk = new byte[chunk.Length];
            Array.Copy(chunk, plainChunk, chunk.Length);
            _dec.Transform(plainChunk);
            _plainBuf.AddRange(plainChunk);

            var parts = new List<byte[]>();

            while (_cipherBuf.Count > 0)
            {
                int? packetLen = NextPacketLen();
                if (packetLen == null) break;

                if (packetLen <= 0)
                {
                    parts.Add(_cipherBuf.ToArray());
                    _cipherBuf.Clear();
                    _plainBuf.Clear();
                    _disabled = true;
                    break;
                }

                if (_cipherBuf.Count < packetLen.Value) break;

                parts.Add(_cipherBuf.Take(packetLen.Value).ToArray());
                _cipherBuf.RemoveRange(0, packetLen.Value);
                _plainBuf.RemoveRange(0, packetLen.Value);
            }

            return parts;
        }

        private int? NextPacketLen()
        {
            if (_plainBuf.Count == 0) return null;

            if (_proto == 0xEFEFEFEF)
            {
                byte first = _plainBuf[0];
                if (first == 0x7F || first == 0xFF)
                {
                    if (_plainBuf.Count < 4) return null;
                    int payloadLen = (_plainBuf[1] | (_plainBuf[2] << 8) | (_plainBuf[3] << 16)) * 4;
                    return 4 + payloadLen;
                }
                else
                {
                    int payloadLen = (first & 0x7F) * 4;
                    return 1 + payloadLen;
                }
            }
            else if (_proto == 0xEEEEEEEE || _proto == 0xDDDDDDDD)
            {
                if (_plainBuf.Count < 4) return null;
                int payloadLen = BinaryPrimitives.ReadInt32LittleEndian(_plainBuf.Take(4).ToArray()) & 0x7FFFFFFF;
                return 4 + payloadLen;
            }

            return 0;
        }
    }
}
