using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace FusionAssist {
 public static class Activation {
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern IntPtr FindWindow(string cls,string title);
  [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr window,int command);
  [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr window);
  [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
  [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
  [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern uint RegisterWindowMessage(string name);
  [DllImport("user32.dll")] static extern bool PostMessage(IntPtr window,uint message,IntPtr wp,IntPtr lp);
  [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
  [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr process,uint flags,StringBuilder path,ref int size);
  [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
  public static readonly uint Message=RegisterWindowMessage("FusionAssist.ShowSettings.28581d97");
  public static bool RestoreExisting(string root,bool includeLegacy=true) {
   var titles=includeLegacy?new[]{MainForm.WindowTitle,"Shortcut Assist  /  v1.1.16","Shortcut Assist  /  v1.1.15","Shortcut Assist  /  v1.1.14","Shortcut Assist  /  v1.1.13","Shortcut Assist  /  v1.1.12","Shortcut Assist  /  v1.1.11","Shortcut Assist  /  v1.1.10","Shortcut Assist  /  v1.1.9","Shortcut Assist  /  v1.1.8","Shortcut Assist  /  v1.1.7","Shortcut Assist  /  v1.1.6","Shortcut Assist  /  v1.1.5","Shortcut Assist  /  v1.1.4","Shortcut Assist  /  v1.1.3","Shortcut Assist  /  v1.1.2","Shortcut Assist  /  v1.1.1","Shortcut Assist  /  v1.1","Shortcut Assist  /  v1.0","Shortcut Assist  /  v0.8.16","Shortcut Assist  /  v0.8.15","Shortcut Assist  /  v0.8.14","Shortcut Assist  /  v0.8.13","Shortcut Assist  /  v0.8.12","Shortcut Assist  /  v0.8.11","Shortcut Assist  /  v0.8.10","Shortcut Assist  /  v0.8.9","Shortcut Assist  /  v0.8.8","Shortcut Assist  /  v0.8.7","Shortcut Assist  /  v0.8.6","Shortcut Assist  /  v0.8.5","Shortcut Assist  /  v0.8.4","Shortcut Assist  /  v0.8.3","Shortcut Assist  /  v0.8.2","Shortcut Assist  /  v0.8.1","Shortcut Assist  /  v0.8","Shortcut Assist  /  v0.7","Shortcut Assist  /  v0.6.1","Shortcut Assist  /  v0.6","Fusion Assist  /  v0.5","Fusion Assist  /  v0.4","Fusion Assist  /  v0.3 - Shortcuts","Fusion Assist  /  v0.2.2 - Shortcuts","Fusion Assist  /  v0.2.1 - Shortcuts","Fusion Assist  /  v0.2 - Shortcuts","Fusion Assist  /  v0.1.2","Fusion Assist  /  v0.1.1","Fusion Assist  /  v0.1"}:new[]{MainForm.WindowTitle,"Shortcut Assist  /  v1.1.16","Shortcut Assist  /  v1.1.15","Shortcut Assist  /  v1.1.14","Shortcut Assist  /  v1.1.13","Shortcut Assist  /  v1.1.12","Shortcut Assist  /  v1.1.11","Shortcut Assist  /  v1.1.10","Shortcut Assist  /  v1.1.9","Shortcut Assist  /  v1.1.8","Shortcut Assist  /  v1.1.7","Shortcut Assist  /  v1.1.6","Shortcut Assist  /  v1.1.5","Shortcut Assist  /  v1.1.4","Shortcut Assist  /  v1.1.3","Shortcut Assist  /  v1.1.2","Shortcut Assist  /  v1.1.1","Shortcut Assist  /  v1.1","Shortcut Assist  /  v1.0","Shortcut Assist  /  v0.8.16","Shortcut Assist  /  v0.8.15","Shortcut Assist  /  v0.8.14","Shortcut Assist  /  v0.8.13","Shortcut Assist  /  v0.8.12","Shortcut Assist  /  v0.8.11","Shortcut Assist  /  v0.8.10","Shortcut Assist  /  v0.8.9","Shortcut Assist  /  v0.8.8","Shortcut Assist  /  v0.8.7","Shortcut Assist  /  v0.8.6","Shortcut Assist  /  v0.8.5","Shortcut Assist  /  v0.8.4","Shortcut Assist  /  v0.8.3","Shortcut Assist  /  v0.8.2","Shortcut Assist  /  v0.8.1","Shortcut Assist  /  v0.8","Shortcut Assist  /  v0.7","Shortcut Assist  /  v0.6.1","Shortcut Assist  /  v0.6","Fusion Assist  /  v0.5","Fusion Assist  /  v0.4","Fusion Assist  /  v0.3 - Shortcuts","Fusion Assist  /  v0.2.2 - Shortcuts","Fusion Assist  /  v0.2.1 - Shortcuts","Fusion Assist  /  v0.2 - Shortcuts"};
   foreach(string title in titles) {
    IntPtr window=FindWindow(null,title);if(window==IntPtr.Zero)continue;
    uint pid;GetWindowThreadProcessId(window,out pid);IntPtr process=OpenProcess(0x1000,false,pid);
    if(process==IntPtr.Zero)continue;
    bool sameFolder=false;
    try {var path=new StringBuilder(4096);int size=path.Capacity;
     sameFolder=QueryFullProcessImageName(process,0,path,ref size) && String.Equals(Path.GetDirectoryName(path.ToString()).TrimEnd('\\','/'),root.TrimEnd('\\','/'),StringComparison.OrdinalIgnoreCase);
    }finally{CloseHandle(process);}
    if(!sameFolder)continue;
    RestoreWindow(window);return true;
   }
   return false;
  }
  public static void RestoreWindow(IntPtr window) {
   // Works on hidden legacy windows too; no polling of input or screen capture.
   PostMessage(window,Message,IntPtr.Zero,IntPtr.Zero);
   ShowWindowAsync(window,9);ShowWindowAsync(window,5);SetForegroundWindow(window);
  }
 }
}







