using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace ResourceBaseBlock
{
    public class Tools
    {

        public static long ModMessageId = 999888777;

        public const ushort ModId = 53237;
        public const string ModName = "Resource Bases";
        public const string Keyword = "/rbase";

        public static void Log(MyLogSeverity level, string message)
        {
            MyLog.Default.Log(level, $"[{ModName}] {message}");
        }

        public static MyModStorageComponentBase GetStorage(IMyEntity entity)
        {
            return entity.Storage ?? (entity.Storage = new MyModStorageComponent());
        }
    }
}
