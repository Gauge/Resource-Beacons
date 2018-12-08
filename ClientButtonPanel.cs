using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.Game.ModAPI;
using System;
using VRage.Utils;

namespace ResourceBaseBlock
{
    public class ClientButtonPanel
    {
        IMyButtonPanel panel;

        private event Action<long, long, int, string> buttonPressed;

        public event Action<long, long, int, string> ButtonPressed
        {
            add
            {
                panel.ButtonPressed += pressed;
                buttonPressed += value;
            }
            remove
            {
                panel.ButtonPressed -= pressed;
                buttonPressed -= value;
            }
        }

        public long GridId => panel.CubeGrid.EntityId;
        public string GridName => panel.CubeGrid.DisplayName;
        public string Name => panel.DisplayNameText;

        public ClientButtonPanel(IMyButtonPanel p)
        {
            panel = p;
        }

        private void pressed(int index)
        {
            MyObjectBuilder_ButtonPanel builder = panel.GetObjectBuilderCubeBlock(false) as MyObjectBuilder_ButtonPanel;

            if (builder == null || builder.Toolbar == null && builder.Toolbar.Slots.Count > index)
            {
                return;
            }

            MyObjectBuilder_ToolbarItemTerminalBlock item = (builder.Toolbar.Slots[index].Data as MyObjectBuilder_ToolbarItemTerminalBlock);
            if (item == null) return;

            buttonPressed.Invoke(panel.CubeGrid.EntityId, item.BlockEntityId, index, item._Action);
        }


    }
}
