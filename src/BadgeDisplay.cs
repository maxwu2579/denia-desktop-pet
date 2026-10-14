using System;
internal sealed class BadgeDisplay {
    internal const int Always=0,OnInput=1,Hidden=2;
    internal int Mode=OnInput;
    InputSnapshot last=new InputSnapshot();long lastInput=-1;
    internal static bool Active(InputSnapshot input){return input.Keys.Length>0||input.Left>0||input.Right>0||input.Middle>0||Math.Abs(input.Wheel)>.05;}
    internal void Clear(){last=new InputSnapshot();lastInput=-1;}
    internal void SetMode(int mode){Mode=mode>=0&&mode<=2?mode:OnInput;Clear();}
    internal void Update(InputSnapshot input,long now){
        if(Mode!=OnInput)return;
        if(Active(input)){last=new InputSnapshot{Keys=(int[])input.Keys.Clone(),KeyCount=input.KeyCount,KeyDown=input.KeyDown,KeyOffset=input.KeyOffset,Left=input.Left,Right=input.Right,Middle=input.Middle,Wheel=input.Wheel};lastInput=now;}
    }
    internal InputSnapshot View(InputSnapshot current,long now,out float opacity){
        opacity=Mode==Hidden?0:1;if(Mode!=OnInput)return current;
        if(lastInput<0){opacity=0;return last;}
        long age=Math.Max(0,now-lastInput);opacity=age<=700?1:age>=1000?0:(1000-age)/300f;return last;
    }
}
