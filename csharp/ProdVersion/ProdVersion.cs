using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProdVersion
{
    public enum ReleaseChannelType : char
    {
        Dev         = 'd',
        Internal    = 'i',
        Alpha       = 'a',
        Beta        = 'b',
        Candidate   = 'c',
        Release     = 'r',
        Factory     = 'f'
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ProdVersion
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 26)] // 25 chars + null terminator
        private string product;

        public ushort Major;
        public ushort Minor;
        public ushort Patch;
        
        public ushort Build;
        public ReleaseChannelType ReleaseChannel;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)] // 15 chars + null terminator
        private string metadata;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)] // 7 chars + null terminator
        private string commitHash;

        private ulong unixTimestamp; // Stored as Unix time (seconds since epoch)

        public string Product
        {
            get => product?.TrimEnd('\0');
            set => product = (value ?? "").PadRight(26, '\0').Substring(0, 26);
        }

        public string Metadata
        {
            get => metadata?.TrimEnd('\0');
            set => metadata = (value ?? "").PadRight(16, '\0').Substring(0, 16);
        }

        public string CommitHash
        {
            get => commitHash?.TrimEnd('\0');
            set => commitHash = (value ?? "").PadRight(8, '\0').Substring(0, 8);
        }

        public DateTime Date
        {
            get => DateTimeOffset.FromUnixTimeSeconds((long)unixTimestamp).UtcDateTime;
            set => unixTimestamp = (ulong)new DateTimeOffset(value).ToUnixTimeSeconds();
        }

        public static byte[] Encode(ProdVersion version)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(ProdVersion))];
            int offset = 0;

            Encoding.ASCII.GetBytes(version.Product.PadRight(26, '\0')).CopyTo(buffer, offset);
            offset += 26;

            BitConverter.GetBytes(version.Major).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Minor).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Patch).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            BitConverter.GetBytes(version.Build).CopyTo(buffer, offset);
            offset += sizeof(ushort);

            buffer[offset++] = (byte)version.ReleaseChannel;

            Encoding.ASCII.GetBytes(version.Metadata.PadRight(16, '\0')).CopyTo(buffer, offset);
            offset += 16;

            Encoding.ASCII.GetBytes(version.CommitHash.PadRight(8, '\0')).CopyTo(buffer, offset);
            offset += 8;

            BitConverter.GetBytes(version.unixTimestamp).CopyTo(buffer, offset);
            offset += sizeof(ulong);

            return buffer;
        }

        public static bool Decode(byte[] buffer, out ProdVersion version)
        {
            version = new ProdVersion();
            if (buffer == null || buffer.Length < Marshal.SizeOf(typeof(ProdVersion)))
                return false;

            int offset = 0;

            version.Product = Encoding.ASCII.GetString(buffer, offset, 26).TrimEnd('\0');
            offset += 26;

            version.Major = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Minor = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Patch = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.Build = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);

            version.ReleaseChannel = (ProdVersionChannel)buffer[offset++];

            version.Metadata = Encoding.ASCII.GetString(buffer, offset, 16).TrimEnd('\0');
            offset += 16;

            version.CommitHash = Encoding.ASCII.GetString(buffer, offset, 8).TrimEnd('\0');
            offset += 8;

            version.unixTimestamp = BitConverter.ToUInt64(buffer, offset);
            offset += sizeof(ulong);

            return true;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} {1}.{2}.{3}{4}{5}{6} b{7}",
                Product,
                Major,
                Minor,
                Patch,
                (char)ReleaseChannel,
                string.IsNullOrEmpty(Metadata) ? "" : "-" + Metadata,
                ReleaseChannel != ProdVersionChannel.Release ? " " + CommitHash : "",
                Build
            );
        }
    }
}