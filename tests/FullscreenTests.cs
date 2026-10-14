using System;
using System.Drawing;
using System.IO;
internal static class FullscreenTests {
    static int checks;static void Check(bool value,string name){if(!value)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    public static int Main(string[] args){try{
        Rectangle monitor=new Rectangle(-1920,0,1920,1080);
        Check(FullscreenProbe.Covers(monitor,monitor,0,true,false,"Game"),"borderless negative-coordinate monitor");
        Check(FullscreenProbe.Covers(new Rectangle(-1921,-1,1922,1082),monitor,0,true,false,"Browser"),"two pixel tolerance");
        Check(!FullscreenProbe.Covers(new Rectangle(-1920,0,1920,1040),monitor,0,true,false,"App"),"working area maximize excluded");
        Check(!FullscreenProbe.Covers(monitor,monitor,0x00c00000,true,false,"App"),"captioned maximize excluded");
        Check(!FullscreenProbe.Covers(monitor,monitor,0,false,false,"Game"),"hidden excluded");
        Check(!FullscreenProbe.Covers(monitor,monitor,0,true,true,"Game"),"minimized excluded");
        foreach(string name in new[]{"Progman","WorkerW","Shell_TrayWnd","Shell_SecondaryTrayWnd"})Check(!FullscreenProbe.Covers(monitor,monitor,0,true,false,name),"shell excluded "+name);
        Check(!FullscreenProbe.Covers(new Rectangle(0,0,1920,1080),monitor,0,true,false,"Game"),"different monitor coordinates excluded");
        var policy=new VisibilityPolicy();policy.Sample(true);policy.Sample(false);Check(policy.ShouldShow,"transient fullscreen ignored");
        policy.Sample(true);policy.Sample(true);Check(!policy.ShouldShow,"stable fullscreen hides");
        policy.ManualHidden=true;policy.Sample(false);policy.Sample(false);Check(!policy.ShouldShow,"manual intent survives exit");
        policy.SetEnabled(false);Check(!policy.ShouldShow,"disabled feature preserves manual intent");policy.ManualHidden=false;Check(policy.ShouldShow,"manual show works");
        string path=Path.Combine(args[0],"fullscreen-settings.xml");Directory.CreateDirectory(args[0]);var saved=new UserPreferences{FullscreenHide=false,ManualHidden=true,ClickThrough=true};Check(Preferences.Save(path,saved),"save fullscreen preference");var loaded=Preferences.Load(path);Check(!loaded.FullscreenHide&&loaded.ManualHidden&&loaded.ClickThrough,"new and previous switches roundtrip");
        File.WriteAllText(path,"<UserPreferences><Version>1</Version><Width>420</Width></UserPreferences>");loaded=Preferences.Load(path);Check(loaded.FullscreenHide&&!loaded.ManualHidden&&loaded.Width==420,"old settings compatibility");
        Console.WriteLine("PASSED "+checks+" fullscreen policy and settings checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
