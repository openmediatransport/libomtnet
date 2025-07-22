using System;
using System.Collections.Generic;
using System.Text;

namespace libomtnet
{    internal enum OMTEventType
    {
        None = 0,
        TallyChanged = 1
    }

    internal class OMTEventArgs : EventArgs
    {
        private OMTEventType eventType;
        public OMTEventArgs(OMTEventType eventType)
        {
            this.eventType = eventType;
        }
        public OMTEventType Type { get { return eventType; } set { eventType = value; } }
    }
}
