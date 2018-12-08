using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class ActivateData
    {
        [ProtoMember]
        public long BlockId;

        [ProtoMember]
        public string ActionId;

    }
}
