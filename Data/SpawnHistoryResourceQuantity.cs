using ProtoBuf;
using VRage.Game;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class SpawnHistoryResourceQuantity
    {
        [ProtoMember]
        public string TypeId;

        [ProtoMember]
        public string SubtypeName;

        [ProtoMember]
        public long Amount;
    }
}
