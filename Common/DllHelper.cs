using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public static class DllHelper
    {
        [DllImport("kernel32.dll")]
        public static extern IntPtr LoadLibrary(string path);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetProcAddress(IntPtr lib, string funcName);

        [DllImport("kernel32.dll")]
        public static extern bool FreeLibrary(IntPtr lib);

        public static IntPtr LoadDll(string path)
        {
            return LoadLibrary(path);
        }

        public static bool FreeDll(IntPtr lib)
        {
            return FreeLibrary(lib);
        }

        public static Delegate GetFunctionAddress(IntPtr dllModule, string functionName, Type t)
        {
            var address = GetProcAddress(dllModule, functionName);
            return GetDelegateFromIntPtr(address, t);
        }

        /// 将表示函数地址的IntPtr实例转换成对应的委托
        public static Delegate GetDelegateFromIntPtr(IntPtr address, Type t)
        {
            if (address == IntPtr.Zero)
                return null;
            return Marshal.GetDelegateForFunctionPointer(address, t);
        }

        /// 将表示函数地址的int转换成对应的委托，by jingzhongrong
        public static Delegate GetDelegateFromIntPtr(int address, Type t)
        {
            if (address == 0)
                return null;
            return Marshal.GetDelegateForFunctionPointer(new IntPtr(address), t);
        }
    }
}
