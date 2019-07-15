using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public class DllManager
    {
        [DllImport("quicklz.dll")]
        public static extern IntPtr qlz_compress(byte[] source, byte[] destination, IntPtr size, byte[] scratch);
        [DllImport("quicklz.dll")]
        public static extern IntPtr qlz_decompress(byte[] source, byte[] destination, byte[] scratch);
        [DllImport("quicklz.dll")]
        public static extern IntPtr qlz_size_compressed(byte[] source);
        [DllImport("quicklz.dll")]
        public static extern IntPtr qlz_size_decompressed(byte[] source);
        [DllImport("quicklz.dll")]
        public static extern int qlz_get_setting(int setting);
    }
}
