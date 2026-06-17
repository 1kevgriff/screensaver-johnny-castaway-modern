using System.Runtime.InteropServices;

namespace JohnnyCastaway.ScreenSaver;

internal static partial class Native
{
    [LibraryImport("user32.dll")]
    public static partial nint SetParent(nint child, nint parent);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(nint hwnd, out RECT rect);
}
