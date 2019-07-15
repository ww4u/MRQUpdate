using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using MegaRobo_Update.Model;

namespace MegaRobo_Update.Common
{
    public static class CanHelper
    {
        public delegate uint DelegateFindDevice(uint DeviceType, byte[] Receive,int ReceiveLen);
        public delegate uint DelegateOpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);
        public delegate uint DelegateCloseDevice(uint DeviceType, uint DeviceInd);
        public delegate uint DelegateInitCan(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);
        public delegate uint DelegateReadBoardInfo(uint DeviceType, uint DeviceInd, ref VCI_BOARD_INFO pInfo);
        public delegate uint DelegateGetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);
        public delegate uint DelegateClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);
        public delegate uint DelegateStartCan(uint DeviceType, uint DeviceInd, uint CANInd);
        public delegate uint DelegateResetCan(uint DeviceType, uint DeviceInd, uint CANInd);
        public delegate uint DelegateTransmit(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pSend, uint Len);
        public delegate uint DelegateReceive(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pReceive, uint Len, int WaitTime);
        #region Mrh special
        public delegate int DelegateWrite(uint DeviceType, uint DeviceInd, byte[] Send, uint Len);
        public delegate int DelegateRead(uint DeviceType, uint DeviceInd, byte[] Receive, uint Len, int WaitTime);
        //public delegate int DelegateFind(uint DeviceType, byte[] Receive);
        public delegate uint DelegateConnectDevice(uint DevType, uint DevIndex);
        public delegate uint DelegateUsbDeviceReset(uint DevType, uint DevIndex, uint Reserved);
        public delegate uint DelegateFindUsbDevice(ref VCI_BOARD_INFO1 pInfo);
        #endregion Mrh special

        public static DelegateOpenDevice VCI_OpenDevice;
        public static DelegateCloseDevice VCI_CloseDevice;
        public static DelegateInitCan VCI_InitCAN;
        public static DelegateReadBoardInfo VCI_ReadBoardInfo;
        public static DelegateGetReceiveNum VCI_GetReceiveNum;
        public static DelegateClearBuffer VCI_ClearBuffer;
        public static DelegateStartCan VCI_StartCAN;
        public static DelegateResetCan VCI_ResetCAN;
        public static DelegateTransmit VCI_Transmit;
        public static DelegateReceive VCI_Receive;
        public static DelegateWrite VCI_Write;
        public static DelegateRead VCI_Read;
        public static DelegateFindDevice VCI_FindDevice;
        //public static DelegateConnectDevice VCI_ConnectDevice;
        //public static DelegateUsbDeviceReset VCI_UsbDeviceReset;
        //public static DelegateFindUsbDevice VCI_FindUsbDevice;

        private static IntPtr dllmodule = IntPtr.Zero;
        public static void CloseDll()
        {
            if (dllmodule != IntPtr.Zero)
            {
                DllHelper.FreeDll(dllmodule);
                dllmodule = IntPtr.Zero;
            }
        }
        public static void LoadDll(string deviceType)
        {
            switch (deviceType)
            {
                case "USBCAN2":
                    dllmodule = DllHelper.LoadDll("ECanVci.dll");
                    VCI_OpenDevice = (DelegateOpenDevice)DllHelper.GetFunctionAddress(dllmodule, "VCI_OpenDevice", typeof(DelegateOpenDevice));
                    VCI_CloseDevice = (DelegateCloseDevice)DllHelper.GetFunctionAddress(dllmodule, "VCI_CloseDevice", typeof(DelegateCloseDevice));
                    VCI_InitCAN = (DelegateInitCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_InitCAN", typeof(DelegateInitCan));
                    VCI_ReadBoardInfo = (DelegateReadBoardInfo)DllHelper.GetFunctionAddress(dllmodule, "VCI_ReadBoardInfo", typeof(DelegateReadBoardInfo)); ;
                    VCI_GetReceiveNum = (DelegateGetReceiveNum)DllHelper.GetFunctionAddress(dllmodule, "VCI_GetReceiveNum", typeof(DelegateGetReceiveNum));
                    VCI_ClearBuffer = (DelegateClearBuffer)DllHelper.GetFunctionAddress(dllmodule, "VCI_ClearBuffer", typeof(DelegateClearBuffer));
                    VCI_StartCAN = (DelegateStartCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_StartCAN", typeof(DelegateStartCan));
                    VCI_ResetCAN = (DelegateResetCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_ResetCAN", typeof(DelegateResetCan));
                    VCI_Transmit = (DelegateTransmit)DllHelper.GetFunctionAddress(dllmodule, "VCI_Transmit", typeof(DelegateTransmit));
                    VCI_Receive = (DelegateReceive)DllHelper.GetFunctionAddress(dllmodule, "VCI_Receive", typeof(DelegateReceive));
                    break;
                case "MRH-U":
                    dllmodule = DllHelper.LoadDll("ControlCAN.dll");
                    VCI_OpenDevice = (DelegateOpenDevice)DllHelper.GetFunctionAddress(dllmodule, "VCI_OpenDevice", typeof(DelegateOpenDevice));
                    VCI_CloseDevice = (DelegateCloseDevice)DllHelper.GetFunctionAddress(dllmodule, "VCI_CloseDevice", typeof(DelegateCloseDevice));
                    VCI_InitCAN = (DelegateInitCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_InitCAN", typeof(DelegateInitCan));
                    VCI_ReadBoardInfo = (DelegateReadBoardInfo)DllHelper.GetFunctionAddress(dllmodule, "VCI_ReadBoardInfo", typeof(DelegateReadBoardInfo)); ;
                    VCI_GetReceiveNum = (DelegateGetReceiveNum)DllHelper.GetFunctionAddress(dllmodule, "VCI_GetReceiveNum", typeof(DelegateGetReceiveNum));
                    VCI_ClearBuffer = (DelegateClearBuffer)DllHelper.GetFunctionAddress(dllmodule, "VCI_ClearBuffer", typeof(DelegateClearBuffer));
                    VCI_StartCAN = (DelegateStartCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_StartCAN", typeof(DelegateStartCan));
                    VCI_ResetCAN = (DelegateResetCan)DllHelper.GetFunctionAddress(dllmodule, "VCI_ResetCAN", typeof(DelegateResetCan));
                    VCI_Transmit = (DelegateTransmit)DllHelper.GetFunctionAddress(dllmodule, "VCI_Transmit", typeof(DelegateTransmit));
                    VCI_Receive = (DelegateReceive)DllHelper.GetFunctionAddress(dllmodule, "VCI_Receive", typeof(DelegateReceive));
                    break;
                case "MRH-T":
                case "MRH-E":
                    dllmodule = DllHelper.LoadDll("MegaCanDevice.dll");
                    //dllmodule = DllHelper.LoadDll(@"D:\work\MegaRobo_Update\MegaRobo_Update\MegaRobo_Update\bin\Debug\MegaCanDevice.dll");
                    VCI_OpenDevice = (DelegateOpenDevice)DllHelper.GetFunctionAddress(dllmodule, "_VCI_OpenDevice@12", typeof(DelegateOpenDevice));
                    VCI_CloseDevice = (DelegateCloseDevice)DllHelper.GetFunctionAddress(dllmodule, "_VCI_CloseDevice@8", typeof(DelegateCloseDevice));
                    VCI_InitCAN = (DelegateInitCan)DllHelper.GetFunctionAddress(dllmodule, "_VCI_InitCAN@16", typeof(DelegateInitCan));
                    VCI_ReadBoardInfo = (DelegateReadBoardInfo)DllHelper.GetFunctionAddress(dllmodule, "_VCI_ReadBoardInfo@12", typeof(DelegateReadBoardInfo)); ;
                    VCI_GetReceiveNum = (DelegateGetReceiveNum)DllHelper.GetFunctionAddress(dllmodule, "_VCI_GetReceiveNum@12", typeof(DelegateGetReceiveNum));
                    VCI_ClearBuffer = (DelegateClearBuffer)DllHelper.GetFunctionAddress(dllmodule, "_VCI_ClearBuffer@12", typeof(DelegateClearBuffer));
                    VCI_StartCAN = (DelegateStartCan)DllHelper.GetFunctionAddress(dllmodule, "_VCI_StartCAN@12", typeof(DelegateStartCan));
                    VCI_ResetCAN = (DelegateResetCan)DllHelper.GetFunctionAddress(dllmodule, "_VCI_ResetCAN@12", typeof(DelegateResetCan));
                    VCI_Transmit = (DelegateTransmit)DllHelper.GetFunctionAddress(dllmodule, "_VCI_Transmit@20", typeof(DelegateTransmit));
                    VCI_Receive = (DelegateReceive)DllHelper.GetFunctionAddress(dllmodule, "_VCI_Receive@24", typeof(DelegateReceive));
                    VCI_Write = (DelegateWrite)DllHelper.GetFunctionAddress(dllmodule, "_VCI_Write@16", typeof(DelegateWrite));
                    VCI_Read = (DelegateRead)DllHelper.GetFunctionAddress(dllmodule, "_VCI_Read@20", typeof(DelegateRead));
                    VCI_FindDevice = (DelegateFindDevice)DllHelper.GetFunctionAddress(dllmodule, "_VCI_FindDevice@12", typeof(DelegateFindDevice));
                    break;
            }
        }
    }
}
