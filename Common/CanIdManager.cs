using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MegaRobo_Update.Common
{
    public class CanIdManager
    {
        public static uint GetAvailableId(List<uint> idList,int type)
        {
            uint id = 0;
            if (type == (int)CanIdType.SENDID)
            {
                id = 0x00000200;//send id 开始地址
            }
            else if (type == (int)CanIdType.RECEIVEID)
            {
                id = 0x00000180; //target id 开始地址
            }
            for (uint i = 0; i < 256; i++, id++)
            {
                if (!idList.Contains(id))
                {
                    break;
                }
            }
            return id;
        }
    }
}
