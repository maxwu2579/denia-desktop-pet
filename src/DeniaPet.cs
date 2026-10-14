using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

public sealed class Pet : Form {
    readonly Bitmap head, blink, happy, focused, body, keyboardHand, mouseHand;
    Bitmap cachedBody,cachedKeyboard,cachedMouse;Bitmap[] cachedFaces;int cacheWidth;
    readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
    readonly System.Diagnostics.Stopwatch clock=System.Diagnostics.Stopwatch.StartNew();
    readonly ContextMenuStrip menu=new ContextMenuStrip();
    readonly NotifyIcon tray=new NotifyIcon();
    readonly VisibilityPolicy visibility=new VisibilityPolicy();
    readonly System.Windows.Forms.Timer fullscreenTimer=new System.Windows.Forms.Timer();
    ToolStripMenuItem manualHiddenItem;
    readonly TemporaryHide temporaryHide=new TemporaryHide();
    readonly System.Windows.Forms.Timer temporaryTimer=new System.Windows.Forms.Timer();
    ToolStripMenuItem temporaryStatus,endTemporaryItem;
    readonly System.Collections.Generic.List<ToolStripMenuItem> temporaryChoices=new System.Collections.Generic.List<ToolStripMenuItem>();
    bool settingsSaveFailed,waitingTemporaryRestore;
    SettingsPanel settingsPanel;
    readonly IStartupRegistration startupRegistration;
    bool updatingMenu,applyingPanel;
    readonly System.Collections.Generic.Dictionary<string,ToolStripMenuItem> toggleItems=new System.Collections.Generic.Dictionary<string,ToolStripMenuItem>();
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    readonly InputEngine inputState=new InputEngine();
    readonly HandMotion keyboardMotion=new HandMotion();
    readonly ExpressionFeedback expressions=new ExpressionFeedback();
    readonly FaceTransition faceTransition=new FaceTransition();
    bool expressionFeedback=true;
    readonly BadgeDisplay badgeDisplay=new BadgeDisplay();
    float reservedLogicalHeight=860;

    long previousAnimationTime;
    RawInputService rawInput;
    InputSnapshot visibleInput=new InputSnapshot();
    float sensitivity=1;
    bool clickThrough,positionLocked;
    bool input=true, mouseInput=true, paused, dragging, moved,sessionLocked,suspended,closing;
    Point dragStart, startPosition;
    float mx,my,lean,keyX,keyY,mousePress,mouseRight,mouseMiddle,wheelMotion;
    long greetUntil,lastActive;
    int width=340,ticks,renders;
    readonly bool smoke;
    readonly bool preview;
    readonly bool verifyInput;
    readonly bool performanceSmoke;
    readonly bool interactionSmoke,fullscreenSmoke,settingsSmoke;
    System.Windows.Forms.Timer performanceController;
    public Pet(bool test,bool offline,bool checkInput=false,bool performance=false,bool interaction=false,bool fullscreenTest=false,bool settingsTest=false){
        smoke=test;preview=offline;startupRegistration=(smoke||preview)?(IStartupRegistration)new MemoryStartup():new RegistryStartup();verifyInput=checkInput;performanceSmoke=performance;interactionSmoke=interaction;fullscreenSmoke=fullscreenTest;settingsSmoke=settingsTest;
        head=LoadAsset("head");blink=LoadAsset("blink");happy=LoadAsset("happy");body=LoadAsset("body");keyboardHand=LoadAsset("keyboard-hand");mouseHand=LoadAsset("mouse-hand");
        using(Bitmap generated=LoadAsset("focused")){focused=FaceArtwork.Compose(head,generated);}
        AutoScaleMode=AutoScaleMode.None;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;StartPosition=FormStartPosition.Manual;
        UserPreferences saved=(!smoke&&!preview)?Preferences.Load(Preferences.DefaultPath):new UserPreferences();
        sensitivity=saved.Sensitivity;Feedback.TextSize=saved.FontSize;input=saved.Keyboard;mouseInput=saved.Mouse;paused=saved.Paused;clickThrough=saved.ClickThrough;positionLocked=saved.PositionLocked;
        expressionFeedback=saved.Expressions;badgeDisplay.SetMode(saved.BadgeMode);visibility.Enabled=saved.FullscreenHide;visibility.ManualHidden=saved.ManualHidden;
        inputState.KeyboardEnabled=input&&!paused;inputState.MouseEnabled=mouseInput&&!paused;
        Text="达妮娅 · 鼠标键盘伴侣 v18";SetSize(saved.Width);ResetPosition();if(saved.HasPosition){Location=new Point(saved.Left,saved.Top);Clamp();}
        menu.Items.Add("打开设置…",null,delegate{ShowSettings();});menu.Items.Add(new ToolStripSeparator());
        AddToggle("键盘联动",input,delegate(bool value){input=value;UpdateInputSettings();SaveSettings();});
        AddToggle("鼠标联动",mouseInput,delegate(bool value){mouseInput=value;UpdateInputSettings();SaveSettings();});
        AddToggle("暂停动画",paused,delegate(bool value){paused=value;UpdateInputSettings();SaveSettings();});
        AddToggle("鼠标穿透（从托盘关闭）",clickThrough,delegate(bool value){SetInteraction(value,positionLocked);SaveSettings();});
        AddToggle("锁定位置",positionLocked,delegate(bool value){SetInteraction(clickThrough,value);SaveSettings();});
        var sizes=new ToolStripMenuItem("角色大小");
        foreach(int n in new int[]{260,340,420}){int size=n;sizes.DropDownItems.Add(n+" px",null,delegate{SetSize(size);Clamp();DrawWindow();SaveSettings();});}
        menu.Items.Add(sizes);menu.Items.Add("回到右下角",null,delegate{ResetPosition();DrawWindow();SaveSettings();});
        var sensitivities=new ToolStripMenuItem("鼠标灵敏度");
        foreach(float value in new float[]{.6f,1,1.4f}){float selected=value;sensitivities.DropDownItems.Add(value+"x",null,delegate{sensitivity=selected;SaveSettings();});}menu.Items.Add(sensitivities);
        var textSizes=new ToolStripMenuItem("提示字号");
        foreach(int value in new[]{14,18,22}){int chosen=value;textSizes.DropDownItems.Add(value+" px",null,delegate{Feedback.TextSize=chosen;SetSize(width);Clamp();DrawWindow();SaveSettings();});}menu.Items.Add(textSizes);
        AddToggle("全屏时自动隐藏",visibility.Enabled,delegate(bool value){visibility.SetEnabled(value);UpdateFullscreenTimer();ApplyVisibility();SaveSettings();});
        manualHiddenItem=new ToolStripMenuItem("手动隐藏");manualHiddenItem.CheckOnClick=true;manualHiddenItem.Checked=visibility.ManualHidden;
        manualHiddenItem.CheckedChanged+=delegate{if(updatingMenu)return;visibility.ManualHidden=manualHiddenItem.Checked;if(visibility.ManualHidden)CancelTemporary();ApplyVisibility();SaveSettings();};menu.Items.Add(manualHiddenItem);
        var temporaryMenu=new ToolStripMenuItem("临时隐藏");
        foreach(int duration in new[]{15,30,60}){int minutes=duration;var choice=new ToolStripMenuItem(minutes+" 分钟");choice.Click+=delegate{StartTemporary(minutes);};temporaryChoices.Add(choice);temporaryMenu.DropDownItems.Add(choice);}
        temporaryMenu.DropDownItems.Add(new ToolStripSeparator());
        endTemporaryItem=new ToolStripMenuItem("结束临时隐藏");endTemporaryItem.Click+=delegate{EndTemporary(false);};temporaryMenu.DropDownItems.Add(endTemporaryItem);
        temporaryStatus=new ToolStripMenuItem("当前未计时");temporaryStatus.Enabled=false;temporaryMenu.DropDownItems.Add(temporaryStatus);menu.Items.Add(temporaryMenu);
        menu.Items.Add(new ToolStripSeparator());menu.Items.Add("退出",null,delegate{Close();});
        tray.Icon=SystemIcons.Information;tray.Text="达妮娅 v18 · 双击显示，右键设置";tray.ContextMenuStrip=menu;
        tray.DoubleClick+=delegate{EndTemporary(true);};tray.Visible=!smoke&&!preview;
        timer.Interval=33;timer.Tick+=Tick;fullscreenTimer.Interval=500;fullscreenTimer.Tick+=delegate{PollFullscreen();};temporaryTimer.Interval=1000;temporaryTimer.Tick+=delegate{ProcessTemporary(DateTime.UtcNow.Ticks);};UpdateTemporaryMenu();
    }
    Bitmap LoadAsset(string name){return new Bitmap(Path.Combine(root,"assets",name+".png"));}
    void ClearInputVisual(){expressions.Reset(inputState.TypingPresses,inputState.ClickPresses);faceTransition.Reset();badgeDisplay.Clear();inputState.Reset();visibleInput=new InputSnapshot();keyboardMotion.Reset();mousePress=mouseRight=mouseMiddle=wheelMotion=keyX=0;keyY=-10;previousAnimationTime=0;}
    void UpdateInputSettings(){inputState.KeyboardEnabled=input&&!paused;inputState.MouseEnabled=mouseInput&&!paused;ClearInputVisual();DrawWindow();Wake();}
    UserPreferences CaptureSettings(){return new UserPreferences{Expressions=expressionFeedback,BadgeMode=badgeDisplay.Mode,StartAtLogin=startupRegistration.Configured,Width=width,FontSize=Feedback.TextSize,Sensitivity=sensitivity,Keyboard=input,Mouse=mouseInput,Paused=paused,ClickThrough=clickThrough,PositionLocked=positionLocked,FullscreenHide=visibility.Enabled,ManualHidden=visibility.ManualHidden,HasPosition=true,Left=Left,Top=Top};}
    bool SaveSettings(){bool saved=smoke||preview||Preferences.Save(Preferences.DefaultPath,CaptureSettings());settingsSaveFailed=!saved;UpdateTemporaryMenu();if(!applyingPanel&&settingsPanel!=null&&!settingsPanel.IsDisposed)settingsPanel.ExternalUpdate();return saved;}
    void ShowSettings(){
        if(settingsPanel==null||settingsPanel.IsDisposed){settingsPanel=new SettingsPanel(CaptureSettings,ApplyPanel,delegate{ResetPosition();DrawWindow();SaveSettings();});settingsPanel.FormClosed+=delegate{settingsPanel=null;};settingsPanel.Show();}
        else{if(settingsPanel.WindowState==FormWindowState.Minimized)settingsPanel.WindowState=FormWindowState.Normal;settingsPanel.Activate();}
    }
    bool ApplyPanel(UserPreferences values){
        values.Normalize();applyingPanel=true;
        try{
            string startupError;
            if((values.StartAtLogin||startupRegistration.Configured)&&!startupRegistration.Set(values.StartAtLogin,Application.ExecutablePath,out startupError)){
                if(settingsPanel!=null)settingsPanel.ApplyError=startupError;return false;
            }
            Feedback.TextSize=values.FontSize;int bottom=Bottom;SetSize(values.Width);Top=bottom-Height;Clamp();
            expressionFeedback=values.Expressions;badgeDisplay.SetMode(values.BadgeMode);sensitivity=values.Sensitivity;input=values.Keyboard;mouseInput=values.Mouse;paused=values.Paused;
            SetInteraction(values.ClickThrough,values.PositionLocked);
            if(visibility.Enabled!=values.FullscreenHide)visibility.SetEnabled(values.FullscreenHide);
            visibility.ManualHidden=values.ManualHidden;if(visibility.ManualHidden)CancelTemporary();
            updatingMenu=true;try{toggleItems["键盘联动"].Checked=input;toggleItems["鼠标联动"].Checked=mouseInput;toggleItems["暂停动画"].Checked=paused;toggleItems["鼠标穿透（从托盘关闭）"].Checked=clickThrough;toggleItems["锁定位置"].Checked=positionLocked;toggleItems["全屏时自动隐藏"].Checked=visibility.Enabled;manualHiddenItem.Checked=visibility.ManualHidden;}finally{updatingMenu=false;}
            UpdateFullscreenTimer();ApplyVisibility();UpdateInputSettings();return SaveSettings();
        }finally{applyingPanel=false;}
    }

