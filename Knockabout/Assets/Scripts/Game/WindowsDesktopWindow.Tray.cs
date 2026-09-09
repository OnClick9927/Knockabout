using UnityEngine;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
#endif

public sealed partial class WindowsDesktopWindow
{
    public void MinimizeToTray()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (ready && trayAdded) ShowWindow(window, 0);
#endif
    }

    public void RestoreFromTray()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        if (!ready) return;
        ShowWindow(window, 9);
        SetForegroundWindow(window);
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const uint TrayMessage = 0x8001;
    private static WindowsDesktopWindow trayOwner;
    private static readonly WindowProcedure trayProcedure = TrayWindowProcedure;
    private IntPtr originalWindowProcedure;
    private NotifyIconData trayIcon;
    private uint taskbarCreated;
    private bool trayAdded, trayHideRequested, trayRestoreRequested, trayQuitRequested;

    private void InitializeTray()
    {
        trayOwner = this;
        taskbarCreated = RegisterWindowMessage("TaskbarCreated");
        originalWindowProcedure = SetWindowLongPtr(window, -4, Marshal.GetFunctionPointerForDelegate(trayProcedure));
        var icon = SendMessage(window, 0x7f, new IntPtr(1), IntPtr.Zero);
        if (icon == IntPtr.Zero) icon = LoadIcon(IntPtr.Zero, new IntPtr(32512));
        trayIcon = new NotifyIconData
        {
            size = (uint)Marshal.SizeOf<NotifyIconData>(), window = window, id = 1,
            flags = 7, callbackMessage = TrayMessage, icon = icon, tip = Application.productName,
        };
        trayAdded = ShellNotifyIcon(0, ref trayIcon);
        if (!trayAdded) throw new InvalidOperationException("Could not create the desktop tray icon.");
        // Tool windows remain visible on the desktop without a taskbar button.
        ShowWindow(window, 0);
        SetWindowLong(window, GwlExStyle, (GetWindowLong(window, GwlExStyle) | 0x80) & ~0x40000);
        ShowWindow(window, 5);
    }

    private void UpdateTray()
    {
        if (trayHideRequested) { trayHideRequested = false; MinimizeToTray(); }
        if (trayRestoreRequested) { trayRestoreRequested = false; RestoreFromTray(); }
        if (trayQuitRequested) { trayQuitRequested = false; Application.Quit(); }
    }

    [AOT.MonoPInvokeCallback(typeof(WindowProcedure))]
    private static IntPtr TrayWindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        var owner = trayOwner;
        if (owner == null) return DefWindowProc(hwnd, message, wParam, lParam);
        try
        {
            if (message == TrayMessage)
            {
                int mouseMessage = lParam.ToInt32() & 0xffff;
                if (mouseMessage == 0x202 || mouseMessage == 0x203) owner.trayRestoreRequested = true;
                if (mouseMessage == 0x205 || mouseMessage == 0x7b) owner.ShowTrayMenu();
                return IntPtr.Zero;
            }
            if (message == owner.taskbarCreated) owner.trayAdded = ShellNotifyIcon(0, ref owner.trayIcon);
            if (message == 5 && wParam.ToInt64() == 1) owner.trayHideRequested = true;
        }
        catch (Exception ex) { Debug.LogException(ex); }
        return CallWindowProc(owner.originalWindowProcedure, hwnd, message, wParam, lParam);
    }

    private void ShowTrayMenu()
    {
        var menu = CreatePopupMenu();
        if (menu == IntPtr.Zero) return;
        try
        {
            AppendMenu(menu, 0, new UIntPtr(1), "Show");
            AppendMenu(menu, 0, new UIntPtr(2), "Minimize to tray");
            AppendMenu(menu, alwaysOnTop ? 8u : 0u, new UIntPtr(3), "Always on top");
            AppendMenu(menu, 0x800, UIntPtr.Zero, null);
            AppendMenu(menu, 0, new UIntPtr(4), "Exit");
            GetCursorPos(out var cursor);
            SetForegroundWindow(window);
            uint command = TrackPopupMenu(menu, 0x102, cursor.x, cursor.y, 0, window, IntPtr.Zero);
            if (command == 1) trayRestoreRequested = true;
            if (command == 2) trayHideRequested = true;
            if (command == 3) alwaysOnTop = !alwaysOnTop;
            if (command == 4) trayQuitRequested = true;
            PostMessage(window, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally { DestroyMenu(menu); }
    }

    private void DisposeTray()
    {
        if (trayAdded) ShellNotifyIcon(2, ref trayIcon);
        trayAdded = false;
        if (originalWindowProcedure != IntPtr.Zero && IsWindow(window))
            SetWindowLongPtr(window, -4, originalWindowProcedure);
        originalWindowProcedure = IntPtr.Zero;
        if (trayOwner == this) trayOwner = null;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint size;
        public IntPtr window;
        public uint id, flags, callbackMessage;
        public IntPtr icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string tip;
        public uint state, stateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string info;
        public uint version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string infoTitle;
        public uint infoFlags;
        public Guid guid;
        public IntPtr balloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll", EntryPoint = "CallWindowProcW")] private static extern IntPtr CallWindowProc(IntPtr procedure, IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "DefWindowProcW")] private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)] private static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll", EntryPoint = "LoadIconW")] private static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "PostMessageW")] private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern IntPtr CreatePopupMenu();
    [DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode)] private static extern bool AppendMenu(IntPtr menu, uint flags, UIntPtr id, string text);
    [DllImport("user32.dll")] private static extern uint TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);
    [DllImport("user32.dll")] private static extern bool DestroyMenu(IntPtr menu);
#endif
}
