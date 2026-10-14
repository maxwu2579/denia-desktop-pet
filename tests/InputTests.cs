using System;
internal static class InputTests {
    static int count;
    static void Check(bool value,string name){if(!value)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
    static byte[] Packet(int ptr,bool mouse,int key,int flags,int delta){int h=8+2*ptr;byte[] b=new byte[h+(mouse?24:16)];Array.Copy(BitConverter.GetBytes(mouse?0:1),0,b,0,4);Array.Copy(BitConverter.GetBytes(b.Length),0,b,4,4);b[8]=1;
        if(mouse){Array.Copy(BitConverter.GetBytes((ushort)flags),0,b,h+4,2);Array.Copy(BitConverter.GetBytes((short)delta),0,b,h+6,2);}else{b[h]=30;Array.Copy(BitConverter.GetBytes((ushort)flags),0,b,h+2,2);Array.Copy(BitConverter.GetBytes((ushort)key),0,b,h+6,2);}return b;}
    public static int Main(){try{var e=new InputEngine();IntPtr a=new IntPtr(1),b=new IntPtr(2);
        e.Key(a,65,true,0);e.Key(a,65,false,5);Check(e.Snapshot(33).KeyDown,"5ms tap survives next render");Check(!e.Snapshot(100).KeyDown,"tap releases after minimum visual pulse");
        e.Key(a,65,true,200);e.Key(a,68,true,201);Check(e.Snapshot(300).KeyCount==2,"chord retains both keys");Check(e.Snapshot(300).Keys.Length==2&&e.Snapshot(300).Keys[0]==65&&e.Snapshot(300).Keys[1]==68,"feedback identifies both chord keys");e.Key(a,65,false,301);Check(e.Snapshot(400).KeyCount==1,"releasing one key preserves other");
        long edges=e.KeyEdges;e.Key(a,68,true,401);Check(e.KeyEdges==edges,"auto repeat is not another down edge");
        e.Key(b,68,true,402);e.Key(a,68,false,403);Check(e.Snapshot(500).KeyDown,"second keyboard retains held key");e.Remove(b);Check(!e.Snapshot(600).KeyDown,"device removal resets held inputs");
        e.Mouse(a,1,0,700);e.Mouse(a,4,0,701);var s=e.Snapshot(800);Check(s.Left==8&&s.Right==6,"left and right tracked independently");e.Mouse(a,2,0,801);s=e.Snapshot(900);Check(s.Left==0&&s.Right==6,"left release does not release right");
        e.Mouse(a,0x400,120,1000);Check(e.Snapshot(1000).Wheel>0,"wheel up signed correctly");e.Mouse(a,0x400,-240,1010);Check(e.Snapshot(1010).Wheel<0,"wheel down signed correctly");Check(e.Snapshot(1300).Wheel==0,"wheel effect expires");
        e.Mouse(a,0x400,120,1400);Check(e.Snapshot(1400).Wheel>0,"expired wheel direction does not contaminate next scroll");
        e.ReconcileMouse(1400,delegate(int k){return false;});Check(e.Snapshot(1400).Right==0,"missing mouse release reconciles");
        e.ClearMouse();Check(e.Snapshot(1400).Right==0,"disable clears mouse");e.Key(a,65,true,1500);e.Reconcile(1800,delegate(int k){return false;});Check(!e.Snapshot(1800).KeyDown,"missing release reconciles");
        foreach(int ptr in new[]{4,8}){e.Reset();Check(RawDecoder.Decode(Packet(ptr,false,65,0,0),ptr,e,2000)&&e.Snapshot(2000).KeyDown,"keyboard packet ABI "+ptr);Check(RawDecoder.Decode(Packet(ptr,false,65,1,0),ptr,e,2100)&&!e.Snapshot(2200).KeyDown,"key up ABI "+ptr);Check(RawDecoder.Decode(Packet(ptr,true,0,0x400,-120),ptr,e,2300)&&e.Snapshot(2300).Wheel<0,"mouse wheel ABI "+ptr);}
        Check(!RawDecoder.Decode(new byte[3],8,e,0),"truncated packet rejected");e.Reset();Check(e.Snapshot(9999).KeyCount==0&&e.Snapshot(9999).Wheel==0,"session reset clears state");
        Console.WriteLine("PASSED "+count+" input checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
