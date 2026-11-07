using System;
using System.Drawing;
using System.Windows.Forms;

namespace FusionAssist {
 public partial class MainForm {
  internal bool LightMode{get{return catalog.UiTheme=="Light";}}

  internal bool SetLightMode(bool light,bool persist=true) {
   string previous=catalog.UiTheme;
   catalog.UiTheme=light?"Light":"Dark";
   foreach(var profile in catalog.Profiles)profile.Preferences.UiTheme=catalog.UiTheme;
   if(persist&&!SaveChange(()=>{
    catalog.UiTheme=previous;
    foreach(var profile in catalog.Profiles)profile.Preferences.UiTheme=previous;
   }))return false;
   WheelVisual.Light=light;
   WheelVisual.Recolor(this);
   RefreshSelection();
   ring.Invalidate();
   mini.Invalidate();
   return true;
  }

  internal bool MiniHudVisible{get{return catalog.ShowMiniHud;}}
  internal string HudCorner{get{return mini.AnchorPosition;}}
  internal string SharedKey{get{return ActivationChord();}}

  internal bool SetSharedKey(string chord) {
   var binding=string.IsNullOrWhiteSpace(chord)?new SharedActivation{Trigger=""}:SharedActivation.Parse(chord);
   if(binding==null)return false;
   var previous=catalog.Activation;
   catalog.Activation=binding;
   catalog.ApplyActivation();RefreshEmptyEditorSettings();
   if(!SaveChange(()=>{catalog.Activation=previous;catalog.ApplyActivation();RefreshEmptyEditorSettings();}))return false;
   RefreshActivationButton();
   InvalidateSession();
   return true;
  }

  internal void SetHudVisible(bool visible) {
   bool previous=catalog.ShowMiniHud;
   catalog.ShowMiniHud=visible;
   if(!SaveChange(()=>catalog.ShowMiniHud=previous))return;
   if(!visible)mini.Hide();
   lastStatus=0;Tick();
  }
  internal void SetHudCorner(string corner){mini.SetAnchor(corner);lastStatus=0;Tick();}

  internal void SetSlotCount(int slots) {
   if(editingProfileId==null||slots<1||slots>12||slots==settings.Count)return;
   var profile=settings;
   int previous=profile.Count;
   profile.Count=slots;
   if(!SaveChange(()=>profile.Count=previous))return;
   if(RuntimeSettings==profile)ring.Hide();
   wheelEditor.SelectSlot(wheelEditor.SelectedSlot);
  }

  internal void SetWheelSize(int diameter) {
   if(diameter<160||diameter>800)return;
   var previous=catalog.Wheel;
   var updated=SharedWheel.From(settings);
   updated.Radius=diameter/2;
   catalog.Wheel=updated;catalog.ApplySharedWheel();RefreshEmptyEditorSettings();
   if(!SaveChange(()=>{catalog.Wheel=previous;catalog.ApplySharedWheel();RefreshEmptyEditorSettings();}))return;
   ring.Hide();
   wheelEditor.Invalidate();
  }
 }

