using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace FusionAssist {
 static class Store {
  public static readonly string Root=AppDomain.CurrentDomain.BaseDirectory;
  public static string Data{get{return Path.Combine(Root,"data");}}
  public static readonly JavaScriptSerializer Json=new JavaScriptSerializer {MaxJsonLength=8000000};
  public static double Now {get{return (DateTime.UtcNow-new DateTime(1970,1,1)).TotalSeconds;}}
  public static void Write(string name, object value) {
   Directory.CreateDirectory(Data); string path=Path.Combine(Data,name),tmp=path+"."+Guid.NewGuid().ToString("N")+".tmp";
   try {File.WriteAllText(tmp,Json.Serialize(value));
    if(!Native.MoveFileEx(tmp,path,9))throw new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error());
   }finally{try{File.Delete(tmp);}catch{}}
  }
  public static T Read<T>(string name) {return Json.Deserialize<T>(File.ReadAllText(Path.Combine(Data,name)));}
  public static void Delete(string name) {try{File.Delete(Path.Combine(Data,name));}catch{}}
 }
 static class Program {
  [STAThread] static void Main() {
   if(Activation.RestoreExisting(Store.Root,false))return;
   string identity;
   using(var sha=System.Security.Cryptography.SHA256.Create())identity=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Path.GetFullPath(Store.Root).ToUpperInvariant()))).Replace("-","");
   bool created; using(var mutex=new Mutex(true,"Local\\FusionAssist_"+identity,out created)) {
    if(!created) {
     if(Activation.RestoreExisting(Store.Root,false))return;
     Activation.RestoreExisting(Store.Root);
     MessageBox.Show("Save your edits and choose Exit from the running Shortcut Assist tray icon, then open Shortcut Assist again. The X button only hides it.","Restart Shortcut Assist");return;
    }
    Application.EnableVisualStyles(); Native.SetProcessDPIAware(); Application.SetCompatibleTextRenderingDefault(false);
    try {using(var launch=new LaunchContext())Application.Run(launch);}
    catch(Exception ex) {MessageBox.Show(ex.Message,"Shortcut Assist could not start");}
   }
  }
 }
 public partial class MainForm:Form {
  protected override bool ShowWithoutActivation{get{return StartupPending;}}
  Settings settings;
  bool quitting;
  IntPtr target=IntPtr.Zero;
  readonly TriggerGate gate=new TriggerGate();
  readonly Queue<ShortcutSpec> macroSteps=new Queue<ShortcutSpec>();
  double nextMacroStep;
  double lastStatus;
  readonly System.Windows.Forms.Timer timer=new System.Windows.Forms.Timer();
  readonly NotifyIcon tray=new NotifyIcon(); readonly Ring ring;readonly MiniHud mini;
  readonly Icon inactiveIcon=MakeIcon(Color.IndianRed), waitingIcon=MakeIcon(Color.Goldenrod), activeIcon=MakeIcon(UiPalette.Orange);
  Native.WinEvent foregroundCallback; IntPtr foregroundHook;
  public MainForm():this(false) {}
  public MainForm(bool startupPending) {
   StartupPending=startupPending;
   Text=startupPending ? "Shortcut Assist startup host" : WindowTitle; Size=new Size(1250,790); MinimumSize=new Size(990,640);
   StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(18,23,32); ForeColor=Color.White;
   Font=new Font("Segoe UI",10); Icon=(Icon)AppAssets.AppIcon.Clone();
   try {settings=Store.Read<Settings>("settings.json"); if(settings==null || settings.Tools==null) throw new Exception();}
   catch {settings=new Settings();}
   settings.UiTheme=UiPalette.For(settings.UiTheme).Dark?"Dark":"Light";
   var defaults=new Dictionary<string,string>{{"Extrude","E"},{"Fillet","F"},{"Measure","I"},{"MoveCopy","M"},{"PressPull","Q"},{"Hole","H"}};
   foreach(var tool in settings.Tools)if(tool.Shortcut==null) {string shortcut;if(defaults.TryGetValue(tool.Command??"",out shortcut))tool.Shortcut=shortcut;else {tool.Shortcut="";tool.Enabled=false;}}
   settings.Count=Math.Max(1,Math.Min(12,settings.Count));settings.Radius=Math.Max(80,Math.Min(400,settings.Radius));
   InitializeProfiles();
   WheelVisual.Light=LightMode;
   ring=new Ring(this);mini=new MiniHud(this);
   BuildWheelScreen();
   tray.Icon=waitingIcon;tray.Text="Shortcut Assist: Active";tray.Visible=!StartupPending;
   var menu=new ContextMenuStrip(); menu.Items.Add("My Wheel",null,(s,e)=>OpenSettings());
   menu.Items.Add("Exit",null,(s,e)=>Quit());
   tray.ContextMenuStrip=menu;tray.MouseClick+=(s,e)=>{if(e.Button==MouseButtons.Left)OpenSettings();};
   // This hook reports focus changes only; it never receives keys, clicks, or pixels.
   foregroundCallback=(h,ev,w,o,c,t,ms)=>{if(IsHandleCreated && !IsDisposed) BeginInvoke((Action)(()=>{
    if(quitting)return;
    if(w!=target)InvalidateSession();Tick();
   }));};
   foregroundHook=Native.SetWinEventHook(3,3,IntPtr.Zero,foregroundCallback,0,0,0);
   timer.Interval=25;timer.Tick+=(s,e)=>Tick();timer.Start();
   FormClosing+=(s,e)=>{if(!quitting&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;Hide();}};
  }
  static Icon MakeIcon(Color color){return AppAssets.StatusIcon(color);}
  void InvalidateSession(){gate.Reset();macroSteps.Clear();ring.Hide();lastStatus=0;}
  public void OpenSettings(){
   if(StartupPending)return;
   ShowInTaskbar=true;
   Show();
   if(WindowState==FormWindowState.Minimized)WindowState=FormWindowState.Normal;
   BringToFront();
   Activate();
  }
  protected override void WndProc(ref Message message) {
   if(message.Msg==(int)Activation.Message){OpenSettings();return;}base.WndProc(ref message);
  }
  public bool Allowed() {return !StartupPending&&!quitting&&!string.IsNullOrEmpty(settings.Trigger)&&foregroundHook!=IntPtr.Zero&&ActiveProfile!=null&&Native.IsForegroundTarget(target,appFocus.ProfileId,catalog);}
  public Rectangle OverlayBounds() {
   var screen=Screen.FromHandle(target).WorkingArea;Native.Rect rect;
   if(target!=IntPtr.Zero&&Native.GetWindowRect(target,out rect))return Rectangle.Intersect(screen,Rectangle.FromLTRB(rect.Left,rect.Top,rect.Right,rect.Bottom));
   return screen;
  }
  public void LayoutMessage(string message){wheelFooter.Text=message;}
  public Settings CurrentSettings{get{return settings;}}
  bool ReadTrigger() {
   var prefs=RuntimeSettings;
   if(prefs==null)return false;
   bool key=Native.Down(Native.TriggerCode(prefs.Trigger),target,appFocus.ProfileId,catalog);
   return key && TriggerGate.Matches(prefs,key,Native.Down(17,target,appFocus.ProfileId,catalog),Native.Down(18,target,appFocus.ProfileId,catalog),Native.Down(16,target,appFocus.ProfileId,catalog));
  }
  void Tick() {
   if(StartupPending)return;
   if(keyButton.NeedsAttention&&keyButton.Visible&&WindowState!=FormWindowState.Minimized)keyButton.Invalidate();
   ObserveApplication(Native.ForegroundApplication(catalog));
   double now=Store.Now;
   if(now-lastStatus>.4) {
    lastStatus=now;
    string app=ActiveApplicationName;
    bool inactive=string.IsNullOrEmpty(settings.Trigger)||foregroundHook==IntPtr.Zero;
    tray.Text="Shortcut Assist: "+(inactive?"Inactive":app==null?"Active / waiting":app+" / Active");
    tray.Icon=inactive?inactiveIcon:Allowed()?activeIcon:waitingIcon;
    if(catalog.ShowMiniHud) {
     mini.UpdateStatus(target,inactive,ActivationChord());
     mini.KeepAboveWindows();
    }else if(mini.Visible)mini.Hide();
   }
   if(!Allowed()){gate.Reset();macroSteps.Clear();ring.Hide();return;}
   if(macroSteps.Count>0){
    if(now>=nextMacroStep)PlayNextStep();
    return;
   }
   var prefs=RuntimeSettings;
   if(gate.Sample(Allowed,ReadTrigger)) {
    if(ring.Visible)ring.Hide();
    else {
     var tools=Ranking.Wheel(prefs);
     Native.Point cursor;
     if(tools.Any(t=>t.Enabled)&&Allowed()&&Native.GetCursorPos(out cursor)&&Allowed())
      ring.Open(tools,cursor.X,cursor.Y,prefs.Radius);
    }
   }
   if(ring.Visible) {
    if(Native.Down(27,target,appFocus.ProfileId,catalog)||now-ring.Opened>10){ring.Hide();return;}
    Native.Point pointer;
    if(Allowed()&&Native.GetCursorPos(out pointer)&&Allowed()&&ring.DismissAt(new Point(pointer.X,pointer.Y),now))ring.Hide();
   }
  }
  public void Execute(Tool tool) {
   if(!Allowed())return;
   var steps=ShortcutSequence.Parse(tool.Shortcut);
   if(steps==null)return;
   macroSteps.Clear();
   foreach(var step in steps)macroSteps.Enqueue(step);
   ring.Hide();
   PlayNextStep();
  }
  void PlayNextStep() {
   if(!Native.SendChord(macroSteps.Dequeue(),target,appFocus.ProfileId,catalog)){
    macroSteps.Clear();
    LayoutMessage("Playback stopped. Keep the program in focus and release any held modifier keys.");
   }
   nextMacroStep=Store.Now+.085;
  }
  public void Shutdown() {Quit();}
  void Quit() {
   if(quitting)return;
   quitting=true;timer.Stop();ring.Close();mini.Close();
   if(foregroundHook!=IntPtr.Zero)Native.UnhookWinEvent(foregroundHook);
   programsMenu.Dispose();programIcons.Dispose();tray.Visible=false;tray.Dispose();inactiveIcon.Dispose();waitingIcon.Dispose();activeIcon.Dispose();Close();
  }
 }
 public class MiniHud:Form {
  readonly MainForm owner;bool paused,focused,wheelEnabled;string trigger="",appName="";
  HudPlacement placement;
  readonly ToolTip hint=new ToolTip();
  public MiniHud(MainForm parent) {
   owner=parent;Size=new Size(284,40);FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   BackColor=Color.FromArgb(20,31,42);DoubleBuffered=true;Cursor=Cursors.Hand;
   hint.SetToolTip(this,"Click to open Shortcut Assist.");
   try{placement=Store.Read<HudPlacement>("hud-layout.json")??new HudPlacement();}catch{placement=new HudPlacement();}
   FormClosed+=(s,e)=>hint.Dispose();
  }
  protected override bool ShowWithoutActivation{get{return true;}}
  public static string StatusLabel(bool active,bool focused,string name){return !active?"Inactive":focused&&!String.IsNullOrEmpty(name)?name:"Active";}
  public string AnchorPosition{get{return placement.Anchor=="Bottom left"?"Bottom left":"Bottom right";}}
  public void SetAnchor(string anchor){placement.Anchor=anchor=="Bottom left"?"Bottom left":"Bottom right";Persist();}
  void Persist(){try{Store.Write("hud-layout.json",placement);}catch(Exception ex){owner.LayoutMessage("HUD position kept for this session. Could not save it: "+ex.Message);}}
  protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
  protected override void WndProc(ref Message message){if(message.Msg==0x21){message.Result=(IntPtr)3;return;}base.WndProc(ref message);}
  internal void KeepAboveWindows() {
   if(Visible)Native.RaiseTopmostWithoutActivation(Handle);
  }
  public void UpdateStatus(IntPtr fusion,bool isPaused,string key) {
   bool isFocused=fusion!=IntPtr.Zero;
   string name=owner.ActiveApplicationName??"";bool enabled=owner.ActiveWheelEnabled;
   bool changed=paused!=isPaused||focused!=isFocused||trigger!=key||appName!=name||wheelEnabled!=enabled||!Visible;paused=isPaused;focused=isFocused;trigger=key;appName=name;wheelEnabled=enabled;
   var screen=Screen.PrimaryScreen??Screen.AllScreens[0];
   Point desired=OverlayLayout.DesktopHud(screen.WorkingArea,Size,AnchorPosition);
   if(Location!=desired)Location=desired;
   if(!Visible)Show();if(changed){hint.SetToolTip(this,(focused?appName+" / "+trigger+". ":"")+"Click to open Shortcut Assist.");Invalidate();}
  }
  protected override void OnMouseDown(MouseEventArgs e) {
   base.OnMouseDown(e);if(e.Button!=MouseButtons.Left)return;
   owner.OpenSettings();
  }
  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   DrawPreview(e.Graphics,ClientSize,UiPalette.For(owner.LightMode?"Light":"Dark",owner.ActiveProfileId),!paused,focused,appName);
  }
  internal static void DrawPreview(Graphics g,Size size,UiPalette palette,bool active,bool focused,string name) {
   using(var background=new SolidBrush(palette.Surface))g.FillRectangle(background,0,0,size.Width,size.Height);
   AppAssets.DrawMark(g,new Rectangle(8,5,30,30),focused?palette.Accent:palette.Muted);
   using(var font=new Font("Segoe UI",9,FontStyle.Bold))using(var text=new SolidBrush(palette.Ink)) {
    string label=StatusLabel(active,focused,name);
    using(var format=new StringFormat{Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})
     g.DrawString(label,font,text,new RectangleF(43,11,size.Width-78,25),format);
    g.DrawString("...",font,text,size.Width-26,9);
   }
   using(var pen=new Pen(palette.Line))g.DrawRectangle(pen,0,0,size.Width-1,size.Height-1);
  }
 }
 public class Ring:Form {
  readonly MainForm owner; List<Tool> tools=new List<Tool>();HalfWheel wheel;readonly ToolTip hint=new ToolTip();
  int hover=-1;public double Opened;
  readonly DismissalTracker dismissal=new DismissalTracker();Point origin;
  public Ring(MainForm parent) {
   owner=parent;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
   BackColor=Color.Magenta;TransparencyKey=Color.Magenta;DoubleBuffered=true;
   Cursor=Cursors.Hand;FormClosed+=(s,e)=>hint.Dispose();
  }
  protected override bool ShowWithoutActivation {get{return true;}}
  protected override CreateParams CreateParams {get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x80;return p;}}
  protected override void WndProc(ref Message m){if(m.Msg==0x21){m.Result=(IntPtr)3;return;}base.WndProc(ref m);}
  public bool Open(List<Tool> tools,int x,int y,int radius) {
   if(!owner.Allowed())return false;
   var bounds=owner.OverlayBounds();bounds.Inflate(-8,-8);
   Point? location=null;
   for(int fitRadius=radius;fitRadius>=80;fitRadius-=10) {
    Prepare(tools,fitRadius);
    int top=Math.Max(bounds.Top,y-(int)wheel.Center.Y);
    if(Size.Width<=bounds.Width&&top+Height<=bounds.Bottom){location=new Point(Math.Max(bounds.Left,Math.Min(bounds.Right-Width,x-(int)wheel.Center.X)),top);break;}
   }
   if(!location.HasValue)return false;Location=location.Value;
   origin=new Point(x,y);dismissal.Reset();Opened=Store.Now;Show();Invalidate();return true;
  }
  public void Prepare(List<Tool> tools,int radius) {
   this.tools=tools.Take(12).ToList();wheel=new HalfWheel(this.tools.Count,radius);Size=wheel.Size;hover=-1;
  }
  public bool DismissAt(Point cursor,double now){return wheel!=null&&dismissal.Expired(cursor,origin,(wheel.InnerRadius+wheel.Radius)/2.0,now,180);}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(!owner.Allowed()){Hide();return;}int next=HitTool(e.Location);Cursor=next>=0?Cursors.Hand:Cursors.Default;if(next!=hover){hover=next;hint.SetToolTip(this,hover>=0?tools[hover].Label+"  ("+tools[hover].Shortcut+")":"");Invalidate();}}
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=-1;hint.SetToolTip(this,"");Invalidate();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button!=MouseButtons.Left||wheel==null||!owner.Allowed())return;int index=HitTool(e.Location);if(index>=0){Hide();owner.Execute(tools[index]);}}
  internal int HitTool(Point location) {
   int index=wheel==null?-1:wheel.Hit(location);
   return index>=0&&index<tools.Count&&tools[index].Enabled?index:-1;
  }
  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);e.Graphics.Clear(BackColor);if(wheel==null)return;
   var settings=owner.RuntimeSettings??owner.CurrentSettings;
   WheelVisual.Draw(e.Graphics,wheel,tools,settings.AppId,hover,false);
  }
 }
}













