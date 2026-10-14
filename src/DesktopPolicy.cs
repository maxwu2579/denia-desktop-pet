using System;
using System.Drawing;
internal static class DesktopPolicy {
    internal static int Interval(long quietMilliseconds,bool visible,bool paused,bool locked,bool suspended){
        if(!visible||paused||locked||suspended)return 0;
        if(quietMilliseconds>=45000)return 1000;
        return quietMilliseconds>=2500?200:16;
    }
    internal static Point Clamp(Rectangle window,Rectangle[] workingAreas){
        if(workingAreas==null||workingAreas.Length==0)return window.Location;
        Rectangle chosen=workingAreas[0];long bestArea=-1;double bestDistance=double.MaxValue;
        foreach(Rectangle area in workingAreas){Rectangle intersection=Rectangle.Intersect(window,area);long overlap=(long)Math.Max(0,intersection.Width)*Math.Max(0,intersection.Height);
            double x=window.Left+window.Width/2.0,y=window.Top+window.Height/2.0;
            double dx=Math.Max(area.Left-x,Math.Max(0,x-area.Right)),dy=Math.Max(area.Top-y,Math.Max(0,y-area.Bottom));double distance=dx*dx+dy*dy;
            if(overlap>bestArea||(overlap==bestArea&&distance<bestDistance)){chosen=area;bestArea=overlap;bestDistance=distance;}
        }
        return new Point(Math.Max(chosen.Left,Math.Min(window.Left,chosen.Right-window.Width)),Math.Max(chosen.Top,Math.Min(window.Top,chosen.Bottom-window.Height)));
    }
}
