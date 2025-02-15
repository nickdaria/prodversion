using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProdVersion
{
    public enum ReleaseChannel : byte
    {
        Dev         = (byte)'d',
        Internal    = (byte)'i',
        Alpha       = (byte)'a',
        Beta        = (byte)'b',
        Candidate   = (byte)'c',
        Release     = (byte)'r',
        Factory     = (byte)'f'
    }

    public struct VersionInfo
    {
        private const byte ENCODING_VERSION = 0x01;
        private const int PRODUCT_SIZE = 24;
        private const int METADATA_SIZE = 15;
        private const int COMMIT_HASH_SIZE = 7;

        private string _product;
        private ushort _major;
        private ushort _minor;
        private ushort _patch;
        private ushort _build;
        private ReleaseChannel _releaseChannel;
        private string _metadata;
        private string _commitHash;
        private ulong _unixTimestamp; // Stored as Unix time (seconds since epoch)

        public string Product
        {
            get => (_product ?? "").TrimEnd('\0');
            set
            {
                //  Enforce a max length so you never store an overly long string.
                if (value != null && value.Length > PRODUCT_SIZE)
                    value = value.Substring(0, PRODUCT_SIZE);

                _product = value ?? string.Empty;
            }
        }

        public ushort Major
        {
            get => _major;
            set => _major = value;
        }

        public ushort Minor
        {
            get => _minor;
            set => _minor = value;
        }

        public ushort Patch
        {
            get => _patch;
            set => _patch = value;
        }

        public ushort Build
        {
            get => _build;
            set => _build = value;
        }

        public ReleaseChannel ReleaseChannel
        {
            get => _releaseChannel;
            set => _releaseChannel = value;
        }

        public string Metadata
        {
            get => (_metadata ?? "").TrimEnd('\0');
            set
            {
                //  Enforce max length
                if (value != null && value.Length > METADATA_SIZE)
                    value = value.Substring(0, METADATA_SIZE);

                _metadata = value ?? string.Empty;
            }
        }

        public string CommitHash
        {
            get => (_commitHash ?? "").TrimEnd('\0');
            set
            {
                //  Enforce a max length.
                if (value != null && value.Length > COMMIT_HASH_SIZE)
                    value = value.Substring(0, COMMIT_HASH_SIZE);

                _commitHash = value ?? string.Empty;
            }
        }

        public DateTime Date
        {
            get => DateTimeOffset.FromUnixTimeSeconds((long)_unixTimestamp).UtcDateTime;
            set => _unixTimestamp = (ulong)new DateTimeOffset(value).ToUnixTimeSeconds();
        }

        public static byte[] Encode(VersionInfo version)
        {
            byte[] buffer = new byte[64];
            int offset = 0;

            //  Version structure byte
            buffer[offset++] = ENCODING_VERSION;

            //  Product/Part ID
            Encoding.ASCII.GetBytes(version.Product).CopyTo(buffer, offset);
            offset += PRODUCT_SIZE;

            //  Semantic version
            buffer[offset++] = (byte)(version.Major >> 8);
            buffer[offset++] = (byte)version.Major;

            buffer[offset++] = (byte)(version.Minor >> 8);
            buffer[offset++] = (byte)version.Minor;

            buffer[offset++] = (byte)(version.Patch >> 8);
            buffer[offset++] = (byte)version.Patch;

            buffer[offset++] = (byte)(version.Build >> 8);
            buffer[offset++] = (byte)version.Build;

            //  Release channel
            buffer[offset++] = (byte)version.ReleaseChannel;

            //  Metadata
            Encoding.ASCII.GetBytes(version.Metadata).CopyTo(buffer, offset);
            offset += METADATA_SIZE;

            //  Commit hash
            Encoding.ASCII.GetBytes(version.CommitHash).CopyTo(buffer, offset);
            offset += COMMIT_HASH_SIZE;

            //  Timestamp
            buffer[offset++] = (byte)(version._unixTimestamp >> 56);
            buffer[offset++] = (byte)(version._unixTimestamp >> 48);
            buffer[offset++] = (byte)(version._unixTimestamp >> 40);
            buffer[offset++] = (byte)(version._unixTimestamp >> 32);
            buffer[offset++] = (byte)(version._unixTimestamp >> 24);
            buffer[offset++] = (byte)(version._unixTimestamp >> 16);
            buffer[offset++] = (byte)(version._unixTimestamp >> 8);
            buffer[offset++] = (byte)version._unixTimestamp;

            return buffer;
        }

        public static bool Decode(byte[] buffer, out VersionInfo version)
        {
            version = new VersionInfo();
            if (buffer == null || buffer.Length != 64)
                return false;

            int offset = 0;

            //  Verify structure version
            byte ver = buffer[offset++];
            if(ver != 1) { throw new NotImplementedException($"Unsupported structure version: {ver}"); }

            //  Part/Product Identifier
            version.Product = GetCString(buffer, offset, PRODUCT_SIZE);
            offset += PRODUCT_SIZE;

            //  Semantic Version
            version.Major = (ushort)((buffer[offset++] << 8) | buffer[offset++]);
            version.Minor = (ushort)((buffer[offset++] << 8) | buffer[offset++]);
            version.Patch = (ushort)((buffer[offset++] << 8) | buffer[offset++]);
            version.Build = (ushort)((buffer[offset++] << 8) | buffer[offset++]);

            //  Release channel
            version.ReleaseChannel = (ReleaseChannel)buffer[offset++];

            //  Metadata
            version.Metadata = GetCString(buffer, offset, METADATA_SIZE);
            offset += METADATA_SIZE;

            //  Commit
            version.CommitHash = GetCString(buffer, offset, COMMIT_HASH_SIZE);
            offset += COMMIT_HASH_SIZE;

            //  Timestamp
            version._unixTimestamp =
                ((ulong)buffer[offset++] << 56) |
                ((ulong)buffer[offset++] << 48) |
                ((ulong)buffer[offset++] << 40) |
                ((ulong)buffer[offset++] << 32) |
                ((ulong)buffer[offset++] << 24) |
                ((ulong)buffer[offset++] << 16) |
                ((ulong)buffer[offset++] << 8) |
                buffer[offset++];

            return true;
        }

        private static string GetCString(byte[] buffer, int offset, int length)
        {
            string str = Encoding.ASCII.GetString(buffer, offset, length);

            int nullIndex = str.IndexOf('\0');
            if (nullIndex >= 0)
            {
                str = str.Substring(0, nullIndex);
            }

            return str;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1}.{2}.{3}{4}{5}{6}{7}",
                Product,
                Major,
                Minor,
                Patch,
                (char)ReleaseChannel,
                string.IsNullOrEmpty(Metadata.TrimEnd('\0')) ? "" : $"-{Metadata.TrimEnd('\0')}",
                ReleaseChannel != ReleaseChannel.Release ? $" ({CommitHash.TrimEnd('\0')})" : "",
                Build == 0 ? "" : $" build {Build}"
            );
        }
    }
}
