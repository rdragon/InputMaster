using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Security;
using InputMaster.Win32;
using System.Windows.Forms;

namespace InputMaster.Win32
{
  [SuppressUnmanagedCodeSecurity]
  class NativeMethods
  {
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr handle);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern HookHandle SetWindowsHookEx(HookType hookType, HookProcedureFunction procedure, IntPtr dllHandle, int threadId);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr dummy, int code, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, BestFitMapping = false)]
    public static extern int GetWindowText(IntPtr window, StringBuilder text, int maxLength);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr window);

    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr window, out int id);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnableWindow(IntPtr window, [MarshalAs(UnmanagedType.Bool)]bool enable);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr window);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern int GetScrollPos(IntPtr window, Orientation orientation);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(IntPtr window, ShowWindowArgument argument);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref NativePoint lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SendNotifyMessage(IntPtr window, WindowMessage message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SendInput(int count, NativeInput[] inputs, int inputSize);

    [DllImport("user32.dll")]
    public static extern short GetKeyState(Input input);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryPerformanceCounter(out long count);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool QueryPerformanceFrequency(out long frequency);

    #region overloads
    public static IntPtr SendMessage(IntPtr window, int message, int wParam, int lParam)
    {
      return SendMessage(window, message, (IntPtr)wParam, (IntPtr)lParam);
    }
    #endregion
  }
}
