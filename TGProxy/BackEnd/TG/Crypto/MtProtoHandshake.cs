namespace TGProxy.BackEnd.TG.Crypto
{
    using System;
    using System.Buffers.Binary;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text;

    public class MtProtoHandshake
    {
        public const int HandshakeLength = 64;

        public static readonly byte[] TagAbridged = { 0xef, 0xef, 0xef, 0xef };
        public static readonly byte[] TagIntermediate = { 0xee, 0xee, 0xee, 0xee };
        public static readonly byte[] TagSecure = { 0xdd, 0xdd, 0xdd, 0xdd };

        public uint ProtoInt { get; private set; }

        public int DcId { get; private set; }
        public bool IsMedia { get; private set; }
        public byte[] ProtoTag { get; private set; }
        public byte[] ClientPrekeyAndIv { get; private set; }

        public bool TryParse(Span<byte> handshake, byte[] secret)
        {
            if (handshake.Length < HandshakeLength) return false;

            ClientPrekeyAndIv = handshake.Slice(8, 48).ToArray();
            var decPrekey = ClientPrekeyAndIv.AsSpan(0, 32);
            var decIv = ClientPrekeyAndIv.AsSpan(32, 16);

            byte[] decKey;
            using (var sha256 = SHA256.Create())
            {
                var buffer = new byte[decPrekey.Length + secret.Length];
                decPrekey.CopyTo(buffer);
                secret.CopyTo(buffer.AsSpan(decPrekey.Length));
                decKey = sha256.ComputeHash(buffer);
            }

            var decrypted = handshake.ToArray();
            using (var decryptor = new AesCtr(decKey, decIv.ToArray()))
            {
                decryptor.Transform(decrypted);
            }

            ProtoTag = decrypted.AsSpan(56, 4).ToArray();
            ProtoInt = BinaryPrimitives.ReadUInt32LittleEndian(ProtoTag);

            if (!IsKnownProtoTag(ProtoTag)) return false;

            short dcIdx = BinaryPrimitives.ReadInt16LittleEndian(decrypted.AsSpan(60, 2));
            DcId = Math.Abs(dcIdx);
            IsMedia = dcIdx < 0;

            return true;
        }

        public byte[] GenerateRelayInit(short dcIdx)
        {
            var rnd = new byte[HandshakeLength];
            using (var rng = RandomNumberGenerator.Create())
            {
                // Сделать проверку на зарезервированные байты
                rng.GetBytes(rnd);
            }

            var encKey = rnd.AsSpan(8, 32).ToArray();
            var encIv = rnd.AsSpan(40, 16).ToArray();

            var tailPlain = new byte[8];
            ProtoTag.CopyTo(tailPlain, 0);
            BinaryPrimitives.WriteInt16LittleEndian(tailPlain.AsSpan(4), dcIdx);

            using var encryptor = new AesCtr(encKey, encIv);
            var encryptedFull = rnd.ToArray();
            encryptor.Transform(encryptedFull);

            var keystreamTail = new byte[8];
            for (int i = 0; i < 8; i++)
                keystreamTail[i] = (byte)(encryptedFull[56 + i] ^ rnd[56 + i]);

            for (int i = 0; i < 8; i++)
                rnd[56 + i] = (byte)(tailPlain[i] ^ keystreamTail[i]);

            return rnd;
        }

        private bool IsKnownProtoTag(ReadOnlySpan<byte> tag)
        {
            return tag.SequenceEqual(TagAbridged) || tag.SequenceEqual(TagIntermediate) || tag.SequenceEqual(TagSecure);
        }
    }
}
