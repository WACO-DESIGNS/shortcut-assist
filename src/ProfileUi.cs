using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FusionAssist {
 public partial class MainForm {
  ProfileCatalog catalog;
  readonly ProfileFocus appFocus=new ProfileFocus();
  string editingProfileId="fusion";
  public string ActiveProfileId{get{return appFocus.ProfileId;}}
  public string EditingProfileId{get{return editingProfileId;}}
  public string ActiveApplicationName {
   get {var profile=ActiveProfile;return profile==null?null:profile.Definition().Name;}
  }
  public bool ActiveWheelEnabled{get{return ActiveProfile!=null;}}
  AppProfile ActiveProfile{get{return catalog.Find(appFocus.ProfileId);}}
  internal Settings RuntimeSettings{get{var profile=ActiveProfile;return profile==null?null:profile.Preferences;}}

  void InitializeProfiles() {
   try{catalog=Store.Read<ProfileCatalog>("profiles.json");}catch{
    string path=Path.Combine(Store.Data,"profiles.json");
    if(File.Exists(path))try{File.Copy(path,path+".unreadable-"+Guid.NewGuid().ToString("N")+".bak");}catch{}
    catalog=null;
   }
   if(catalog==null)catalog=ProfileCatalog.Create(settings);
   bool purgeRemoved=catalog.Profiles!=null&&catalog.Profiles.Any(p=>p!=null&&p.Removed);
   catalog.Normalize(settings);
   ChooseEditor(catalog.LastEditedId);
   if(purgeRemoved)SaveChange(()=>{});
   else if(catalog.Find("fusion")==null)Store.Delete("settings.json");
  }

  bool SaveChange(Action undo) {
   string previousTab=catalog.LastEditedId;
   catalog.LastEditedId=editingProfileId;
   try {Store.Write("profiles.json",catalog);}
   catch(Exception ex) {
    catalog.LastEditedId=previousTab;
    undo();
    MessageBox.Show(this,"Your change was not saved.\n\n"+ex.Message,"Shortcut Assist",MessageBoxButtons.OK,MessageBoxIcon.Warning);
    return false;
   }
   // Keep the old Fusion-only file as a compatibility copy.
   var fusion=catalog.Find("fusion");
   if(fusion==null)Store.Delete("settings.json");
   else try{Store.Write("settings.json",fusion.Preferences);}catch{}
   return true;
  }

  public void SelectAppProfile(string id) {
   if(catalog.Find(id)==null||id==editingProfileId)return;
   ChooseEditor(id);
   ResetEditor();
  }

  void ChooseEditor(string id) {
   var profile=catalog.Find(id)??catalog.Profiles.FirstOrDefault();
   editingProfileId=profile==null?null:profile.Id;
   settings=profile==null?new Settings{Tools=new System.Collections.Generic.List<Tool>()}:profile.Preferences;
   RefreshEmptyEditorSettings();
  }

  void RefreshEmptyEditorSettings() {
   if(editingProfileId!=null)return;
   catalog.Wheel.Apply(settings);
   catalog.Activation.Apply(settings);
  }

  public void ObserveApplication(FocusedApplication application) {
   if(application!=null&&catalog.Find(application.ProfileId)==null)application=null;
   if(!appFocus.Observe(application))return;
   target=appFocus.Window;
   if(application!=null) {
    var tab=appTabs.FirstOrDefault(button=>button.Name==application.ProfileId);
    if(tab!=null&&tab.ProgramImage==null) {
     tab.ProgramImage=programIcons.Observe(application.ProfileId,application.ProcessId);
     tab.Invalidate();
    }
   }
   InvalidateSession();
  }

  string ActivationChord() {
   if(string.IsNullOrEmpty(settings.Trigger))return "";
   return (settings.Ctrl?"Ctrl+":"")+(settings.Alt?"Alt+":"")+(settings.Shift?"Shift+":"")+settings.Trigger;
  }
 }
}