    protected override void OnFormClosing(FormClosingEventArgs e){base.OnFormClosing(e);if(!e.Cancel){SaveSettings();closing=true;timer.Stop();fullscreenTimer.Stop();temporaryTimer.Stop();}}
    void UpdateTemporaryMenu(){
        bool active=temporaryHide.Active;
        foreach(var item in temporaryChoices)item.Enabled=!visibility.ManualHidden;
        if(endTemporaryItem!=null)endTemporaryItem.Enabled=active||waitingTemporaryRestore;
        int minutes=temporaryHide.RemainingMinutes(DateTime.UtcNow.Ticks);
        if(temporaryStatus!=null)temporaryStatus.Text=active?"剩余约 "+minutes+" 分钟":waitingTemporaryRestore?"已结束，等待恢复":visibility.ManualHidden?"手动隐藏中；取消后可临时隐藏":"当前未计时";
        tray.Text=settingsSaveFailed?"达妮娅 v18 · 设置保存失败":active?"达妮娅 v18 · 临时隐藏，剩余 "+minutes+" 分钟":waitingTemporaryRestore?"达妮娅 v18 · 临时隐藏已结束，等待恢复":"达妮娅 v18 · 双击显示，右键设置";
    }
    void StartTemporary(int minutes){
        if(visibility.ManualHidden)return;
        waitingTemporaryRestore=false;temporaryHide.Start(minutes,DateTime.UtcNow.Ticks);visibility.TemporaryHidden=true;temporaryTimer.Start();UpdateTemporaryMenu();ApplyVisibility();
    }
    void CancelTemporary(){waitingTemporaryRestore=false;temporaryHide.Cancel();visibility.TemporaryHidden=false;temporaryTimer.Stop();UpdateTemporaryMenu();}
    void EndTemporary(bool clearManual){
        bool needed=temporaryHide.Active||waitingTemporaryRestore||(clearManual&&visibility.ManualHidden);
        if(!needed){RefreshFullscreenBeforeRestore();ApplyVisibility();return;}
        CancelTemporary();waitingTemporaryRestore=true;visibility.TemporaryHidden=true;
        if(clearManual)manualHiddenItem.Checked=false;ProcessTemporary(DateTime.UtcNow.Ticks);ApplyVisibility();
    }
    bool RefreshFullscreenBeforeRestore(){
        if(smoke||preview||!visibility.Enabled)return true;
        if(sessionLocked||suspended||menu.Visible||(settingsPanel!=null&&settingsPanel.Visible&&settingsPanel.WindowState!=FormWindowState.Minimized))return false;
        bool fullscreen;if(!FullscreenProbe.Read(Handle,Bounds,out fullscreen))return false;visibility.ConfirmFullscreen(fullscreen);return true;
    }
    void ProcessTemporary(long nowUtcTicks){
        if(temporaryHide.Expire(nowUtcTicks))waitingTemporaryRestore=true;
        bool restored=waitingTemporaryRestore&&!sessionLocked&&!suspended&&RefreshFullscreenBeforeRestore();if(restored)waitingTemporaryRestore=false;
        visibility.TemporaryHidden=temporaryHide.Active||waitingTemporaryRestore;
        if(visibility.TemporaryHidden)temporaryTimer.Start();else temporaryTimer.Stop();UpdateTemporaryMenu();
        if(restored)ApplyVisibility();
    }
    void ApplyVisibility(){
        if(closing||IsDisposed||!IsHandleCreated)return;
        bool show=visibility.ShouldShow;
        if(show!=Visible){if(show)Show();else Hide();}
    }
    void UpdateFullscreenTimer(){
        if(!smoke&&!preview&&visibility.Enabled&&!closing&&IsHandleCreated)fullscreenTimer.Start();else fullscreenTimer.Stop();
    }
    void PollFullscreen(){
        if(closing||sessionLocked||suspended||menu.Visible||(settingsPanel!=null&&settingsPanel.Visible&&settingsPanel.WindowState!=FormWindowState.Minimized))return;
        bool fullscreen;if(FullscreenProbe.Read(Handle,Bounds,out fullscreen)){
            if(waitingTemporaryRestore){visibility.ConfirmFullscreen(fullscreen);waitingTemporaryRestore=false;visibility.TemporaryHidden=temporaryHide.Active;if(!temporaryHide.Active)temporaryTimer.Stop();UpdateTemporaryMenu();}
            else visibility.Sample(fullscreen);ApplyVisibility();
        }
    }
    void UpdateSchedule(){if(timer==null||clock==null||closing||preview)return;int interval=!Visible||paused||sessionLocked||suspended?0:smoke&&!performanceSmoke?33:DesktopPolicy.Interval(clock.ElapsedMilliseconds-lastActive,Visible,paused,sessionLocked,suspended);if(interval==0){timer.Stop();return;}if(timer.Interval!=interval)timer.Interval=interval;if(!timer.Enabled)timer.Start();}
    void Wake(){lastActive=clock.ElapsedMilliseconds;UpdateSchedule();}
    protected override void OnVisibleChanged(EventArgs e){base.OnVisibleChanged(e);if(!IsHandleCreated||closing)return;ClearInputVisual();if(Visible){Wake();Clamp();DrawWindow();}else UpdateSchedule();}
    void DesktopChanged(){if(closing||IsDisposed)return;SetSize(width);Clamp();if(Visible&&!sessionLocked&&!suspended)DrawWindow();SaveSettings();Wake();}
    void SessionChanged(int change){ClearInputVisual();if(change==7||change==2||change==4||change==6)sessionLocked=true;else if(change==8||change==1||change==3||change==5)sessionLocked=false;if(!sessionLocked){ProcessTemporary(DateTime.UtcNow.Ticks);RefreshFullscreenBeforeRestore();ApplyVisibility();if(Visible)DrawWindow();}Wake();}
    void StartPerformanceSmoke(){
        string[] names={"active","idle","hidden","sleep","paused","locked","resumed"};int[] durations={1200,2000,1200,2000,1000,1200,1200};
        int phase=0,before=renders;long started=clock.ElapsedMilliseconds;double cpu=System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
        var rates=new double[7];var counts=new int[7];var records=new System.Collections.Generic.List<string>();
        performanceController=new System.Windows.Forms.Timer();performanceController.Interval=50;
        performanceController.Tick+=delegate{
            long now=clock.ElapsedMilliseconds;
            if(phase==0||phase==6){lastActive=now;inputState.Reset();inputState.Key(new IntPtr(99),new[]{81,80,162,32}[(now/400)%4],true,now);}
            if(now-started<durations[phase])return;
            double seconds=(now-started)/1000.0,newCpu=System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
            counts[phase]=renders-before;rates[phase]=counts[phase]/seconds;
            records.Add("{\"mode\":\""+names[phase]+"\",\"renders\":"+counts[phase]+",\"seconds\":"+seconds.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+",\"cpuMs\":"+(newCpu-cpu).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"}");
            phase++;
            if(phase==names.Length){performanceController.Stop();bool ok=counts[2]==0&&counts[4]==0&&counts[5]==0&&rates[0]>10&&rates[6]>10&&rates[1]<rates[0]/2&&rates[3]<rates[1];
                File.WriteAllText(Path.Combine(root,"performance-smoke-result.json"),"{\"ok\":"+ok.ToString().ToLowerInvariant()+",\"note\":\"Simulated modes; actual window renders and process CPU time\",\"phases\":["+string.Join(",",records.ToArray())+"]}");Close();return;
            }
            if(phase==1){ClearInputVisual();lastActive=now-5000;UpdateSchedule();}
            else if(phase==2)Hide();
            else if(phase==3){Show();lastActive=now-50000;UpdateSchedule();}
            else if(phase==4){paused=true;UpdateInputSettings();}
            else if(phase==5){paused=false;UpdateInputSettings();SessionChanged(7);}
            else if(phase==6)SessionChanged(8);
            started=clock.ElapsedMilliseconds;before=renders;cpu=System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds;
        };performanceController.Start();
    }
    void AddToggle(string text,bool initial,Action<bool> action){var item=new ToolStripMenuItem(text);item.CheckOnClick=true;item.Checked=initial;item.CheckedChanged+=delegate{if(!updatingMenu)action(item.Checked);};toggleItems.Add(text,item);menu.Items.Add(item);}
    void SetInteraction(bool through,bool locked){
        clickThrough=through;positionLocked=locked;
        if(dragging){dragging=false;Capture=false;Clamp();DrawWindow();}
        if(IsHandleCreated){
            int style=Native.GetWindowLong(Handle,-20);
            int updated=clickThrough?style|0x20:style&~0x20;
            // Update only the extended style: keep the HWND and Raw Input registration.
            if(style!=updated){Native.SetWindowLong(Handle,-20,updated);
                if(Native.GetWindowLong(Handle,-20)!=updated)throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());}
        }
    }
    void SetSize(int size){width=size;using(Bitmap measure=new Bitmap(1,1)){measure.SetResolution(96,96);using(Graphics g=Graphics.FromImage(measure)){g.PageUnit=GraphicsUnit.Pixel;reservedLogicalHeight=Feedback.ReservedHeight(g,width);}}ClientSize=new Size(width,(int)Math.Ceiling(width*reservedLogicalHeight/720));}
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x80000|0x08000000|0x80;if(clickThrough)cp.ExStyle|=0x20;return cp;}}
    protected override void WndProc(ref Message m){
        if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
        if(rawInput!=null){
            if(m.Msg==0xff){if(rawInput.Read(m.LParam,clock.ElapsedMilliseconds))Wake();}
            else if(m.Msg==0xfe&&m.WParam.ToInt64()==2){inputState.Remove(m.LParam);Wake();}
            else if(m.Msg==0x2b1)SessionChanged(m.WParam.ToInt32());
        }
        if(m.Msg==0x218){int power=m.WParam.ToInt32();if(power==4){suspended=true;ClearInputVisual();UpdateSchedule();}else if(power==7||power==18){suspended=false;ClearInputVisual();ProcessTemporary(DateTime.UtcNow.Ticks);if(!sessionLocked){RefreshFullscreenBeforeRestore();ApplyVisibility();}Wake();}}
        bool desktopChange=m.Msg==0x7e||m.Msg==0x1a||m.Msg==0x2e0;
        base.WndProc(ref m); // DefWindowProc owns raw-input cleanup; legacy clicks still work.
        if(desktopChange&&!closing&&IsHandleCreated)BeginInvoke(new Action(DesktopChanged));
    }
    protected override void OnShown(EventArgs e){base.OnShown(e);if((!smoke||verifyInput)&&!preview&&rawInput==null){rawInput=new RawInputService(Handle,inputState);rawInput.Start();}if(!smoke&&!preview){UpdateFullscreenTimer();PollFullscreen();ApplyVisibility();}DrawWindow();Wake();if(performanceSmoke)StartPerformanceSmoke();if(interactionSmoke)BeginInvoke(new Action(delegate{TestInteraction();Close();}));if(fullscreenSmoke)BeginInvoke(new Action(delegate{TestFullscreen();Close();}));if(settingsSmoke)BeginInvoke(new Action(delegate{TestSettings();Close();}));}
    protected override void OnHandleDestroyed(EventArgs e){if(rawInput!=null){rawInput.Dispose();rawInput=null;}base.OnHandleDestroyed(e);}
    internal static float Ease(float current,float target){return current+(target-current)*0.23f;}
    internal static float KeyOffset(int key){
        string row="QWERTYUIOP";int i=row.IndexOf((char)key);if(i>=0)return (i-4.5f)*4;
        row="ASDFGHJKL";i=row.IndexOf((char)key);if(i>=0)return (i-4f)*4;
        row="ZXCVBNM";i=row.IndexOf((char)key);if(i>=0)return (i-3f)*4;
        return key==32?0:(key%10-4.5f)*3;
    }
    void Advance(float targetX,float targetY,int key,bool click,bool animate){
        mx=Ease(mx,targetX);my=Ease(my,targetY);lean=Ease(lean,targetX*0.13f);
        keyX=Ease(keyX,key>=0?KeyOffset(key):0);keyY=Ease(keyY,key>=0?10:-3);
        mousePress=Ease(mousePress,click?6:0);
    }
    void AdvanceInput(float x,float y,InputSnapshot state,long timestamp=-1){
        visibleInput=state;
        long now=timestamp>=0?timestamp:clock.ElapsedMilliseconds;badgeDisplay.Update(state,now);expressions.Observe(inputState.TypingPresses,inputState.ClickPresses,now);float dt=preview?1f/30:previousAnimationTime==0?1f/30:(now-previousAnimationTime)/1000f;previousAnimationTime=now;
        keyboardMotion.Step(state,dt);
        mx=Ease(mx,x);my=Ease(my,y);lean=Ease(lean,x*.13f);
        keyX=Ease(keyX,-state.KeyOffset*1.8f);keyY=Ease(keyY,state.KeyDown?20:-10);
        mousePress=Ease(mousePress,state.Left);mouseRight=Ease(mouseRight,state.Right);
        mouseMiddle=Ease(mouseMiddle,state.Middle);wheelMotion=Ease(wheelMotion,state.Wheel*5);
    }
    void Tick(object sender,EventArgs e){
        ticks++;long now=clock.ElapsedMilliseconds;
        if(smoke&&!performanceSmoke){Advance((float)Math.Sin(ticks*.3)*20,(float)Math.Cos(ticks*.3)*10,ticks%10<5?65:-1,ticks%12<6,true);DrawWindow();
            if(ticks>=60){int style=Native.GetWindowLong(Handle,-20);bool styles=(style&0x80000)!=0&&(style&0x08000000)!=0;
                bool logic=Math.Abs(Ease(0,10)-2.3f)<.01&&KeyOffset(81)<KeyOffset(80);
                File.WriteAllText(Path.Combine(root,"smoke-result.json"),"{\"ok\":"+(styles&&logic&&renders>=60&&(!verifyInput||rawInput!=null)?"true":"false")+",\"renderedFrames\":"+renders+",\"inputInterpolation\":"+logic.ToString().ToLowerInvariant()+",\"layeredNoActivate\":"+styles.ToString().ToLowerInvariant()+",\"rawInputRegistered\":"+(rawInput!=null?"true":"false")+"}");Close();}return;}
        if(paused||!Visible||sessionLocked||suspended){UpdateSchedule();return;}
        inputState.Reconcile(now,delegate(int vk){return(Native.GetAsyncKeyState(vk)&0x8000)!=0;});
        inputState.ReconcileMouse(now,delegate(int vk){return(Native.GetAsyncKeyState(vk)&0x8000)!=0;});
        InputSnapshot state=inputState.Snapshot(now);
        Point cursor=Cursor.Position;Rectangle area=Screen.FromPoint(cursor).Bounds;
        float x=mouseInput&&!performanceSmoke?Math.Max(-40,Math.Min(40,((cursor.X-area.Left)/(float)area.Width-.5f)*80*sensitivity)):0;
        float y=mouseInput&&!performanceSmoke?Math.Max(-16,Math.Min(16,((cursor.Y-area.Top)/(float)area.Height-.5f)*32*sensitivity)):0;
        if(state.KeyDown||state.Left>0||state.Right>0||state.Middle>0||state.Wheel!=0||Math.Abs(x-mx)>.5||Math.Abs(y-my)>.5)lastActive=now;
        AdvanceInput(x,y,state);DrawWindow();UpdateSchedule();
    }
    Bitmap Render(int expression,double seconds){
        EnsureArtworkCache();
        Bitmap bitmap=new Bitmap(width,Height,PixelFormat.Format32bppArgb);bitmap.SetResolution(96,96);
        using(Graphics g=Graphics.FromImage(bitmap)){
            g.PageUnit=GraphicsUnit.Pixel;
            g.Clear(Color.Transparent);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.SmoothingMode=SmoothingMode.AntiAlias;
            g.ScaleTransform(width/720f,width/720f);
            // All pixels are generated artwork; transforms animate separate physical parts.
            g.DrawImage(cachedBody,new RectangleF(20,405,680,300));
            Feedback.DrawDesk(g,visibleInput);
            // Sleeve endpoints stay fixed at the shoulders. Only the wrist end moves.
            DrawArm(g,cachedMouse,258,72,480,180,mx*.55f+mouseRight*1.4f,my*.55f+mousePress*1.5f+mouseMiddle-wheelMotion*1.6f);
            DrawArm(g,cachedKeyboard,562,380,480,152,keyboardMotion.WristX,keyboardMotion.WristY);
            var state=g.Save();g.TranslateTransform(395,425);g.RotateTransform(lean*.5f);g.TranslateTransform(-395,-425);
            float breath=paused?0:(float)Math.Sin(seconds*2.3)*1.6f;
            expression=Math.Max(0,Math.Min(3,expression));int from;float blend;faceTransition.Step(expression,(long)(seconds*1000),out from,out blend);
            var faceBounds=new RectangleF(120+mx*.07f,10+my*.1f+breath,550,536);
            if(blend<1){g.DrawImage(cachedFaces[from],faceBounds);using(var attributes=new ImageAttributes()){var matrix=new ColorMatrix();matrix.Matrix33=blend;attributes.SetColorMatrix(matrix);g.DrawImage(cachedFaces[expression],new[]{new PointF(faceBounds.Left,faceBounds.Top),new PointF(faceBounds.Right,faceBounds.Top),new PointF(faceBounds.Left,faceBounds.Bottom)},new RectangleF(0,0,cachedFaces[expression].Width,cachedFaces[expression].Height),GraphicsUnit.Pixel,attributes);}}
            else g.DrawImage(cachedFaces[expression],faceBounds);g.Restore(state);
            float opacity;InputSnapshot badge=badgeDisplay.View(visibleInput,(long)(seconds*1000),out opacity);Feedback.DrawBadges(g,badge,width,opacity);
        }return bitmap;
    }
    Bitmap CacheImage(Bitmap image,int w,int h){Bitmap fitted=new Bitmap(Math.Max(1,w),Math.Max(1,h),PixelFormat.Format32bppArgb);fitted.SetResolution(96,96);using(Graphics g=Graphics.FromImage(fitted)){g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,w,h);}return fitted;}
    void DisposeArtworkCache(){foreach(Bitmap b in new Bitmap[]{cachedBody,cachedKeyboard,cachedMouse})if(b!=null)b.Dispose();if(cachedFaces!=null)foreach(Bitmap b in cachedFaces)if(b!=null)b.Dispose();cachedBody=cachedKeyboard=cachedMouse=null;cachedFaces=null;cacheWidth=0;}
    void EnsureArtworkCache(){
        if(cacheWidth==width&&cachedFaces!=null)return;DisposeArtworkCache();float scale=width/720f;
        cachedBody=CacheImage(body,(int)Math.Ceiling(680*scale),(int)Math.Ceiling(300*scale));
        cachedFaces=new[]{CacheImage(head,(int)Math.Ceiling(550*scale),(int)Math.Ceiling(536*scale)),CacheImage(blink,(int)Math.Ceiling(550*scale),(int)Math.Ceiling(536*scale)),CacheImage(happy,(int)Math.Ceiling(550*scale),(int)Math.Ceiling(536*scale)),CacheImage(focused,(int)Math.Ceiling(550*scale),(int)Math.Ceiling(536*scale))};
        int handWidth=(int)Math.Ceiling(280*scale);cachedKeyboard=CacheImage(keyboardHand,handWidth,(int)Math.Ceiling(handWidth*keyboardHand.Height/(float)keyboardHand.Width));cachedMouse=CacheImage(mouseHand,handWidth,(int)Math.Ceiling(handWidth*mouseHand.Height/(float)mouseHand.Width));cacheWidth=width;
    }
    void DrawArm(Graphics g,Bitmap image,float shoulderX,float outerX,float top,float height,float wristX,float wristY){
        PointF[] corners={new PointF(shoulderX,top),new PointF(outerX,top),new PointF(shoulderX+wristX,top+height+wristY)};
        g.DrawImage(image,corners,new RectangleF(0,0,image.Width,image.Height),GraphicsUnit.Pixel);
    }
    void DrawWindow(){if(!Visible||closing)return;long now=clock.ElapsedMilliseconds;int expression=expressions.Pick(now,expressionFeedback,paused,greetUntil,now-lastActive);
        using(Bitmap measure=new Bitmap(1,1)){measure.SetResolution(96,96);using(Graphics g=Graphics.FromImage(measure)){
            g.PageUnit=GraphicsUnit.Pixel;float opacity;InputSnapshot badge=badgeDisplay.View(visibleInput,now,out opacity);
            reservedLogicalHeight=Math.Max(reservedLogicalHeight,opacity>0?Feedback.RequiredHeight(g,badge,width):860);int needed=(int)Math.Ceiling(reservedLogicalHeight*width/720f);
            if(needed!=Height){ClientSize=new Size(width,needed);Clamp();}
        }}
        using(Bitmap b=Render(expression,now/1000.0)){Present(b);}renders++;
    }
    void Present(Bitmap bitmap){
        IntPtr screen=Native.GetDC(IntPtr.Zero),dc=IntPtr.Zero,hb=IntPtr.Zero,old=IntPtr.Zero;
        try{dc=Native.CreateCompatibleDC(screen);hb=bitmap.GetHbitmap(Color.FromArgb(0));old=Native.SelectObject(dc,hb);
            var dst=new Native.Point(Left,Top);var origin=new Native.Point(0,0);var size=new Native.Size(Width,Height);var blend=new Native.Blend();blend.alpha=255;blend.format=1;
            if(!Native.UpdateLayeredWindow(Handle,screen,ref dst,ref size,dc,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }finally{if(old!=IntPtr.Zero)Native.SelectObject(dc,old);if(hb!=IntPtr.Zero)Native.DeleteObject(hb);if(dc!=IntPtr.Zero)Native.DeleteDC(dc);if(screen!=IntPtr.Zero)Native.ReleaseDC(IntPtr.Zero,screen);}
    }
    void ResetPosition(){Rectangle a=Screen.PrimaryScreen.WorkingArea;Location=new Point(a.Right-Width-24,a.Bottom-Height-12);}
    void Clamp(){Location=DesktopPolicy.Clamp(Bounds,Array.ConvertAll(Screen.AllScreens,delegate(Screen s){return s.WorkingArea;}));}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(clickThrough)return;Wake();if(e.Button==MouseButtons.Right){menu.Show(this,e.Location);return;}if(e.Button==MouseButtons.Left){if(positionLocked){greetUntil=clock.ElapsedMilliseconds+1400;DrawWindow();return;}dragging=true;moved=false;dragStart=Cursor.Position;startPosition=Location;Capture=true;}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!dragging||clickThrough||positionLocked)return;Point p=Cursor.Position;int dx=p.X-dragStart.X,dy=p.Y-dragStart.Y;if(Math.Abs(dx)+Math.Abs(dy)>4)moved=true;Location=new Point(startPosition.X+dx,startPosition.Y+dy);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);if(e.Button!=MouseButtons.Left)return;if(dragging){dragging=false;Capture=false;Clamp();if(!moved)greetUntil=clock.ElapsedMilliseconds+1400;DrawWindow();if(moved)SaveSettings();}}
    protected override void OnMouseCaptureChanged(EventArgs e){base.OnMouseCaptureChanged(e);if(!Capture)dragging=false;}
    protected override void Dispose(bool disposing){if(disposing){if(settingsPanel!=null&&!settingsPanel.IsDisposed)settingsPanel.Close();timer.Stop();timer.Dispose();fullscreenTimer.Stop();fullscreenTimer.Dispose();temporaryTimer.Stop();temporaryTimer.Dispose();if(performanceController!=null){performanceController.Stop();performanceController.Dispose();}if(rawInput!=null){rawInput.Dispose();rawInput=null;}tray.Visible=false;tray.Dispose();menu.Dispose();DisposeArtworkCache();foreach(Bitmap b in new Bitmap[]{head,blink,happy,focused,body,keyboardHand,mouseHand})if(b!=null)b.Dispose();}base.Dispose(disposing);}
    void TestInteraction(){
        var results=new System.Collections.Generic.List<string>();
        Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);results.Add("PASS "+name);};
        IntPtr original=Handle;RawInputService registration=rawInput;int initial=Native.GetWindowLong(Handle,-20);
        check((initial&0x20)==0,"default receives mouse interactions");
        SetInteraction(true,false);
        check((Native.GetWindowLong(Handle,-20)&0x20)!=0,"live layered HWND enables WS_EX_TRANSPARENT");
        check(Handle==original&&rawInput==registration&&registration!=null,"toggle preserves HWND and Raw Input registration");
        Point before=Location;OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,20,20,0));
        check(!dragging&&Location==before,"click through rejects pet drag handlers");
        inputState.Key(new IntPtr(1),65,true,clock.ElapsedMilliseconds);
        check(inputState.Snapshot(clock.ElapsedMilliseconds).KeyDown,"keyboard state still updates while click through");
        SetInteraction(false,true);
        check((Native.GetWindowLong(Handle,-20)&0x20)==0,"tray unlock removes transparent style");
        OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,20,20,0));
        check(!dragging&&Location==before,"position lock prevents drag but permits click expression");
        SetInteraction(false,false);OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,20,20,0));
        check(dragging&&Capture,"unlocked pet starts drag");SetInteraction(true,true);
        check(!dragging&&!Capture,"changing mode releases an ongoing drag");
        for(int i=0;i<20;i++){SetInteraction(false,false);SetInteraction(true,true);}
        check(Handle==original&&rawInput==registration,"repeated toggles preserve input receiver");
        check((Native.GetWindowLong(Handle,-20)&(0x80000|0x08000000|0x80))==(initial&(0x80000|0x08000000|0x80)),"toggles preserve layered noactivate toolwindow styles");
        SetInteraction(false,false);DrawWindow();
        results.Add("PASSED "+results.Count+" interaction checks; window styles and handlers tested, physical cross-application clicks not simulated.");
        File.WriteAllLines(Path.Combine(root,"interaction-test-results.txt"),results.ToArray());
    }
    void TestFullscreen(){
        var records=new System.Collections.Generic.List<string>();Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);records.Add("PASS "+name);};
        Point original=Location;IntPtr handle=Handle;RawInputService receiver=rawInput;fullscreenTimer.Start();
        visibility.Sample(true);ApplyVisibility();check(Visible,"one sample avoids flicker");
        visibility.Sample(true);ApplyVisibility();check(!Visible&&!timer.Enabled,"fullscreen hide stops render timer");check(fullscreenTimer.Enabled,"detection timer continues while hidden");fullscreenTimer.Stop();
        int count=renders;DrawWindow();check(count==renders,"hidden state does not draw");
        manualHiddenItem.Checked=true;visibility.Sample(false);visibility.Sample(false);ApplyVisibility();check(!Visible,"manual hide survives fullscreen exit");
        manualHiddenItem.Checked=false;check(Visible&&Location==original,"manual show restores original position");
        visibility.Sample(true);visibility.Sample(true);ApplyVisibility();visibility.SetEnabled(false);ApplyVisibility();check(Visible,"disabling auto hide restores automatically hidden pet");
        manualHiddenItem.Checked=true;visibility.SetEnabled(false);ApplyVisibility();check(!Visible,"disabling auto hide preserves manual hide");
        manualHiddenItem.Checked=false;visibility.SetEnabled(true);visibility.Sample(true);visibility.Sample(true);ApplyVisibility();visibility.Sample(false);visibility.Sample(false);ApplyVisibility();
        check(Visible&&Location==original&&Handle==handle&&rawInput==receiver&&receiver!=null,"automatic restore preserves position HWND and input receiver");
        using(Form fixture=new Form()){
            fixture.FormBorderStyle=FormBorderStyle.None;fixture.ShowInTaskbar=false;fixture.StartPosition=FormStartPosition.Manual;fixture.Bounds=Screen.FromRectangle(Bounds).Bounds;
            // Create a hidden test HWND; never activate a foreground application.
            IntPtr fixtureHandle=fixture.Handle;bool detected;
            check(FullscreenProbe.ReadWindow(fixtureHandle,Bounds,out detected)&&!detected,"hidden full monitor window excluded");
        }
        records.Add("PASSED "+records.Count+" window checks; fullscreen transitions injected, foreground application not controlled.");File.WriteAllLines(Path.Combine(root,"fullscreen-window-tests.txt"),records.ToArray());
    }
    void TestSettings(){
        var records=new System.Collections.Generic.List<string>();Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);records.Add("PASS "+name);};
        IntPtr original=Handle;RawInputService receiver=rawInput;
        ShowSettings();SettingsPanel first=settingsPanel;ShowSettings();check(settingsPanel==first,"one settings window reused");
        first.SizeChoice.SelectedIndex=2;first.FontChoice.SelectedIndex=2;first.BadgeChoice.SelectedIndex=2;first.SensitivityChoice.SelectedIndex=0;first.Keyboard.Checked=false;first.Mouse.Checked=false;first.Paused.Checked=true;first.Through.Checked=true;first.Locked.Checked=true;first.AutoHide.Checked=false;first.ManualHide.Checked=true;first.StartAtLogin.Checked=true;first.Expressions.Checked=false;
        check(width==340&&!clickThrough&&Visible,"editing controls does not apply changes");
        first.ApplyButton.PerformClick();UserPreferences applied=CaptureSettings();
        check(!applied.Expressions&&applied.BadgeMode==2&&applied.Width==420&&applied.FontSize==22&&Math.Abs(applied.Sensitivity-.6f)<.001,"apply size font sensitivity");
        check(!applied.Keyboard&&!applied.Mouse&&applied.Paused&&applied.ClickThrough&&applied.PositionLocked&&!applied.FullscreenHide&&applied.ManualHidden,"apply all switches");
        check(!Visible&&!timer.Enabled&&(Native.GetWindowLong(Handle,-20)&0x20)!=0,"settings apply hidden paused click through behavior");
        check(toggleItems["暂停动画"].Checked&&toggleItems["锁定位置"].Checked&&manualHiddenItem.Checked&&!toggleItems["键盘联动"].Checked,"panel synchronizes tray controls");
        check(Handle==original&&rawInput==receiver,"apply keeps input receiver and HWND");
        check(startupRegistration.Configured&&applied.StartAtLogin,"panel enables startup registration in isolated provider");
        var startupMemory=(MemoryStartup)startupRegistration;startupMemory.Fail=true;first.StartAtLogin.Checked=false;first.SizeChoice.SelectedIndex=0;first.ApplyPending();
        check(first.Dirty&&first.Status.Contains("开机启动")&&width==420&&startupRegistration.Configured,"startup failure reports and preserves previous settings");
        startupMemory.Fail=false;first.LoadValues(CaptureSettings());
        string file=Path.Combine(root,"settings-roundtrip-"+Guid.NewGuid().ToString("N")+".xml");try{check(Preferences.Save(file,applied),"panel values save to isolated settings file");var restored=Preferences.Load(file);check(!restored.Expressions&&restored.BadgeMode==2&&restored.Width==420&&restored.FontSize==22&&Math.Abs(restored.Sensitivity-.6f)<.001&&!restored.Keyboard&&!restored.Mouse&&restored.Paused&&restored.ClickThrough&&restored.PositionLocked&&!restored.FullscreenHide&&restored.ManualHidden&&restored.StartAtLogin&&restored.HasPosition&&restored.Left==applied.Left&&restored.Top==applied.Top,"panel values and position restore from XML");}finally{if(File.Exists(file))File.Delete(file);}
        first.SizeChoice.SelectedIndex=0;first.Close();check(width==420,"closing discards pending edits");
        ShowSettings();check(settingsPanel!=first&&settingsPanel.SizeChoice.SelectedIndex==2,"reopen reads current values");
        settingsPanel.DefaultsButton.PerformClick();check(width==420&&settingsPanel.Dirty,"defaults stay pending until apply");
        settingsPanel.ApplyButton.PerformClick();check(width==340&&expressionFeedback&&badgeDisplay.Mode==1&&Feedback.TextSize==18&&input&&mouseInput&&!paused&&!clickThrough&&!positionLocked&&visibility.Enabled&&!visibility.ManualHidden&&Visible&&!startupRegistration.Configured,"default values apply and restore visibility");
        toggleItems["键盘联动"].Checked=false;check(!settingsPanel.Keyboard.Checked&&!settingsPanel.Dirty,"tray changes refresh clean panel");
        settingsPanel.FontChoice.SelectedIndex=2;toggleItems["鼠标联动"].Checked=false;check(settingsPanel.Dirty&&settingsPanel.FontChoice.SelectedIndex==2,"tray changes preserve pending edits");
        using(var failedPanel=new SettingsPanel(CaptureSettings,delegate(UserPreferences values){return false;},delegate{})){failedPanel.ApplyPending();check(failedPanel.Dirty&&failedPanel.Status.Contains("保存失败"),"save failure stays pending and is visible");}
        settingsPanel.LoadValues(new UserPreferences());settingsPanel.ApplyPending();
        foreach(CheckBox toggle in new[]{settingsPanel.Keyboard,settingsPanel.Mouse,settingsPanel.Paused,settingsPanel.Through,settingsPanel.Locked,settingsPanel.AutoHide,settingsPanel.ManualHide,settingsPanel.StartAtLogin,settingsPanel.Expressions})check(toggle.Font.Unit==GraphicsUnit.Pixel&&toggle.Width>=toggle.GetPreferredSize(Size.Empty).Width&&toggle.Height>=toggle.GetPreferredSize(Size.Empty).Height,"toggle text fits: "+toggle.Text);
        using(Bitmap screenshot=new Bitmap(settingsPanel.Width,settingsPanel.Height)){settingsPanel.DrawToBitmap(screenshot,new Rectangle(Point.Empty,screenshot.Size));screenshot.Save(Path.Combine(root,"settings-panel-preview.png"));}
        settingsPanel.Close();records.Add("PASSED "+records.Count+" settings window checks; controls driven in process, production settings not written.");File.WriteAllLines(Path.Combine(root,"settings-window-tests.txt"),records.ToArray());
    }
    void TestBadges(){
        var records=new System.Collections.Generic.List<string>();Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);records.Add("PASS "+name);};
        int originalHeight=Height;Point originalPosition=Location;
        badgeDisplay.SetMode(BadgeDisplay.OnInput);var key=new InputSnapshot{Keys=new[]{65,162},KeyDown=true};
        AdvanceInput(0,0,key,0);DrawWindow();check(Height==originalHeight&&Location==originalPosition,"combo uses fixed reserved space");
        AdvanceInput(0,0,new InputSnapshot(),50);DrawWindow();check(Height==originalHeight&&Location==originalPosition,"release does not move or resize pet");
        for(int mode=0;mode<3;mode++){badgeDisplay.SetMode(mode);DrawWindow();check(Height==originalHeight&&Location==originalPosition,"mode switch preserves canvas "+mode);}
        badgeDisplay.SetMode(BadgeDisplay.OnInput);int[] many=new int[80];for(int i=0;i<many.Length;i++)many[i]=i+32;AdvanceInput(0,0,new InputSnapshot{Keys=many,KeyDown=true},0);DrawWindow();int expanded=Height;Point after=Location;
        AdvanceInput(0,0,new InputSnapshot(),2000);badgeDisplay.Clear();DrawWindow();check(Height==expanded&&Location==after,"exceptional large combo expansion never shrinks on release");
        records.Add("PASSED "+records.Count+" actual window canvas checks; input injected in process.");File.WriteAllLines(Path.Combine(root,"badge-window-tests.txt"),records.ToArray());
    }
    void TestTemporary(){
        var records=new System.Collections.Generic.List<string>();Action<bool,string> check=delegate(bool ok,string name){if(!ok)throw new Exception(name);records.Add("PASS "+name);};
        Point position=Location;IntPtr handle=Handle;RawInputService receiver=rawInput;
        visibility.SetEnabled(false);StartTemporary(15);check(!Visible&&!timer.Enabled&&temporaryTimer.Enabled,"temporary timer works with fullscreen feature off and render stopped");
        long deadline=temporaryHide.DeadlineUtcTicks;ProcessTemporary(deadline-1);check(!Visible&&temporaryHide.Active,"not restored before deadline");ProcessTemporary(deadline);
        check(Visible&&!temporaryHide.Active&&!temporaryTimer.Enabled&&Location==position,"expiry restores position and stops timer");
        StartTemporary(30);manualHiddenItem.Checked=true;check(!temporaryHide.Active&&!temporaryTimer.Enabled&&!Visible&&visibility.ManualHidden,"manual hide cancels timed restore");ProcessTemporary(long.MaxValue);check(!Visible,"cancelled timer cannot undo manual hide");
        StartTemporary(15);check(!temporaryHide.Active,"manual hide prevents starting a conflicting timer");manualHiddenItem.Checked=false;
        visibility.SetEnabled(true);visibility.ConfirmFullscreen(true);StartTemporary(15);ProcessTemporary(temporaryHide.DeadlineUtcTicks);check(!Visible&&!temporaryHide.Active,"expiry keeps fullscreen pet hidden");visibility.ConfirmFullscreen(false);ApplyVisibility();check(Visible,"fullscreen exit restores expired temporary hide");
        StartTemporary(15);long earlier=temporaryHide.DeadlineUtcTicks;StartTemporary(60);check(temporaryHide.DeadlineUtcTicks>earlier,"new duration replaces previous deadline");CancelTemporary();ApplyVisibility();check(Visible&&!temporaryTimer.Enabled,"early finish restores visibility");
        StartTemporary(15);sessionLocked=true;ProcessTemporary(temporaryHide.DeadlineUtcTicks);check(!Visible&&!temporaryHide.Active,"locked expiry defers display");SessionChanged(8);check(Visible&&Location==position&&Handle==handle&&rawInput==receiver,"unlock restores same window position and input receiver");
        StartTemporary(15);check(!CaptureSettings().ManualHidden,"temporary state is not saved as permanent manual hide");CancelTemporary();ApplyVisibility();
        StartTemporary(15);sessionLocked=true;ProcessTemporary(temporaryHide.DeadlineUtcTicks);manualHiddenItem.Checked=true;check(!waitingTemporaryRestore&&!temporaryTimer.Enabled,"manual hide also cancels a deferred expiry");SessionChanged(8);check(!Visible,"unlock does not override later manual hide");manualHiddenItem.Checked=false;
        StartTemporary(30);EndTemporary(true);check(Visible&&!temporaryHide.Active&&!waitingTemporaryRestore&&!temporaryTimer.Enabled,"tray show ends temporary hiding");EndTemporary(true);check(Visible&&!temporaryTimer.Enabled,"tray show is idempotent without countdown");
        records.Add("PASSED "+records.Count+" temporary window checks; accelerated UTC deadlines and injected fullscreen/session states.");File.WriteAllLines(Path.Combine(root,"temporary-window-tests.txt"),records.ToArray());
    }
    void TestTemporaryLive(){
        Point position=Location;IntPtr handle=Handle;RawInputService receiver=rawInput;
        visibility.SetEnabled(false);StartTemporary(15);
        // Shorten only the fixture deadline; production menu durations remain 15/30/60 minutes.
        temporaryHide.Start(15,DateTime.UtcNow.Ticks-15*TimeSpan.TicksPerMinute+1500*TimeSpan.TicksPerMillisecond);
        long began=clock.ElapsedMilliseconds;int before=renders;bool quiet=true;
        performanceController=new System.Windows.Forms.Timer();performanceController.Interval=100;
        performanceController.Tick+=delegate{
            if(!Visible&&renders!=before)quiet=false;
            if(clock.ElapsedMilliseconds-began<2600)return;
            bool ok=quiet&&Visible&&!temporaryHide.Active&&!temporaryTimer.Enabled&&Location==position&&Handle==handle&&rawInput==receiver&&receiver!=null;
            File.WriteAllText(Path.Combine(root,"temporary-live-test.json"),"{\"ok\":"+ok.ToString().ToLowerInvariant()+",\"hiddenRenderStopped\":"+quiet.ToString().ToLowerInvariant()+",\"note\":\"Actual 1-second timer; fixture deadline shortened to 1.5 seconds, not a 15-minute elapsed test\"}");
            if(!ok)throw new Exception("Live temporary timer restore failed");Close();
        };performanceController.Start();
    }
    [STAThread]public static int Main(string[] args){
        try{if(!Native.SetProcessDpiAwarenessContext(new IntPtr(-4)))Native.SetProcessDPIAware();}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
        bool temporaryTest=Array.IndexOf(args,"--temporary-smoke-test")>=0;
        bool temporaryLive=Array.IndexOf(args,"--temporary-live-test")>=0;
        bool automaticStart=Array.IndexOf(args,"--autostart")>=0;
        bool badgeTest=Array.IndexOf(args,"--badge-smoke-test")>=0;
        bool settingsTest=Array.IndexOf(args,"--settings-smoke-test")>=0;
        bool fullscreenTest=Array.IndexOf(args,"--fullscreen-smoke-test")>=0;
        bool interactionTest=Array.IndexOf(args,"--interaction-smoke-test")>=0;
        bool checkInput=Array.IndexOf(args,"--input-smoke-test")>=0,performance=Array.IndexOf(args,"--performance-smoke-test")>=0;
        bool expressionPreview=Array.IndexOf(args,"--expression-preview")>=0;
        bool armPreview=Array.IndexOf(args,"--arm-preview")>=0,badgePreview=Array.IndexOf(args,"--badge-preview")>=0;
        bool preview=expressionPreview||badgePreview||armPreview||Array.IndexOf(args,"--preview")>=0,smoke=performance||checkInput||Array.IndexOf(args,"--smoke-test")>=0;
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
        if(temporaryLive){try{using(Pet p=new Pet(true,false,true)){p.Shown+=delegate{p.BeginInvoke(new Action(p.TestTemporaryLive));};Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"temporary-live-test.json"),"FAIL "+ex);return 1;}}
        if(temporaryTest){try{using(Pet p=new Pet(true,false,true)){p.Shown+=delegate{p.BeginInvoke(new Action(delegate{p.TestTemporary();p.Close();}));};Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"temporary-window-tests.txt"),"FAIL "+ex);return 1;}}
        if(badgeTest){try{using(Pet p=new Pet(true,false,true)){p.Shown+=delegate{p.BeginInvoke(new Action(delegate{p.TestBadges();p.Close();}));};Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"badge-window-tests.txt"),"FAIL "+ex);return 1;}}
        if(settingsTest){try{using(Pet p=new Pet(true,false,true,false,false,false,true)){Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"settings-window-tests.txt"),"FAIL "+ex);return 1;}}
        if(fullscreenTest){try{using(Pet p=new Pet(true,false,true,false,false,true)){Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"fullscreen-window-tests.txt"),"FAIL "+ex);return 1;}}
        if(interactionTest){try{using(Pet p=new Pet(true,false,true,false,true)){Application.Run(p);}return 0;}
            catch(Exception ex){File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"interaction-test-results.txt"),"FAIL "+ex);return 1;}}
        if(preview){using(Pet p=new Pet(false,true)){p.SetSize(340);string dir=args[1];Directory.CreateDirectory(dir);if(badgePreview&&args.Length>2){int mode;if(int.TryParse(args[2],out mode))p.badgeDisplay.SetMode(mode);}
                for(int i=0;i<(expressionPreview?300:badgePreview?120:armPreview?240:90);i++){var state=new InputSnapshot();state.Keys=i>=10&&i<32?new[]{65}:i>=32&&i<52?new[]{65,68}:i>=52&&i<65?new[]{65,162}:new int[0];if(armPreview)state.Keys=i<210?new[]{new[]{81,80,49,8,162,32,65}[i/30]}:new int[0];if(expressionPreview){long at=i*1000/30;state.Keys=i>=30&&i<75&&i%5<3?new[]{65+(i/5)%4}:new int[0];if(i>=30&&i<75&&i%5==0){p.inputState.Key(new IntPtr(99),65+(i/5)%4,true,at);p.inputState.Key(new IntPtr(99),65+(i/5)%4,false,at+1);}if(i==150||i==156||i==162){p.inputState.Mouse(new IntPtr(99),1,0,at);p.inputState.Mouse(new IntPtr(99),2,0,at+1);}}if(badgePreview)state.Keys=i>=10&&i<35?new[]{65,162}:new int[0];state.KeyCount=state.Keys.Length;state.KeyDown=state.KeyCount>0;foreach(int key in state.Keys)state.KeyOffset+=InputEngine.Offset(key);if(state.KeyCount>0)state.KeyOffset/=state.KeyCount;state.Left=!expressionPreview&&!badgePreview&&!armPreview&&i>=65&&i<72?8:0;state.Right=!expressionPreview&&!badgePreview&&!armPreview&&i>=72&&i<79?6:0;state.Wheel=expressionPreview||badgePreview||armPreview?0:i>=79&&i<85?1:i>=85?-1:0;p.AdvanceInput((float)Math.Sin(i*.1)*40,(float)Math.Cos(i*.1)*16,state,i*1000/30);
                    using(Bitmap b=p.Render(expressionPreview?p.expressions.Pick(i*1000/30,true,false,0,0):i%70<4?1:i>75?2:0,i/30.0))b.Save(Path.Combine(dir,i.ToString("D3")+".png"),ImageFormat.Png);}}return 0;}
        using(Mutex m=new Mutex(false,smoke?"Local\\DeniaDesktopPetSmoke_v18":"Local\\DeniaDesktopPet")){if(!m.WaitOne(0,false)){if(automaticStart)return 0;MessageBox.Show("达妮娅已经在运行；如果正在升级，请先从托盘退出旧版。");return 0;}
            try{using(Pet p=new Pet(smoke,false,checkInput,performance))Application.Run(p);return 0;}
            catch(Exception ex){if(smoke)File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"smoke-error.txt"),ex.ToString());else MessageBox.Show("无法启动桌宠："+ex.Message);return 1;}
            finally{m.ReleaseMutex();}}
    }
}
