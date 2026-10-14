using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

internal sealed class InputSnapshot {
    internal int KeyCount; internal float KeyOffset, Left, Right, Middle, Wheel;
    internal bool KeyDown;
    internal int[] Keys=new int[0];
}
internal sealed class InputEngine {
    sealed class Device {
        internal readonly HashSet<int> Keys=new HashSet<int>();
        internal readonly Dictionary<int,long> Pulses=new Dictionary<int,long>();
        internal readonly Dictionary<int,long> Since=new Dictionary<int,long>();
        internal int Buttons;internal readonly long[] ButtonPulses=new long[3],ButtonSince=new long[3];
    }
    readonly Dictionary<IntPtr,Device> devices=new Dictionary<IntPtr,Device>();
    internal bool KeyboardEnabled=true,MouseEnabled=true;
    internal long KeyEdges,MouseEdges; // Aggregate counts only; no input history.
    internal long TypingPresses,ClickPresses;
    float wheel;long wheelAt;
    Device Get(IntPtr id){Device d;if(!devices.TryGetValue(id,out d)){d=new Device();devices[id]=d;}return d;}
    internal void Key(IntPtr id,int key,bool down,long now){
        if(!KeyboardEnabled||key<8||key>=255)return;
        Device d=Get(id);
        if(down){if(d.Keys.Add(key)){KeyEdges++;if(key==8||key==9||key==13||key==32||key>=48&&key<=90||key>=96&&key<=111||key>=186&&key<=222)TypingPresses++;d.Pulses[key]=now+90;d.Since[key]=now;}}
        else if(d.Keys.Remove(key)){KeyEdges++;d.Since.Remove(key);}
    }
    internal void Mouse(IntPtr id,ushort flags,short delta,long now){
        if(!MouseEnabled)return;Device d=Get(id);
        for(int i=0;i<3;i++){
            int bit=1<<i,downFlag=1<<(i*2),upFlag=2<<(i*2);
            if((flags&downFlag)!=0&&(d.Buttons&bit)==0){d.Buttons|=bit;d.ButtonPulses[i]=now+90;d.ButtonSince[i]=now;MouseEdges++;ClickPresses++;}
            if((flags&upFlag)!=0&&(d.Buttons&bit)!=0){d.Buttons&=~bit;MouseEdges++;}
        }
        if((flags&0x400)!=0){if(now-wheelAt>=180)wheel=0;wheel=Math.Max(-2,Math.Min(2,wheel+delta/120f));wheelAt=now;MouseEdges++;}
    }
    internal void ClearKeyboard(){foreach(Device d in devices.Values){d.Keys.Clear();d.Pulses.Clear();d.Since.Clear();}}
    internal void ClearMouse(){foreach(Device d in devices.Values){d.Buttons=0;Array.Clear(d.ButtonPulses,0,3);}wheel=0;}
    internal void Reset(){devices.Clear();wheel=0;}
    internal void Remove(IntPtr id){devices.Remove(id);}
    internal void Reconcile(long now,Func<int,bool> isDown){
        foreach(Device d in devices.Values){var stale=new List<int>();foreach(int key in d.Keys)if(now-d.Since[key]>250&&!isDown(key))stale.Add(key);
            foreach(int key in stale){d.Keys.Remove(key);d.Since.Remove(key);d.Pulses.Remove(key);}}
    }
    internal void ReconcileMouse(long now,Func<int,bool> isDown){
        int[] codes={1,2,4};foreach(Device d in devices.Values)for(int i=0;i<3;i++)if((d.Buttons&(1<<i))!=0&&now-d.ButtonSince[i]>250&&!isDown(codes[i])){d.Buttons&=~(1<<i);d.ButtonPulses[i]=0;}
    }
    internal InputSnapshot Snapshot(long now){
        var result=new InputSnapshot();var keys=new HashSet<int>();bool[] buttons=new bool[3];
        foreach(Device d in devices.Values){
            if(KeyboardEnabled){foreach(int key in d.Keys)keys.Add(key);var expired=new List<int>();foreach(var p in d.Pulses)if(p.Value>now)keys.Add(p.Key);else expired.Add(p.Key);foreach(int k in expired)d.Pulses.Remove(k);}
            if(MouseEnabled)for(int i=0;i<3;i++)buttons[i]|=(d.Buttons&(1<<i))!=0||d.ButtonPulses[i]>now;
        }
        result.KeyCount=keys.Count;result.KeyDown=keys.Count>0;
        result.Keys=new int[keys.Count];keys.CopyTo(result.Keys);Array.Sort(result.Keys);
        foreach(int key in keys)result.KeyOffset+=Offset(key);if(keys.Count>0)result.KeyOffset/=keys.Count;
        result.Left=buttons[0]?8:0;result.Right=buttons[1]?6:0;result.Middle=buttons[2]?4:0;
        result.Wheel=MouseEnabled?wheel*Math.Max(0,1-(now-wheelAt)/180f):0;return result;
    }
    internal static float Offset(int key){string[] rows={"QWERTYUIOP","ASDFGHJKL","ZXCVBNM"};foreach(string row in rows){int i=row.IndexOf((char)key);if(i>=0)return(i-(row.Length-1)/2f)*4;}return key==32?0:(key%10-4.5f)*3;}
}

