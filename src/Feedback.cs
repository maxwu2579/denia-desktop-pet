using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;
internal static class Feedback {
    internal static string Label(int key){
        if(key>=65&&key<=90||key>=48&&key<=57)return((char)key).ToString();
        if(key>=112&&key<=135)return"F"+(key-111);
        if(key>=96&&key<=105)return"Num"+(key-96);
        switch(key){case 8:return"Back";case 9:return"Tab";case 13:return"Enter";case 16:case 160:case 161:return"Shift";case 17:case 162:case 163:return"Ctrl";case 18:case 164:case 165:return"Alt";case 20:return"Caps";case 27:return"Esc";case 32:return"Space";case 33:return"PgUp";case 34:return"PgDn";case 35:return"End";case 36:return"Home";case 37:return"←";case 38:return"↑";case 39:return"→";case 40:return"↓";case 45:return"Ins";case 46:return"Del";case 91:case 92:return"Win";case 186:return";";case 187:return"=";case 188:return",";case 189:return"-";case 190:return".";case 191:return"/";case 192:return"`";case 219:return"[";case 220:return"\\";case 221:return"]";case 222:return"'";default:return"Key"+key;}
    }
    internal static bool Position(int key,out float col,out int row){
        col=0;row=0;string[] rows={"1234567890","QWERTYUIOP","ASDFGHJKL","ZXCVBNM"};
        for(int i=0;i<rows.Length;i++){int index=rows[i].IndexOf((char)key);if(index>=0){col=index+1+i*.35f;row=i;return true;}}
        if(key==32){col=5;row=3;return true;}if(key==13){col=11;row=2;return true;}
        if(key==8){col=11;row=0;return true;}if(key==9){col=0;row=1;return true;}
        if(key==16||key==160||key==161){col=0;row=3;return true;}
        if(key==17||key==162||key==163||key==18||key==164||key==165||key==91||key==92){col=0;row=2;return true;}
        return false;
    }
    internal static PointF KeyPoint(float column,float row){float screenRow=3-row,screenColumn=12-column;return new PointF(280-screenRow*9+screenColumn*(28+screenRow*1.6f),612+screenRow*19);}
    internal static void DrawDesk(Graphics g,InputSnapshot state){
        using(Font font=new Font("Segoe UI",15,FontStyle.Bold,GraphicsUnit.Pixel))using(var text=new SolidBrush(Color.FromArgb(35,25,62)))using(var fill=new SolidBrush(Color.FromArgb(205,255,158,210)))using(var edge=new Pen(Color.FromArgb(255,218,60,148),2)){
            foreach(int key in state.Keys){float col;int row;if(!Position(key,out col,out row))continue;
                float span=key==32?3:1;PointF[] corners={KeyPoint(col,row),KeyPoint(col+span-.1f,row),KeyPoint(col+span-.1f,row+.8f),KeyPoint(col,row+.8f)};
                g.FillPolygon(fill,corners);g.DrawPolygon(edge,corners);
                string label=Label(key);if(label=="Shift")label="Sh";if(label=="Enter")label="Ent";if(label=="Back")label="Bk";if(label=="Space")label="Sp";
                using(Font keyFont=new Font("Segoe UI",label.Length==1?15:10,FontStyle.Bold,GraphicsUnit.Pixel))using(StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone()){
                    SizeF size=g.MeasureString(label,keyFont,new SizeF(100,100),format);float cx=(corners[0].X+corners[2].X)/2,cy=(corners[0].Y+corners[2].Y)/2;
                    var saved=g.Save();g.TranslateTransform(cx,cy);g.RotateTransform(180);g.DrawString(label,keyFont,text,-size.Width/2,-size.Height/2,format);g.Restore(saved);
                }
            }
        }
        if(state.Left>0||state.Right>0||state.Middle>0||Math.Abs(state.Wheel)>.05){
            Color color=state.Left>0?Color.DeepPink:state.Right>0?Color.DeepSkyBlue:Color.Gold;
            using(var pen=new Pen(color,6))g.DrawEllipse(pen,27,629,214,65);
        }
    }
    internal sealed class Badge{internal string Text;internal RectangleF Bounds;internal Badge(string text,RectangleF bounds){Text=text;Bounds=bounds;}}
    internal static List<string> Labels(InputSnapshot state){
        var labels=new List<string>();string mouse=state.Left>0?"L":"";
        if(state.Right>0)mouse+=(mouse.Length>0?" + ":"")+"R";
        if(state.Middle>0)mouse+=(mouse.Length>0?" + ":"")+"M";
        if(Math.Abs(state.Wheel)>.05)mouse+=(mouse.Length>0?" ":"")+(state.Wheel>0?"↑":"↓");
        if(mouse.Length>0)labels.Add("Mouse "+mouse);
        foreach(int key in state.Keys){string label=Label(key);if(!labels.Contains(label))labels.Add(label);}
        return labels;
    }
    internal static List<Badge> Layout(Graphics g,InputSnapshot state,Font font){
        var badges=new List<Badge>();float x=24,y=724;
        using(StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone()){
            format.FormatFlags|=StringFormatFlags.NoWrap;
            foreach(string label in Labels(state)){
                SizeF text=g.MeasureString(label,font,new SizeF(672,1000),format);
                float w=(float)Math.Ceiling(text.Width)+24,h=(float)Math.Ceiling(text.Height)+16;
                if(x+w>696&&x>24){x=24;y=badges[badges.Count-1].Bounds.Bottom+8;}
                badges.Add(new Badge(label,new RectangleF(x,y,w,h)));x+=w+8;
            }
        }return badges;
    }
    internal static int TextSize=18;
    internal static float FontPixels(int outputWidth){return TextSize*720f/outputWidth;}
    internal static float RequiredHeight(Graphics g,InputSnapshot state,int outputWidth){
        using(Font font=new Font("Segoe UI",FontPixels(outputWidth),FontStyle.Bold,GraphicsUnit.Pixel)){
            var badges=Layout(g,state,font);return badges.Count==0?860:Math.Max(860,badges[badges.Count-1].Bounds.Bottom+24);
        }
    }
    internal static float ReservedHeight(Graphics g,int outputWidth){
        using(Font font=new Font("Segoe UI",FontPixels(outputWidth),FontStyle.Bold,GraphicsUnit.Pixel))using(StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone()){
            float row=(float)Math.Ceiling(g.MeasureString("Mouse L + R + M",font,new SizeF(672,1000),format).Height)+16;
            return Math.Max(860,724+3*row+16+24);
        }
    }
    internal static void DrawBadges(Graphics g,InputSnapshot state,int outputWidth,float opacity=1){
        opacity=Math.Max(0,Math.Min(1,opacity));if(opacity<=0)return;
        using(Font font=new Font("Segoe UI",FontPixels(outputWidth),FontStyle.Bold,GraphicsUnit.Pixel))using(var text=new SolidBrush(Color.FromArgb((int)Math.Round(255*opacity),75,34,66)))using(var fill=new SolidBrush(Color.FromArgb((int)Math.Round(245*opacity),255,227,241)))using(StringFormat format=(StringFormat)StringFormat.GenericTypographic.Clone()){
            format.FormatFlags|=StringFormatFlags.NoWrap;
            foreach(Badge badge in Layout(g,state,font)){g.FillRectangle(fill,badge.Bounds);var bounds=badge.Bounds;bounds.Inflate(-12,-8);g.DrawString(badge.Text,font,text,bounds,format);}
        }
    }
}
