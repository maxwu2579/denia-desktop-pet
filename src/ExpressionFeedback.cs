using System;
using System.Collections.Generic;
internal sealed class ExpressionFeedback {
    readonly Queue<long> keys=new Queue<long>(),clicks=new Queue<long>();
    long previousKeys,previousClicks,focusUntil,happyUntil,lastSwitch=-1000;int current;
    internal void Reset(long keyPresses,long clickPresses){keys.Clear();clicks.Clear();previousKeys=keyPresses;previousClicks=clickPresses;focusUntil=happyUntil=0;current=0;lastSwitch=-1000;}
    static void Record(Queue<long> queue,long count,long now,int window){
        while(queue.Count>0&&now-queue.Peek()>window)queue.Dequeue();
        for(int i=0;i<Math.Min(16,Math.Max(0,count));i++){if(queue.Count>=64)queue.Dequeue();queue.Enqueue(now);}
    }
    internal void Observe(long keyPresses,long clickPresses,long now){
        long newKeys=Math.Max(0,keyPresses-previousKeys),newClicks=Math.Max(0,clickPresses-previousClicks);previousKeys=keyPresses;previousClicks=clickPresses;
        Record(keys,newKeys,now,1000);Record(clicks,newClicks,now,650);
        if(newKeys>0&&keys.Count>=5)focusUntil=now+1200;
        if(newClicks>0&&clicks.Count>=3)happyUntil=now+1300;
    }
    internal int Pick(long now,bool enabled,bool paused,long greetingUntil,long quietMilliseconds){
        if(paused||!enabled){current=0;return paused?0:now<greetingUntil?2:quietMilliseconds>45000||now%4600<300?1:0;}
        int desired=now<greetingUntil||now<happyUntil?2:now<focusUntil?3:0;
        if(desired!=current&&now-lastSwitch>=250){current=desired;lastSwitch=now;}
        return current==0?(quietMilliseconds>45000||now%4600<300?1:0):current;
    }
}
internal sealed class FaceTransition {
    int current,previous;long started=-1000;
    internal void Reset(){current=previous=0;started=-1000;}
    internal void Step(int target,long now,out int from,out float blend){
        if(target!=current){previous=current;current=target;started=now;}
        float t=Math.Max(0,Math.Min(1,(now-started)/160f));blend=t*t*(3-2*t);from=previous;
    }
}
