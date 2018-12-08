using ProtoBuf;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class ResourceBeaconStorage
    {
        [ProtoMember]
        public BaseState State;

        [ProtoMember]
        public Resource ActiveResource;

        [ProtoMember]
        public long TimeRemaining;

        //[ProtoMember]
        //public string GridName;

        //[ProtoMember]
        //public string Name;

        //[ProtoMember]
        //public List<SpawnHistory> SpawnHistories = new List<SpawnHistory>();
    }
}
