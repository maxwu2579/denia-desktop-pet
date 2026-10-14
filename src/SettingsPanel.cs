using System;
using System.Drawing;
using System.Windows.Forms;
internal sealed class SettingsPanel:Form {
    readonly Func<UserPreferences> current;
    readonly Func<UserPreferences,bool> apply;
    readonly Action resetPosition;
    internal readonly ComboBox SizeChoice=new ComboBox(),FontChoice=new ComboBox(),SensitivityChoice=new ComboBox();
    internal readonly ComboBox BadgeChoice=new ComboBox();
    internal readonly CheckBox Keyboard=new SettingsToggle(),Mouse=new SettingsToggle(),Paused=new SettingsToggle(),Through=new SettingsToggle(),Locked=new SettingsToggle(),AutoHide=new SettingsToggle(),ManualHide=new SettingsToggle();
    internal readonly CheckBox StartAtLogin=new SettingsToggle();
    internal readonly CheckBox Expressions=new SettingsToggle();
    internal readonly Button ApplyButton=new Button(),DefaultsButton=new Button(),PositionButton=new Button();
    readonly Label status=new Label();bool loading,dirty;
    readonly Font panelFont=new Font("Microsoft YaHei UI",14f,FontStyle.Regular,GraphicsUnit.Pixel);
    internal bool Dirty{get{return dirty;}}
    internal string Status{get{return status.Text;}}
    internal string ApplyError;
    internal SettingsPanel(Func<UserPreferences> getCurrent,Func<UserPreferences,bool> applyValues,Action moveToCorner){
        current=getCurrent;apply=applyValues;resetPosition=moveToCorner;
        Text="设置";Font=panelFont;AutoScaleMode=AutoScaleMode.None;
        StartPosition=FormStartPosition.CenterScreen;FormBorderStyle=FormBorderStyle.Sizable;ShowIcon=false;MinimizeBox=true;MaximizeBox=false;
        ClientSize=new Size(460,Math.Min(770,Math.Max(400,Screen.PrimaryScreen.WorkingArea.Height-130)));MinimumSize=new Size(430,480);
        var footer=new TableLayoutPanel{Dock=DockStyle.Bottom,AutoSize=true,ColumnCount=1,Padding=new Padding(14,6,14,12)};
        status.AutoSize=true;status.Dock=DockStyle.Fill;status.Margin=new Padding(0,0,0,6);footer.Controls.Add(status);
        var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoSize=true,WrapContents=true,FlowDirection=FlowDirection.RightToLeft,Margin=Padding.Empty};
        var close=new Button{Text="关闭"};CompactButton(close,76);close.Click+=delegate{Close();};buttons.Controls.Add(close);
        ApplyButton.Text="应用";CompactButton(ApplyButton,76);ApplyButton.Click+=delegate{ApplyPending();};buttons.Controls.Add(ApplyButton);
        PositionButton.Text="回到右下角";CompactButton(PositionButton,114);PositionButton.Click+=delegate{resetPosition();status.Text=dirty?"位置已恢复；其他修改尚未应用":"位置已恢复";};buttons.Controls.Add(PositionButton);
        DefaultsButton.Text="恢复默认";CompactButton(DefaultsButton,94);DefaultsButton.Click+=delegate{LoadValues(new UserPreferences());MarkDirty();};buttons.Controls.Add(DefaultsButton);footer.Controls.Add(buttons);
        var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(14,12,14,4)};
        var layout=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1};layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(layout);
        var startup=Group(layout,"启动");Toggle(startup,StartAtLogin,"登录 Windows 时启动");
        int hintRow=startup.RowCount++;startup.RowStyles.Add(new RowStyle(SizeType.Absolute,26));var startupHint=new Label{Text="移动文件夹后，重新应用即可更新路径。",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(2,0,2,0)};startup.Controls.Add(startupHint,0,hintRow);startup.SetColumnSpan(startupHint,2);
        var display=Group(layout,"显示");Choice(display,"角色大小",SizeChoice,new[]{"260 px","340 px","420 px"});Choice(display,"提示字号",FontChoice,new[]{"14 px","18 px","22 px"});Choice(display,"鼠标灵敏度",SensitivityChoice,new[]{"0.6 倍","1 倍","1.4 倍"});
        Choice(display,"提示文字",BadgeChoice,new[]{"始终显示","按键时显示","隐藏文字"});
        var interaction=Group(layout,"键鼠与操作");Toggle(interaction,Keyboard,"键盘联动");Toggle(interaction,Mouse,"鼠标联动");Toggle(interaction,Paused,"暂停动画");Toggle(interaction,Through,"鼠标穿透");Toggle(interaction,Locked,"锁定位置");
        Toggle(interaction,Expressions,"表情反馈");
        var visibility=Group(layout,"隐藏行为");Toggle(visibility,AutoHide,"全屏时自动隐藏");Toggle(visibility,ManualHide,"手动隐藏（退出全屏后仍保持隐藏）");
        var help=new Label{AutoSize=true,Dock=DockStyle.Fill,Text="鼠标穿透后，从托盘打开设置。",Margin=new Padding(4,6,4,6)};layout.Controls.Add(help);
        Controls.Add(scroll);Controls.Add(footer);AcceptButton=ApplyButton;CancelButton=close;
        LoadValues(current());Activated+=delegate{RefreshIfClean();};
    }
    static void CompactButton(Button button,int width){button.AutoSize=false;button.Size=new Size(width,32);button.Margin=new Padding(4,2,0,2);}
    TableLayoutPanel Group(TableLayoutPanel layout,string title){
        var box=new GroupBox{Text=title,Dock=DockStyle.Top,AutoSize=true,Padding=new Padding(10,18,10,8),Margin=new Padding(0,0,0,10)};
        var table=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=2};table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,38));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,62));box.Controls.Add(table);layout.Controls.Add(box);return table;
    }
    void Choice(TableLayoutPanel table,string name,ComboBox control,string[] options){
        int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Absolute,36));var label=new Label{Text=name,AutoSize=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Margin=new Padding(2,4,6,4)};table.Controls.Add(label,0,row);
        control.DropDownStyle=ComboBoxStyle.DropDownList;control.Items.AddRange(options);control.Dock=DockStyle.Fill;control.Margin=new Padding(2,4,2,4);control.SelectedIndexChanged+=delegate{MarkDirty();};table.Controls.Add(control,1,row);
    }
    void Toggle(TableLayoutPanel table,CheckBox control,string name){int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Absolute,32));control.Text=name;control.AutoSize=true;control.Margin=new Padding(2,4,2,4);control.CheckedChanged+=delegate{MarkDirty();};table.Controls.Add(control,0,row);table.SetColumnSpan(control,2);}
    void MarkDirty(){if(loading)return;dirty=true;status.Text="有未应用的修改";}
    internal void LoadValues(UserPreferences value){
        loading=true;try{SizeChoice.SelectedIndex=value.Width==260?0:value.Width==420?2:1;FontChoice.SelectedIndex=value.FontSize==14?0:value.FontSize==22?2:1;SensitivityChoice.SelectedIndex=value.Sensitivity<.8f?0:value.Sensitivity>1.2f?2:1;
            BadgeChoice.SelectedIndex=value.BadgeMode>=0&&value.BadgeMode<=2?value.BadgeMode:1;
            Expressions.Checked=value.Expressions;
            Keyboard.Checked=value.Keyboard;Mouse.Checked=value.Mouse;Paused.Checked=value.Paused;Through.Checked=value.ClickThrough;Locked.Checked=value.PositionLocked;AutoHide.Checked=value.FullscreenHide;ManualHide.Checked=value.ManualHidden;StartAtLogin.Checked=value.StartAtLogin;
        }finally{loading=false;}dirty=false;status.Text="当前设置已加载";
    }
    internal UserPreferences ReadValues(){return new UserPreferences{Width=new[]{260,340,420}[SizeChoice.SelectedIndex],FontSize=new[]{14,18,22}[FontChoice.SelectedIndex],Sensitivity=new[]{.6f,1,1.4f}[SensitivityChoice.SelectedIndex],Keyboard=Keyboard.Checked,Mouse=Mouse.Checked,Paused=Paused.Checked,ClickThrough=Through.Checked,PositionLocked=Locked.Checked,FullscreenHide=AutoHide.Checked,ManualHidden=ManualHide.Checked,StartAtLogin=StartAtLogin.Checked,BadgeMode=BadgeChoice.SelectedIndex,Expressions=Expressions.Checked};}
    internal void ApplyPending(){ApplyError=null;bool saved=apply(ReadValues());dirty=!saved;status.Text=saved?"已应用并保存":ApplyError??"已应用，但保存失败；请重试";}
    internal void RefreshIfClean(){if(!dirty)LoadValues(current());}
    internal void ExternalUpdate(){if(dirty)status.Text="托盘设置已更新；应用将使用面板中的值";else LoadValues(current());}
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)panelFont.Dispose();}
}
internal sealed class SettingsToggle:CheckBox {
    internal SettingsToggle(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
    public override Size GetPreferredSize(Size proposedSize){Size text=TextRenderer.MeasureText(Text,Font,Size.Empty,TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);return new Size(text.Width+28,Math.Max(24,text.Height+4));}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(BackColor);Rectangle box=new Rectangle(1,(Height-16)/2,16,16);
        using(var fill=new SolidBrush(Checked?Color.FromArgb(53,112,191):Color.White))using(var edge=new Pen(Enabled?Color.FromArgb(90,100,115):Color.Gray)){
            e.Graphics.FillRectangle(fill,box);e.Graphics.DrawRectangle(edge,box);
        }
        if(Checked)using(var pen=new Pen(Color.White,2)){e.Graphics.DrawLines(pen,new[]{new Point(box.Left+3,box.Top+8),new Point(box.Left+6,box.Top+11),new Point(box.Left+13,box.Top+4)});}
        TextRenderer.DrawText(e.Graphics,Text,Font,new Rectangle(26,0,Math.Max(0,Width-26),Height),Enabled?ForeColor:Color.Gray,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.SingleLine);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(24,1,Math.Max(0,Width-25),Height-2));
    }
}
