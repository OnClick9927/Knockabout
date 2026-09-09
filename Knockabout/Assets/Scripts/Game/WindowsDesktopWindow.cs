using UnityEngine;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
#endif

[DisallowMultipleComponent]
public sealed partial class WindowsDesktopWindow : MonoBehaviour
{
    [Tooltip("Keep the player above other windows after login.")]
    public bool alwaysOnTop = true;
    public bool transparentBackground = true;
    [Tooltip("Pass mouse input to the desktop outside visible UGUI graphics.")]
    public bool clickThrough = true;

    private bool loggedIn;
    public void SetLoggedIn(bool value) => loggedIn = value;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const int GwlStyle = -16, GwlExStyle = -20;
    private const int WsOverlappedWindow = 0x00cf0000, WsPopup = unchecked((int)0x80000000);
    private const int WsExLayered = 0x00080000, WsExTransparent = 0x20, WsExTopmost = 8;
    private const uint SwpNoActivate = 0x10, SwpFrameChanged = 0x20, SwpNoMove = 2, SwpNoSize = 1;
    private IntPtr window;
    private int originalStyle, originalExStyle;
    private WindowRect originalRect;
    private bool ready, isTransparent, isTopmost, isClickThrough;
    private Coroutine setup;
    private readonly Dictionary<Camera, CameraState> cameras = new Dictionary<Camera, CameraState>();

    private struct CameraState
    {
        public CameraClearFlags clearFlags;
        public Color background;
        public bool hdr, postProcessing;
    }

    private void OnEnable()
    {
        Application.runInBackground = true;
        setup = StartCoroutine(InitializeWindow());
    }

    private IEnumerator InitializeWindow()
    {
        while (window == IntPtr.Zero)
        {
            window = GetActiveWindow();
            yield return null;
        }
        originalStyle = GetWindowLong(window, GwlStyle);
        originalExStyle = GetWindowLong(window, GwlExStyle);
        GetWindowRect(window, out originalRect);
        var monitor = new MonitorInfo { size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(window, 2), ref monitor))
            yield break;

