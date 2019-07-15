using MegaRobo_Update.Common;
using MegaRobo_Update.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MegaRobo_Update
{
    class Program
    {
        private static Communication Comm;
        private static readonly List<DeviceInfo> DeviceList = new List<DeviceInfo>();
        private const string MRQ_C = "MRQ-C";
        private const string MRQ_M = "MRQ-M";
        private const string MRQ_MV = "MRQ-MV";
        private const string MRQ_MC = "MRQ-MC";
        private const string MRQ_MH = "MRQ-MH";
        private const string MRV = "MRV";
        private static string FilePath;
        private static string Ip;
        private static uint RecieveId;
        private static int bootIndex;
        private static int armIndex;
        private static int fpga1Index;
        private static int fpga2Index;
        private static int updateType;
        private static FileStream file;
        private static FileHeader fileHeader;
        private static BinaryReader BReader;
        private const int MaxLen = 7;
        private const int bootMaxLen = 6;
        private static bool restart; 
        // 主函数需要指定文件路径、MRH地址、设备的RecieveID
        static void Main(string[] args)
        {
            // 参数为空
            if (args == null || args.Length != 3)
            {
                Console.WriteLine("error:you need input three params!");
                return;
            }
            FilePath = args[0];
            Ip = args[1];
            RecieveId = Convert.ToUInt32(args[2]);
            //FilePath = C:\\Users\\DINGJIANWEI\\Desktop\\Sinanju Update\\MRQMV(Software)Update(Normal)_00.00.02.00.03.upd;
            //Ip = TCPIP0::169.254.1.2::inst0::INSTR;
            //RecieveId = 384;
            // 搜索-T
            Comm = new Communication("MRH-T");
            if (Comm.OpenCan(Ip) == 0)
            {
                Console.WriteLine("Notify:Open MRH-T Success");
            }
            else
            {
                Console.WriteLine("Notify:Open MRH-T Failed");
                return;
            }
            // 切换模式   
            string cmd = $"SYSTEM:MODE:SWITCH 0\n";
            CanHelper.VCI_Write(Comm.currentDeviceType, Comm.selectedDeviceIndex, Encoding.Default.GetBytes(cmd),
            (uint)cmd.Length);
            // 搜索设备
            if(! SearchDevices())
            {
                return;
            }
            // 检查设备类型
            if(!CheckDevices())
            {
                return;
            }
            // 开始升级
            StartUpdate();
        }
        private static bool SearchDevices()
        {
            // 先建立连接
            if (SendData(RecieveId, (byte)DriveControllerMainCommand.Link, (byte)LinkSubCommand.INTFC,
                new byte[] { (byte)LinkType.CAN }) < 0)
            {
                Console.WriteLine("Error:Cannot establish communication! Please check the connection !");
                return false;
            }         
                DeviceInfo info = new DeviceInfo
                {
                    RecieveId = RecieveId
                };
                // 查询设备型号
                SendData(RecieveId, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.TYPEQuery);
                Thread.Sleep(10);
                var result = Comm.ReceiveData((byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.TYPEQuery);
                if (result.Count > 0)
                {
                    if (result[0].Result[2] == 1)
                    {
                        info.DeviceType = MRV;
                    }
                    else
                    {
                        switch (result[0].Result[3])
                        {
                            case 0:
                            case 1:
                                info.DeviceType = MRQ_C;
                                break;
                            case 2:
                                info.DeviceType = MRQ_M;
                                break;
                            case 3:
                                info.DeviceType = MRQ_MV;
                                break;
                            case 4:
                                info.DeviceType = MRQ_MC;
                                break;
                            case 5:
                                info.DeviceType = MRQ_MH;
                                break;
                        }
                    }
                }
                DeviceList.Add(info);
            Console.WriteLine("Notify:Connect to the device");
            return true;
        }
        private static bool CheckDevices()
        {
            string type = DeviceList[0].DeviceType;
            for (int i = 1; i < DeviceList.Count; i++)
            {
                if(type  == DeviceList[0].DeviceType)
                {
                    Console.WriteLine("Only can update one kind of device !");
                    return false;
                }
            }
            return true;
        }
        private static void StartUpdate()
        {
            file = new FileStream(FilePath, FileMode.Open);
            BReader = new BinaryReader(file);
            fileHeader = new FileHeader();
            fileHeader.Flag = BReader.ReadUInt32();
            if (fileHeader.Flag != 0x4450555f)
            {
                Console.WriteLine("Error:The update file is invalid !");            
                return;
            }
            fileHeader.Type = BReader.ReadUInt32();
            string tempType = string.Empty;
            switch (fileHeader.Type)
            {
                case 0:
                    tempType = MRQ_C;
                    break;
                case 1:
                    tempType = MRQ_M;
                    break;
                case 2:
                    tempType = MRQ_MV;
                    break;
                case 3:
                    tempType = MRQ_MC;
                    break;
                case 4:
                    tempType = MRQ_MH;
                    break;
                case 5:
                    tempType = MRV;
                    break;
            }
            var deviceType = DeviceList[0].DeviceType;
            //if (tempType != deviceType)
            //{
            //    Console.WriteLine("Error:The update file type is invalid !");
            //    return;
            //}
            // 读取文件头长度
            fileHeader.HeaderLen = BReader.ReadUInt32();
            // 读取文件长度
            fileHeader.BootLen = BReader.ReadUInt32();
            // 读取boot的偏移
            fileHeader.BootOffset = BReader.ReadUInt32();
            // boot地址
            fileHeader.BootAddr = BReader.ReadUInt32();
            // boot版本号
            fileHeader.BootVersion = new byte[] { BReader.ReadByte(), BReader.ReadByte() };
            // boot块数
            fileHeader.BootBlockCount = BReader.ReadUInt32();
            // boot校验码
            byte[] bootHash = new byte[16];
            for (int i = 0; i < bootHash.Length; i++)
            {
                bootHash[i] = BReader.ReadByte();
            }
            fileHeader.BootBlockHash = bootHash;
            // 读取文件长度
            fileHeader.ArmLen = BReader.ReadUInt32();
            // arm偏移
            fileHeader.ArmOffset = BReader.ReadUInt32();
            // arm地址
            fileHeader.ArmAddr = BReader.ReadUInt32();
            // arm版本号
            fileHeader.ArmVersion = new byte[] { BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte() };
            // arm块数
            fileHeader.ArmBlockCount = BReader.ReadUInt32();
            // arm校验码
            byte[] armHash = new byte[16];
            for (int i = 0; i < armHash.Length; i++)
            {
                armHash[i] = BReader.ReadByte();
            }
            fileHeader.ArmBlockHash = armHash;
            // 读取文件长度
            fileHeader.Fpga1Len = BReader.ReadUInt32();
            // fpga偏移
            fileHeader.Fpga1Offset = BReader.ReadUInt32();
            // fpga地址
            fileHeader.Fpga1Addr = BReader.ReadUInt32();
            // fpga版本号
            fileHeader.Fpga1Version = new byte[] { BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte(),
                BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte() };
            // fpga块数
            fileHeader.Fpga1BlockCount = BReader.ReadUInt32();
            // fpga1校验码
            byte[] fpga1Hash = new byte[16];
            for (int i = 0; i < fpga1Hash.Length; i++)
            {
                fpga1Hash[i] = BReader.ReadByte();
            }
            // 读取文件长度
            fileHeader.Fpga2Len = BReader.ReadUInt32();
            // fpga偏移
            fileHeader.Fpga2Offset = BReader.ReadUInt32();
            // fpga地址
            fileHeader.Fpga2Addr = BReader.ReadUInt32();
            // fpga版本号
            fileHeader.Fpga2Version = new byte[] { BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte(),
                BReader.ReadByte(), BReader.ReadByte(), BReader.ReadByte() };
            // fpga块数
            fileHeader.Fpga2BlockCount = BReader.ReadUInt32();
            // fpga校验码
            byte[] fpga2Hash = new byte[16];
            for (int i = 0; i < fpga2Hash.Length; i++)
            {
                fpga2Hash[i] = BReader.ReadByte();
            }
            // 文件头校验
            byte[] tempHash = new byte[16];
            for (int i = 0; i < tempHash.Length; i++)
            {
                tempHash[i] = BReader.ReadByte();
            }
            fileHeader.HeaderHash = tempHash;

            // 读取并校验 boot,arm,fpga版本号
            if (!CheckVer())
            {
                BReader.Close();
                file.Close();
                return;
            }

            if (!PreUpdate())
            {
                BReader.Close();
                file.Close();            
                return;
            }
            // 开始升级
            Update();
        }
        private static bool CheckVer()
        {
            bool armFlg, fpgaFlg;
            //读取boot版本号
            CanHelper.VCI_ClearBuffer(Comm.currentDeviceType, Comm.selectedDeviceIndex, 0);
            Thread.Sleep(100);
            SendData(RecieveId, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.BOOTVERQuery);
            var result = Comm.ReceiveData((byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.BOOTVERQuery);
            if (result.Count == 0 || result.Count != DeviceList.Count)
            {
                Console.WriteLine("Error:Cannot get the BOOT version!");
                return false;
            }
            int mainVer, subVer, branchVer, childVer1, childVer2, childVer3;
            bootIndex = 3;
            for (int j = 0; j < result.Count; j++)
            {
                mainVer = result[j].Result[2];
                subVer = result[j].Result[3];
                if (mainVer <= 2)
                {
                    bootIndex = 4;
                    break;
                }
                if (mainVer < fileHeader.BootVersion[0])
                {
                    bootIndex = 3;
                    break;
                }
                if (mainVer > fileHeader.BootVersion[0])
                {
                    bootIndex = 0;
                    break;
                }
                if (subVer > fileHeader.BootVersion[1])
                {
                    bootIndex = 2;
                    break;
                }
                bootIndex = subVer < fileHeader.BootVersion[1] ? 3 : 1;
            }
            //读取Arm版本号
            SendData(RecieveId, (byte)DriveControllerMainCommand.Factory, 2);
            result = Comm.ReceiveData((byte)DriveControllerMainCommand.Factory, 2);
            armIndex = 3;
            if (result.Count == 0 || result.Count != DeviceList.Count)
            {
                armFlg = false;
            }
            else
            {
                armFlg = true;
                for (int j = 0; j < result.Count; j++)
                {
                    mainVer = result[j].Result[2];
                    subVer = result[j].Result[3];
                    branchVer = result[j].Result[4];
                    childVer1 = result[j].Result[5];
                    if (result[j].Result.Length <= 6)
                    {
                        childVer2 = 0;
                    }
                    else
                    {
                        childVer2 = result[j].Result[6];
                    }
                    if (branchVer < fileHeader.ArmVersion[2])
                    {
                        armIndex = 3;
                        break;
                    }
                    if (branchVer > fileHeader.ArmVersion[2])
                    {
                        armIndex = 2;
                        break;
                    }
                    if (childVer1 < fileHeader.ArmVersion[3])
                    {
                        armIndex = 3;
                        break;
                    }
                    if (childVer1 > fileHeader.ArmVersion[3])
                    {
                        armIndex = 2;
                        break;
                    }
                    if (childVer2 < fileHeader.ArmVersion[4])
                    {
                        armIndex = 3;
                        break;
                    }
                    if (childVer2 > fileHeader.ArmVersion[4])
                    {
                        armIndex = 2;
                        break;
                    }
                    if (mainVer != fileHeader.ArmVersion[0])
                    {
                        armIndex = 3;
                        break;
                    }
                    armIndex = 1;
                }
            }
            //读取fpga版本号
            SendData(RecieveId, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.FPGAVERQuery);
            result = Comm.ReceiveData((byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.FPGAVERQuery);
            fpga1Index = 3;
            fpga2Index = 3;
            if (result.Count == 0 || result.Count != DeviceList.Count)
            {
                fpgaFlg = false;
            }
            else
            {
                fpgaFlg = true;
                for (int j = 0; j < result.Count; j++)
                {
                    mainVer = result[j].Result[2];
                    subVer = result[j].Result[3];
                    branchVer = result[j].Result[4];
                    childVer1 = result[j].Result[5];
                    childVer2 = result[j].Result[6];
                    childVer3 = result[j].Result[7];
                    if (j % 2 == 0)
                    {
                        if (mainVer != fileHeader.Fpga1Version[0] || subVer != fileHeader.Fpga1Version[1] ||
                            branchVer != fileHeader.Fpga1Version[2]
                            || childVer1 != fileHeader.Fpga1Version[3] || childVer2 != fileHeader.Fpga1Version[4] ||
                            childVer3 != fileHeader.Fpga1Version[5])
                        {
                            fpga1Index = 0;
                            break;
                        }
                        fpga1Index = 1;
                    }
                    else
                    {
                        if (mainVer != fileHeader.Fpga2Version[0] || subVer != fileHeader.Fpga2Version[1] ||
                            branchVer != fileHeader.Fpga2Version[2]
                            || childVer1 != fileHeader.Fpga2Version[3] || childVer2 != fileHeader.Fpga2Version[4] ||
                            childVer3 != fileHeader.Fpga2Version[5])
                        {
                            fpga2Index = 0;
                            break;
                        }
                        fpga2Index = 1;
                    }
                }
            }

            if (!armFlg && !fpgaFlg)
            {
                updateType = 1;
            }
            else
            {
                updateType = 0;
            }
            return true;
        }
        private static bool PreUpdate()
        {
            // boot
            // 0:不能升级 版本太旧
            // 1:不升级
            // 2:旧版本 需要提示升级
            // 3:新版本 直接升级
            if (bootIndex == 0)
            {
                Console.WriteLine("Error:The BOOT version is very old,Cannot update to this version!");
                return false;
            }
            if (updateType == 1)
            {
                if (bootIndex == 4)
                {
                    Console.WriteLine("Error:The BOOT version is very old,Cannot update to this version!");
                    return false;
                }
            }
            return true;
        }
        private static void Update()
        {
            var deviceList = DeviceList;
            if (updateType == 0)
            {
                if (bootIndex != 1)
                {
                    // 下发boot数据
                    if (!UpdateBoot(deviceList))
                    {
                        BReader.Close();
                        file.Close();
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Notify:BOOT version is the same, ignoring upgrades !");
                    BReader.ReadBytes((int)(fileHeader.BootLen + 28));
                }
            }
            else
            {
                // 读到Arm处
                BReader.ReadBytes((int)(fileHeader.BootLen + 28));
            }

            //! \note do update
            if ( armIndex == 1 
                || armIndex == 2 
                || armIndex == 3 )
            {
                // 下发arm数据
                if (!UpdateArm(deviceList))
                {
                    BReader.Close();
                    file.Close();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Notify:ARM version is the same, ignoring upgrades !");
                BReader.ReadBytes((int)(fileHeader.ArmLen + fileHeader.ArmBlockCount * 28));
            }

            //! \note do update
            if ( fpga1Index == 1 || fpga1Index == 0 )
            {
                // 下发fpga1数据
                if (!UpdateFpga1())
                {
                    BReader.Close();
                    file.Close();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Notify:FPGA1 version is the same, ignoring upgrades !");
                BReader.ReadBytes((int)(fileHeader.Fpga1Len +
                    fileHeader.Fpga1BlockCount * 28));
            }

            //! \note do update
            if (fpga2Index == 1 || fpga2Index == 0)
            {
                // 下发fpga2数据
                if (!UpdateFpga2())
                {
                    BReader.Close();
                    file.Close();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Notify:FPGA2 version is the same, ignoring upgrades !");
            }
            if (updateType == 1)
            {
                // 跳转
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.JUMP);
                Thread.Sleep(5000);
                int tryTimes = 0;
                while (tryTimes <= 3)
                {
                    SendData(RecieveId, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.SOFTVERQuery);
                    if (CheckState(deviceList, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.SOFTVERQuery))
                    {
                        break;
                    }
                    tryTimes++;
                    if (tryTimes == 3)
                    {
                        Console.WriteLine("Error:Reload failed !");
                        return;
                    }
                }
                //读取Arm版本号
                SendData(RecieveId, (byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.SOFTVERQuery);
                var result = Comm.ReceiveData((byte)DriveControllerMainCommand.System, (byte)SystemSubCommand.SOFTVERQuery);
                if (result.Count == 0 || result.Count != deviceList.Count)
                {
                    Console.WriteLine("Error:Cannot get the software version of the device !");
                    return;
                }
                BReader.Close();
                file.Close();
                file = new FileStream(FilePath, FileMode.Open);
                BReader = new BinaryReader(file);
                BReader.ReadBytes((int)fileHeader.HeaderLen);
                // 下发boot数据
                if (!UpdateBoot(deviceList))
                {
                    BReader.Close();
                    file.Close();
                    return;
                }
            }
            for (int i = 0; i < deviceList.Count; i++)
            {
                if (updateType == 0)
                {
                    // 跳转
                    SendData(deviceList[i].RecieveId, (byte)DriveControllerMainCommand.Update,
                        (byte)UpdateSubCommand.JUMP);
                }
            }
            BReader.Close();
            file.Close();
            Console.WriteLine("Notify:Update Complete!");
        }
        private static bool UpdateBoot(List<DeviceInfo> list)
        {
            BlockHeader blockHeader = new BlockHeader
            {
                HeaderLen = BReader.ReadUInt32(),
                Index = BReader.ReadUInt32(),
                Len = BReader.ReadUInt32()
            };
            var tempHash = new byte[16];
            for (int i = 0; i < tempHash.Length; i++)
            {
                tempHash[i] = BReader.ReadByte();
            }
            blockHeader.BlockHash = tempHash;
            if (blockHeader.Len > 0)
            {
                var buff = BReader.ReadBytes((int)blockHeader.Len);
                MD5 md5 = new MD5CryptoServiceProvider();
                var hashBytes = md5.ComputeHash(buff.ToArray());
                if (hashBytes.Where((t, i) => t != fileHeader.BootBlockHash[i]).Any())
                {
                   Console.WriteLine("Error:ARM file has validation error, file may be corrupted,Stop updating!");
                    return false;
                };
                // 发送升级命令
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                    null, (byte)UpdateType.BOOT);
                if (!CheckState(list, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START))
                {
                    return false;
                }
                byte[] len = BitConverter.GetBytes(32 * 1024);
                // 擦除BOOT命令
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE,
                    len, (byte)UpdateType.BOOT);
                if (!CheckState(list, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE))
                {
                    return false;
                }
                // 下发数据
               Console.WriteLine("Notify:Send BOOT data,please wait for a moment...");
                for (int j = 0; j < 4; j++)
                {
                    byte[] param = new byte[4];
                    Array.Copy(blockHeader.BlockHash, 4 * j, param, 0, 4);
                    byte index = (byte)(((j + 1) << 4) + 4);
                    // 发送哈希校验
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.MD5,
                        param, index);
                }
                Thread.Sleep(10);
                int sendBytes;
                int sendCount = 0;
                int preValue = 0;
                int tempCount = 0;
                for (int i = 0; i < (int)blockHeader.Len; i += sendBytes)
                {
                    if (i + bootMaxLen > blockHeader.Len)
                    {
                        sendBytes = (int)blockHeader.Len - i;
                    }
                    else
                    {
                        sendBytes = bootMaxLen;
                    }
                    byte[] sendData = new byte[sendBytes];
                    Array.Copy(buff, i, sendData, 0, sendBytes);
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.DATA,
                    sendData);
                    sendCount += sendBytes;
                    tempCount += sendBytes;
                    var value = (int)(sendCount * 1.0 / fileHeader.BootLen * 100);
                    if (preValue != value)
                    {
                        Console.WriteLine($"Progress:{value}%");
                    }
                    preValue = value;
                    if (tempCount >= 2048)
                    {
                        Thread.Sleep(100);
                        tempCount -= 2048;
                    }
                }
                Thread.Sleep(100);
                CanHelper.VCI_ClearBuffer(Comm.currentDeviceType, Comm.selectedDeviceIndex, 0);
                Thread.Sleep(100);
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END);
                if (!CheckState(list, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END))
                {
                    Console.WriteLine("Error:BOOT data download failed !");
                    return false;
                }
                // 等待返回
                Console.WriteLine("Notify:BOOT data download complete !");
            }
            else
            {
                Console.WriteLine("Notify:No boot !");
            }
            return true;
        }

        private static bool UpdateArm(List<DeviceInfo> list)
        {
            restart = false;
            if (fileHeader.ArmLen == 0)
            {
                Console.WriteLine("Notify:No ARM !");
                return true;
            }
            Console.WriteLine("Notify:Prepare to update ARM !");
            // 发送升级命令
            SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                null, (byte)UpdateType.APP);
            Thread.Sleep(100);
            // 先重启
            Console.WriteLine("Notify:Prepare to reload !");
            int tryTimes = 0;
            while (tryTimes <= 200)
            {
                SendData(RecieveId, (byte)DriveControllerMainCommand.Link,
                        (byte)LinkSubCommand.INTFCQuery);
                Thread.Sleep(100);
                var result = Comm.ReceiveData((byte)DriveControllerMainCommand.Link,
                        (byte)LinkSubCommand.INTFCQuery);
                if (result.Count != list.Count)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    Console.WriteLine("Notify:Reload successfully!");
                    break;
                }
                tryTimes++;
            }
            if (tryTimes >= 200)
            {
                Console.WriteLine("Notify:Reload failed!");
                return false;
            }
            int sendCount = 0;
            int preValue = 0;
            for (int i = 0; i < fileHeader.ArmBlockCount; i++)
            {
                BlockHeader header = new BlockHeader();
                header.HeaderLen = BReader.ReadUInt32();
                header.Index = BReader.ReadUInt32();
                header.Len = BReader.ReadUInt32();
                byte[] blockHash = new byte[16];
                for (int j = 0; j < blockHash.Length; j++)
                {
                    blockHash[j] = BReader.ReadByte();
                }
                header.BlockHash = blockHash;
                if (header.Len > 0)
                {
                    var buffer = BReader.ReadBytes((int)header.Len);
                    MD5 md5 = new MD5CryptoServiceProvider();
                    var hashBytes = md5.ComputeHash(buffer.ToArray());
                    if (hashBytes.Where((t, k) => t != header.BlockHash[k]).Any())
                    {
                        Console.WriteLine("Error:ARM file has validation error, file may be corrupted,Stop updating!");
                        return false;
                    }
                    if (i == 0)
                    {
                        var addr = fileHeader.ArmAddr;
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update,
                            (byte)UpdateSubCommand.APPADDR,
                            BitConverter.GetBytes(addr));
                        Thread.Sleep(10);
                        CanHelper.VCI_ClearBuffer(Comm.currentDeviceType, Comm.selectedDeviceIndex, 0);
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update,
                            (byte)UpdateSubCommand.START, null, (byte)UpdateType.APP);
                        byte[] len = BitConverter.GetBytes(192 * 1024);
                        // 擦除arm命令
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE
                            , len, (byte)UpdateType.APP);
                        if (!CheckState(list, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE))
                        {
                            return false;
                        }
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        byte[] param = new byte[4];
                        Array.Copy(header.BlockHash, 4 * j, param, 0, 4);
                        byte index = (byte)(((j + 1) << 4) + 4);
                        // 发送哈希校验
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.MD5,
                            param, index);
                    }
                    if (i == 0)
                    {
                        // 下发数据
                        Console.WriteLine("Notify:Send ARM data,please wait for a moment...");
                    }
                    int sendBytes;
                    for (int j = 0; j < (int)header.Len; j += sendBytes)
                    {
                        if (j + MaxLen > header.Len)
                        {
                            sendBytes = (int)header.Len - j;
                        }
                        else
                        {
                            sendBytes = MaxLen;
                        }
                        byte[] sendData = new byte[sendBytes];
                        Array.Copy(buffer, j, sendData, 0, sendBytes);
                        //SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.DATA,
                        //    sendData);
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Data, sendData);
                        sendCount += sendBytes;
                        var value = (int)(sendCount * 1.0 / fileHeader.ArmLen * 100);
                        if (preValue != value)
                        {
                            Console.WriteLine($"Progress:{value}%");
                        }
                        preValue = value;
                    }
                    // 发送完成
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END);
                    if (!CheckState(list, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END))
                    {
                        Console.WriteLine("Error:Does not respond");
                        return false;
                    }
                }
            }

            //! send the completed ARM
            SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END_ALL);
            if (!CheckState(list,
                             (byte)DriveControllerMainCommand.Update,
                             (byte)UpdateSubCommand.END_ALL, 
                             new byte[] { 0xA5 }))
            {
                Console.WriteLine("Error:Does not respond");
                return false;
            }
            else
            {  }

            Console.WriteLine("Notify:ARM data download completed !");
            restart = true;
            return true;
        }
        private static bool UpdateFpga1()
        {
            if (fileHeader.Fpga1Len == 0)
            {
                Console.WriteLine("Notify:No FPGA1 !");
                return true;
            }
            Console.WriteLine("Notify:Prepare to update FPGA1 !");
            if (!restart)
            {
                // 发送升级命令
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                    null, (byte)UpdateType.APP);
                Thread.Sleep(100);
                // 先重启
                Console.WriteLine("Notify:Prepare to reload !");
                int tryTimes = 0;
                while (tryTimes <= 50)
                {
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Link,
                            (byte)LinkSubCommand.INTFCQuery);
                    Thread.Sleep(100);
                    var result = Comm.ReceiveData((byte)DriveControllerMainCommand.Link,
                            (byte)LinkSubCommand.INTFCQuery);
                    if (result.Count != DeviceList.Count)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine("Notify:Reload successfully!");
                        break;
                    }
                    tryTimes++;
                }
                if (tryTimes >= 50)
                {
                    Console.WriteLine("Error:Reload failed!");
                    return false;
                }
                restart = true;
            }
            int sendCount = 0;
            int preValue = 0;
            for (int i = 0; i < fileHeader.Fpga1BlockCount; i++)
            {
                BlockHeader header = new BlockHeader();
                header.HeaderLen = BReader.ReadUInt32();
                header.Index = BReader.ReadUInt32();
                header.Len = BReader.ReadUInt32();
                byte[] blockHash = new byte[16];
                for (int j = 0; j < blockHash.Length; j++)
                {
                    blockHash[j] = BReader.ReadByte();
                }
                header.BlockHash = blockHash;
                if (header.Len > 0)
                {
                    var buffer = BReader.ReadBytes((int)header.Len);
                    MD5 md5 = new MD5CryptoServiceProvider();
                    var hashBytes = md5.ComputeHash(buffer.ToArray());
                    if (hashBytes.Where((t, k) => t != header.BlockHash[k]).Any())
                    {
                        Console.WriteLine("Error:FPGA1 file has validation error, file may be corrupted,Stop updating!");
                        return false;
                    }
                    if (i == 0)
                    {
                        var addr = fileHeader.Fpga1Addr;
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update,
                            (byte)UpdateSubCommand.FPGAADDR,
                            BitConverter.GetBytes(addr));
                        Thread.Sleep(10);
                        // 发送升级命令
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                            null, (byte)UpdateType.FPGA);
                        Thread.Sleep(100);
                        CanHelper.VCI_ClearBuffer(Comm.currentDeviceType, Comm.selectedDeviceIndex, 0);
                        Console.WriteLine("Notify:Prepare to erase the FLASH !");
                        byte[] len = BitConverter.GetBytes(768 * 1024);
                        // 擦除fpga命令
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE,
                            len, (byte)UpdateType.FPGA);
                        if (!CheckState(DeviceList, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE))
                        {
                            Console.WriteLine("Error:Erase the FLASH failed!");
                            return false;
                        }
                        // 下发数据
                        Console.WriteLine("Notify:Send FPGA1 data,please wait for a moment...");
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        byte[] param = new byte[4];
                        Array.Copy(header.BlockHash, 4 * j, param, 0, 4);
                        byte index = (byte)(((j + 1) << 4) + 4);
                        // 发送哈希校验
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.MD5, param, index);
                    }
                    int sendBytes;
                    for (int j = 0; j < (int)header.Len; j += sendBytes)
                    {
                        if (j + MaxLen > header.Len)
                        {
                            sendBytes = (int)header.Len - j;
                        }
                        else
                        {
                            sendBytes = MaxLen;
                        }
                        byte[] sendData = new byte[sendBytes];
                        Array.Copy(buffer, j, sendData, 0, sendBytes);
                        //SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.DATA,
                        //    sendData);
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Data, sendData);
                        sendCount += sendBytes;
                        var value = (int)(sendCount * 1.0 / fileHeader.Fpga1Len * 100);
                        if (preValue != value)
                        {
                            Console.WriteLine($"Progress:{value}%");
                        }
                        preValue = value;
                    }
                    // 发送完成
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END);
                    if (!CheckState(DeviceList, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END))
                    {
                        Console.WriteLine("Error:Does not respond");
                        return false;
                    }
                }
            }
            Console.WriteLine("FPGA1 data download completed !");
            return true;
        }
        private static bool UpdateFpga2()
        {
            if (fileHeader.Fpga2Len == 0)
            {
                Console.WriteLine("Notify:No FPGA2 !");
                return true;
            }
            Console.WriteLine("Notify:Prepare to update FPGA2 !");
            if (!restart)
            {
                // 发送升级命令
                SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                    null, (byte)UpdateType.APP);
                Thread.Sleep(100);
                // 先重启
                Console.WriteLine("Notify:Prepare to reload !");
                int tryTimes = 0;
                while (tryTimes <= 50)
                {
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Link,
                            (byte)LinkSubCommand.INTFCQuery);
                    Thread.Sleep(100);
                    var result = Comm.ReceiveData((byte)DriveControllerMainCommand.Link,
                            (byte)LinkSubCommand.INTFCQuery);
                    if (result.Count != DeviceList.Count)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        Console.WriteLine("Notify:Reload successfully!");
                        break;
                    }
                    tryTimes++;
                }
                if (tryTimes >= 50)
                {
                    Console.WriteLine("Notify:Reload failed!");
                    return false;
                }
                restart = true;
            }
            int sendCount = 0;
            int preValue = 0;
            for (int i = 0; i < fileHeader.Fpga2BlockCount; i++)
            {
                BlockHeader header = new BlockHeader();
                header.HeaderLen = BReader.ReadUInt32();
                header.Index = BReader.ReadUInt32();
                header.Len = BReader.ReadUInt32();
                byte[] blockHash = new byte[16];
                for (int j = 0; j < blockHash.Length; j++)
                {
                    blockHash[j] = BReader.ReadByte();
                }
                header.BlockHash = blockHash;
                if (header.Len > 0)
                {
                    var buffer = BReader.ReadBytes((int)header.Len);
                    MD5 md5 = new MD5CryptoServiceProvider();
                    var hashBytes = md5.ComputeHash(buffer.ToArray());
                    if (hashBytes.Where((t, k) => t != header.BlockHash[k]).Any())
                    {
                        Console.WriteLine("Error:FPGA2 file has validation error, file may be corrupted,Stop updating!");
                        return false;
                    }
                    if (i == 0)
                    {
                        var addr = fileHeader.Fpga2Addr;
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update,
                            (byte)UpdateSubCommand.FPGAADDR,
                            BitConverter.GetBytes(addr));
                        // 发送升级命令
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.START,
                            null, (byte)UpdateType.FPGA);
                        Thread.Sleep(100);
                        CanHelper.VCI_ClearBuffer(Comm.currentDeviceType, Comm.selectedDeviceIndex, 0);
                        Console.WriteLine("Notify:Prepare to erase the FLASH !");
                        byte[] len = BitConverter.GetBytes(1023 * 1024);
                        // 擦除fpga命令
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE,
                            len, (byte)UpdateType.FPGA);
                        Thread.Sleep(1000);
                        if (!CheckState(DeviceList, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.ERASE))
                        {
                            return false;
                        }
                        // 下发数据
                        Console.WriteLine("Notify:Send FPGA2 data,please wait for a moment...");
                    }
                    for (int j = 0; j < 4; j++)
                    {
                        byte[] param = new byte[4];
                        Array.Copy(header.BlockHash, 4 * j, param, 0, 4);
                        byte index = (byte)(((j + 1) << 4) + 4);
                        // 发送哈希校验
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.MD5, param, index);
                    }
                    int sendBytes;
                    for (int j = 0; j < (int)header.Len; j += sendBytes)
                    {
                        if (j + MaxLen > header.Len)
                        {
                            sendBytes = (int)header.Len - j;
                        }
                        else
                        {
                            sendBytes = MaxLen;
                        }
                        byte[] sendData = new byte[sendBytes];
                        Array.Copy(buffer, j, sendData, 0, sendBytes);
                        //SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.DATA,
                        //    sendData);
                        SendData(RecieveId, (byte)DriveControllerMainCommand.Data, sendData);
                        sendCount += sendBytes;
                        var value = (int)(sendCount * 1.0 / fileHeader.Fpga2Len * 100);
                        if (preValue != value)
                        {
                            Console.WriteLine($"Progress:{value}%");
                        }
                        preValue = value;
                    }
                    // 发送完成
                    SendData(RecieveId, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END);
                    if (!CheckState(DeviceList, (byte)DriveControllerMainCommand.Update, (byte)UpdateSubCommand.END))
                    {
                        Console.WriteLine("Notify:Does not respond");
                        return false;
                    }
                }
            }
            Console.WriteLine("FPGA2 data  download completed !");
            return true;
        }
        private static int SendData(uint id, byte main, byte sub, byte[] param = null, int channel = -1, int index = -1)
        {
            List<byte> data = new List<byte> { main, sub };
            if (channel != -1)
            {
                data.Add((byte)channel);
            }
            if (index != -1)
            {
                data.Add((byte)index);
            }
            if (param != null)
            {
                data.AddRange(param);
            }
            return Comm.SendData(id, data.ToArray());
        }
        private static int SendData(uint id, byte main, byte[] param)
        {
            List<byte> data = new List<byte> { main };
            if (param != null)
            {
                data.AddRange(param);
            }
            return Comm.SendData(id, data.ToArray());
        }
        private static bool CheckState(List<DeviceInfo> list, byte main, byte sub)
        {
            int tryCount = 0;
            List<ExecResult> resultList = new List<ExecResult>();
            while (tryCount <= 200)
            {
                var result = Comm.ReceiveData(main, sub);
                if (result.Count == 0)
                {
                    tryCount++;
                    Thread.Sleep(100);
                    continue;
                }
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Result[0] == main
                        && result[i].Result[1] == sub)
                    {
                        resultList.Add(result[i]);
                    }
                }
                if (resultList.Count == list.Count)
                {
                    break;
                }
                Thread.Sleep(100);
                tryCount++;
            }
            if (tryCount >= 100)
            {
                return false;
            }
            return true;
        }

        private static bool CheckState(List<DeviceInfo> list, byte main, byte sub, byte []datasets )
        {
            int tryCount = 0;
            List<ExecResult> resultList = new List<ExecResult>();
            while (tryCount <= 200)
            {
                var result = Comm.ReceiveData(main, sub);
                if (result.Count == 0)
                {
                    tryCount++;
                    Thread.Sleep(100);
                    continue;
                }

                //! foreach frame
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Result[0] == main
                        && result[i].Result[1] == sub)
                    {
                        if (result[i].Result.Length == datasets.Length + 2 )
                        { }
                        else
                        { continue;  }

                        //! check the return
                        for (int tmpK = 0; tmpK < datasets.Length - 2; tmpK++)
                        {
                            if (result[i].Result[tmpK + 2] != datasets[tmpK])
                            { continue; }
                            else
                            { }
                        }
                        return true;
                    }
                }
                if (resultList.Count == list.Count)
                {
                    break;
                }
                Thread.Sleep(100);
                tryCount++;
            }
            if (tryCount >= 100)
            {
                return false;
            }
            return true;
        }
    }  
}
