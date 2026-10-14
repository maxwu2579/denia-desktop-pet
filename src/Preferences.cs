using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
public sealed class UserPreferences {
    public int Version=1,Width=340,FontSize=18,Left,Top;
    public float Sensitivity=1;
    public bool HasPosition,Keyboard=true,Mouse=true,Paused,ClickThrough,PositionLocked;
    public bool FullscreenHide=true,ManualHidden;
    public bool StartAtLogin;
    public int BadgeMode=1;
    public bool Expressions=true;
    public void Normalize(){
        if(Width!=260&&Width!=340&&Width!=420)Width=340;
        if(FontSize!=14&&FontSize!=18&&FontSize!=22)FontSize=18;
        if(BadgeMode<0||BadgeMode>2)BadgeMode=1;
        if(float.IsNaN(Sensitivity)||float.IsInfinity(Sensitivity)||(Math.Abs(Sensitivity-.6f)>.001&&Math.Abs(Sensitivity-1)>.001&&Math.Abs(Sensitivity-1.4f)>.001))Sensitivity=1;
        if(Left< -100000||Left>100000||Top< -100000||Top>100000)HasPosition=false;
    }
}
internal static class Preferences {
    internal static string DefaultPath{get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"DeniaDesktopPet","settings.xml");}}
    internal static UserPreferences Load(string path){
        try{if(!File.Exists(path)||new FileInfo(path).Length>32768)return new UserPreferences();
            var options=new XmlReaderSettings();options.DtdProcessing=DtdProcessing.Prohibit;options.XmlResolver=null;
            using(var reader=XmlReader.Create(path,options)){var p=(UserPreferences)new XmlSerializer(typeof(UserPreferences)).Deserialize(reader);if(p.Version!=1)return new UserPreferences();p.Normalize();return p;}
        }catch(IOException){return new UserPreferences();}catch(UnauthorizedAccessException){return new UserPreferences();}catch(InvalidOperationException){return new UserPreferences();}catch(XmlException){return new UserPreferences();}
    }
    internal static bool Save(string path,UserPreferences value){
        string temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try{value.Normalize();Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            using(FileStream stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new XmlSerializer(typeof(UserPreferences)).Serialize(stream,value);stream.Flush(true);}
            if(File.Exists(path))File.Replace(temporary,path,path+".bak");else File.Move(temporary,path);return true;
        }catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}
        finally{try{if(File.Exists(temporary))File.Delete(temporary);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
}
