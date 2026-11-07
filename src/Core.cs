using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FusionAssist {
 public class Tool {
  public string Id {get;set;}
  public string Label {get;set;} public string Command {get;set;}
  public int Priority {get;set;} public bool Enabled {get;set;} public string Shortcut {get;set;}
  public Tool() { Id=Guid.NewGuid().ToString("N");Enabled=true; }
 }
 public class Settings {
  public string Trigger {get;set;} public bool Ctrl {get;set;} public bool Alt {get;set;} public bool Shift {get;set;}
  public int Count {get;set;} public int Radius {get;set;} public List<Tool> Tools {get;set;}
  public string UiTheme {get;set;}
  public string AppId {get;set;} public List<string> WheelSlots {get;set;}
  public List<string> HiddenPresets {get;set;}
  public Settings() {
   Trigger="Z"; Count=6; Radius=190; AppId="fusion";UiTheme="Dark";
   Tools=new List<Tool> {
    new Tool {Label="Extrude",Command="Extrude",Shortcut="E",Priority=10},
    new Tool {Label="Fillet",Command="Fillet",Shortcut="F",Priority=9},
    new Tool {Label="Measure",Command="Measure",Shortcut="I",Priority=8},
    new Tool {Label="Move / Copy",Command="MoveCopy",Shortcut="M",Priority=7},
    new Tool {Label="Press Pull",Command="PressPull",Shortcut="Q",Priority=6},
    new Tool {Label="Hole",Command="Hole",Shortcut="H",Priority=5}
   };
  }
 }
 // Input is never queried until the focus gate succeeds. The first focused
 // sample primes the latch, so a key held during app switching cannot open a ring.
 public class TriggerGate {
  public static bool Matches(Settings s,bool key,bool ctrl,bool alt,bool shift){int code=Native.TriggerCode(s.Trigger);return key&&(code==17||s.Ctrl==ctrl)&&(code==18||s.Alt==alt)&&(code==16||s.Shift==shift);}
  bool primed, previous;
  public void Reset() { primed=false; previous=false; }
  public bool Sample(Func<bool> allowed, Func<bool> read) {
   if(!allowed()) { Reset(); return false; }
   bool down=read();
   if(!primed) { primed=true; previous=down; return false; }
   bool fired=down && !previous; previous=down; return fired;
  }
 }
 public static class Ranking {
  public static List<Tool> Wheel(Settings settings) {
   if(settings.WheelSlots!=null)return WheelAssignments.Visible(settings);
   int count=Math.Max(1,Math.Min(12,settings.Count));var result=Manual(settings).Take(count).ToList();
   while(result.Count<count)result.Add(new Tool{Label="Add shortcut",Shortcut="",Enabled=false});return result;
  }
  public static List<Tool> Manual(Settings settings) {
   return settings.Tools.Where(t=>t.Enabled&&ShortcutSequence.Parse(t.Shortcut)!=null).OrderByDescending(t=>t.Priority).Take(settings.Count).ToList();
  }
 }
 public static class WheelAssignments {
  public static void Ensure(Settings settings) {
   var ids=new HashSet<string>();foreach(var tool in settings.Tools){if(String.IsNullOrEmpty(tool.Id)||!ids.Add(tool.Id)){tool.Id=Guid.NewGuid().ToString("N");ids.Add(tool.Id);}}
   if(settings.WheelSlots==null)settings.WheelSlots=Ranking.Manual(settings).Select(t=>t.Id).ToList();
   while(settings.WheelSlots.Count<12)settings.WheelSlots.Add(null);
   var used=new HashSet<string>();for(int i=0;i<settings.WheelSlots.Count;i++)if(!ids.Contains(settings.WheelSlots[i]??"")||!used.Add(settings.WheelSlots[i]))settings.WheelSlots[i]=null;
  }
  public static List<Tool> Visible(Settings settings) {
   Ensure(settings);var result=new List<Tool>();
   for(int i=0;i<Math.Max(1,Math.Min(12,settings.Count));i++){string id=settings.WheelSlots[i];var tool=settings.Tools.FirstOrDefault(t=>t.Id==id&&t.Enabled&&ShortcutSequence.Parse(t.Shortcut)!=null);result.Add(tool??new Tool{Label="Add shortcut",Shortcut="",Enabled=false});}return result;
  }
  public static bool Assign(Settings settings,string id,int slot,bool swap=true) {
   Ensure(settings);var tool=settings.Tools.FirstOrDefault(t=>t.Id==id);if(tool==null||ShortcutSequence.Parse(tool.Shortcut)==null||slot<0||slot>=settings.Count)return false;
   int source=settings.WheelSlots.IndexOf(id);if(source==slot)return true;
   string replaced=settings.WheelSlots[slot];if(source>=0)settings.WheelSlots[source]=swap?replaced:null;
   settings.WheelSlots[slot]=id;tool.Enabled=true;return true;
  }
  public static void Remove(Settings settings,string id){Ensure(settings);for(int i=0;i<settings.WheelSlots.Count;i++)if(settings.WheelSlots[i]==id)settings.WheelSlots[i]=null;}
 }
 public class DetectionNotice {
  string pending,last;double since,lastTime;
  public void Focus(string id,double now){pending=id;since=now;}
  public string Poll(string active,bool enabled,double now) {
   if(!enabled||active!=pending){pending=null;return null;}if(pending==null||now-since<.25)return null;
   string result=pending;pending=null;if(result==last&&now-lastTime<3)return null;last=result;lastTime=now;return result;
  }
 }
 public class ShortcutSpec {
  static readonly Regex WrittenPlus=new Regex(@"\s+plus\s+",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant);
  static readonly char[] Spacing={' ','\t'};
  public List<int> Modifiers=new List<int>();public int Key;
  public static ShortcutSpec Parse(string text) {
   if(String.IsNullOrWhiteSpace(text)||text.IndexOfAny(ShortcutSequence.Separators)>=0)return null;
   text=text.Trim();
   if(text.IndexOf(' ')>=0||text.IndexOf('\t')>=0)text=WrittenPlus.Replace(text,"+");
   var parts=text.IndexOf('+')>=0?text.Split('+'):text.Split(Spacing,StringSplitOptions.RemoveEmptyEntries);
   var result=new ShortcutSpec();bool hasKey=false;
   foreach(string part in parts) {
    string value=part.Trim();int modifier=value.Equals("Ctrl",StringComparison.OrdinalIgnoreCase)||value.Equals("Control",StringComparison.OrdinalIgnoreCase)?17:value.Equals("Alt",StringComparison.OrdinalIgnoreCase)?18:value.Equals("Shift",StringComparison.OrdinalIgnoreCase)?16:0;
    if(parts.Length==1&&modifier!=0){result.Key=modifier;return result;}
    if(modifier!=0){if(hasKey||result.Modifiers.Contains(modifier))return null;result.Modifiers.Add(modifier);continue;}
    if(hasKey||String.IsNullOrEmpty(value))return null;
    if(value=="`"||value=="~")value="Oemtilde";
    if(value.Length==1&&Char.IsDigit(value[0]))value="D"+value;
    if(value.Equals("Esc",StringComparison.OrdinalIgnoreCase))value="Escape";
    if(value.StartsWith("Arrow",StringComparison.OrdinalIgnoreCase))value=value.Substring(5);
    System.Windows.Forms.Keys key;
    if(!Enum.TryParse<System.Windows.Forms.Keys>(value,true,out key))return null;
    int code=(int)key;
    if(!((code>=65&&code<=90)||(code>=48&&code<=57)||(code>=96&&code<=111)||(code>=112&&code<=135)||(code>=166&&code<=183)||new[]{8,9,13,19,20,27,32,33,34,35,36,37,38,39,40,44,45,46,145,186,187,188,189,190,191,192,219,220,221,222}.Contains(code)))return null;
    result.Key=code;hasKey=true;
   }
   return hasKey?result:null;
  }
 }
 static class ShortcutSequence {
  internal const int MaximumSteps=64;
  internal static readonly char[] Separators={',','\r','\n'};

  internal static List<ShortcutSpec> Parse(string text) {
   if(String.IsNullOrWhiteSpace(text))return null;
   var steps=new List<ShortcutSpec>();
   foreach(string line in text.Trim().Split(new[]{"\r\n","\n","\r"},StringSplitOptions.None)) {
    if(String.IsNullOrWhiteSpace(line))continue;
    foreach(string chord in line.Split(',')) {
     var step=ShortcutSpec.Parse(chord);
     if(step==null||steps.Count==MaximumSteps)return null;
     steps.Add(step);
    }
   }
   return steps.Count>0?steps:null;
  }

  internal static string Badge(string shortcut) {
   return shortcut!=null&&shortcut.IndexOfAny(Separators)>=0?"Macro":shortcut;
  }
 }
 public class DismissalTracker {
  double outsideSince=-1;
  public void Reset(){outsideSince=-1;}
  public bool Expired(System.Drawing.Point cursor,System.Drawing.Point origin,double radius,double now,int delay) {
   double dx=(double)cursor.X-origin.X,dy=(double)cursor.Y-origin.Y;
   if(dx*dx+dy*dy<=radius*radius){Reset();return false;}
   if(outsideSince<0)outsideSince=now;
   return (now-outsideSince)*1000>=delay;
  }
 }
}
