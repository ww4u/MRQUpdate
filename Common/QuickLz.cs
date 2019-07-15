using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public class QuickLz
    {
        private byte[] state_compress;
        private byte[] state_decompress;
        public QuickLz()
        {
            state_compress = new byte[DllManager.qlz_get_setting(1)];
            state_decompress = (uint)DllManager.qlz_get_setting(3) == 0 ? 
                state_compress : new byte[DllManager.qlz_get_setting(2)];
        }

        public byte[] Compress(byte[] source)
        {
            byte[] d = new byte[source.Length + 400];
            var s = (uint)DllManager.qlz_compress(source, d, (IntPtr)source.Length, state_compress);
            byte[] d2 = new byte[s];
            Array.Copy(d, d2, s);
            return d2;
        }

        public byte[] Decompress(byte[] source)
        {
            byte[] d = new byte[(uint)DllManager.qlz_size_decompressed(source)];
            var s = (uint)DllManager.qlz_decompress(source, d, state_decompress);
            return d;
        }

        public uint SizeCompressed(byte[] source)
        {
            return (uint)DllManager.qlz_size_compressed(source);
        }

        public uint SizeDecompressed(byte[] source)
        {
            return (uint)DllManager.qlz_size_decompressed(source);
        }
    }
}