 abstract class WheelDialog:Form {
  protected WheelDialog(string title,int width,int height) {
   Text=title;ClientSize=new Size(width,height);StartPosition=FormStartPosition.CenterParent;
   FormBorderStyle=FormBorderStyle.FixedDialog;MinimizeBox=false;MaximizeBox=false;ShowInTaskbar=false;
   BackColor=WheelVisual.Surface;ForeColor=WheelVisual.Ink;Font=new Font("Segoe UI",10);
   Icon=(Icon)AppAssets.AppIcon.Clone();
  }
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.TitleTheme(Handle,WheelVisual.Light?"Light":"Dark");}
  protected Label LabelAt(string text,int x,int y,int width,int height,bool bold=false) {
   var label=new Label{Text=text,Bounds=new Rectangle(x,y,width,height),UseMnemonic=false,
    Font=new Font("Segoe UI",bold?11:10,bold?FontStyle.Bold:FontStyle.Regular),
    TextAlign=ContentAlignment.MiddleLeft,ForeColor=bold?WheelVisual.Ink:WheelVisual.Muted};
   Controls.Add(label);return label;
  }
  protected WheelButton ButtonAt(string text,int x,int y,int width,Action action) {
   var button=new WheelButton(text){Bounds=new Rectangle(x,y,width,34)};
   button.Click+=(s,e)=>action();Controls.Add(button);return button;
  }
  protected void InputAt(TextBox input,string name,int x,int y,int width) {
   input.SetBounds(x,y,width,30);input.BorderStyle=BorderStyle.FixedSingle;input.BackColor=WheelVisual.Background;
   input.ForeColor=WheelVisual.Ink;input.AccessibleName=name;Controls.Add(input);
  }
  protected ShortcutInput AddRecorder(int y,bool mouse,string initial,Action<string> committed,int inputWidth=270) {
   var input=new ShortcutInput{AllowMouse=mouse,Text=initial};
   InputAt(input,"Shortcut keys",20,y+3,inputWidth);
   var record=ButtonAt("Record",32+inputWidth,y,108,()=>{
    input.Focus();input.SelectAll();input.SetRecording(!input.Recording);
   });
   input.RecordingChanged+=(s,e)=>{
    record.Text=input.Recording?(input.RecordSequence?"Stop":"Listening…"):"Record";record.Invalidate();
   };
   if(committed!=null)input.TextChanged+=(s,e)=>{if(input.Recording)committed(input.Text);};
   return input;
  }
 }

 sealed class PreferencesDialog:WheelDialog {
  const string ReleasePage="https://github.com/WACO-DESIGNS/shortcut-assist/releases";

  internal PreferencesDialog(MainForm owner):base("Shortcut Assist settings",430,280) {
   LabelAt("Activation key",20,13,390,26,true);
   var key=AddRecorder(43,true,owner.SharedKey,chord=>owner.SetSharedKey(chord),180);
   var keyHint=LabelAt("Record a key or mouse button, or type a binding and press Enter.",20,82,390,43);
   ButtonAt("Clear key",330,43,80,()=>{
    key.SetRecording(false);
    if(owner.SetSharedKey("")){
     key.Text="";
     keyHint.Text="Activation key cleared. Record a key or mouse button to use the wheel.";
    }
   });
   key.KeyDown+=(s,e)=>{
    if(e.KeyCode!=Keys.Enter||key.Recording)return;
    if(owner.SetSharedKey(key.Text)){
     key.Text=owner.SharedKey;
     keyHint.Text=key.Text.Length==0?"Activation key cleared. Record a key or mouse button to use the wheel.":"Saved for all apps. The key also reaches the focused app.";
    }
    else keyHint.Text="Enter a valid key or combination, such as Z or Ctrl+F8.";
    e.SuppressKeyPress=true;
   };
   key.LostFocus+=(s,e)=>key.Text=owner.SharedKey;
   key.RecordingChanged+=(s,e)=>{
    if(key.Recording)keyHint.Text="Press a key or mouse button. Esc cancels.";
    else {key.Text=owner.SharedKey;keyHint.Text="The activation key also reaches the focused app.";}
   };

   var hud=new OrangeCheckBox{Text="Show Mini HUD",ForeColor=WheelVisual.Ink,BackColor=BackColor,Checked=owner.MiniHudVisible};
   hud.SetBounds(20,134,211,31);Controls.Add(hud);
   hud.CheckedChanged+=(s,e)=>{owner.SetHudVisible(hud.Checked);if(hud.Checked!=owner.MiniHudVisible)hud.Checked=owner.MiniHudVisible;};

   LabelAt("Appearance",20,177,160,30,true);
   var theme=new ThemeSwitch{Dark=!owner.LightMode,Accent=UiPalette.Orange,BackColor=BackColor,
    ForeColor=WheelVisual.Ink,Bounds=new Rectangle(283,176,127,32),AccessibleName="Switch between light and dark mode"};
   theme.Click+=(s,e)=>{
    if(!owner.SetLightMode(theme.Dark))return;
    theme.Dark=!owner.LightMode;
    WheelVisual.Recolor(this);
   };
   Controls.Add(theme);

   var advancedTitle=LabelAt("Applies to all apps",20,277,390,29,true);
   var diameterLabel=LabelAt("Live wheel diameter (px)",20,313,270,29);
   var size=Number(292,313,160,800,owner.CurrentSettings.Radius*2,10,"Wheel diameter in pixels");
   var positionLabel=LabelAt("Mini HUD placement",20,356,220,30);
   var position=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=WheelVisual.Background,
    ForeColor=WheelVisual.Ink,FlatStyle=FlatStyle.Flat,AccessibleName="Mini HUD placement"};
   position.Items.AddRange(new[]{"Bottom right","Bottom left"});position.SelectedItem=owner.HudCorner;
   position.SetBounds(250,357,160,30);Controls.Add(position);
   position.SelectedIndexChanged+=(s,e)=>owner.SetHudCorner((string)position.SelectedItem);
   var advanced=new Control[]{advancedTitle,diameterLabel,size,positionLabel,position};
   const int expandedHeight=410;
   foreach(var control in advanced)control.Visible=false;
   bool expanded=false;
   var more=ButtonAt("Advanced  +",20,226,126,()=>{});
   more.Click+=(s,e)=>{
    expanded=!expanded;ClientSize=new Size(430,expanded?expandedHeight:280);
    foreach(var control in advanced)control.Visible=expanded;
    more.Text=expanded?"Advanced  −":"Advanced  +";more.Invalidate();
   };
   bool resetting=false;
   size.ValueChanged+=(s,e)=>{
    if(resetting)return;
    owner.SetWheelSize((int)size.Value);
    resetting=true;size.Value=owner.CurrentSettings.Radius*2;resetting=false;
   };
   var updates=ButtonAt("Check for updates",154,226,155,OpenReleasePage);
   updates.AccessibleDescription="Opens the Shortcut Assist releases page on GitHub in your browser.";
   CancelButton=ButtonAt("Done",315,226,95,Close);
  }

  void OpenReleasePage() {
   try {
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ReleasePage){UseShellExecute=true});
   }catch(System.ComponentModel.Win32Exception) {
    MessageBox.Show(this,"Windows could not open the releases page. Open this address in your browser:\n\n"+ReleasePage,
     "Check for updates",MessageBoxButtons.OK,MessageBoxIcon.Information);
   }
  }

  NumericUpDown Number(int x,int y,int min,int max,int value,int step,string name) {
   var field=new NumericUpDown{Minimum=min,Maximum=max,Increment=step,Value=value,
    BackColor=WheelVisual.Background,ForeColor=WheelVisual.Ink,AccessibleName=name,ReadOnly=true};
   field.SetBounds(x,y,118,30);Controls.Add(field);return field;
  }
 }

 sealed class CustomShortcutDialog:WheelDialog {
  internal ToolChoice Choice{get;private set;}
  internal CustomShortcutDialog():base("Add custom shortcut",430,315) {
   LabelAt("Tool name",20,13,390,25,true);
   var name=new TextBox{MaxLength=80};
   InputAt(name,"Tool name",20,40,390);
   LabelAt("Shortcut or macro",20,77,390,25,true);
   var keys=AddRecorder(107,false,"",null);
   keys.RecordSequence=true;
   keys.Multiline=true;keys.AcceptsReturn=true;keys.ScrollBars=ScrollBars.Vertical;
   keys.Height=80;keys.MaxLength=2048;
   const string examples="Use + for keys pressed together. Separate steps with commas or new lines. Try Ctrl+2, Up, Down.";
   var hint=LabelAt(examples,20,199,390,50);
   keys.RecordingChanged+=(s,e)=>{
    keys.ReadOnly=keys.Recording;
    hint.Text=keys.Recording?"Press your keys in order, then click Stop. You can review and edit the sequence before adding it.":examples;
   };
   Deactivate+=(s,e)=>keys.SetRecording(false);
   CancelButton=ButtonAt("Cancel",188,260,104,Close);
   AcceptButton=ButtonAt("Add",302,260,108,()=>{
    keys.SetRecording(false);
    string label=name.Text.Trim(),chord=keys.Text.Trim();
    if(label.Length==0||ShortcutSequence.Parse(chord)==null){hint.Text="Enter a tool name and up to 64 valid steps. Try Ctrl+2, Up, Down.";return;}
    Choice=new ToolChoice(null,label,chord);DialogResult=DialogResult.OK;
   });
  }
 }
}
