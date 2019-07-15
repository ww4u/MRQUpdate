using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public enum Mode
    {
        用户模式,
        开发者模式
    }
    /// <summary>
    ///     对话框的可选项
    /// </summary>
    [Flags]
    public enum MessageBoxOperation
    {
        Ok = 1,
        Yes = 2,
        Cancel = 4,
        OkCancel = Ok | Cancel,
        OkYesCancel = Ok | Yes | Cancel
    }
    public enum DriveControllerMainCommand
    {
        Link = 1,
        System,
        Rs232,
        Can, 
        Data = 200,      
        Update = 201,
        Factory
    }
    public enum LinkSubCommand
    {
        INTFC,
        INTFCQuery
    }
    public enum SystemSubCommand
    {
        TYPEQuery=5,
        SOFTVERQuery,
        FPGAVERQuery,
        BOOTVERQuery=9
    }
    public enum Rs232SubCommand
    {
        BAUD,
        BAUDQuery,
        WORDLEN,
        WORDLENQuery,
        FLOWCTL,
        FLOWCTLQuery,
        PARITY,
        PARITYQuery,
        STOPBIT,
        STOPBITQuery,
        APPLYPARA
    }

    public enum CanSubCommand
    {
        TYPE,
        TYPEQuery,
        BAUD,
        BAUDQuery,
        GROUP,
        GROUPQuery,
        SENDID,
        SENDIDQuery,
        receiveID,
        receiveIDQuery,
        GROUPID1,
        GROUPID1Query,
        GROUPID2,
        GROUPID2Query,
        BROADCASTID,
        BROADCASTIDQuery,
        APPLYPARA,
        CANCMD_NMTLED,
        CANCMD_NMTSTATE,
        CANCMD_NMTSTATEQuery,
        CANCMD_NMTSETID,
        CANCMD_NMTIDQuery,
        CANCMD_NMTHASH,
        CANCMD_NMTHASHQuery,
        CANCMD_NMTSIGNATURE,
        CANCMD_NMTSIGNATUREQuery,
        CANCMD_NMTSIGNATURESIZEQuery
    }
    public enum UpdateSubCommand
    {
        START,
        ERASE,
        DATA,
        END,
        JUMP,
        APPADDR,
        APPADDRQuery,
        FPGAADDR,
        FPGAADDRQuery,
        
        MD5=21,
        END_ALL = 22,

    }

    public enum LinkType
    {
        NONE,
        CAN,
        RS232
    }

    public enum CanNmtState
    {
        IDLE,
        HASH,
        SIGNATURE
    }
    public enum CanIdType
    {
        SENDID=1,
        RECEIVEID
    }
    public enum UpdateType
    {
        BOOT,
        APP,
        FPGA,
    }
}
