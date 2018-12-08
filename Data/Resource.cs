using ProtoBuf;
using System.Collections.Generic;
using VRage.Game;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class Resource
    {
        [ProtoMember]
        public string TypeId { get; set; }

        [ProtoMember]
        public string SubtypeName { get; set; }

        [ProtoMember]
        public List<string> AlternateNames { get; set; } = new List<string>();

        [ProtoMember]
        public int Amount { get; set; }

        [ProtoMember]
        public int SpawnTime { get; set; }

        [ProtoMember]
        public int Cooldown { get; set; }

        public string PrimaryName => ((AlternateNames.Count > 0) ? AlternateNames[0] : $"{SubtypeName} {TypeId}");

        public static long ConvertSecondsToTicks(int seconds)
        {
            return seconds * 60;
        }

        public MyObjectBuilder_PhysicalObject GetFloatingObject()
        {
            MyObjectBuilder_PhysicalObject ObjectBuilder = new MyObjectBuilder_PhysicalObject();

            switch (
TypeId)
            {
                case "Ore":
                    ObjectBuilder = new MyObjectBuilder_Ore() { SubtypeName = SubtypeName };
                    break;

                case "Ingot":
                    ObjectBuilder = new MyObjectBuilder_Ingot() { SubtypeName = SubtypeName };
                    break;

                case "Component":
                    ObjectBuilder = new MyObjectBuilder_Component() { SubtypeName = SubtypeName };
                    break;

                case "AmmoMagazine":
                    ObjectBuilder = new MyObjectBuilder_AmmoMagazine() { SubtypeName = SubtypeName };
                    break;
            }

            return ObjectBuilder;
        }

        public MyDefinitionId GetDefinitionId()
        {
            return new MyDefinitionId(GetFloatingObject().TypeId, SubtypeName);
        }

        // Look up PhysicalItems.sbc for more options
        public static List<Resource> Default()
        {
            List<Resource> set = new List<Resource>();

            set.Add(new Resource()
            {
                TypeId = "Ore",
                SubtypeName = "Organic",
                AlternateNames = new List<string>() {"Point", "Organic", "Points"},
                Amount = 1,
                SpawnTime = 1200,
                Cooldown = 21600
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Stone",
                AlternateNames = new List<string>() { "Gravel" },
                Amount = 5000,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Iron",
                AlternateNames = new List<string>() { "Fe","Iron"  },
                Amount = 15000,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Nickel",
                AlternateNames = new List<string>() { "Ni", "Nickel" },
                Amount = 5000,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Cobalt",
                AlternateNames = new List<string>() {"Co", "Cobalt"},
                Amount = 5000,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Silicon",
                AlternateNames = new List<string>() {"Si", "Silicon"},
                Amount = 1000,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Silver",
                AlternateNames = new List<string>() {"Ag", "Silver"},
                Amount = 500,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Gold",
                AlternateNames = new List<string>() {"Au", "Gold"},
                Amount = 500,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Uranium",
                AlternateNames = new List<string>() {"U", "Uranium"},
                Amount = 200,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Magnesium",
                AlternateNames = new List<string>() {"Mg", "Magnesium" },
                Amount = 200,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            set.Add(new Resource()
            {
                TypeId = "Ingot",
                SubtypeName = "Platinum",
                AlternateNames = new List<string>() { "Platinum", "Pt" },
                Amount = 100,
                SpawnTime = 1200,
                Cooldown = 5400
            });

            return set;
        }
    }
}
