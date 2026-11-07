using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace FusionAssist {
 public class AppDefinition {
  public string Id,Name,Process,Badge;
  public AppDefinition(string id,string name,string process,string badge){Id=id;Name=name;Process=process;Badge=badge;}
 }
 public static class SupportedApps {
  public static readonly AppDefinition[] All={new AppDefinition("fusion","Fusion 360","Fusion360","F"),new AppDefinition("solidworks","SolidWorks","SLDWORKS","SW"),new AppDefinition("photoshop","Photoshop","Photoshop","Ps"),new AppDefinition("illustrator","Illustrator","Illustrator","Ai")};
  public static AppDefinition ById(string id){return All.FirstOrDefault(a=>String.Equals(a.Id,id,StringComparison.Ordinal));}
  public static AppDefinition ByProcess(string name) {
   if(String.IsNullOrWhiteSpace(name))return null;
   if(name.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))name=name.Substring(0,name.Length-4);
   return All.FirstOrDefault(a=>String.Equals(a.Process,name,StringComparison.OrdinalIgnoreCase));
  }
 }
 public class AppProfile {
  public string ProgramName {get;set;}
  public string ExecutablePath {get;set;}
  public bool Removed {get;set;}
  internal AppDefinition Definition() {
   var builtin=SupportedApps.ById(Id);
   if(builtin!=null)return builtin;
   if(String.IsNullOrWhiteSpace(ProgramName)||String.IsNullOrWhiteSpace(ExecutablePath))return null;
   return new AppDefinition(Id,ProgramName,Path.GetFileNameWithoutExtension(ExecutablePath),ProgramName.Substring(0,1).ToUpperInvariant());
  }
  public string Id {get;set;} public bool WheelEnabled {get;set;} public Settings Preferences {get;set;} public int StarterToolsVersion {get;set;}
 }
 public class ProfileCatalog {
  public int Version {get;set;} public string UiTheme {get;set;} public string LastEditedId {get;set;} public List<AppProfile> Profiles {get;set;} public bool ShowDetectionNotifications {get;set;}
  public SharedWheel Wheel {get;set;}
  public SharedActivation Activation {get;set;} public bool ShowMiniHud {get;set;}
  public ProfileCatalog(){ShowMiniHud=true;Version=2;Profiles=new List<AppProfile>();LastEditedId="fusion";UiTheme="Dark";ShowDetectionNotifications=true;}
  public AppProfile Find(string id){return Profiles.FirstOrDefault(p=>p!=null&&p.Id==id);}
  internal AppProfile FindExecutable(string path) {
   var builtin=SupportedApps.ByProcess(Path.GetFileName(path));
   return builtin!=null?Find(builtin.Id):Profiles.FirstOrDefault(p=>String.Equals(p.ExecutablePath,path,StringComparison.OrdinalIgnoreCase));
  }
  internal string MatchProcess(string process,Func<string> readPath) {
   var builtin=SupportedApps.ByProcess(process);
   if(builtin!=null)return Find(builtin.Id)==null?null:builtin.Id;
   string path=null;
   foreach(var profile in Profiles) {
    var app=profile.Definition();
    if(profile.ExecutablePath==null||!String.Equals(app.Process,process,StringComparison.OrdinalIgnoreCase))continue;
    if(path==null)path=readPath()??"";
    if(String.Equals(path,profile.ExecutablePath,StringComparison.OrdinalIgnoreCase))return profile.Id;
   }
   return null;
  }
  public static ProfileCatalog Create(Settings fusion) {
   var catalog=new ProfileCatalog{UiTheme=fusion.UiTheme};
   foreach(var app in SupportedApps.All) {
    Settings prefs=app.Id=="fusion"?fusion:Store.Json.Deserialize<Settings>(Store.Json.Serialize(fusion));
    prefs.AppId=app.Id;if(app.Id!="fusion"){prefs.Tools=new List<Tool>();prefs.WheelSlots=null;}
    catalog.Profiles.Add(new AppProfile{Id=app.Id,WheelEnabled=true,Preferences=prefs});
   }return catalog;
  }
  internal const int CompleteProgramListVersion=5;
  const int PerProgramSlotsVersion=6;
  public void Normalize(Settings legacyFusion) {
   if(Profiles==null)Profiles=new List<AppProfile>();Profiles=Profiles.Where(p=>p!=null&&p.Definition()!=null).GroupBy(p=>p.Id).Select(g=>g.First()).ToList();
   var fallback=Create(legacyFusion);
   foreach(var app in SupportedApps.All) {
    var found=Find(app.Id);
    if(found==null&&Version<CompleteProgramListVersion)Profiles.Add(fallback.Find(app.Id));
    else if(found!=null&&found.Preferences==null)found.Preferences=fallback.Find(app.Id).Preferences;
   }
   Profiles.RemoveAll(profile=>profile.Removed);
   foreach(var profile in Profiles) {
    var prefs=profile.Preferences??(profile.Preferences=new Settings{Tools=new List<Tool>()});if(prefs.Tools==null)prefs.Tools=new List<Tool>();
    if(Version<PerProgramSlotsVersion&&Wheel!=null&&Wheel.Count>0)prefs.Count=Wheel.Count;
    prefs.Count=Math.Max(1,Math.Min(12,prefs.Count));prefs.Radius=Math.Max(80,Math.Min(400,prefs.Radius));
    prefs.AppId=profile.Id;WheelAssignments.Ensure(prefs);
   }
   if(Find(LastEditedId)==null)LastEditedId=Profiles.Select(p=>p.Id).FirstOrDefault();
   UiTheme=UiPalette.For(UiTheme).Dark?"Dark":"Light";
   if(Wheel==null)Wheel=SharedWheel.From(Find("fusion")==null?legacyFusion:Find("fusion").Preferences);
   ApplySharedWheel();foreach(var profile in Profiles){profile.Preferences.UiTheme=UiTheme;StarterTools.Seed(profile);}
   if(Activation==null)Activation=SharedActivation.From(Find(LastEditedId)==null?legacyFusion:Find(LastEditedId).Preferences);
   ApplyActivation();foreach(var profile in Profiles)profile.WheelEnabled=true;Version=PerProgramSlotsVersion;
  }
  public void ApplySharedWheel(){if(Wheel!=null)foreach(var profile in Profiles)Wheel.Apply(profile.Preferences);}
  public void UpdateSharedWheel(Settings source){Wheel=SharedWheel.From(source);ApplySharedWheel();}
  public void ApplyActivation(){if(Activation!=null)foreach(var profile in Profiles)Activation.Apply(profile.Preferences);}
  public void UpdateActivation(Settings source){Activation=SharedActivation.From(source);ApplyActivation();}
 }
 public class FocusedApplication {
  public IntPtr Window;public string ProfileId;public uint ProcessId;
 }
 // Focus identity only. Unsupported windows clear the active target; no document
 // title, screen content, keyboard value or mouse history is retained here.
 public class ProfileFocus {
  public string ProfileId {get;private set;} public IntPtr Window {get;private set;} public int Generation {get;private set;}
  public bool Observe(FocusedApplication application) {
   string id=application!=null&&!String.IsNullOrEmpty(application.ProfileId)&&application.Window!=IntPtr.Zero?application.ProfileId:null;
   IntPtr window=id==null?IntPtr.Zero:application.Window;
   bool changed=id!=ProfileId||window!=Window;if(changed){ProfileId=id;Window=window;Generation++;}return changed;
  }
 }
}

