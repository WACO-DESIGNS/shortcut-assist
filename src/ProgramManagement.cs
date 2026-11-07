using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FusionAssist {
 public partial class MainForm {
  internal IEnumerable<AppProfile> InstalledPrograms{get{return catalog.Profiles;}}


  internal bool AddProgram(string path) {
   path=Path.GetFullPath(path);
   if(!Native.IsLocalDrivePath(path)||!File.Exists(path)||!String.Equals(Path.GetExtension(path),".exe",StringComparison.OrdinalIgnoreCase)||
    String.Equals(path,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase))return false;
   var existing=catalog.FindExecutable(path);
   if(existing!=null)return false;
   var builtin=SupportedApps.ByProcess(Path.GetFileName(path));

   var name=Path.GetFileNameWithoutExtension(path);
   try {
    string title=System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileDescription;
    if(!String.IsNullOrWhiteSpace(title))name=title.Trim();
   }catch(System.ComponentModel.Win32Exception){}
   string id=builtin==null?"app-"+Guid.NewGuid().ToString("N"):builtin.Id;
   var prefs=new Settings{AppId=id,UiTheme=catalog.UiTheme};
   if(id!="fusion")prefs.Tools.Clear();
   catalog.Wheel.Apply(prefs);catalog.Activation.Apply(prefs);WheelAssignments.Ensure(prefs);
   var profile=new AppProfile{Id=id,ProgramName=name,ExecutablePath=path,Preferences=prefs,WheelEnabled=true};
   StarterTools.Seed(profile);
   string previous=editingProfileId;
   catalog.Profiles.Add(profile);ChooseEditor(id);
   if(!SaveChange(()=>{catalog.Profiles.Remove(profile);ChooseEditor(previous);}))return false;
   RebuildProgramTabs();ResetEditor();
   return true;
  }

  internal bool RemoveProgram(AppProfile profile) {
   int index=catalog.Profiles.IndexOf(profile);
   if(index<0)return false;
   string previous=editingProfileId;
   catalog.Profiles.RemoveAt(index);
   ChooseEditor(previous);
   if(!SaveChange(()=>{catalog.Profiles.Insert(index,profile);ChooseEditor(previous);}))return false;
   if(appFocus.ProfileId==profile.Id)ObserveApplication(null);
   RebuildProgramTabs();ResetEditor();Tick();
   return true;
  }

  void ShowProgramManager(){using(var dialog=new ProgramManager(this))dialog.ShowDialog(this);}
 }

 sealed class ProgramManager:WheelDialog {
  readonly MainForm owner;
  readonly ListBox programs=new ListBox();
  readonly WheelButton action;

  internal ProgramManager(MainForm owner):base("Manage programs",490,344) {
   this.owner=owner;
   LabelAt("Programs",20,12,450,32,true);
   LabelAt("Add a Windows program by choosing its .exe file.",20,45,450,30);
   programs.SetBounds(20,83,450,172);
   programs.IntegralHeight=false;programs.BackColor=WheelVisual.Background;programs.ForeColor=WheelVisual.Ink;
   programs.AccessibleName="Installed programs";Controls.Add(programs);
   ButtonAt("Add program…",20,280,146,ChooseExecutable);
   action=ButtonAt("Remove",174,280,140,ChangeSelected);
   CancelButton=ButtonAt("Done",364,280,106,Close);
   programs.SelectedIndexChanged+=(s,e)=>UpdateAction();
   RefreshPrograms();
  }

  void RefreshPrograms() {
   var selected=Selected();
   programs.Items.Clear();
   foreach(var profile in owner.InstalledPrograms)programs.Items.Add(new ProgramRow(profile));
   for(int i=0;i<programs.Items.Count;i++)if(((ProgramRow)programs.Items[i]).Profile==selected){programs.SelectedIndex=i;break;}
   UpdateAction();
  }
  AppProfile Selected(){var row=programs.SelectedItem as ProgramRow;return row==null?null:row.Profile;}
  void UpdateAction(){action.Enabled=Selected()!=null;}
  void ChooseExecutable() {
   using(var picker=new OpenFileDialog{Title="Choose the program's executable",Filter="Windows programs (*.exe)|*.exe",CheckFileExists=true}) {
    if(picker.ShowDialog(this)!=DialogResult.OK)return;
    if(owner.AddProgram(picker.FileName))RefreshPrograms();
    else MessageBox.Show(this,"Choose a different executable. This program may already be on your list.","Add program",MessageBoxButtons.OK,MessageBoxIcon.Information);
   }
  }
  void ChangeSelected() {
   var profile=Selected();
   if(profile==null)return;
   if(MessageBox.Show(this,"Remove "+profile.Definition().Name+
    " from Shortcut Assist? This deletes its saved wheel and shortcuts. The Windows program stays installed.",
    "Remove program?",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
   if(owner.RemoveProgram(profile))RefreshPrograms();
  }

  sealed class ProgramRow {
   internal readonly AppProfile Profile;
   internal ProgramRow(AppProfile profile){Profile=profile;}
   public override string ToString(){return Profile.Definition().Name;}
  }
 }
}
