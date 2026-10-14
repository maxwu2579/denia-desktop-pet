using System;
using System.Drawing;
using System.Windows.Forms;
internal static class ReliabilityTests {
    static int passed;static void Check(bool yes,string name){if(!yes)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
    public static int Main(){try{
        Check(DesktopPolicy.Interval(0,true,false,false,false)==16,"active input schedules at 16ms");
        Check(DesktopPolicy.Interval(2500,true,false,false,false)==200,"quiet idle 200ms");
        Check(DesktopPolicy.Interval(45000,true,false,false,false)==1000,"sleep 1000ms");
        Check(DesktopPolicy.Interval(0,false,false,false,false)==0,"hidden stops render timer");
        Check(DesktopPolicy.Interval(0,true,true,false,false)==0,"paused stops render timer");
        Check(DesktopPolicy.Interval(0,true,false,true,false)==0,"locked stops render timer");
        Check(DesktopPolicy.Interval(0,true,false,false,true)==0,"suspended stops render timer");
        Check(DesktopPolicy.Interval(0,true,false,false,false)==16,"new input immediately resumes active rate");
        Rectangle[] dual={new Rectangle(-1920,0,1920,1040),new Rectangle(0,0,2560,1400)};
        var win=new Rectangle(-1700,300,340,407);Check(DesktopPolicy.Clamp(win,dual)==win.Location,"negative-coordinate monitor retained");
        Check(DesktopPolicy.Clamp(new Rectangle(-4000,300,340,407),dual).X==-1920,"offscreen left clamps to nearest monitor");
        Point point=DesktopPolicy.Clamp(new Rectangle(2450,1300,420,502),dual);Check(point==new Point(2140,898),"right and bottom edges clamp inside work area");
        Rectangle[] removed={new Rectangle(0,0,1920,1040)};point=DesktopPolicy.Clamp(win,removed);Check(point.X>=0&&point.Y>=0&&point.X+340<=1920&&point.Y+407<=1040,"removed display restores window to remaining screen");
        Rectangle[] stacked={new Rectangle(0,-1440,2560,1400),new Rectangle(0,0,1920,1040)};point=DesktopPolicy.Clamp(new Rectangle(200,-1300,420,502),stacked);Check(point.Y==-1300,"screen above primary retained");
        point=DesktopPolicy.Clamp(new Rectangle(0,0,420,502),new[]{new Rectangle(0,0,300,200)});Check(point==Point.Empty,"oversize window keeps top corner recoverable");
        Rectangle[] actual=Array.ConvertAll(Screen.AllScreens,delegate(Screen screen){return screen.WorkingArea;});point=DesktopPolicy.Clamp(new Rectangle(100000,100000,340,407),actual);bool contained=false;foreach(var area in actual)if(area.Contains(new Rectangle(point,new Size(340,407))))contained=true;Check(contained,"current real desktop recovery");
        var engine=new InputEngine();engine.Key(new IntPtr(1),65,true,0);engine.Mouse(new IntPtr(1),1,0,0);engine.Reset();Check(!engine.Snapshot(100).KeyDown&&engine.Snapshot(100).Left==0,"lock recovery clears held inputs");
        Console.WriteLine("Actual monitor count: "+actual.Length);Console.WriteLine("PASSED "+passed+" reliability checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