internal static class RawDecoder {
    internal static bool Decode(byte[] packet,int pointerSize,InputEngine engine,long now){
        if(packet==null||(pointerSize!=4&&pointerSize!=8))return false;
        int header=8+2*pointerSize;if(packet.Length<header)return false;
        uint type=BitConverter.ToUInt32(packet,0),size=BitConverter.ToUInt32(packet,4);
        if(size>packet.Length||size<header)return false;
        IntPtr device=pointerSize==8?new IntPtr(BitConverter.ToInt64(packet,8)):new IntPtr(BitConverter.ToInt32(packet,8));
        if(type==1){if(size<header+16)return false;ushort key=BitConverter.ToUInt16(packet,header+6),flags=BitConverter.ToUInt16(packet,header+2),scan=BitConverter.ToUInt16(packet,header);
            if(key==255||scan==255)return true;
            if(key==16)key=(ushort)(scan==0x36?161:160);
            else if(key==17)key=(ushort)((flags&2)!=0?163:162);
            else if(key==18)key=(ushort)((flags&2)!=0?165:164);
            engine.Key(device,key,(flags&1)==0,now);return true;
        }
        if(type==0){if(size<header+24)return false;engine.Mouse(device,BitConverter.ToUInt16(packet,header+4),BitConverter.ToInt16(packet,header+6),now);return true;}
        return false;
    }
}

internal sealed class RawInputService : IDisposable {
    [StructLayout(LayoutKind.Sequential)] struct DeviceRegistration{internal ushort Page,Usage;internal uint Flags;internal IntPtr Target;}
    [DllImport("user32.dll",SetLastError=true)]static extern bool RegisterRawInputDevices(DeviceRegistration[] devices,uint count,uint size);
    [DllImport("user32.dll",SetLastError=true)]static extern uint GetRawInputData(IntPtr handle,uint command,IntPtr data,ref uint size,uint headerSize);
    [DllImport("wtsapi32.dll",SetLastError=true)]static extern bool WTSRegisterSessionNotification(IntPtr hwnd,uint flags);
    [DllImport("wtsapi32.dll")]static extern bool WTSUnRegisterSessionNotification(IntPtr hwnd);
    readonly InputEngine engine;readonly IntPtr hwnd;bool started,session;
    internal RawInputService(IntPtr handle,InputEngine target){hwnd=handle;engine=target;}
    internal void Start(){
        var list=new[]{new DeviceRegistration{Page=1,Usage=2,Flags=0x2100,Target=hwnd},new DeviceRegistration{Page=1,Usage=6,Flags=0x2100,Target=hwnd}};
        if(!RegisterRawInputDevices(list,2,(uint)Marshal.SizeOf(typeof(DeviceRegistration))))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        started=true;session=WTSRegisterSessionNotification(hwnd,0);
        if(!session){int error=Marshal.GetLastWin32Error();Dispose();throw new System.ComponentModel.Win32Exception(error);}
    }
    internal bool Read(IntPtr input,long now){
        uint size=0,header=(uint)(8+2*IntPtr.Size);
        if(GetRawInputData(input,0x10000003,IntPtr.Zero,ref size,header)==uint.MaxValue||size<header||size>1024)return false;
        IntPtr memory=Marshal.AllocHGlobal((int)size);
        try{uint got=GetRawInputData(input,0x10000003,memory,ref size,header);if(got==uint.MaxValue||got!=size)return false;byte[] packet=new byte[size];Marshal.Copy(memory,packet,0,(int)size);if(!RawDecoder.Decode(packet,IntPtr.Size,engine,now))return false;
            uint type=BitConverter.ToUInt32(packet,0);if(type==1)return engine.KeyboardEnabled;
            return type==0&&engine.MouseEnabled&&(BitConverter.ToUInt16(packet,(int)header+4)!=0||BitConverter.ToInt32(packet,(int)header+12)!=0||BitConverter.ToInt32(packet,(int)header+16)!=0);
        }
        finally{Marshal.FreeHGlobal(memory);}
    }
    public void Dispose(){
        if(session){WTSUnRegisterSessionNotification(hwnd);session=false;}
        if(started){RegisterRawInputDevices(new[]{new DeviceRegistration{Page=1,Usage=2,Flags=1},new DeviceRegistration{Page=1,Usage=6,Flags=1}},2,(uint)Marshal.SizeOf(typeof(DeviceRegistration)));started=false;}
        engine.Reset();
    }
}
