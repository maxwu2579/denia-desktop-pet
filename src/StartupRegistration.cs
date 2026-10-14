using System;
using System.IO;
using System.Security;
using Microsoft.Win32;
internal interface IStartupRegistration {
    bool Configured{get;}
    bool Set(bool enabled,string executable,out string error);
}
internal sealed class RegistryStartup:IStartupRegistration {
    internal const string RunPath=@"Software\Microsoft\Windows\CurrentVersion\Run",ValueName="DeniaDesktopPet";
    readonly string keyPath;
    internal RegistryStartup():this(RunPath){}
    internal RegistryStartup(string isolatedPath){keyPath=isolatedPath;}
    internal static string Command(string executable){
        if(string.IsNullOrEmpty(executable)||executable.IndexOf('"')>=0||!Path.IsPathRooted(executable))throw new ArgumentException("Invalid executable path");
        return "\""+Path.GetFullPath(executable)+"\" --autostart";
    }
    internal string Read(){using(var key=Registry.CurrentUser.OpenSubKey(keyPath,false)){return key==null?null:key.GetValue(ValueName,null,RegistryValueOptions.DoNotExpandEnvironmentNames) as string;}}
    public bool Configured{get{try{return !string.IsNullOrEmpty(Read());}catch(SecurityException){return false;}catch(UnauthorizedAccessException){return false;}catch(IOException){return false;}}}
    public bool Set(bool enabled,string executable,out string error){
        error=null;try{
            string command=enabled?Command(executable):null;
            if(enabled){using(var key=Registry.CurrentUser.CreateSubKey(keyPath)){if(key==null)throw new IOException();key.SetValue(ValueName,command,RegistryValueKind.String);}}
            else using(var key=Registry.CurrentUser.OpenSubKey(keyPath,true)){if(key!=null)key.DeleteValue(ValueName,false);}
            if(enabled?Read()!=command:Read()!=null)throw new IOException();return true;
        }catch(SecurityException){error="无法修改开机启动，请检查当前用户权限。";}
        catch(UnauthorizedAccessException){error="无法修改开机启动，请检查当前用户权限。";}
        catch(IOException){error="开机启动设置未能保存，请重试。";}
        catch(ArgumentException){error="程序路径无效，无法设置开机启动。";}
        return false;
    }
}
internal sealed class MemoryStartup:IStartupRegistration {
    internal bool Enabled,Fail;internal string LastCommand;
    public bool Configured{get{return Enabled;}}
    public bool Set(bool enabled,string executable,out string error){error=Fail?"开机启动设置未能保存，请重试。":null;if(Fail)return false;Enabled=enabled;LastCommand=enabled?RegistryStartup.Command(executable):null;return true;}
}