        int width = monitor.monitor.right - monitor.monitor.left;
        int height = monitor.monitor.bottom - monitor.monitor.top;
        // DWM requires a composited, borderless window instead of exclusive fullscreen.
        Screen.SetResolution(width, height, FullScreenMode.Windowed);
        yield return null;
        yield return null;
        SetWindowLong(window, GwlStyle, (originalStyle & ~WsOverlappedWindow) | WsPopup);
        SetWindowPos(window, new IntPtr(-2), monitor.monitor.left, monitor.monitor.top,
            width, height, SwpNoActivate | SwpFrameChanged);
        ready = true;
        InitializeTray();
        RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
        ApplyTransparency();
        Debug.Log("Desktop window ready: " + width + "x" + height);
    }

    private void LateUpdate()
    {
        if (!ready) return;
        UpdateTray();
        if (isTransparent != transparentBackground) ApplyTransparency();
        bool topmost = alwaysOnTop && loggedIn;
        if (isTopmost != topmost)
        {
            isTopmost = topmost;
            SetWindowPos(window, new IntPtr(topmost ? -1 : -2), 0, 0, 0, 0,
                SwpNoActivate | SwpNoMove | SwpNoSize);
        }

        // Preserve the input recipient throughout both UI and desktop drags.
        if (!clickThrough || !AnyMouseButtonDown())
        {
            bool pass = clickThrough && !IsPointerOverUI();
            if (pass != isClickThrough)
            {
                isClickThrough = pass;
                ApplyExtendedStyle();
            }
        }
    }

    private bool IsPointerOverUI()
    {
        if (!GetCursorPos(out Point cursor) || !ScreenToClient(window, ref cursor) ||
            !GetClientRect(window, out WindowRect client) || client.right <= 0 || client.bottom <= 0)
            return false;
        var position = new Vector2(cursor.x * Screen.width / (float)client.right,
            Screen.height - cursor.y * Screen.height / (float)client.bottom);
        var raycasters = RaycasterManager.GetRaycasters();
        for (int i = 0; i < raycasters.Count; i++)
        {
            if (!(raycasters[i] is GraphicRaycaster raycaster) || !raycaster.isActiveAndEnabled) continue;
            var graphics = GraphicRegistry.GetGraphicsForCanvas(raycaster.GetComponent<Canvas>());
            for (int j = 0; j < graphics.Count; j++)
            {
                var graphic = graphics[j];
                if (!graphic.isActiveAndEnabled || graphic.depth == -1 || graphic.canvasRenderer.cull ||
                    graphic.color.a <= 0.001f || graphic.canvasRenderer.GetInheritedAlpha() <= 0.001f) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, position, raycaster.eventCamera)
                    && graphic.Raycast(position, raycaster.eventCamera)) return true;
            }
        }
        return false;
    }

    private static bool AnyMouseButtonDown()
        => ((GetAsyncKeyState(1) | GetAsyncKeyState(2) | GetAsyncKeyState(4)) & 0x8000) != 0;

    private void ApplyTransparency()
    {
        isTransparent = transparentBackground;
        var margins = new Margins { left = isTransparent ? -1 : 0 };
        Marshal.ThrowExceptionForHR(DwmExtendFrameIntoClientArea(window, ref margins));
        ApplyExtendedStyle();
        if (!isTransparent) RestoreCameras();
    }

    private void ApplyExtendedStyle()
    {
        int style = GetWindowLong(window, GwlExStyle) & ~(WsExLayered | WsExTransparent);
        if (isTransparent || isClickThrough) style |= WsExLayered;
        if (isClickThrough) style |= WsExTransparent;
        SetWindowLong(window, GwlExStyle, style);
    }

    private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!isTransparent || camera.targetTexture != null) return;
        camera.TryGetComponent(out UniversalAdditionalCameraData data);
        if (!cameras.ContainsKey(camera))
            cameras.Add(camera, new CameraState { clearFlags = camera.clearFlags, background = camera.backgroundColor,
                hdr = camera.allowHDR, postProcessing = data != null && data.renderPostProcessing });
        if (camera.clearFlags == CameraClearFlags.Skybox || camera.clearFlags == CameraClearFlags.SolidColor)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
        }
        camera.allowHDR = false;
        if (data != null) data.renderPostProcessing = false;
    }

    private void RestoreCameras()
    {
        foreach (var entry in cameras)
        {
            if (!entry.Key) continue;
            entry.Key.clearFlags = entry.Value.clearFlags;
            entry.Key.backgroundColor = entry.Value.background;
            entry.Key.allowHDR = entry.Value.hdr;
            if (entry.Key.TryGetComponent(out UniversalAdditionalCameraData data))
                data.renderPostProcessing = entry.Value.postProcessing;
        }
        cameras.Clear();
    }

    private void OnDisable()
    {
        if (setup != null) StopCoroutine(setup);
        RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
        RestoreCameras();
        DisposeTray();
        if (ready && IsWindow(window))
        {
            var margins = new Margins();
            DwmExtendFrameIntoClientArea(window, ref margins);
            SetWindowLong(window, GwlStyle, originalStyle);
            SetWindowLong(window, GwlExStyle, originalExStyle);
            SetWindowPos(window, new IntPtr((originalExStyle & WsExTopmost) != 0 ? -1 : -2),
                originalRect.left, originalRect.top, originalRect.right - originalRect.left,
                originalRect.bottom - originalRect.top, SwpNoActivate | SwpFrameChanged);
        }
        ready = isTransparent = isTopmost = isClickThrough = false;
        window = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)] private struct Point { public int x, y; }
    [StructLayout(LayoutKind.Sequential)] private struct WindowRect { public int left, top, right, bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct Margins { public int left, right, top, bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int size; public WindowRect monitor, work; public uint flags; }
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hwnd, ref Point point);
    [DllImport("user32.dll")] private static extern bool GetClientRect(IntPtr hwnd, out WindowRect rect);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out WindowRect rect);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);
#endif
}
