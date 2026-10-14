using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.IO;
internal static class FeedbackTests {
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static int Main(string[] args){
        try{string dir=args.Length>0?args[0]:"feedback-test-output";Directory.CreateDirectory(dir);int checks=0;
            InputSnapshot[] samples={new InputSnapshot{Keys=new[]{164},Left=8,Right=6,Middle=4,Wheel=1},new InputSnapshot{Keys=new[]{13,160,162},Left=8,Right=6},new InputSnapshot{Keys=new[]{8,9,13,20,32,33,34,35,36,65,66,67,91,112,113,160,162,164},Left=8,Right=6,Middle=4,Wheel=-1}};
            foreach(int textSize in new[]{14,18,22})foreach(int width in new[]{260,340,420})foreach(int dpi in new[]{96,192,384})for(int scene=0;scene<samples.Length;scene++){
                Feedback.TextSize=textSize;
                InputSnapshot sample=samples[scene];List<Feedback.Badge> badges;float logicalHeight;
                using(Bitmap measure=new Bitmap(1,1)){measure.SetResolution(dpi,dpi);using(Graphics g=Graphics.FromImage(measure)){
                    g.PageUnit=GraphicsUnit.Pixel;using(Font font=new Font("Segoe UI",Feedback.FontPixels(width),FontStyle.Bold,GraphicsUnit.Pixel))badges=Feedback.Layout(g,sample,font);
                    logicalHeight=Feedback.RequiredHeight(g,sample,width);
                    Check(badges.Count==Feedback.Labels(sample).Count,"Lost feedback labels");
                    foreach(var badge in badges){Check(badge.Bounds.Left>=24&&badge.Bounds.Right<=696&&badge.Bounds.Bottom<logicalHeight,"Badge outside canvas");}
                    for(int i=0;i<badges.Count;i++)for(int j=i+1;j<badges.Count;j++)Check(!badges[i].Bounds.IntersectsWith(badges[j].Bounds),"Overlapping badges");
                }}
                int height=(int)Math.Ceiling(logicalHeight*width/720f);
                using(Bitmap image=new Bitmap(width,height,PixelFormat.Format32bppArgb)){image.SetResolution(dpi,dpi);using(Graphics g=Graphics.FromImage(image)){
                    g.PageUnit=GraphicsUnit.Pixel;g.Clear(Color.Transparent);g.ScaleTransform(width/720f,width/720f);Feedback.DrawBadges(g,sample,width);
                }
                    // Pixel containment catches font/DPI overflow, not just rectangle arithmetic.
                    for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(image.GetPixel(x,y).A>0){bool contained=false;float lx=x*720f/width,ly=y*720f/width;foreach(var badge in badges){RectangleF b=badge.Bounds;b.Inflate(3,3);if(b.Contains(lx,ly)){contained=true;break;}}Check(contained,"Text pixels spill outside badge");}
                    image.Save(Path.Combine(dir,"text-"+textSize+"-"+width+"-"+dpi+"-"+scene+".png"),ImageFormat.Png);
                }checks++;Console.WriteLine("PASS font="+textSize+" width="+width+" DPI="+dpi+" scene="+scene+" labels="+badges.Count);
            }
            using(Bitmap b=new Bitmap(1,1)){b.SetResolution(384,384);using(Graphics g=Graphics.FromImage(b))using(Font oldFont=new Font("Segoe UI",23,FontStyle.Bold)){
                float oldWidth=g.MeasureString("Mouse L + R + M ↑",oldFont).Width;Check(oldWidth>190,"Expected old mouse label overflow not reproduced");Console.WriteLine("Old mouse label width at DPI384="+oldWidth+"; old fixed badge width=190");}}
            Check(Feedback.KeyPoint(1,2).X>Feedback.KeyPoint(8,2).X,"Character-left keys must be screen-right");
            Check(Feedback.KeyPoint(5,3).Y<Feedback.KeyPoint(5,0).Y,"Space row must be nearest character");
            Console.WriteLine("PASS character-facing keyboard orientation");
            Console.WriteLine("PASSED "+checks+" render scenarios");return 0;
        }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}
    }
}
