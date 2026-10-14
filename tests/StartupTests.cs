using System;
using Microsoft.Win32;
internal static class StartupTests {
    static int checks;static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
    public static int Main(){string path=@"Software\DeniaDesktopPet\Tests\Startup-"+Guid.NewGuid().ToString("N");try{
        var production=new RegistryStartup();string before=production.Read();var test=new RegistryStartup(path);string error;
        Check(!test.Configured,"absent entry defaults off");
        Check(test.Set(false,null,out error),"disable absent entry succeeds");
        Check(Registry.CurrentUser.OpenSubKey(path)==null,"disabled default does not create registry key");
        string first=@"C:\桌宠 测试\DeniaPet.exe";Check(test.Set(true,first,out error),"write isolated current-user entry");
        Check(test.Read()=="\""+first+"\" --autostart"&&test.Configured,"quoted absolute unicode path and autostart flag");
        using(var key=Registry.CurrentUser.OpenSubKey(path,true)){key.SetValue("OtherTestEntry","keep");}
        string moved=@"D:\Pets New\DeniaPet.exe";Check(test.Set(true,moved,out error)&&test.Read()==RegistryStartup.Command(moved),"reapply updates moved folder path");
        Check(test.Set(false,null,out error)&&!test.Configured,"disable removes own value");
        using(var key=Registry.CurrentUser.OpenSubKey(path)){Check((string)key.GetValue("OtherTestEntry")=="keep","other startup values preserved");}
        Check(!test.Set(true,"relative.exe",out error)&&!test.Configured,"invalid relative path rejected");
        Check(!test.Set(true,"C:\\bad\"\\DeniaPet.exe",out error)&&!test.Configured,"embedded quote rejected");
        Check(production.Read()==before,"real Run entry unchanged by tests");
        Console.WriteLine("PASSED "+checks+" startup registration checks; writes only isolated non-Run test key");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}finally{Registry.CurrentUser.DeleteSubKey(path,false);}}
}
