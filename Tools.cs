using Sandbox.ModAPI;
using VRage.Utils;

namespace ResourceBaseBlock
{
    public class Tools
    {

        public static long ModMessageId = 999888777;

        public const ushort ModId = 53237;
        public const string ModName = "Resource Beacons";
        public const string Keyword = "/rbeacon";

        public static void Log(MyLogSeverity level, string message)
        {
            MyLog.Default.Log(level, $"[{ModName}] {message}");
        }
    }
}
