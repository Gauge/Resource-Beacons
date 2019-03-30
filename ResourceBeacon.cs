using ResourceBaseBlock.Data;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace ResourceBaseBlock
{
    public enum BaseState { Ready, Spawn, Cooldown }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "ResourceBeacon")]
    public class ResourceBeacon : MyGameLogicComponent
    {
        public readonly static Guid StorageGuid = new Guid("B7AF750E-68E3-4826-BD0E-A75BF36BA5E6");

        private static bool ControlsInitialized = false;

        private bool FirstTimeLoad = false;

        public IMyBeacon ModBlock { get; private set; }

        public int Designation { get; private set; }

        public string Name => ModBlock.CustomName.Split(new string[] { " | " }, StringSplitOptions.RemoveEmptyEntries)[0];

        public BaseState State { get; private set; }

        public Resource ActiveResource { get; private set; }

        public long TimeRemaining { get; private set; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;

            ModBlock = Entity as IMyBeacon;
            ModBlock.Radius = 99999999f;
            Core.OnUpdateInterval += OnDisplayUpdateInterval;
            Core.RegisterResourceBeacon(this);

            if (!ControlsInitialized)
            {
                InitializeActions();
                ControlsInitialized = true;
            }
        }

        public override void Close()
        {
            try
            {
                Core.OnUpdateInterval -= OnDisplayUpdateInterval;
                Core.UnRegisterResourceBeacon(this);
                base.Close();
            }
            catch (Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
        }

        public static void InitializeActions()
        {
            List<IMyTerminalAction> existingActions;
            MyAPIGateway.TerminalControls.GetActions<IMyBeacon>(out existingActions);

            foreach (Resource r in Core.Config.Resources)
            {
                string actionName = $"Spawn {r.PrimaryName}";
                IMyTerminalAction act = existingActions.Find(a => a.Id == actionName);
                if (act != null)
                {
                    MyAPIGateway.TerminalControls.RemoveAction<IMyBeacon>(act);
                }

                IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyBeacon>(actionName);
                action.Name.Append(actionName);
                action.Writer = (b, str) => str.Append($"{r.PrimaryName}");
                action.Enabled = (block) => { return block.GameLogic.GetAs<ResourceBeacon>() != null; };
                action.Action = (b) => { };

                MyAPIGateway.TerminalControls.AddAction<IMyBeacon>(action);
            }
        }

        public void Activate(string action, ulong steamId)
        {
            if (State == BaseState.Ready && ModBlock.IsWorking)
            {
                foreach (Resource r in Core.Config.Resources)
                {
                    string actionName = $"Spawn {r.PrimaryName}";
                    if (actionName == action)
                    {
                        Activate(r, steamId);
                        break;
                    }
                }
            }
        }

        public void Activate(Resource resource, ulong steamId)
        {
            MyLog.Default.Info($"{resource.Amount}");

            TimeRemaining = Resource.ConvertSecondsToTicks(resource.SpawnTime);
            ActiveResource = new Resource()
            {
                TypeId = resource.TypeId,
                AlternateNames = resource.AlternateNames,
                Amount = resource.Amount,
                Cooldown = resource.Cooldown,
                SpawnTime = resource.SpawnTime,
                SubtypeName = resource.SubtypeName
            };

            IMyPlayer player = null;
            List<IMyPlayer> userGetter = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(userGetter, (p) => p.SteamUserId == steamId);

            if (userGetter.Count != 0)
            {
                player = userGetter[0];
            }

            if (Core.Config.EnemyActivityBonus && player != null)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                players.Add(player);
                MyAPIGateway.Players.GetPlayers(players, (p) => player.GetRelationTo(p.IdentityId) == MyRelationsBetweenPlayerAndBlock.Enemies);

                ActiveResource.Amount = (int)((float)ActiveResource.Amount * (Core.Config.EnemyActivityMultiplier * (float)players.Count));
            }

            State = BaseState.Spawn;

            Core.Network.SendCommand("messages", $"Spawning {ActiveResource.SubtypeName} {ActiveResource.Amount} {ActiveResource.TypeId} at \"{Name}\" in {GetTimeRemainingFormatted()}");
        }

        public void OnDisplayUpdateInterval()
        {
            if (Core.Config == null) return;

            if (!FirstTimeLoad)
            {
                Load();
                FirstTimeLoad = true;
            }

            TimeRemaining -= Core.Config.UpdateInterval;

            if (TimeRemaining <= 0)
            {
                StringBuilder message = new StringBuilder();
                TimeRemaining = 0;

                if (State == BaseState.Spawn)
                {
                    State = BaseState.Cooldown;
                    TimeRemaining = Resource.ConvertSecondsToTicks(ActiveResource.Cooldown);

                    message.Append(SpawnResources(ActiveResource));
                    message.Append($"\n\"{Name}\" is on cooldown for {GetTimeRemainingFormatted()}");
                }
                else if (State == BaseState.Cooldown)
                {
                    State = BaseState.Ready;
                    message.Append($"\"{Name}\" is Ready");
                }

                Core.Network.SendCommand("messages", message.ToString());
            }

            ModBlock.CustomName = string.Format("{0} | {1} {2} {3}",
                ModBlock.CustomName.Split(new string[] { " | " }, StringSplitOptions.RemoveEmptyEntries)[0],
                State,
                (State == BaseState.Spawn ? $"{ActiveResource.PrimaryName} in" : ""),
                (State != BaseState.Ready ? GetTimeRemainingFormatted() : ""));

            Save();
        }

        private string SpawnResources(Resource resource)
        {
            MyObjectBuilder_PhysicalObject ObjectBuilder = resource.GetFloatingObject();
            MyInventory inventory = (MyInventory)ModBlock.GetInventory();
            MyFixedPoint size = inventory.ComputeAmountThatFits(ObjectBuilder.GetId());

            string msg = string.Empty;
            if (size > resource.Amount)
            {
                inventory.AddItems(resource.Amount, ObjectBuilder);
                msg = $"Spawned {resource.Amount} {resource.SubtypeName} {resource.TypeId} at \"{ModBlock.CubeGrid.DisplayName}\"";
            }
            else if (resource.Amount - size > 1)
            {
                inventory.AddItems(size, ObjectBuilder);
                msg = $"Spawned {size} of {resource.Amount} {resource.SubtypeName} {resource.TypeId} at \"{ModBlock.CubeGrid.DisplayName}\". Cargo is full";
            }
            else
            {
                msg = $"Failed to spawn {resource.Amount} {resource.SubtypeName} {resource.TypeId} at \"{ModBlock.CubeGrid.DisplayName}\". Cargo is full";
            }

            return msg;
        }

        public ResourceBeaconStorage GetBaseInfo()
        {
            return new ResourceBeaconStorage
            {
                ActiveResource = ActiveResource,
                State = State,
                TimeRemaining = TimeRemaining
            };
        }

        public string GetTimeRemainingFormatted()
        {
            return TimeSpan.FromMilliseconds((TimeRemaining / 60) * 1000).ToString("g").Split('.')[0];
        }

        public void Save()
        {
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            ResourceBeaconStorage data = GetBaseInfo();
            string xml = MyAPIGateway.Utilities.SerializeToXML(data);

            if (Entity.Storage.ContainsKey(StorageGuid))
            {
                Entity.Storage[StorageGuid] = xml;
            }
            else
            {
                Entity.Storage.Add(new KeyValuePair<Guid, string>(StorageGuid, xml));
            }
        }

        public void Load()
        {
            if (Entity.Storage == null)
            {
                Entity.Storage = new MyModStorageComponent();
            }

            if (Entity.Storage.ContainsKey(StorageGuid))
            {
                ResourceBeaconStorage data = MyAPIGateway.Utilities.SerializeFromXML<ResourceBeaconStorage>(Entity.Storage[StorageGuid]);

                ActiveResource = data.ActiveResource;
                TimeRemaining = data.TimeRemaining;
                State = data.State;
            }
            else
            {
                MyLog.Default.Info($"No data saved for:{Entity.EntityId}. Loading Defaults");
            }
        }
    }
}
