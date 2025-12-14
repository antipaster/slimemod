using ImGuiNET;
using bepinex_test.Modules.Slimes;

namespace bepinex_test.Modules.Slimes
{
    public class TeleportModule
    {
        public void Draw()
        {
            if (ImGui.Button("Teleport all Slimes"))
            {
                var count = SlimeUtil.TeleportAll();
                ImGui.Text($"Teleported {count} slimes");
            }
        }
    }
}
