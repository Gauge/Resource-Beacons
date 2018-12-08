using ProtoBuf;
using System.Collections.Generic;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class SpawnHistory
    {
        [ProtoMember]
        public ulong SteamId;

        [ProtoMember]
        public string Name;

        [ProtoMember]
        public long FactionId;

        [ProtoMember]
        public string FactionTag;

        [ProtoMember]
        public string FactionName;

        [ProtoMember]
        public int SpawnCount;

        [ProtoMember]
        public List<SpawnHistoryResourceQuantity> Resources = new List<SpawnHistoryResourceQuantity>();

    }
}
