using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MegaRobo_Update.Model;

namespace MegaRobo_Update.Common
{
    public class Communication
    {
        public readonly uint currentDeviceType;
        public uint selectedDeviceIndex;
        private readonly uint selectedCanIndex;
        private bool canIsOpened;
        private readonly VCI_CAN_OBJ[] receivedDataBuff;
        private static readonly object Locker =new object();
        public Communication(string type, uint index = 0)
        {
            switch (type)
            {
                case "MRH-U":
                case "USBCAN2":
                    currentDeviceType = 4;
                    break;
                case "MRH-E":
                    currentDeviceType = 6;
                    break;
                case "MRH-T":
                    currentDeviceType = 8;
                    break;
            }
            selectedDeviceIndex = index;
            selectedCanIndex = 0;
            canIsOpened = false;
            receivedDataBuff = new VCI_CAN_OBJ[5000];
            CanHelper.LoadDll(type);
        }
        /// <summary>
        /// 启动Can
        /// </summary>        
        public int OpenCan(string ip)
        {            
                byte[] outPut = new byte[1024];
                int len = (int)CanHelper.VCI_FindDevice(currentDeviceType, outPut, outPut.Length);
                if (len == 0)
                {
                    return -1;
                }
                string[] s = Encoding.ASCII.GetString(outPut, 0, len).Split(';');
                if (s[0] == "")
                {
                    return -1;
                }
            var count = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (ip == s[i])
                {
                    selectedDeviceIndex = (uint)i;
                    break;
                }
                count++;
            } 
            if(count == s.Length)
            {
                return -1;
            }         
            return InitCan(selectedDeviceIndex, selectedCanIndex);
        }
        public int CloseCan()
        {
            canIsOpened = false;
            return (int) CanHelper.VCI_CloseDevice(currentDeviceType, selectedCanIndex);
        }
        /// <summary>
        /// 打开Can
        /// </summary>
        /// <param name="devind">设备号</param>
        /// <param name="canind">can通道号</param>
        private int InitCan(uint devind, uint canind)
        {
            if (CanHelper.VCI_CloseDevice == null)
            {
                return -1;
            }
            try
            {
                 // 先关闭设备
                 CanHelper.VCI_CloseDevice(currentDeviceType, devind);
            }
            catch (Exception)
            {
                return -1;
            }
            // 打开设备
            if (CanHelper.VCI_OpenDevice(currentDeviceType, devind, 0) != 1)
            {
                return -1;
            }
            var config = new VCI_INIT_CONFIG
            {
                AccCode = 0,
                AccMask = 0xFFFFFFFF,
                Timing0 = 0x00,
                Timing1 = 0x14,
                Filter = 0,
                Mode = 0,
                Baud = 1000000
            };
            //不关心所有的位,即,在CAN总线上的所有帧,主机都要接收
            //1M波特率
            //接收全部帧(标准帧或扩展帧)
            CanHelper.VCI_ClearBuffer(currentDeviceType, devind, canind);
            //配置can口参数，初始化失败，关闭can
            if (CanHelper.VCI_InitCAN(currentDeviceType, devind, canind, ref config) != 1)
            {
                CanHelper.VCI_CloseDevice(currentDeviceType, devind);
                return -1;
            }
            CanHelper.VCI_StartCAN(currentDeviceType, devind, canind);
            canIsOpened = true;
            return 0;
        }

        /// <summary>
        ///     发送给通讯设备（是发送帧数为空，返回-1，不为空返回0）
        /// </summary>
        /// <param name="id">接收Id</param>
        /// <param name="data">通讯数据</param>
        public unsafe int SendData(uint id, byte[] data)
        {
            //lock (Locker)
            //{
                var length = (byte) data.Length;
                var sendobj = new VCI_CAN_OBJ();

                sendobj.RemoteFlag = 0;
                sendobj.ExternFlag = 1;
                sendobj.ID = id;
                sendobj.DataLen = length;
                //数据帧
                //扩展
                //暂时认为,命令+参数不会超过8个字节
                for (var i = 0; i < length; i++)
                {
                    sendobj.Data[i] = data[i];
                }
                if (CanHelper.VCI_Transmit(currentDeviceType, selectedDeviceIndex,
                    selectedCanIndex, ref sendobj, 1) == 0)
                {
                    if (CanHelper.VCI_Transmit(currentDeviceType, selectedDeviceIndex,
                        selectedCanIndex, ref sendobj, 1) == 0)
                    {
                        return -1;
                    }
                }
                return 0;
            //}
        }
        public unsafe List<ExecResult> ReceiveData(int main,int sub,int channel =-1,int index =-1)
        {
            // 计数器--数据个数
            int count = 0;
            int queryCount = 0;
            while (queryCount <= 20)
            {
                int res = (int)CanHelper.VCI_Receive(currentDeviceType, selectedDeviceIndex, selectedCanIndex,
                    ref receivedDataBuff[0 + count], 1, 5);
                Thread.Sleep(10);
                if (res == 0)
                {
                    res = (int)CanHelper.VCI_Receive(currentDeviceType, selectedDeviceIndex, selectedCanIndex,
                        ref receivedDataBuff[0 + count], 1, 5);
                    queryCount++;
                    if (res == 0)
                    {
                        break;
                    }
                    count ++;
                }
                else
                {
                    count ++;
                }
            }
            List < ExecResult > result = new List<ExecResult>();
            for (int i = 0; i < count; i++)
            {
                ExecResult tempResult = new ExecResult();
                var data = receivedDataBuff[i];
                var len = data.DataLen;
                if (len < 2)
                {
                    continue;
                }              
                byte[] temp = new byte[len];
                for (int j = 0; j < len; j++)
                {
                    temp[j] = data.Data[j];
                }
                if (temp[0] != main || temp[1] != sub)
                {
                    continue;
                }
                if (channel != -1)
                {
                    if (temp[2] != channel)
                    {
                        continue;
                    }
                }
                if (index != -1)
                {
                    if (temp[3] != channel)
                    {
                        continue;
                    }
                }
                tempResult.SendId = data.ID;
                tempResult.Result = temp;
                result.Add(tempResult);
            }
            return result;
        }
    }
}
