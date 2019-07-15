using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Model
{
    /*升级文件结构体*/
    public struct FileHeader
    {
        // 头标识
        public uint Flag;
        public uint Type;
        //文件头长度
        public uint HeaderLen;
        //文件大小,不包含文件头 
        public uint BootLen;
        //boot代码在升级文件中的偏移(块头)  
        public uint BootOffset;
        //boot代码在升级文件中的地址  
        public uint BootAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        //boot文件版本号
        public byte[] BootVersion;
        //boot代码在升级文件中的块数  
        public uint BootBlockCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //boot代码校验值  
        public byte[] BootBlockHash;
        //文件大小,不包含文件头 
        public uint ArmLen;
        //arm代码块在升级文件中的偏移（块头）
        public uint ArmOffset;
        //arm代码块在升级文件中的地址
        public uint ArmAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        //arm文件版本号
        public byte[] ArmVersion;
        //arm代码块在升级文件中的块数 
        public uint ArmBlockCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //arm代码校验值 
        public byte[] ArmBlockHash;
        //文件大小,不包含文件头 
        public uint Fpga1Len;
        //Fpga代码块在升级文件中的偏移（块头）
        public uint Fpga1Offset;
        //Fpga代码块在升级文件中的地址
        public uint Fpga1Addr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        //Fpga文件版本号
        public byte[] Fpga1Version;
        //Fpga代码块在升级文件中的块数 
        public uint Fpga1BlockCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //Fpga代码校验值 
        public byte[] Fpga1BlockHash;
        //文件大小,不包含文件头 
        public uint Fpga2Len;
        //Fpga代码块在升级文件中的偏移（块头）
        public uint Fpga2Offset;
        //Fpga代码块在升级文件中的地址
        public uint Fpga2Addr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        //Fpga文件版本号
        public byte[] Fpga2Version;
        //Fpga代码块在升级文件中的块数 
        public uint Fpga2BlockCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //Fpga代码校验值 
        public byte[] Fpga2BlockHash;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //文件头校验
        public byte[] HeaderHash; 
    }

    public struct BlockHeader
    {
        public uint HeaderLen;
        public uint Index;  //块索引
        public uint Len;  //块大小,不包含块头
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        //块校验
        public byte[] BlockHash;
    }
}
