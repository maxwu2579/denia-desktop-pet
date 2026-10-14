using System;
internal static class TemporaryHideTests {
    static int count;static void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;Console.WriteLine("PASS "+name);}
    public static int Main(){try{
        const long start=100000000000;var timer=new TemporaryHide();Check(!timer.Active&&!timer.Expire(start),"inactive by default");
        foreach(int minutes in new[]{15,30,60}){timer.Start(minutes,start);Check(timer.DeadlineUtcTicks==start+minutes*TimeSpan.TicksPerMinute&&timer.RemainingMinutes(start)==minutes,"duration "+minutes);}
        timer.Start(15,start);Check(!timer.Expire(timer.DeadlineUtcTicks-1),"does not expire early");Check(timer.RemainingMinutes(start+TimeSpan.TicksPerSecond)==15,"remaining minutes round upward");Check(timer.Expire(timer.DeadlineUtcTicks)&&!timer.Active,"expires at deadline");Check(!timer.Expire(long.MaxValue),"expiry fires once");
        timer.Start(15,start);timer.Start(30,start+TimeSpan.TicksPerMinute);Check(timer.RemainingMinutes(start+TimeSpan.TicksPerMinute)==30,"new duration replaces old timer");timer.Cancel();Check(!timer.Active,"early cancel");
        timer.Start(15,start);Check(timer.Expire(start+TimeSpan.TicksPerHour),"late tick after sleep expires");
        bool rejected=false;try{timer.Start(1,start);}catch(ArgumentOutOfRangeException){rejected=true;}Check(rejected&&!timer.Active,"unsupported duration rejected");
        var visibility=new VisibilityPolicy{TemporaryHidden=true};Check(!visibility.ShouldShow,"temporary state hides independently");visibility.ManualHidden=true;visibility.TemporaryHidden=false;Check(!visibility.ShouldShow,"manual hide takes priority after expiry");visibility.ManualHidden=false;visibility.ConfirmFullscreen(true);Check(!visibility.ShouldShow,"fullscreen still blocks restore");visibility.ConfirmFullscreen(false);Check(visibility.ShouldShow,"fullscreen exit allows restore");
        visibility.TemporaryHidden=true;visibility.SetEnabled(false);Check(!visibility.ShouldShow,"disabling fullscreen does not cancel temporary hide");
        Console.WriteLine("PASSED "+count+" temporary hide policy checks");return 0;
    }catch(Exception ex){Console.WriteLine("FAIL "+ex.Message);return 1;}}
}
