using BepInEx;
using UnityEngine;
using ImGuiNET;
using System;
using System.Runtime.InteropServices;
using System.Numerics;
using bepinex_test.Modules.Currency;
using bepinex_test.Modules.Slimes;

[BepInPlugin("github.antipaster.sr", "slimemod", "1.0.0")]
public class main : BaseUnityPlugin
{
    private ImGuiController _controller;
    private bool _show;
    private CurrencyModule _currency;
    private TeleportModule _teleport;
    private ESPModule _esp;
    void Awake()
    {
        Logger.LogInfo("sucessfully loaded");
        ImGui.CreateContext();
        _controller = new ImGuiController();
        _currency = new CurrencyModule();
        _teleport = new TeleportModule();
        _esp = new ESPModule();
    }

    void Update()
    {
        if (UnityEngine.Input.GetKeyDown(KeyCode.Insert))
        {
            _show = !_show;
            Cursor.visible = _show;
        }
    }

    void OnRenderObject()
    {
        if (_controller == null) return;
        _controller.Render(() =>
        {
            if (_show)
            {
                ImGui.Begin("slimemod");
                ImGui.Text("github.com/antipaster");
                _currency.Draw();
                _teleport.Draw();
                _esp.DrawUI();
                ImGui.End();
                _esp.RenderOverlay();
            }
        });
    }

    void OnDestroy()
    {
        _controller?.Dispose();
    }
}

class ImGuiController : IDisposable
{
    private Texture2D _fontTexture;
    private Material _material;

    public ImGuiController()
    {
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.Fonts.AddFontDefault();
        BuildFontTexture();
        CreateMaterial();
    }

    void BuildFontTexture()
    {
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        var size = width * height * 4;
        var data = new byte[size];
        Marshal.Copy(pixels, data, 0, size);
        _fontTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        _fontTexture.filterMode = FilterMode.Point;
        _fontTexture.LoadRawTextureData(data);
        _fontTexture.Apply();
        io.Fonts.SetTexID(new IntPtr(1));
        io.Fonts.ClearTexData();
    }

    void CreateMaterial()
    {
        var shader = Shader.Find("Sprites/Default");
        _material = new Material(shader);
        _material.hideFlags = HideFlags.HideAndDontSave;
        _material.mainTexture = _fontTexture;
    }

    public void Render(Action drawUI)
    {
        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(Screen.width, Screen.height);
        io.DeltaTime = Time.deltaTime > 0 ? Time.deltaTime : 0.016f;
        UpdateInput();
        ImGui.NewFrame();
        drawUI?.Invoke();
        ImGui.Render();
        var drawData = ImGui.GetDrawData();
        if (drawData.CmdListsCount == 0) return;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, io.DisplaySize.X, io.DisplaySize.Y, 0);
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            _material.mainTexture = _fontTexture;
            _material.SetPass(0);
            GL.Begin(GL.TRIANGLES);
            for (int i = 0; i < cmdList.IdxBuffer.Size; i += 3)
            {
                ushort idx0 = cmdList.IdxBuffer[i];
                ushort idx1 = cmdList.IdxBuffer[i + 1];
                ushort idx2 = cmdList.IdxBuffer[i + 2];
                var v0 = cmdList.VtxBuffer[idx0];
                var v1 = cmdList.VtxBuffer[idx1];
                var v2 = cmdList.VtxBuffer[idx2];
                SetColor(v0.col);
                GL.TexCoord2(v0.uv.X, v0.uv.Y);
                GL.Vertex3(v0.pos.X, v0.pos.Y, 0);
                SetColor(v1.col);
                GL.TexCoord2(v1.uv.X, v1.uv.Y);
                GL.Vertex3(v1.pos.X, v1.pos.Y, 0);
                SetColor(v2.col);
                GL.TexCoord2(v2.uv.X, v2.uv.Y);
                GL.Vertex3(v2.pos.X, v2.pos.Y, 0);
            }
            GL.End();
        }
        GL.PopMatrix();
    }

    void UpdateInput()
    {
        var io = ImGui.GetIO();
        var mp = UnityEngine.Input.mousePosition;
        io.MousePos = new System.Numerics.Vector2(mp.x, io.DisplaySize.Y - mp.y);
        io.MouseDown[0] = UnityEngine.Input.GetMouseButton(0);
        io.MouseDown[1] = UnityEngine.Input.GetMouseButton(1);
        io.MouseWheel = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
    }

    static void SetColor(uint col)
    {
        float r = ((col >> 0) & 0xFF) / 255f;
        float g = ((col >> 8) & 0xFF) / 255f;
        float b = ((col >> 16) & 0xFF) / 255f;
        float a = ((col >> 24) & 0xFF) / 255f;
        GL.Color(new Color(r, g, b, a));
    }

    public void Dispose()
    {
        if (_material != null)
        {
            UnityEngine.Object.Destroy(_material);
            _material = null;
        }
        if (_fontTexture != null)
        {
            UnityEngine.Object.Destroy(_fontTexture);
            _fontTexture = null;
        }
    }
}
