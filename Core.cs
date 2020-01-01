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
using VRageMath;

namespace ResourceBaseBlock
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Core : MySessionComponentBase
    {
        public static Settings Config { get; private set; }

        public static NetworkAPI Network => NetworkAPI.Instance;

        public static event Action OnUpdateInterval = delegate { };

        private static Dictionary<long, ResourceBeacon> RegisteredBeacons = new Dictionary<long, ResourceBeacon>();

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
                Network.RegisterChatCommand("load", (text) => { Network.SendCommand("load"); });


                Network.RegisterNetworkCommand("status", Status_ServerCallback);

                Network.RegisterNetworkCommand("load", Load_ServerCallback);

                Network.RegisterNetworkCommand("messages", (id, cmd, data) => { });
            }
            else
            {
                Network.RegisterNetworkCommand("status", Status_ClientCallback);
                Network.RegisterNetworkCommand("load", Load_ClientCallback);
                Network.RegisterNetworkCommand("load_request", LoadRequest_ClientCallback);
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

            if (MyAPIGateway.Session.IsServer)
            {
                Config = Settings.Load();
                if (!MyAPIGateway.Utilities.IsDedicated) {
                    ResourceBeacon.InitializeActions();
                }
            }
        }

        int waitInterval = 0;
        public override void UpdateBeforeSimulation()
        {
            if (Network.NetworkType == NetworkTypes.Client)
            {
                // make sure the client has what it needs to setup resource bases panels
                if (Config == null || Config.Resources.Count == 0)
                {
                    if (waitInterval == 60)
                    {
                        Network.SendCommand("load_request");
                    }

                    waitInterval++;
                }

                return;
            }

            if (interval >= Config.UpdateInterval)
            {
                OnUpdateInterval.Invoke();
                interval = 0;
            }

            interval++;
        }

        public static void Activate(long blockId, string actionId)
        {
            RegisteredBeacons[blockId].Activate(actionId);
        }

        public static void RegisterResourceBeacon(ResourceBeacon rbase)
        {
            RegisteredBeacons.Add(rbase.ModBlock.EntityId, rbase);
        }

        public static void UnRegisterResourceBeacon(ResourceBeacon rbase)
        {
            if (RegisteredBeacons.ContainsKey(rbase.ModBlock.EntityId))
            {
                RegisteredBeacons.Remove(rbase.ModBlock.EntityId);
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
                response.AppendLine($"{b.Name} |---| {b.ModBlock.CubeGrid.CustomName}");

                Vector3D position = b.ModBlock.CubeGrid.GridIntegerToWorld(b.ModBlock.Position);
                response.AppendLine($"Location: GPS:{b.Name}:{position.X.ToString("n3")}:{position.Y.ToString("n3")}:{position.Z.ToString("n3")}:");
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

        public void Status_ServerCallback(ulong steamId, string command, byte[] data)
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Tools.ModName, "", "", MyAPIGateway.Utilities.SerializeFromBinary<string>(data));
        }

        public void Status_ClientCallback(ulong steamId, string command, byte[] data)
        {

            Network.SendCommand("status", data: MyAPIGateway.Utilities.SerializeToBinary(BasicStatus()), steamId: steamId);
        }

        public void Load_ClientCallback(ulong steamId, string command, byte[] data)
        {
            Config = Settings.Load();
            ResourceBeacon.InitializeActions();

            Network.SendCommand("load", message: "Config loaded", data: MyAPIGateway.Utilities.SerializeToBinary(Config));
        }

        public void LoadRequest_ClientCallback(ulong steamId, string command, byte[] data)
        {
            Network.SendCommand("load", data: MyAPIGateway.Utilities.SerializeToBinary(Config), steamId: steamId);
        }

        public void Load_ServerCallback(ulong steamId, string command, byte[] data)
        {
            Config = MyAPIGateway.Utilities.SerializeFromBinary<Settings>(data);
            ResourceBeacon.InitializeActions();
        }

        public void Activate_ClientCall(ulong steamId, string command, byte[] data)
        {
            ActivateData activateData = MyAPIGateway.Utilities.SerializeFromBinary<ActivateData>(data);

            Activate(activateData.BlockId, activateData.ActionId);
        }
    }
}
