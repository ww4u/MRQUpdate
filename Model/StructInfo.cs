using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public struct VCI_BOARD_INFO
    {
        public ushort hw_Version;
        public ushort fw_Version;
        public ushort dr_Version;
        public ushort in_Version;
        public ushort irq_Num;
        public byte can_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Reserved;
    }

    /////////////////////////////////////////////////////
    //2.定义CAN信息帧的数据类型。
    public unsafe struct VCI_CAN_OBJ //使用不安全代码
    {
        public uint ID;
        public uint TimeStamp; //时间标识
        public byte TimeFlag; //是否使用时间标识
        public byte SendType; //发送标志。保留，未用
        public byte RemoteFlag; //是否是远程帧
        public byte ExternFlag; //是否是扩展帧
        public byte DataLen; //数据长度
        public fixed byte Data[8]; //数据
        public fixed byte Reserved[3]; //保留位
    }

    //3.定义初始化CAN的数据类型
    public struct VCI_INIT_CONFIG
    {
        public uint AccCode;
        public uint AccMask;
        public uint Baud;
        public byte Filter; //0或1接收所有帧。2标准帧滤波，3是扩展帧滤波。
        public byte Timing0; //波特率参数，具体配置，请查看二次开发库函数说明书。
        public byte Timing1;
        public byte Mode; //模式，0表示正常模式，1表示只听模式,2自测模式
    }

    /*------------其他数据结构描述---------------------------------*/
    //4.USB-CAN总线适配器板卡信息的数据类型1，该类型为VCI_FindUsbDevice函数的返回参数。
    public struct VCI_BOARD_INFO1
    {
        public ushort hw_Version;
        public ushort fw_Version;
        public ushort dr_Version;
        public ushort in_Version;
        public ushort irq_Num;
        public byte can_Num;
        public byte Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] str_Serial_Num;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] str_hw_Type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] str_Usb_Serial;
    }

    /*------------数据结构描述完成---------------------------------*/

    public struct CHGDESIPANDPORT
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public byte[] szpwd;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] szdesip;
        public int desport;

        public void Init()
        {
            szpwd = new byte[10];
            szdesip = new byte[20];
        }
    }
}
