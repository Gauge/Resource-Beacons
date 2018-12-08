using ResourceBaseBlock.Data;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using ModNetworkAPI;
using VRage.Game.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace ResourceBaseBlock
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static Settings Config { get; private set; }

        public static NetworkAPI Network => NetworkAPI.Instance;

        public static event Action OnUpdateInterval = delegate { };

        private static Dictionary<long, ResourceBeacon> RegisteredBeacons = new Dictionary<long, ResourceBeacon>();

        private static Dictionary<long, List<ClientButtonPanel>> RegisteredGridPanels = new Dictionary<long, List<ClientButtonPanel>>();

        private int interval = 0;

        private enum Operations {none, status, help, load }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            if (Network == null)
            {
                NetworkAPI.Init(Tools.ModId, Tools.ModName, Tools.Keyword);
            }

            Network.RegisterChatCommand(string.Empty, Chat_Help);
            Network.RegisterChatCommand("help", Chat_Help);

            if (Network.NetworkType == NetworkTypes.Client)
            {
                Network.RegisterChatCommand("status", (text) => { Network.SendCommand("status"); });
                Network.RegisterNetworkCommand("status", Status_ServerCall);

                Network.RegisterChatCommand("load", (text) => { Network.SendCommand("load"); });
                Network.RegisterNetworkCommand("load", (id, cmd, data) => { });
                Network.RegisterNetworkCommand("messages", (id, cmd, data) => { });
            }
            else
            {
                Network.RegisterNetworkCommand("status", Status_ClientCall);
                Network.RegisterNetworkCommand("load", Load_ClientCall);
                Network.RegisterNetworkCommand("activate", Activate_ClientCall);

                Network.RegisterChatCommand("status", (text) =>
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Resource Bases", "", "", BasicStatus());
                });

                Network.RegisterChatCommand("load", (text) =>
                {
                    Config = Settings.Load();
                    ResourceBeacon.InitializeActions();

                    MyAPIGateway.Utilities.ShowMessage(Network.ModName, "Config loaded");
                });
            }

            if (Network.NetworkType != NetworkTypes.Client)
            {
                Config = Settings.Load();
            } 
        }

        public override void UpdateBeforeSimulation()
        {
            if (Network.NetworkType == NetworkTypes.Client) return;

            if (interval == Config.UpdateInterval)
            {
                OnUpdateInterval.Invoke();
                interval = 0;
            }

            interval++;
        }

        public static void RegisterResourceBeacon(ResourceBeacon rbase)
        {
            RegisteredBeacons.Add(rbase.ModBlock.EntityId, rbase);

            if (Network.NetworkType != NetworkTypes.Dedicated)
            {
                RegisterGrid(rbase.ModBlock.CubeGrid);
            }
        }

        public static void UnRegisterResourceBeacon(ResourceBeacon rbase)
        {
            if (RegisteredBeacons.ContainsKey(rbase.ModBlock.EntityId))
            {
                RegisteredBeacons.Remove(rbase.ModBlock.EntityId);
            }

            if (Network.NetworkType != NetworkTypes.Dedicated)
            {
                UnRegisterGrid(rbase.ModBlock.CubeGrid);
            }
        }

        private static void RegisterGrid(IMyCubeGrid grid)
        {
            if (!RegisteredGridPanels.ContainsKey(grid.EntityId))
            {
                List<ClientButtonPanel> panels = new List<ClientButtonPanel>();

                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, b => b.FatBlock is IMyButtonPanel);

                foreach (IMySlimBlock block in blocks)
                {
                    ClientButtonPanel panel = new ClientButtonPanel(block.FatBlock as IMyButtonPanel);
                    panel.ButtonPressed += ButtonPressed;

                    panels.Add(panel);
                }

                grid.OnBlockAdded += (block) => {
                    if (block.FatBlock is IMyButtonPanel)
                    {
                        ClientButtonPanel panel = new ClientButtonPanel(block.FatBlock as IMyButtonPanel);
                        panel.ButtonPressed += ButtonPressed;

                        RegisteredGridPanels[block.CubeGrid.EntityId].Add(panel);
                    }
                };

                RegisteredGridPanels.Add(grid.EntityId, panels);
            }
        }

        private static void UnRegisterGrid(IMyCubeGrid grid)
        {
            if (RegisteredGridPanels.ContainsKey(grid.EntityId))
            {
                foreach (ClientButtonPanel panel in RegisteredGridPanels[grid.EntityId])
                {
                    panel.ButtonPressed -= ButtonPressed;
                }

                RegisteredGridPanels.Remove(grid.EntityId);
            }
        }

        private static void ButtonPressed(long gridId, long blockId, int index, string actionId)
        {
            if (RegisteredBeacons.ContainsKey(blockId))
            {
                ulong steamId = MyAPIGateway.Session.Player.SteamUserId;
                if (Network.NetworkType == NetworkTypes.Server)
                {
                    RegisteredBeacons[blockId].Activate(actionId, steamId);
                }
                else if (Network.NetworkType == NetworkTypes.Client)
                {
                    Network.SendCommand("activate", data: MyAPIGateway.Utilities.SerializeToBinary(new ActivateData() { BlockId = blockId, ActionId = actionId }));
                }
            }
        } 

        protected override void UnloadData()
        {
            Network.Close();
        }

        public string BasicStatus()
        {
            StringBuilder response = new StringBuilder();
            foreach (ResourceBeacon b in RegisteredBeacons.Values)
            {
                response.AppendLine($"{b.Name} |---| {b.ModBlock.CubeGrid.CustomName}                                                                                    fasfsfkjsdklfsdjfsdfslk;dfjsldkfjsdlkfjkl");
                response.AppendLine($"Location: GPS:{b.Name}:{b.ModBlock.Position.X}:{b.ModBlock.Position.Y}:{b.ModBlock.Position.Z}:");
                response.AppendLine($"State: {b.State}{(b.State == BaseState.Spawn ? $" {b.ActiveResource.Amount} {b.ActiveResource.AlternateNames[0]}" : "")}");
                if (b.State != BaseState.Ready)
                {
                    response.AppendLine($"Time: {b.GetTimeRemainingFormatted()}");
                }
                response.AppendLine();
            }
            return response.ToString();
        }

        public void Chat_Help(string text)
        {
            StringBuilder response = new StringBuilder();
            response.Append($"Command: \"{Tools.Keyword}\"\nExtentions:\n");
            response.Append("help: Displays this message\n");
            response.Append("status: Displays resource base details\n");
            response.Append("load: Loads new resources from config\n");
            response.Append("save: Saves current resources to world file\n");
            response.Append("global: Loads global resources\n");

            MyAPIGateway.Utilities.ShowMessage(Network.ModName, response.ToString());
        }

        public void Status_ServerCall(ulong steamId, string command, byte[] data)
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Tools.ModName, "", "", MyAPIGateway.Utilities.SerializeFromBinary<string>(data));
        }

        public void Status_ClientCall(ulong steamId, string command, byte[] data)
        {

            Network.SendCommand("status", data: MyAPIGateway.Utilities.SerializeToBinary(BasicStatus()), steamId: steamId);
        }

        public void Load_ClientCall(ulong steamId, string command, byte[] data)
        {
            Config = Settings.Load();
            ResourceBeacon.InitializeActions();

            Network.SendCommand("load", message: "Config loaded", steamId: steamId);
        }

        public void Activate_ClientCall(ulong steamId, string command, byte[] data)
        {
            ActivateData activateData = MyAPIGateway.Utilities.SerializeFromBinary<ActivateData>(data);

            RegisteredBeacons[activateData.BlockId].Activate(activateData.ActionId, steamId);
        }
    }
}
