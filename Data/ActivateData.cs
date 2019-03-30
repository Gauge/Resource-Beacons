using ProtoBuf;

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
