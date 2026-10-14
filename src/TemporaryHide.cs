using System;
internal sealed class TemporaryHide {
    internal long DeadlineUtcTicks{get;private set;}
    internal bool Active{get{return DeadlineUtcTicks!=0;}}
    internal void Start(int minutes,long nowUtcTicks){
        if(minutes!=15&&minutes!=30&&minutes!=60)throw new ArgumentOutOfRangeException("minutes");
        DeadlineUtcTicks=checked(nowUtcTicks+minutes*TimeSpan.TicksPerMinute);
    }
    internal void Cancel(){DeadlineUtcTicks=0;}
    internal bool Expire(long nowUtcTicks){if(!Active||nowUtcTicks<DeadlineUtcTicks)return false;Cancel();return true;}
    internal int RemainingMinutes(long nowUtcTicks){return Active?(int)Math.Max(0,Math.Ceiling((DeadlineUtcTicks-nowUtcTicks)/(double)TimeSpan.TicksPerMinute)):0;}
}
