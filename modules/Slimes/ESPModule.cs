using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ImGuiNET;
using System.Numerics;

namespace bepinex_test.Modules.Slimes
{
    public class ESPModule
    {
        bool _enabled;
        readonly List<Identifiable> _targets = new List<Identifiable>();
        readonly Dictionary<Identifiable, HealthProvider> _health = new Dictionary<Identifiable, HealthProvider>();
        float _nextScan;

        public void DrawUI()
        {
            ImGui.Checkbox("Slime ESP", ref _enabled);
        }

        public void RenderOverlay()
        {
            if (!_enabled) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.75f;
                _targets.Clear();
                var all = Object.FindObjectsOfType<Identifiable>();
                for (int i = 0; i < all.Length; i++)
                {
                    var id = all[i];
                    if (id == null) continue;
                    var name = id.id.ToString();
                    if (name.EndsWith("_SLIME"))
                    {
                        _targets.Add(id);
                        if (!_health.ContainsKey(id))
                        {
                            var hp = FindHealthProvider(id.gameObject);
                            if (hp != null) _health[id] = hp;
                        }
                    }
                }
            }
            var io = ImGui.GetIO();
            var dl = ImGui.GetForegroundDrawList();
            for (int i = 0; i < _targets.Count; i++)
            {
                var t = _targets[i];
                if (t == null || t.transform == null) continue;
                if (!TryGetScreenBox(cam, t.transform, io, out var pMin, out var pMax)) continue;
                var col = ImGui.GetColorU32(new System.Numerics.Vector4(1f, 0f, 0f, 1f));
                dl.AddRect(new System.Numerics.Vector2(pMin.X, pMin.Y), new System.Numerics.Vector2(pMax.X, pMax.Y), col, 0f, ImDrawFlags.None, 2f);

                var name = t.id.ToString();
                var txtSize = ImGui.CalcTextSize(name);
                var namePos = new System.Numerics.Vector2((pMin.X + pMax.X) * 0.5f - txtSize.X * 0.5f, pMin.Y - txtSize.Y - 2f);
                dl.AddText(namePos, ImGui.GetColorU32(new System.Numerics.Vector4(1f, 1f, 1f, 1f)), name);

                float barW = 5f;
                float barX1 = pMin.X - (barW + 3f);
                float barX2 = pMin.X - 3f;
                dl.AddRect(new System.Numerics.Vector2(barX1, pMin.Y), new System.Numerics.Vector2(barX2, pMax.Y), ImGui.GetColorU32(new System.Numerics.Vector4(0f, 0f, 0f, 1f)));
                float frac = 1f;
                if (_health.TryGetValue(t, out var provider))
                {
                    frac = provider.GetFraction();
                }
                frac = Mathf.Clamp01(frac);
                float fillTop = pMax.Y - (pMax.Y - pMin.Y) * frac;
                dl.AddRectFilled(new System.Numerics.Vector2(barX1 + 1f, fillTop), new System.Numerics.Vector2(barX2 - 1f, pMax.Y - 1f), ImGui.GetColorU32(new System.Numerics.Vector4(0f, 1f, 0f, 1f)));
            }
        }

        static bool TryGetScreenBox(Camera cam, Transform root, ImGuiIOPtr io, out System.Numerics.Vector2 pMin, out System.Numerics.Vector2 pMax)
        {
            pMin = default;
            pMax = default;
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers == null || renderers.Length == 0) return false;
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            var c = bounds.center;
            var e = bounds.extents;
            var pts = new UnityEngine.Vector3[8]
            {
                new UnityEngine.Vector3(c.x - e.x, c.y - e.y, c.z - e.z),
                new UnityEngine.Vector3(c.x + e.x, c.y - e.y, c.z - e.z),
                new UnityEngine.Vector3(c.x - e.x, c.y + e.y, c.z - e.z),
                new UnityEngine.Vector3(c.x + e.x, c.y + e.y, c.z - e.z),
                new UnityEngine.Vector3(c.x - e.x, c.y - e.y, c.z + e.z),
                new UnityEngine.Vector3(c.x + e.x, c.y - e.y, c.z + e.z),
                new UnityEngine.Vector3(c.x - e.x, c.y + e.y, c.z + e.z),
                new UnityEngine.Vector3(c.x + e.x, c.y + e.y, c.z + e.z)
            };
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            bool any = false;
            for (int i = 0; i < pts.Length; i++)
            {
                var sp = cam.WorldToScreenPoint(pts[i]);
                if (sp.z <= 0) continue;
                any = true;
                float sx = sp.x;
                float sy = io.DisplaySize.Y - sp.y;
                if (sx < minX) minX = sx;
                if (sy < minY) minY = sy;
                if (sx > maxX) maxX = sx;
                if (sy > maxY) maxY = sy;
            }
            if (!any) return false;
            pMin = new System.Numerics.Vector2(minX, minY);
            pMax = new System.Numerics.Vector2(maxX, maxY);
            return true;
        }

        class HealthProvider
        {
            public object Component;
            public MemberInfo Cur;
            public MemberInfo Max;
            public bool IsFloat;
            public float GetFraction()
            {
                if (Component == null || Cur == null) return 1f;
                float cur = ReadValue(Cur, Component, IsFloat);
                float max = Max != null ? ReadValue(Max, Component, IsFloat) : 100f;
                if (max <= 0f) max = 100f;
                return cur / max;
            }
            static float ReadValue(MemberInfo m, object target, bool isFloat)
            {
                object v = null;
                if (m is FieldInfo fi) v = fi.GetValue(target);
                else if (m is PropertyInfo pi) v = pi.GetValue(target, null);
                if (v == null) return isFloat ? 0f : 0f;
                if (v is float f) return f;
                if (v is int i) return i;
                return 0f;
            }
        }

        static HealthProvider FindHealthProvider(GameObject go)
        {
            var mbs = go.GetComponents<MonoBehaviour>();
            foreach (var mb in mbs)
            {
                if (mb == null) continue;
                var t = mb.GetType();
                var flds = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                MemberInfo cur = null, max = null;
                bool isFloat = false;
                foreach (var f in flds)
                {
                    var n = f.Name.ToLowerInvariant();
                    if ((n == "health" || n == "hp" || n == "currhealth") && (f.FieldType == typeof(int) || f.FieldType == typeof(float)))
                    {
                        cur = f; isFloat = f.FieldType == typeof(float);
                    }
                    if ((n == "maxhealth" || n == "maxhp") && (f.FieldType == typeof(int) || f.FieldType == typeof(float)))
                    {
                        max = f; 
                    }
                }
                if (cur == null)
                {
                    var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    foreach (var p in props)
                    {
                        var n = p.Name.ToLowerInvariant();
                        if ((n == "health" || n == "hp" || n == "currhealth") && (p.PropertyType == typeof(int) || p.PropertyType == typeof(float)))
                        {
                            cur = p; isFloat = p.PropertyType == typeof(float);
                        }
                        if ((n == "maxhealth" || n == "maxhp") && (p.PropertyType == typeof(int) || p.PropertyType == typeof(float)))
                        {
                            max = p;
                        }
                    }
                }
                if (cur != null)
                {
                    return new HealthProvider { Component = mb, Cur = cur, Max = max, IsFloat = isFloat };
                }
            }
            return null;
        }
    }
}
