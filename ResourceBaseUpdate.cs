using ProtoBuf;
using VRage;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class ResourceBaseUpdate
    {
        [ProtoMember(1)]
        public BaseState State;

        [ProtoMember(2)]
        public Resource ActiveResource;

        [ProtoMember(3)]
        public long TimeRemaining;
    }

    [ProtoContract]
    public class GPSSignal 
    {
        [ProtoMember(1)]
        public long Id;

        [ProtoMember(2)]
        public string Text;

        [ProtoMember(3)]
        public SerializableVector3D Location;

        [ProtoMember(4)]
        public bool Remove;
    }
}
