using System.Drawing;
using System.Drawing.Drawing2D;
internal static class FaceArtwork {
    internal static Bitmap Compose(Bitmap original,Bitmap generated){
        Bitmap result=(Bitmap)original.Clone();
        using(Graphics g=Graphics.FromImage(result))using(var face=new GraphicsPath()){
            g.PageUnit=GraphicsUnit.Pixel;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            // Preserve the approved hair, accessories and silhouette. Replace only facial patches.
            face.AddEllipse(171,330,111,94);face.AddEllipse(321,340,115,96);face.AddEllipse(255,424,85,38);
            g.SetClip(face);g.DrawImage(generated,new Rectangle(1,1,594,578),new Rectangle(0,0,1254,1220),GraphicsUnit.Pixel);
        }return result;
    }
}
