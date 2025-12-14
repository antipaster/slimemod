using System;
using ImGuiNET;
using bepinex_test.Modules.Utils;

namespace bepinex_test.Modules.Currency
{
    public class CurrencyModule
    {
        int _value = 1000;
        bool _attempted;

        public void Draw()
        {
            if (!_attempted)
            {
                _attempted = true;
                GameUtil.RefreshMapping();
            }
            var cur = GameUtil.TryGetCurrency();
            if (cur.HasValue)
            {
                ImGui.Text("Current: " + cur.Value);
            }
            ImGui.SliderInt("Currency", ref _value, 0, 100000);
            if (ImGui.Button("Set"))
            {
                GameUtil.TrySetCurrency(_value);
            }
            ImGui.SameLine();
            if (ImGui.Button("Set 1000"))
            {
                GameUtil.TrySetCurrency(1000);
            }
            ImGui.SameLine();
            if (ImGui.Button("Refresh link"))
            {
                GameUtil.RefreshMapping();
            }
        }
    }
}
