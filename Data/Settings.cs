using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Utils;

namespace ResourceBaseBlock.Data
{
    [ProtoContract]
    public class Settings
    {
        public const string Filename = "ResourceBaseSettings.cfg";

        [ProtoMember]
        public int UpdateInterval { get; set; }

        [ProtoMember]
        public bool EnemyActivityBonus { get; set; }

        [ProtoMember]
        public float EnemyActivityMultiplier { get; set; }

        [ProtoMember]
        public bool PlayerActivityBonus { get; set; }

        [ProtoMember]
        public float PlayerActivityMultiplier { get; set; }

        [ProtoMember]
        public List<Resource> Resources = new List<Resource>();

        public static Settings Default()
        {
            return new Settings()
            {
                UpdateInterval = 60,
                EnemyActivityBonus = false,
                EnemyActivityMultiplier = 1f,
                PlayerActivityBonus = true,
                PlayerActivityMultiplier = 1f,
            };
        }

        public static Settings Load(bool loadGlobal = false)
        {
            Settings s = null;
            try
            {
                if (loadGlobal)
                {
                    if (MyAPIGateway.Utilities.FileExistsInLocalStorage(Filename, typeof(Settings)))
                    {
                        Tools.Log(MyLogSeverity.Info, "Loading saved settings");
                        TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(Filename, typeof(Settings));
                        string text = reader.ReadToEnd();
                        reader.Close();

                        s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    }
                    else
                    {
                        Tools.Log(MyLogSeverity.Info, "Config file not found. Loading default settings");
                        s = Default();
                        s.Resources = Resource.Default();
                        Save(s, true);
                    }
                }
                else
                {
                    if (MyAPIGateway.Utilities.FileExistsInWorldStorage(Filename, typeof(Settings)))
                    {
                        Tools.Log(MyLogSeverity.Info, "Loading saved settings");
                        TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(Filename, typeof(Settings));
                        string text = reader.ReadToEnd();
                        reader.Close();

                        s = MyAPIGateway.Utilities.SerializeFromXML<Settings>(text);
                    }
                    else
                    {
                        Tools.Log(MyLogSeverity.Info, "Config file not found. Loading default settings");
                        s = Default();
                        s.Resources = Resource.Default();
                        Save(s);
                    }
                }
            }
            catch(Exception e)
            {
                Tools.Log(MyLogSeverity.Warning, $"Failed to load saved configuration. Loading defaults\n {e.ToString()}");
                s = Default();
                s.Resources = Resource.Default();
                Save(s);
            }

            return s;
        }

        public static void Save(Settings settings, bool alsoSaveGlobal = false)
        {
            try
            {
                Tools.Log(MyLogSeverity.Info, "Saving Settings");
                TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(Filename, typeof(Settings));
                writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                writer.Close();

                if (alsoSaveGlobal)
                {
                    writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(Filename, typeof(Settings));
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(settings));
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Tools.Log(MyLogSeverity.Error, $"Failed to save settings\n{e.ToString()}");
            }
        }
    }
}
