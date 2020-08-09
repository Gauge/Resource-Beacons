using ResourceBaseBlock.Data;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using SENetworkAPI;
using VRage.Game.ModAPI;
using Sandbox.Game;

namespace ResourceBaseBlock
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Core : MyNetworkSessionComponent
    {
        public static Settings Config { get; private set; }

        private static Dictionary<long, ResourceBaseNode> RegisteredBaseNodes = new Dictionary<long, ResourceBaseNode>();

        private static Dictionary<long, IMyGps> gpsPoints = new Dictionary<long, IMyGps>();

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            NetworkAPI.LogNetworkTraffic = false;

            if (Network == null)
            {
                NetworkAPI.Init(Tools.ModId, Tools.ModName, Tools.Keyword);
            }

            Network.RegisterChatCommand(string.Empty, Chat_Help);
            Network.RegisterChatCommand("help", Chat_Help);

            if (MyAPIGateway.Session.IsServer)
            {
                Config = Settings.Load();
                ResourceBaseNode.InitializeActions();

                Network.RegisterChatCommand("load", (text) => {
                    Config = Settings.Load();
                    ResourceBaseNode.InitializeActions();
                    MyAPIGateway.Utilities.ShowMessage(Tools.Keyword, "config loaded");
                });

                Network.RegisterNetworkCommand("status", Status_ServerCallback);
                Network.RegisterNetworkCommand("load", Load_ServerCallback);
                Network.RegisterNetworkCommand("load_request", LoadRequest_ServerCallback);
                Network.RegisterChatCommand("status", (text) =>
                {
                    MyAPIGateway.Utilities.ShowMissionScreen("Resource Bases", "", "", BasicStatus());
                });
            }
            else
            {
                Network.RegisterChatCommand("status", (text) => { Network.SendCommand("status"); });
                Network.RegisterChatCommand("load", (text) => { Network.SendCommand("load"); });

                Network.RegisterNetworkCommand("status", Status_ClientCallback);
                Network.RegisterNetworkCommand("load", Load_ClientCallback);
                Network.RegisterNetworkCommand("gps", GPS_ClientCallback);
            }

            if (MyAPIGateway.Session.IsServer)
            {
                SetUpdateOrder(MyUpdateOrder.NoUpdate);
            }

        }

        int waitInterval = 0;
        public override void UpdateAfterSimulation()
        {
            if (Config != null && Config.Resources.Count > 0)
            {
                return;
            }

            // make sure the client has what it needs to setup resource bases panels
            if (waitInterval == 120)
            {
                Network.SendCommand("load_request");
                waitInterval = 0;
            }

            waitInterval++;
        }

        public static void RegisterResourceBaseNode(ResourceBaseNode rbase)
        {
            RegisteredBaseNodes.Add(rbase.Entity.EntityId, rbase);
        }

        public static void UnRegisterResourceBaseNode(ResourceBaseNode rbase)
        {
            if (RegisteredBaseNodes.ContainsKey(rbase.Entity.EntityId))
            {
                RegisteredBaseNodes.Remove(rbase.Entity.EntityId);
            }
        }

        protected override void UnloadData()
        {
            Network.Close();
        }

        public string BasicStatus()
        {
            StringBuilder response = new StringBuilder();
            foreach (ResourceBaseNode b in RegisteredBaseNodes.Values)
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


        public static void UpdateGPSSignal(GPSSignal signal)
        {
            if (!gpsPoints.ContainsKey(signal.Id))
            {
                IMyGps gps = MyAPIGateway.Session.GPS.Create(signal.Id.ToString(), "", signal.Location, true);
                MyVisualScriptLogicProvider.SetGPSColor(signal.Id.ToString(), Color.White);
                gpsPoints.Add(signal.Id, gps);
                MyAPIGateway.Session.GPS.AddLocalGps(gps);
            }

            if (signal.Remove)
            {
                MyAPIGateway.Session.GPS.RemoveLocalGps(gpsPoints[signal.Id]);
                gpsPoints.Remove(signal.Id);
            }
            else
            {
                gpsPoints[signal.Id].Name = signal.Text;
            }
        }

        public void Chat_Help(string text)
        {
            StringBuilder response = new StringBuilder();
            response.Append($"Command: \"{Tools.Keyword}\"\nExtentions:\n");
            response.Append("status: Displays resource base details\n");
            response.Append("load: requests resource data from server again\n");

            MyAPIGateway.Utilities.ShowMessage(Network.ModName, response.ToString());
        }

        public void Message_ServerCallback(ulong steamId, string command, byte[] data, DateTime timestamp) 
        {
            string message = MyAPIGateway.Utilities.SerializeFromBinary<string>(data);
            MyAPIGateway.Utilities.SendModMessage(Tools.ModMessageId, message);

            Network.Say(message);
        }

        public void Status_ServerCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            Network.SendCommand("status", data: MyAPIGateway.Utilities.SerializeToBinary(BasicStatus()), steamId: steamId);
        }

        public void Status_ClientCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Tools.ModName, "", "", MyAPIGateway.Utilities.SerializeFromBinary<string>(data));
        }

        public void LoadRequest_ServerCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            Network.SendCommand("load", data: MyAPIGateway.Utilities.SerializeToBinary(Config), steamId: steamId);
        }

        public void Load_ServerCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            Config = Settings.Load();
            ResourceBaseNode.InitializeActions();

            Network.SendCommand("load", message: "Config loaded", data: MyAPIGateway.Utilities.SerializeToBinary(Config));
        }

        public void Load_ClientCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            Config = MyAPIGateway.Utilities.SerializeFromBinary<Settings>(data);
            ResourceBaseNode.InitializeActions();
        }

        public void GPS_ClientCallback(ulong steamId, string command, byte[] data, DateTime timestamp)
        {
            GPSSignal signal = MyAPIGateway.Utilities.SerializeFromBinary<GPSSignal>(data);
            UpdateGPSSignal(signal);
        }
    }
}
