using System;
using System.IO;
internal static class BehaviorTests {
    static int passed;static void Check(bool ok,string name){if(!ok)throw new Exception(name);passed++;Console.WriteLine("PASS "+name);}
    public static int Main(string[] args){try{
        string folder=Path.GetFullPath(args[0]);Directory.CreateDirectory(folder);string file=Path.Combine(folder,"settings.xml");
        var p=new UserPreferences{Width=420,FontSize=22,Sensitivity=1.4f,Keyboard=false,Mouse=true,Paused=true,ClickThrough=true,PositionLocked=true,HasPosition=true,Left=-1400,Top=200};
        Check(Preferences.Save(file,p),"settings write");var q=Preferences.Load(file);
        Check(q.Width==420&&q.FontSize==22&&Math.Abs(q.Sensitivity-1.4f)<.001,"size font sensitivity roundtrip");
        Check(!q.Keyboard&&q.Mouse&&q.Paused&&q.HasPosition&&q.Left==-1400&&q.Top==200,"switches and negative monitor position roundtrip");
        Check(q.ClickThrough&&q.PositionLocked,"interaction switches persist");
        p.Width=260;Check(Preferences.Save(file,p)&&Preferences.Load(file).Width==260&&File.Exists(file+".bak"),"atomic replacement and backup");
        File.WriteAllText(file,"broken xml");Check(Preferences.Load(file).Width==340,"corrupt settings fallback");
        File.WriteAllText(file,"<UserPreferences><Version>1</Version><Width>420</Width><Keyboard>false</Keyboard></UserPreferences>");q=Preferences.Load(file);Check(q.Width==420&&!q.Keyboard&&!q.ClickThrough&&!q.PositionLocked,"older settings retain values with safe interaction defaults");
        p.Width=1;p.FontSize=999;p.Sensitivity=float.NaN;p.Left=int.MaxValue;p.Normalize();Check(p.Width==340&&p.FontSize==18&&!p.HasPosition,"invalid settings normalized");
        var hand=new HandMotion();var down=new InputSnapshot{Keys=new[]{65},KeyDown=true};System.Drawing.PointF target;Check(HandMotion.Contact(65,out target),"A contact exists");
        for(int i=0;i<60;i++)hand.Step(down,1f/60);Check(Math.Abs(hand.FingerX-target.X)<.1&&Math.Abs(hand.FingerY-target.Y)<.1,"finger settles on highlighted A");
        Check(Math.Abs(HandMotion.BaseX+hand.WristX*HandMotion.FingerV-target.X)<.1,"render fingertip transform agrees with target");
        float pressedY=hand.FingerY;hand.Step(new InputSnapshot(),1f/30);Check(hand.FingerY<pressedY,"release lifts finger first");
        for(int i=0;i<60;i++)hand.Step(new InputSnapshot(),1f/60);Check(Math.Abs(hand.FingerX-HandMotion.BaseX)<.1,"release returns to resting position");
        var combo=new InputSnapshot{Keys=new[]{65,68}};System.Drawing.PointF d;HandMotion.Contact(68,out d);for(int i=0;i<60;i++)hand.Step(combo,1f/60);Check(Math.Abs(hand.FingerX-(target.X+d.X)/2)<.1,"chord targets center of active keys");
        var axis=new DampedAxis(0);float previous=0;for(int i=0;i<60;i++){axis.Step(10,1f/60);Check(axis.Value>=previous-.001&&axis.Value<=10.001,"critical damping avoids overshoot "+i);previous=axis.Value;}
        Console.WriteLine("PASSED "+passed+" behavior checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
