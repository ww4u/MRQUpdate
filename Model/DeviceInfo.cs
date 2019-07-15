using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;

namespace MegaRobo_Update.Model
{
    public class DeviceInfo : PropertyChangedBase
    {
        private string deviceType;
        private uint receiveId;
        public string DeviceType
        {
            get { return deviceType; }
            set
            {
                deviceType = value;
                NotifyOfPropertyChange(() => DeviceType);
            }
        }

        public uint RecieveId
        {
            get { return receiveId; }
            set
            {
                receiveId = value;
                NotifyOfPropertyChange(() => RecieveId);
            }
        }
    }
}
