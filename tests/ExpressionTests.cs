using System;
using System.Drawing;
internal static class ExpressionTests {
    static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
    public static int Main(string[] args){try{
        var engine=new InputEngine();var id=new IntPtr(1);engine.Key(id,65,true,0);for(int i=0;i<50;i++)engine.Key(id,65,true,i*20);Check(engine.TypingPresses==1,"held key autorepeat counts once");engine.Key(id,65,false,1100);engine.Key(id,162,true,1200);Check(engine.TypingPresses==1,"modifiers excluded from typing rate");engine.Mouse(id,1,0,0);engine.Mouse(id,1,0,10);engine.Mouse(id,2,0,20);engine.Mouse(id,0x400,120,30);Check(engine.ClickPresses==1,"only new mouse downs count not wheel or held flags");
        var feedback=new ExpressionFeedback();feedback.Reset(0,0);for(int i=1;i<=5;i++)feedback.Observe(i,0,400+i*150);Check(feedback.Pick(1150,true,false,0,0)==3,"five typing presses per second focus");Check(feedback.Pick(2000,true,false,0,0)==3,"focus held after burst");Check(feedback.Pick(2400,true,false,0,0)==0,"focus recovers after inactivity");
        feedback.Reset(0,0);feedback.Observe(0,1,500);feedback.Observe(0,2,700);Check(feedback.Pick(700,true,false,0,0)==0,"two clicks insufficient");feedback.Observe(0,3,900);Check(feedback.Pick(900,true,false,0,0)==2,"three clicks within650ms happy");Check(feedback.Pick(2300,true,false,0,0)==0,"happy recovers naturally");
        feedback.Reset(0,0);feedback.Observe(5,3,1000);Check(feedback.Pick(1000,true,false,0,0)==2,"click happiness takes priority over typing");Check(feedback.Pick(1000,true,true,0,0)==0,"pause neutral");Check(feedback.Pick(1000,false,false,0,0)==0,"toggle disables reactions");feedback.Reset(50,30);feedback.Observe(50,30,1000);Check(feedback.Pick(1000,true,false,0,0)==0,"reset ignores input accumulated while hidden");
        feedback.Reset(0,0);feedback.Observe(5,0,1000);feedback.Pick(1000,true,false,0,0);feedback.Observe(5,3,1100);Check(feedback.Pick(1100,true,false,0,0)==3,"rapid competing reaction waits minimum dwell");Check(feedback.Pick(1250,true,false,0,0)==2,"competing reaction switches after250ms");
        var transition=new FaceTransition();int from;float blend;transition.Step(3,1000,out from,out blend);Check(from==0&&blend==0,"transition begins from previous face");transition.Step(3,1080,out from,out blend);Check(Math.Abs(blend-.5f)<.001,"smooth transition midpoint");transition.Step(3,1160,out from,out blend);Check(blend==1,"transition completes in160ms");
        using(Bitmap original=new Bitmap(args[0]))using(Bitmap generated=new Bitmap(args[1]))using(Bitmap composed=FaceArtwork.Compose(original,generated)){
            int changed=0;Rectangle facial=new Rectangle(165,325,285,143);for(int y=0;y<original.Height;y++)for(int x=0;x<original.Width;x++){bool different=original.GetPixel(x,y)!=composed.GetPixel(x,y);if(different){changed++;if(!facial.Contains(x,y))throw new Exception("Head changed outside facial patches");}}
            Check(changed>100,"new focus expression modifies face only");composed.Save(args[2]);
        }
        Console.WriteLine("PASSED "+count+" expression trigger transition and artwork checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
