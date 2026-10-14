using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
internal sealed class VisibilityPolicy {
    internal bool Enabled=true,ManualHidden,Fullscreen,TemporaryHidden;
    bool candidate;int samples;
    internal bool ShouldShow{get{return !ManualHidden&&!TemporaryHidden&&!(Enabled&&Fullscreen);}}
    internal void ConfirmFullscreen(bool value){Fullscreen=Enabled&&value;samples=0;}
    internal void SetEnabled(bool value){Enabled=value;Fullscreen=false;samples=0;}
    internal void Sample(bool value){
        if(!Enabled){Fullscreen=false;samples=0;return;}
        if(value==Fullscreen){samples=0;return;}
        if(samples==0||candidate!=value){candidate=value;samples=1;}else samples++;
        if(samples>=2){Fullscreen=value;samples=0;}
    }
}
internal static class FullscreenProbe {
    [StructLayout(LayoutKind.Sequential)]struct Rect{internal int Left,Top,Right,Bottom;internal Rectangle Bounds{get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}
    [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")]static extern bool IsIconic(IntPtr hwnd);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr hwnd,StringBuilder name,int length);
    [DllImport("dwmapi.dll")]static extern int DwmGetWindowAttribute(IntPtr hwnd,int attribute,out Rect rect,int size);
    internal static bool Covers(Rectangle window,Rectangle monitor,int style,bool visible,bool minimized,string name){
        if(!visible||minimized||(style&0x00c00000)==0x00c00000)return false;
        if(name=="Progman"||name=="WorkerW"||name=="Shell_TrayWnd"||name=="Shell_SecondaryTrayWnd")return false;
        return Math.Abs((long)window.Left-monitor.Left)<=2&&Math.Abs((long)window.Top-monitor.Top)<=2&&Math.Abs((long)window.Right-monitor.Right)<=2&&Math.Abs((long)window.Bottom-monitor.Bottom)<=2;
    }
    internal static bool ReadWindow(IntPtr hwnd,Rectangle petBounds,out bool fullscreen){
        fullscreen=false;Rect rect;
        if(!GetWindowRect(hwnd,out rect))return false;
        Rect frame;if(DwmGetWindowAttribute(hwnd,9,out frame,16)==0&&frame.Right>frame.Left&&frame.Bottom>frame.Top)rect=frame;
        Screen monitor=Screen.FromHandle(hwnd),petMonitor=Screen.FromRectangle(petBounds);
        if(monitor.DeviceName!=petMonitor.DeviceName)return true;
        var name=new StringBuilder(256);GetClassName(hwnd,name,name.Capacity);
        fullscreen=Covers(rect.Bounds,monitor.Bounds,Native.GetWindowLong(hwnd,-16),IsWindowVisible(hwnd),IsIconic(hwnd),name.ToString());return true;
    }
    internal static bool Read(IntPtr pet,Rectangle bounds,out bool fullscreen){
        IntPtr foreground=GetForegroundWindow();fullscreen=false;
        if(foreground==IntPtr.Zero||foreground==pet)return false;
        return ReadWindow(foreground,bounds,out fullscreen);
    }
}
