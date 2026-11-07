using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FusionAssist {
 public static class Native {
  [DllImport("user32.dll")]
  static extern bool SetWindowPos(IntPtr window,IntPtr insertAfter,int x,int y,int width,int height,uint flags);

  internal static void RaiseTopmostWithoutActivation(IntPtr window) {
   const uint noSize=0x0001, noMove=0x0002, noActivate=0x0010, noOwnerZOrder=0x0200;
   SetWindowPos(window,new IntPtr(-1),0,0,0,0,noSize|noMove|noActivate|noOwnerZOrder);
  }
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
  [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out Point p);
  [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
  [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
  public static void TitleTheme(IntPtr window,string mode) {
   try {var palette=UiPalette.For(mode);int dark=palette.Dark?1:0;DwmSetWindowAttribute(window,20,ref dark,4);
    int caption=System.Drawing.ColorTranslator.ToWin32(palette.Background),text=System.Drawing.ColorTranslator.ToWin32(palette.Ink);
    DwmSetWindowAttribute(window,35,ref caption,4);DwmSetWindowAttribute(window,36,ref text,4);
   }catch{}
  }
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] public static extern bool MoveFileEx(string from,string to,int flags);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern uint GetDriveType(string root);
  internal static bool IsLocalDrivePath(string path) {
   if(String.IsNullOrWhiteSpace(path)||path.Length<3||path[1]!=':'||(path[2]!='\\'&&path[2]!='/'))return false;
   uint kind=GetDriveType(path.Substring(0,2)+"\\");
   return kind==2||kind==3||kind==5||kind==6;
  }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rect);
  [StructLayout(LayoutKind.Sequential)] public struct Rect {public int Left,Top,Right,Bottom;}
  [StructLayout(LayoutKind.Sequential)] struct MouseInput {public int x,y;public uint data,flags,time;public UIntPtr extra;}
  [StructLayout(LayoutKind.Sequential)] struct KeyInput {public ushort key,scan;public uint flags,time;public UIntPtr extra;}
  [StructLayout(LayoutKind.Explicit)] struct InputUnion {[FieldOffset(0)]public MouseInput mouse;[FieldOffset(0)]public KeyInput key;}
  [StructLayout(LayoutKind.Sequential)] struct Input {public uint type;public InputUnion value;}
  [DllImport("user32.dll",SetLastError=true)] static extern uint SendInput(uint count,Input[] inputs,int size);
  public static bool SendShortcut(string text,IntPtr target,string profileId=null,ProfileCatalog catalog=null) {
   return SendChord(ShortcutSpec.Parse(text),target,profileId,catalog);
  }
  internal static bool SendChord(ShortcutSpec spec,IntPtr target,string profileId,ProfileCatalog catalog) {
   if(spec==null||!IsForegroundTarget(target,profileId,catalog))return false;
   // Do not release a modifier the user is physically holding.
   foreach(int key in new[]{16,17,18,91,92})if(Down(key,target,profileId,catalog))return false;
   var inputs=new System.Collections.Generic.List<Input>();
   Action<int,bool> add=(key,up)=>{
    bool extended=(key>=33&&key<=40)||key==45||key==46||key==111;
    uint flags=(extended?1u:0u)|(up?2u:0u);
    inputs.Add(new Input{type=1,value=new InputUnion{key=new KeyInput{key=(ushort)key,flags=flags}}});
   };
   foreach(int key in spec.Modifiers)add(key,false);add(spec.Key,false);add(spec.Key,true);
   for(int i=spec.Modifiers.Count-1;i>=0;i--)add(spec.Modifiers[i],true);
   if(!IsForegroundTarget(target,profileId,catalog))return false;
   return SendInput((uint)inputs.Count,inputs.ToArray(),Marshal.SizeOf(typeof(Input)))==inputs.Count;
  }
  [DllImport("user32.dll")] public static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr mod,WinEvent callback,uint pid,uint thread,uint flags);
  [DllImport("user32.dll")] public static extern bool UnhookWinEvent(IntPtr hook);
  public delegate void WinEvent(IntPtr h,uint ev,IntPtr window,int obj,int child,uint thread,uint time);
  [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
  public static FocusedApplication ForegroundApplication(ProfileCatalog catalog=null) {
   IntPtr h=GetForegroundWindow(); uint pid;
   if(h==IntPtr.Zero || IsIconic(h) || !IsWindowVisible(h)) return null;
   GetWindowThreadProcessId(h,out pid);
   string profileId;
   try { using(var p=Process.GetProcessById((int)pid)) {
    if(pid==currentProcessId)return null;
    var processName=p.ProcessName;
    if(catalog==null){var definition=SupportedApps.ByProcess(processName);profileId=definition==null?null:definition.Id;}
    else profileId=catalog.MatchProcess(processName,()=>p.MainModule.FileName);
    if(profileId==null)return null;
   }} catch {return null;}
   return GetForegroundWindow()==h?new FocusedApplication{Window=h,ProfileId=profileId,ProcessId=pid}:null;
  }
  static readonly uint currentProcessId=ProcessId();
  static uint ProcessId(){using(var process=Process.GetCurrentProcess())return (uint)process.Id;}
  public static bool IsForegroundTarget(IntPtr target,string profileId,ProfileCatalog catalog=null) {
   if(target==IntPtr.Zero||(catalog==null?SupportedApps.ById(profileId)==null:catalog.Find(profileId)==null))return false;
   var focused=ForegroundApplication(catalog);return focused!=null&&focused.Window==target&&focused.ProfileId==profileId;
  }
  public static int TriggerCode(string s) {
   switch(s) {case "Ctrl":return 17; case "Alt":return 18; case "Shift":return 16; case "`":return 192; case "Mouse middle":return 4; case "Mouse side 1":return 5; case "Mouse side 2":return 6;
    case "Mouse left":return 1; case "Mouse right":return 2;}
   Keys key; if(!Enum.TryParse<Keys>(s,true,out key)) return 0;
   return (int)key;
  }
  public static bool Down(int key,IntPtr target,string profileId=null,ProfileCatalog catalog=null) {
   // Re-check immediately before every OS input query; never inspect history bit.
   return key!=0 && IsForegroundTarget(target,profileId,catalog) && (GetAsyncKeyState(key)&0x8000)!=0;
  }
 }
}
