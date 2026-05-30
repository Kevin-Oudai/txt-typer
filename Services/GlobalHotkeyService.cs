using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TxtTyper.Services.Interfaces;

namespace TxtTyper.Services;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int HotkeyId = 0x4B54;
    private const int WmHotKey = 0x0312;

    private HwndSource? _source;
    private IntPtr _windowHandle = IntPtr.Zero;

    public event EventHandler? HotkeyPressed;

    public bool Register(Window window, ModifierKeys modifiers, Key key)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            return true;
        }

        _windowHandle = new WindowInteropHelper(window).Handle;
        if (_windowHandle == IntPtr.Zero)
        {
            return false;
        }

        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(WndProc);

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (NativeMethods.RegisterHotKey(_windowHandle, HotkeyId, (uint)modifiers, virtualKey))
        {
            return true;
        }

        _source?.RemoveHook(WndProc);
        _source = null;
        _windowHandle = IntPtr.Zero;
        return false;
    }

    public void Unregister(Window window)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
        _source?.RemoveHook(WndProc);
        _source = null;
        _windowHandle = IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_windowHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(_windowHandle, HotkeyId);
        }

        _source?.RemoveHook(WndProc);
        _source = null;
        _windowHandle = IntPtr.Zero;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotKey && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr windowHandle, int id, uint modifiers, uint virtualKey);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr windowHandle, int id);
    }
}
