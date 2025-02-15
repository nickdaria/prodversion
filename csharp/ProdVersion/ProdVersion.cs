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
        private const int PRODUCT_SIZE = 25;
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
                // Ensure we enforce the size constraints
                if (value == null) value = "";
                _product = value.Length > PRODUCT_SIZE
                    ? value[..PRODUCT_SIZE]
                    : value.PadRight(PRODUCT_SIZE, '\0');
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
                if (value == null) value = "";
                _metadata = value.Length > METADATA_SIZE
                    ? value[..METADATA_SIZE]
                    : value.PadRight(METADATA_SIZE, '\0');
            }
        }

        public string CommitHash
        {
            get => (_commitHash ?? "").TrimEnd('\0');
            set
            {
                if (value == null) value = "";
                _commitHash = value.Length > COMMIT_HASH_SIZE
                    ? value[..COMMIT_HASH_SIZE]
                    : value.PadRight(COMMIT_HASH_SIZE, '\0');
            }
        }

        public DateTime Date
        {
            get => DateTimeOffset.FromUnixTimeSeconds((long)_unixTimestamp).UtcDateTime;
            set => _unixTimestamp = (ulong)new DateTimeOffset(value).ToUnixTimeSeconds();
        }

        public static byte[] Encode(VersionInfo version)
        {
            byte[] buffer = new byte[64]; // Hard cap at 64 bytes
            int offset = 0;

            // Use properties so we get properly trimmed/padded strings
            Encoding.ASCII.GetBytes(version.Product).CopyTo(buffer, offset);
            offset += PRODUCT_SIZE; // Hard limit at 25 bytes

            BitConverter.GetBytes(version.Major).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Minor).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Patch).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Build).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            buffer[offset++] = (byte)version.ReleaseChannel;

            Encoding.ASCII.GetBytes(version.Metadata).CopyTo(buffer, offset);
            offset += METADATA_SIZE;

            Encoding.ASCII.GetBytes(version.CommitHash).CopyTo(buffer, offset);
            offset += COMMIT_HASH_SIZE;

            BitConverter.GetBytes(version._unixTimestamp).CopyTo(buffer, offset);
            offset += sizeof(ulong);

            return buffer;
        }

        public static bool Decode(byte[] buffer, out VersionInfo version)
        {
            version = new VersionInfo();
            if (buffer == null || buffer.Length != 64)
                return false;

            int offset = 0;

            var product = Encoding.ASCII.GetString(buffer, offset, PRODUCT_SIZE).TrimEnd('\0');
            version.Product = product;
            offset += PRODUCT_SIZE;

            version.Major = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Minor = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Patch = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Build = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.ReleaseChannel = (ReleaseChannel)buffer[offset++];
            
            var metadata = Encoding.ASCII.GetString(buffer, offset, METADATA_SIZE).TrimEnd('\0');
            version.Metadata = metadata;
            offset += METADATA_SIZE;

            var commitHash = Encoding.ASCII.GetString(buffer, offset, COMMIT_HASH_SIZE).TrimEnd('\0');
            version.CommitHash = commitHash;
            offset += COMMIT_HASH_SIZE;

            version._unixTimestamp = BitConverter.ToUInt64(buffer, offset);
            offset += sizeof(ulong);

            return true;
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
