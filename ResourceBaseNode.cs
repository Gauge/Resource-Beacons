using ResourceBaseBlock.Data;
using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace ResourceBaseBlock
{
    public enum BaseState { Ready, Spawn, Cooldown }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CargoContainer), true, "ResourceBaseNode")]
    public class ResourceBaseNode : MyNetworkAPIGameLogicComponent
    {
        public readonly static Guid StorageGuid = new Guid("B7AF750E-68E3-4826-BD0E-A75BF36BA5E6");
        public IMyCargoContainer ModBlock { get; private set; }
        public string Name => ModBlock.CustomName.Split(new string[] { " | " }, StringSplitOptions.RemoveEmptyEntries)[0];

        public NetSync<ResourceBaseUpdate> Updater;
        public BaseState State { get; private set; }
        public Resource ActiveResource { get; private set; }
        public long TimeRemaining { get; private set; }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            ModBlock = Entity as IMyCargoContainer;

            if (MyAPIGateway.Session.IsServer)
            {
                Load();
            }

            Core.RegisterResourceBaseNode(this);

            Updater = new NetSync<ResourceBaseUpdate>(this, TransferType.Both, new ResourceBaseUpdate());
            Updater.ValueChangedByNetwork += OnValueChangedByNetwork;
            Updater.FetchRequest += OnFetchRequest;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void Close()
        {
            try
            {
                Network.SendCommand("gps", data: MyAPIGateway.Utilities.SerializeToBinary(new GPSSignal() {
                    Id = Entity.EntityId,
                    Remove = true
                }));

                Core.UnRegisterResourceBaseNode(this);
            }
            catch 
            { 
            
            }
        }

        private void OnFetchRequest(ulong sender)
        {
            Updater.Value.State = State;
            Updater.Value.TimeRemaining = TimeRemaining;
        }

        private void OnValueChangedByNetwork(ResourceBaseUpdate original, ResourceBaseUpdate replacement, ulong sender)
        {
            State = replacement.State;
            ActiveResource = replacement.ActiveResource;
            TimeRemaining = replacement.TimeRemaining;
        }

        public override bool IsSerialized()
        {
            Save();
            return base.IsSerialized();
        }

        int currentInterval = 0;
        public override void UpdateBeforeSimulation()
        {
            if (Core.Config == null) return;

            currentInterval++;

            if (currentInterval >= Core.Config.UpdateInterval)
            {
                UpdateDisplay();
                currentInterval = 0;
            }
        }

        public static void InitializeActions()
        {
            if (MyAPIGateway.Utilities.IsDedicated) return;

            Tools.Log(MyLogSeverity.Info, "Initializing Actions");

            List<IMyTerminalAction> existingActions;
            MyAPIGateway.TerminalControls.GetActions<IMyCargoContainer>(out existingActions);

            foreach (Resource r in Core.Config.Resources)
            {
                string actionName = $"Spawn {r.PrimaryName}";
                IMyTerminalAction act = existingActions.Find(a => a.Id == actionName);
                if (act != null)
                {
                    Tools.Log(MyLogSeverity.Info, $"Clearing Action \'{actionName}\'");
                    MyAPIGateway.TerminalControls.RemoveAction<IMyCargoContainer>(act);
                }

                IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyCargoContainer>(actionName);
                action.Name.Append(actionName);
                action.Icon = @"D:\Steam\steamapps\common\SpaceEngineers\Content\Textures\GUI\Icons\AstronautBackpack.dds";
                action.Writer = (block, str) => str.Append($"{r.PrimaryName}");
                action.Enabled = (block) => { return block.GameLogic.GetAs<ResourceBaseNode>() != null; };
                action.Action = (block) =>
                {
                    Tools.Log(MyLogSeverity.Info, $"Triggered Action");
                    ResourceBaseNode rb = block.GameLogic.GetAs<ResourceBaseNode>();
                    if (rb != null)
                    {
                        rb.Activate(r, MyAPIGateway.Session.LocalHumanPlayer);
                    }
                };

                MyAPIGateway.TerminalControls.AddAction<IMyCargoContainer>(action);
                Tools.Log(MyLogSeverity.Info, $"Created Action \'{actionName}\'");
            }
        }

        public void Activate(Resource resource, IMyPlayer player)
        {
            if (State != BaseState.Ready) return;

            TimeRemaining = Resource.ConvertSecondsToTicks(resource.SpawnTime);
            ActiveResource = new Resource()
            {
                TypeId = resource.TypeId,
                AlternateNames = resource.AlternateNames,
                Amount = 0,
                Cooldown = resource.Cooldown,
                SpawnTime = resource.SpawnTime,
                SubtypeName = resource.SubtypeName
            };

            float multiplier = 1;

            if (Core.Config.PlayerActivityBonus)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                float playerMultiplier = 1 + (Core.Config.PlayerActivityMultiplier * ((float)players.Count - 1));
                multiplier += (Core.Config.PlayerActivityMultiplier * ((float)players.Count - 1));

                ActiveResource.Amount += (int)(resource.Amount * playerMultiplier);
            }

            if (Core.Config.EnemyActivityBonus)
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players, (p) => player.GetRelationTo(p.IdentityId) == MyRelationsBetweenPlayerAndBlock.Enemies);

                float enemyMultiplier = 1 + (Core.Config.PlayerActivityMultiplier * players.Count);
                multiplier += (Core.Config.PlayerActivityMultiplier * players.Count);

                ActiveResource.Amount += (int)(resource.Amount * enemyMultiplier);
            }

            State = BaseState.Spawn;

            Updater.Value = new ResourceBaseUpdate()
            {
                TimeRemaining = TimeRemaining,
                ActiveResource = ActiveResource,
                State = State
            };

            string message = $"Spawning {ActiveResource.SubtypeName} {ActiveResource.Amount} {((multiplier > 1f) ? $"({multiplier.ToString("p0")})" : "")} {ActiveResource.TypeId} at \"{Name}\" in {GetTimeRemainingFormatted()}";

            Core.Network.SendCommand("message", message, MyAPIGateway.Utilities.SerializeToBinary(message));
            MyAPIGateway.Utilities.SendModMessage(Tools.ModMessageId, message);
            Tools.Log(MyLogSeverity.Info, message);
        }

        public void UpdateDisplay()
        {
            try
            {
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

                    if (!string.IsNullOrWhiteSpace(message.ToString()) && MyAPIGateway.Session.IsServer)
                    {
                        Core.Network.SendCommand("message", message.ToString());
                        MyAPIGateway.Utilities.SendModMessage(Tools.ModMessageId, message);
                        Tools.Log(MyLogSeverity.Info, message.ToString());
                    }
                }

                if (MyAPIGateway.Session.IsServer)
                {
                    string displayText = string.Format("{0} | {1} {2} {3}",
                            ModBlock.CustomName.Split(new string[] { " | " }, StringSplitOptions.RemoveEmptyEntries)[0],
                            State,
                            (State == BaseState.Spawn ? $"{ActiveResource.PrimaryName} in" : ""),
                            (State != BaseState.Ready ? GetTimeRemainingFormatted() : ""));

                    if (ModBlock.CustomName != displayText)
                    {
                        ModBlock.CustomName = displayText;

                        GPSSignal sig = new GPSSignal() {
                            Id = Entity.EntityId,
                            Text = displayText,
                            Location = new SerializableVector3D(Entity.PositionComp.WorldAABB.Center)
                        };

                        Network.SendCommand("gps", data: MyAPIGateway.Utilities.SerializeToBinary(sig));

                        if (!MyAPIGateway.Utilities.IsDedicated)
                        {
                            Core.UpdateGPSSignal(sig);
                        }
                    }
                }

                Save();
            }
            catch (Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
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

            ResourceBaseUpdate data = new ResourceBaseUpdate
            {
                ActiveResource = ActiveResource,
                State = State,
                TimeRemaining = TimeRemaining
            };
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
            MyModStorageComponentBase storage = Tools.GetStorage(Entity);

            if (storage.ContainsKey(StorageGuid))
            {
                ResourceBaseUpdate data = MyAPIGateway.Utilities.SerializeFromXML<ResourceBaseUpdate>(storage[StorageGuid]);

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
