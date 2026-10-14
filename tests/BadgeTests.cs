using System;
using System.Drawing;
using System.IO;
internal static class BadgeTests {
    static int checks;static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    public static int Main(string[] args){try{
        var b=new BadgeDisplay();var empty=new InputSnapshot();var keys=new InputSnapshot{Keys=new[]{65,162},KeyDown=true};float alpha;
        b.View(empty,0,out alpha);Check(alpha==0,"no badge before first input");
        b.Update(keys,100);var view=b.View(keys,100,out alpha);Check(alpha==1&&view.Keys.Length==2,"keyboard input shows immediately");
        b.Update(empty,200);b.View(empty,800,out alpha);Check(alpha==1,"last combo remains during 700ms hold");b.View(empty,950,out alpha);Check(Math.Abs(alpha-.5f)<.001,"halfway through 300ms fade");b.View(empty,1100,out alpha);Check(alpha==0,"gone after one second");
        b.Update(keys,6000);b.View(keys,6000,out alpha);Check(alpha==1,"held or renewed input stays visible");keys.Keys[0]=90;view=b.View(empty,6100,out alpha);Check(view.Keys[0]==65,"retained snapshot owns key copy");
        b.SetMode(BadgeDisplay.Hidden);b.Update(keys,7000);b.View(keys,7000,out alpha);Check(alpha==0,"hidden mode never draws badges");
        b.SetMode(BadgeDisplay.Always);view=b.View(empty,9000,out alpha);Check(alpha==1&&view.Keys.Length==0,"always mode keeps original current-input semantics");
        Check(BadgeDisplay.Active(new InputSnapshot{Left=8})&&BadgeDisplay.Active(new InputSnapshot{Right=6})&&BadgeDisplay.Active(new InputSnapshot{Middle=4}),"mouse buttons trigger labels");Check(BadgeDisplay.Active(new InputSnapshot{Wheel=1})&&!BadgeDisplay.Active(empty),"wheel triggers but movement alone does not");
        b.SetMode(1);b.Update(keys,0);b.Clear();b.View(keys,1,out alpha);Check(alpha==0,"visibility and pause reset clears fading state");
        using(var bitmap=new Bitmap(720,1600))using(Graphics g=Graphics.FromImage(bitmap)){
            bitmap.SetResolution(96,96);g.PageUnit=GraphicsUnit.Pixel;
            foreach(int width in new[]{260,340,420})foreach(int font in new[]{14,18,22}){Feedback.TextSize=font;float reserve=Feedback.ReservedHeight(g,width),need=Feedback.RequiredHeight(g,new InputSnapshot{Keys=new[]{65,162,164},Left=8,Right=6,Middle=4},width);Check(reserve>=need,"three row reservation fits common combo "+width+"/"+font);}
            Feedback.TextSize=18;g.Clear(Color.Transparent);Feedback.DrawBadges(g,new InputSnapshot{Keys=new[]{65}},720,0);Check(bitmap.GetPixel(25,725).A==0,"zero opacity paints no badge");
            Feedback.DrawBadges(g,new InputSnapshot{Keys=new[]{65}},720,.5f);Check(bitmap.GetPixel(25,725).A>=121&&bitmap.GetPixel(25,725).A<=123,"background alpha follows fade");
        }
        Directory.CreateDirectory(args[0]);string file=Path.Combine(args[0],"badges.xml");Check(Preferences.Save(file,new UserPreferences{BadgeMode=2})&&Preferences.Load(file).BadgeMode==2,"mode persists");File.WriteAllText(file,"<UserPreferences><Version>1</Version></UserPreferences>");Check(Preferences.Load(file).BadgeMode==1,"old settings default to input mode");var p=new UserPreferences{BadgeMode=99};p.Normalize();Check(p.BadgeMode==1,"invalid mode falls back");
        Console.WriteLine("PASSED "+checks+" badge timing rendering and settings checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
