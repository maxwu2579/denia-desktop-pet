using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

internal static class Native {
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr context);
    [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int x,y; public Point(int a,int b){x=a;y=b;} }
    [StructLayout(LayoutKind.Sequential)] internal struct Size { public int cx,cy; public Size(int a,int b){cx=a;cy=b;} }
    [StructLayout(LayoutKind.Sequential, Pack=1)] internal struct Blend { public byte op,flags,alpha,format; }
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern int GetWindowLong(IntPtr hwnd,int index);
    [DllImport("user32.dll",SetLastError=true)] internal static extern int SetWindowLong(IntPtr hwnd,int index,int value);
    [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dc,ref Point dst,ref Size size,IntPtr src,ref Point origin,int key,ref Blend blend,int flags);
}
