using System;
using System.Drawing;
internal sealed class DampedAxis {
    internal float Value,Velocity;
    internal DampedAxis(float value){Value=value;}
    internal void Step(float target,float dt){
        dt=Math.Max(.001f,Math.Min(.1f,dt));const float omega=24;
        float error=Value-target,j=Velocity+omega*error,e=(float)Math.Exp(-omega*dt);
        Value=target+(error+j*dt)*e;Velocity=(Velocity-omega*j*dt)*e;
    }
}
internal sealed class HandMotion {
    internal const float FingerU=.84f,FingerV=.97f;
    internal const float BaseX=562-182*FingerU,BaseY=480+152*FingerV;
    readonly DampedAxis x=new DampedAxis(BaseX),y=new DampedAxis(BaseY);
    float lastX=BaseX,lastY=BaseY;float releasedFor;
    internal float WristX{get{return(x.Value-BaseX)/FingerV;}}
    internal float WristY{get{return(y.Value-BaseY)/FingerV;}}
    internal float FingerX{get{return x.Value;}}internal float FingerY{get{return y.Value;}}
    internal static bool Contact(int key,out PointF contact){
        float col;int row;contact=new PointF(BaseX,BaseY);
        if(!Feedback.Position(key,out col,out row))return false;
        float span=key==32?3:1;PointF a=Feedback.KeyPoint(col,row),b=Feedback.KeyPoint(col+span-.1f,row+.8f);
        contact=new PointF((a.X+b.X)/2,(a.Y+b.Y)/2);return true;
    }
    internal void Step(InputSnapshot state,float dt){
        float tx=0,ty=0;int count=0;foreach(int key in state.Keys){PointF point;if(Contact(key,out point)){tx+=point.X;ty+=point.Y;count++;}}
        if(count>0){lastX=tx/count;lastY=ty/count;releasedFor=0;x.Step(lastX,dt);y.Step(lastY,dt);}
        else{releasedFor+=dt;bool resting=releasedFor>.25;x.Step(resting?BaseX:lastX,dt);y.Step(resting?BaseY-8:lastY-18,dt);}
    }
    internal void Reset(){x.Value=BaseX;x.Velocity=0;y.Value=BaseY-8;y.Velocity=0;releasedFor=0;lastX=BaseX;lastY=BaseY;}
}
