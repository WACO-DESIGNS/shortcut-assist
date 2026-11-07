using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FusionAssist {
 public partial class MainForm {
  readonly List<WheelButton> appTabs=new List<WheelButton>();
  readonly ProgramIcons programIcons=new ProgramIcons();
  BrandMark headerMark;
  Label wordmark,activationLabel,toolsTitle;
  WheelButton keyButton,gear,tutorialButton,clearSlot,addCustom,deleteTools,programsButton,exitButton,removeSlot,addSlot;
  int visibleProgramCount;
  readonly ContextMenuStrip programsMenu=new ContextMenuStrip{ShowCheckMargin=true,ShowImageMargin=true};
  Panel wheelPanel,toolsPanel,activationPanel;
  WheelEditor wheelEditor;
  ToolList toolList;
  TextBox search;
  Label emptySearch,wheelFooter,selectionLabel,slotCount;
  SearchField searchField;

  static Label Copy(string text,float size,bool bold=false) {
   return new Label{Text=text,Font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular),
    ForeColor=WheelVisual.Ink,TextAlign=ContentAlignment.MiddleLeft,UseMnemonic=false};
  }

  void BuildWheelScreen() {
   SetStyle(ControlStyles.ResizeRedraw,true);
   SuspendLayout();
   BackColor=WheelVisual.Background;ForeColor=WheelVisual.Ink;
   headerMark=new BrandMark{BackColor=BackColor,Tint=UiPalette.Orange};
   wordmark=Copy("Shortcut" + Environment.NewLine + "Assist",12,true);
   Controls.AddRange(new Control[]{headerMark,wordmark});
   headerMark.Cursor=Cursors.Default;
   programsButton=new WheelButton("…"){AccessibleName="More programs and program management"};
   programsButton.Click+=(s,e)=>ShowProgramsMenu();
   Controls.Add(programsButton);
   RebuildProgramTabs();
   activationLabel=Copy("Activation key",11,true);
   keyButton=new WheelButton(""){Font=new Font("Segoe UI",9,FontStyle.Bold),AccessibleName="Change shared activation key"};
   RefreshActivationButton();
   gear=new WheelButton("\uE713"){Font=new Font("Segoe MDL2 Assets",13),AccessibleName="Settings"};
   keyButton.Click+=(s,e)=>ShowPreferences();gear.Click+=(s,e)=>ShowPreferences();
   tutorialButton=new WheelButton("?"){Font=new Font("Segoe UI",12,FontStyle.Bold),AccessibleName="Shortcut Assist tutorial"};
   tutorialButton.Click+=(s,e)=>ShowTutorial();
   Controls.AddRange(new Control[]{tutorialButton,gear});

   wheelPanel=new SurfacePanel();toolsPanel=new SurfacePanel();activationPanel=new SurfacePanel();
   activationPanel.Controls.AddRange(new Control[]{activationLabel,keyButton});
   Controls.AddRange(new Control[]{wheelPanel,activationPanel,toolsPanel});
   var heading=Copy("My Wheel",26,true);
   heading.SetBounds(21,12,300,52);
   exitButton=new WheelButton("Exit app"){PowerIcon=true,SelectedAccent=true,
    AccessibleName="Exit Shortcut Assist",AccessibleDescription="Closes Shortcut Assist, the Mini HUD, and the tray icon."};
   exitButton.Click+=(s,e)=>Quit();
   wheelEditor=new WheelEditor(()=>settings){BackColor=WheelVisual.Surface};
   wheelEditor.SlotSelected+=slot=>RefreshSelection();
   wheelEditor.ToolDropped+=(drag,slot)=>AssignChoice(drag.Choice,slot,drag.SourceSlot);
   selectionLabel=Copy("",10,true);selectionLabel.AutoEllipsis=true;
   wheelFooter=Copy("",10);wheelFooter.ForeColor=WheelVisual.Muted;wheelFooter.AutoEllipsis=true;
   clearSlot=new WheelButton("Clear slot");
   clearSlot.Click+=(s,e)=>ClearSelectedSlot();
   removeSlot=new WheelButton("−"){AccessibleName="Remove the last slot from this wheel"};
   addSlot=new WheelButton("+"){AccessibleName="Add a slot to this wheel"};
   removeSlot.Click+=(s,e)=>SetSlotCount(settings.Count-1);
   addSlot.Click+=(s,e)=>SetSlotCount(settings.Count+1);
   slotCount=Copy("",9,true);slotCount.TextAlign=ContentAlignment.MiddleCenter;
   Controls.Add(exitButton);
   wheelPanel.Controls.AddRange(new Control[]{heading,wheelEditor,selectionLabel,wheelFooter,clearSlot,removeSlot,slotCount,addSlot});

   toolsTitle=Copy("",17,true);
   searchField=new SearchField();
   search=searchField.Input;
   search.TextChanged+=(s,e)=>RefreshToolList();
   emptySearch=Copy("No matching tools",11);emptySearch.ForeColor=WheelVisual.Muted;
   toolList=new ToolList(()=>settings);
   toolList.ToolClicked+=choice=>{
    if(wheelEditor.SelectedSlot<0){wheelFooter.Text="Select a slot on the wheel, then click a tool.";return;}
    AssignChoice(choice,wheelEditor.SelectedSlot);
   };
   toolList.ToolDeleted+=choice=>DeleteTool(choice);
   addCustom=new WheelButton("+  Add custom shortcut");
   addCustom.Click+=(s,e)=>ShowCustomShortcut();
   deleteTools=new WheelButton("−"){AccessibleName="Delete tools"};
   deleteTools.Click+=(s,e)=>SetDeleteMode(!toolList.DeleteMode);
   toolsPanel.Controls.AddRange(new Control[]{toolsTitle,searchField,toolList,emptySearch,addCustom,deleteTools});
   ResumeLayout(false);
   LayoutWheelScreen();
   ResetEditor();
  }

  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);Native.TitleTheme(Handle,LightMode?"Light":"Dark");}
  protected override void OnSizeChanged(EventArgs e) {
   base.OnSizeChanged(e);
   LayoutWheelScreen();
   Invalidate(true);
  }

  void RebuildProgramTabs() {
   foreach(var tab in appTabs){Controls.Remove(tab);tab.Dispose();}
   appTabs.Clear();
   foreach(var profile in InstalledPrograms) {
    var app=profile.Definition();
    var tab=new WheelButton(app.Name){Badge=app.Badge,ProgramImage=programIcons.Get(profile),Font=new Font("Segoe UI",9,FontStyle.Bold),Accent=UiPalette.ForApp(app.Id),Name=app.Id,AccessibleName=app.Name};
    tab.Click+=(sender,args)=>SelectAppProfile(app.Id);
    appTabs.Add(tab);Controls.Add(tab);
   }
   LayoutWheelScreen();
  }
  void ShowProgramsMenu() {
   while(programsMenu.Items.Count>0){var item=programsMenu.Items[0];programsMenu.Items.RemoveAt(0);item.Dispose();}
   foreach(var tab in appTabs.Skip(visibleProgramCount)) {
    string id=tab.Name;
    var item=new ToolStripMenuItem(tab.Text){Checked=id==editingProfileId,Image=tab.ProgramImage};
    item.Click+=(s,e)=>SelectOverflowProgram(id);programsMenu.Items.Add(item);
   }
   if(programsMenu.Items.Count>0)programsMenu.Items.Add(new ToolStripSeparator());
   programsMenu.Items.Add("Manage programs…",null,(s,e)=>ShowProgramManager());
   programsMenu.Show(programsButton,new Point(0,programsButton.Height));
  }
  void SelectOverflowProgram(string id) {
   int index=catalog.Profiles.FindIndex(candidate=>candidate.Id==id);
   if(index<0)return;
   if(index<visibleProgramCount||index==0){SelectAppProfile(id);return;}

   string previous=editingProfileId;
   var profile=catalog.Profiles[index];
   catalog.Profiles.RemoveAt(index);
   catalog.Profiles.Insert(0,profile);
   ChooseEditor(id);
   if(!SaveChange(()=>{
    catalog.Profiles.RemoveAt(0);
    catalog.Profiles.Insert(index,profile);
    ChooseEditor(previous);
   }))return;
   RebuildProgramTabs();
   ResetEditor();
  }

  void LayoutWheelScreen() {
   if(addCustom==null)return;
   int width=ClientSize.Width,height=ClientSize.Height;
   SuspendLayout();
   headerMark.SetBounds(16,12,50,53);
   wordmark.SetBounds(70,8,107,61);
   int keyWidth=Math.Max(48,Math.Min(155,TextRenderer.MeasureText(keyButton.Text,keyButton.Font).Width+14));
   exitButton.SetBounds(width-144,17,128,44);
   gear.SetBounds(exitButton.Left-42,19,34,40);
   tutorialButton.SetBounds(gear.Left-42,19,34,40);
   const int tabsStart=184, maxVisiblePrograms=4;
   int tabSpace=Math.Max(0,tutorialButton.Left-tabsStart-49);
   visibleProgramCount=Math.Min(Math.Min(maxVisiblePrograms,appTabs.Count),Math.Max(0,(tabSpace+7)/120));
   int tabWidth=visibleProgramCount==0?0:Math.Min(154,(tabSpace-7*(visibleProgramCount-1))/visibleProgramCount);
   for(int i=0;i<appTabs.Count;i++) {
    appTabs[i].Visible=i<visibleProgramCount;
    if(i<visibleProgramCount)appTabs[i].SetBounds(tabsStart+i*(tabWidth+7),19,tabWidth,40);
   }
   programsButton.SetBounds(tabsStart+visibleProgramCount*(tabWidth+7),19,36,40);
   programsButton.SelectedAccent=appTabs.Skip(visibleProgramCount).Any(tab=>tab.Name==editingProfileId);
   programsButton.Accent=UiPalette.ForApp(editingProfileId);
   programsButton.Invalidate();

   int bodyHeight=height-100;
   int libraryWidth=Math.Max(302,Math.Min(530,(int)(width*.31)));
   int wheelWidth=width-libraryWidth-49;
   int libraryLeft=width-libraryWidth-16;
   wheelPanel.SetBounds(16,80,wheelWidth,bodyHeight);
   activationPanel.SetBounds(libraryLeft,80,libraryWidth,60);
   activationLabel.SetBounds(17,7,libraryWidth-keyWidth-48,46);
   keyButton.SetBounds(libraryWidth-keyWidth-17,13,keyWidth,34);
   int toolsHeight=bodyHeight-72;
   toolsPanel.SetBounds(libraryLeft,152,libraryWidth,toolsHeight);
   wheelEditor.SetBounds(8,68,wheelWidth-16,bodyHeight-155);
   selectionLabel.SetBounds(22,bodyHeight-77,wheelWidth-158,23);
   wheelFooter.SetBounds(22,bodyHeight-49,wheelWidth-44,32);
   clearSlot.SetBounds(wheelWidth-125,bodyHeight-82,103,34);
   bool largeSlots=WindowState==FormWindowState.Maximized||width>=1500;
   int slotButtonWidth=largeSlots?44:34,slotLabelWidth=largeSlots?78:62,slotHeight=largeSlots?38:30;
   int slotTop=38-slotHeight/2,slotRight=wheelWidth-22;
   addSlot.SetBounds(slotRight-slotButtonWidth,slotTop,slotButtonWidth,slotHeight);
   slotCount.SetBounds(addSlot.Left-slotLabelWidth,slotTop,slotLabelWidth,slotHeight);
   removeSlot.SetBounds(slotCount.Left-slotButtonWidth,slotTop,slotButtonWidth,slotHeight);
   SetSlotFont(removeSlot,largeSlots?13:10);
   SetSlotFont(slotCount,largeSlots?11:9);
   SetSlotFont(addSlot,largeSlots?13:10);

   toolsTitle.SetBounds(17,12,libraryWidth-34,47);
   searchField.SetBounds(17,60,libraryWidth-34,36);
   toolList.SetBounds(15,105,libraryWidth-30,toolsHeight-166);
   emptySearch.SetBounds(23,110,libraryWidth-46,40);
   addCustom.SetBounds(17,toolsHeight-49,libraryWidth-78,34);
   deleteTools.SetBounds(libraryWidth-53,toolsHeight-49,36,34);
   ResumeLayout(false);
  }

  static void SetSlotFont(Control control,float size) {
   var previous=control.Font;
   if(previous.Size==size)return;
   control.Font=new Font("Segoe UI",size,FontStyle.Bold);
   previous.Dispose();
  }

  void RefreshActivationButton() {
   string binding=ActivationChord();
   keyButton.NeedsAttention=binding.Length==0;
   keyButton.Text=keyButton.NeedsAttention?"Set key":binding;
   LayoutWheelScreen();
   keyButton.Invalidate();
  }

  void ResetEditor() {
   LayoutWheelScreen();
   foreach(var tab in appTabs){tab.SelectedAccent=tab.Name==editingProfileId;tab.Invalidate();}
   var program=catalog.Find(editingProfileId);
   bool hasProgram=program!=null;
   toolsTitle.Text=hasProgram?(editingProfileId=="fusion"?"Fusion":program.Definition().Name)+" tools":"Programs";
   toolsTitle.AutoEllipsis=true;
   wheelEditor.Visible=hasProgram;searchField.Visible=hasProgram;toolList.Visible=hasProgram;addCustom.Visible=hasProgram;
   deleteTools.Visible=hasProgram;
   SetDeleteMode(false);
   search.Clear();
   var slots=Ranking.Wheel(settings);
   int firstEmpty=slots.FindIndex(t=>!t.Enabled);
   wheelEditor.SelectSlot(hasProgram?firstEmpty:-1);
   RefreshToolList();
  }

  static bool SameChoice(Tool tool,ToolChoice choice) {
   return String.Equals(tool.Label,choice.Label,StringComparison.OrdinalIgnoreCase)&&
    String.Equals(tool.Shortcut,choice.Shortcut,StringComparison.OrdinalIgnoreCase);
  }
  IEnumerable<ToolChoice> Choices() {
   foreach(var tool in settings.Tools.Where(t=>!String.IsNullOrWhiteSpace(t.Label)&&ShortcutSequence.Parse(t.Shortcut)!=null))
    yield return new ToolChoice(tool.Id,tool.Label,tool.Shortcut);
   foreach(var preset in ShortcutPreset.ForApp(settings.AppId)) {
    var choice=new ToolChoice(null,preset.Name,preset.Chord);
    if((settings.HiddenPresets==null||!settings.HiddenPresets.Contains(choice.PresetKey,StringComparer.OrdinalIgnoreCase))&&
     !settings.Tools.Any(t=>SameChoice(t,choice)))yield return choice;
   }
  }

  void RefreshToolList() {
   string filter=search.Text.Trim();
   var matches=Choices().Where(t=>t.Label.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0||
    t.Shortcut.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0).ToArray();
   toolList.ReplaceRows(matches);
   emptySearch.Text=editingProfileId==null?"Add a program from the … menu.":"No matching tools";
   emptySearch.Visible=editingProfileId==null||matches.Length==0;
   emptySearch.BringToFront();
   RefreshSelection();
  }
  void RefreshSelection() {
   int slot=wheelEditor.SelectedSlot;
   var assigned=slot>=0&&slot<settings.Count?Ranking.Wheel(settings)[slot]:null;
   bool filled=assigned!=null&&assigned.Enabled;
   selectionLabel.Text=slot<0?"":("Slot "+(slot+1)+"  ·  "+(filled?assigned.Label:"Empty"));
   selectionLabel.ForeColor=WheelVisual.AccentText(UiPalette.ForApp(editingProfileId));
   wheelFooter.Text=editingProfileId==null?"Choose … to add a program.":slot<0?"Drag a tool onto the wheel, or select a slot.":filled?
    "Click a tool to replace it, or drag to rearrange.":"Click a tool to assign it, or drag it here.";
   if(toolList.DeleteMode)wheelFooter.Text="Click the − beside a tool to delete it.";
   clearSlot.Visible=slot>=0;
   clearSlot.Enabled=filled;
   removeSlot.Visible=slotCount.Visible=addSlot.Visible=editingProfileId!=null;
   removeSlot.Enabled=settings.Count>1;
   addSlot.Enabled=settings.Count<12;
   slotCount.Text=settings.Count+(settings.Count==1?" slot":" slots");
   toolList.AssignedId=filled?assigned.Id:null;
   toolList.Invalidate();
  }

  internal bool AssignChoice(ToolChoice choice,int slot,int sourceSlot=-1) {
   if(editingProfileId==null||slot<0||slot>=settings.Count||ShortcutSequence.Parse(choice.Shortcut)==null)return false;
   var tool=choice.Id==null?settings.Tools.FirstOrDefault(t=>SameChoice(t,choice)):settings.Tools.FirstOrDefault(t=>t.Id==choice.Id);
   if(choice.Id!=null&&tool==null)return false;
   if(sourceSlot>=0&&(sourceSlot>=settings.Count||settings.WheelSlots[sourceSlot]!=choice.Id))return false;
   bool added=tool==null;
   if(added){tool=new Tool{Label=choice.Label,Shortcut=choice.Shortcut};settings.Tools.Add(tool);}
   var previousSlots=settings.WheelSlots.ToList();
   bool previouslyEnabled=tool.Enabled;
   if(!WheelAssignments.Assign(settings,tool.Id,slot,sourceSlot>=0))return false;
   if(!SaveChange(()=>{
    settings.WheelSlots=previousSlots;
    tool.Enabled=previouslyEnabled;
    if(added)settings.Tools.Remove(tool);
   }))return false;

   wheelEditor.SelectSlot(slot);
   if(added)RefreshToolList();
   return true;
  }

  void SetDeleteMode(bool enabled) {
   toolList.DeleteMode=enabled;
   deleteTools.SelectedAccent=enabled;
   deleteTools.AccessibleName=enabled?"Finish deleting tools":"Delete tools";
   deleteTools.Invalidate();
   addCustom.Enabled=!enabled;
   RefreshSelection();
  }

  internal bool DeleteTool(ToolChoice choice) {
   if(editingProfileId==null)return false;
   int toolIndex=choice.Id==null?-1:settings.Tools.FindIndex(candidate=>candidate.Id==choice.Id);
   if(choice.Id!=null&&toolIndex<0)return false;
   var previousSlots=settings.WheelSlots.ToList();
   var previousHidden=settings.HiddenPresets;
   var tool=toolIndex<0?null:settings.Tools[toolIndex];
   if(tool!=null) {
    WheelAssignments.Remove(settings,tool.Id);
    settings.Tools.RemoveAt(toolIndex);
   }
   if(ShortcutPreset.ForApp(settings.AppId).Any(preset=>String.Equals(
    ToolChoice.PresetIdentity(preset.Name,preset.Chord),choice.PresetKey,StringComparison.OrdinalIgnoreCase))) {
    settings.HiddenPresets=new List<string>(previousHidden??Enumerable.Empty<string>());
    if(!settings.HiddenPresets.Contains(choice.PresetKey,StringComparer.OrdinalIgnoreCase))settings.HiddenPresets.Add(choice.PresetKey);
   }
   if(!SaveChange(()=>{
    if(tool!=null)settings.Tools.Insert(toolIndex,tool);
    settings.WheelSlots=previousSlots;
    settings.HiddenPresets=previousHidden;
   }))return false;
   int top=toolList.TopIndex;
   RefreshToolList();
   if(toolList.Items.Count>0)toolList.TopIndex=Math.Min(top,toolList.Items.Count-1);
   wheelEditor.Invalidate();
   if(RuntimeSettings==settings)InvalidateSession();
   return true;
  }

  internal void ClearSelectedSlot() {
   int slot=wheelEditor.SelectedSlot;
   if(slot<0||slot>=settings.Count||settings.WheelSlots[slot]==null)return;
   string previous=settings.WheelSlots[slot];
   settings.WheelSlots[slot]=null;
   if(SaveChange(()=>settings.WheelSlots[slot]=previous))wheelEditor.SelectSlot(slot);
  }

  void ShowCustomShortcut() {
   using(var dialog=new CustomShortcutDialog()) {
    if(dialog.ShowDialog(this)!=DialogResult.OK)return;
    var choice=dialog.Choice;
    int slot=wheelEditor.SelectedSlot;
    if(slot>=0)AssignChoice(choice,slot);
    else {
     var existing=settings.Tools.FirstOrDefault(t=>SameChoice(t,choice));
     if(existing==null) {
      var tool=new Tool{Label=choice.Label,Shortcut=choice.Shortcut};
      settings.Tools.Add(tool);
      if(!SaveChange(()=>settings.Tools.Remove(tool)))return;
     }
    }
    search.Clear();
    RefreshToolList();
    for(int i=0;i<toolList.Items.Count;i++){
     var row=(ToolChoice)toolList.Items[i];
     if(row.Label==choice.Label&&row.Shortcut==choice.Shortcut){toolList.SelectedIndex=i;break;}
    }
   }
  }

  void ShowPreferences(){using(var dialog=new PreferencesDialog(this))dialog.ShowDialog(this);}
 }

 sealed class SurfacePanel:Panel {
  internal SurfacePanel(){DoubleBuffered=true;BackColor=WheelVisual.Surface;ResizeRedraw=true;}
  protected override void OnPaintBackground(PaintEventArgs e) {
   e.Graphics.Clear(Parent==null?WheelVisual.Background:Parent.BackColor);
   e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   WheelVisual.Box(e.Graphics,new RectangleF(.5f,.5f,Width-1,Height-1),BackColor,WheelVisual.Line,11);
  }
 }

 sealed class SearchField:Panel {
  internal readonly TextBox Input=new CueTextBox();
  internal SearchField() {
   DoubleBuffered=true;ResizeRedraw=true;
   Input.BorderStyle=BorderStyle.None;Input.Font=new Font("Segoe UI",11);
   Input.BackColor=WheelVisual.Background;Input.ForeColor=WheelVisual.Ink;
   Input.AccessibleName="Find a tool";Controls.Add(Input);
   Input.GotFocus+=(s,e)=>Invalidate();Input.LostFocus+=(s,e)=>Invalidate();
  }
  protected override void OnResize(EventArgs e) {
   base.OnResize(e);
   if(Input!=null)Input.SetBounds(33,(Height-Input.PreferredHeight)/2,Math.Max(1,Width-43),Input.PreferredHeight);
  }
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Input.Focus();}
  protected override void OnPaintBackground(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(WheelVisual.Surface);
   g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   WheelVisual.Box(g,new RectangleF(.5f,.5f,Width-1,Height-1),WheelVisual.Background,Input.Focused?WheelVisual.Edge:WheelVisual.Line,7);
   int y=Height/2-7;
   using(var pen=new Pen(WheelVisual.Muted,1.5f)) {
    g.DrawEllipse(pen,12,y,10,10);g.DrawLine(pen,20,y+8,25,y+13);
   }
  }

  sealed class CueTextBox:TextBox {
   [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
   static extern IntPtr SendMessage(IntPtr window,uint message,IntPtr showWhileFocused,string text);
   protected override void OnHandleCreated(EventArgs e) {
    base.OnHandleCreated(e);SendMessage(Handle,0x1501,(IntPtr)1,"Find a tool");
   }
  }
 }

 sealed class WheelButton:Button {
  internal string Badge;
  internal Image ProgramImage;
  internal Color Accent=UiPalette.Orange;
  internal bool SelectedAccent,PowerIcon,NeedsAttention;
  bool over;

  internal WheelButton(string text) {
   Text=text;Font=new Font("Segoe UI",10,FontStyle.Bold);ForeColor=WheelVisual.Ink;
   FlatStyle=FlatStyle.Flat;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;
   SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
  }
  protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);over=true;Invalidate();}
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);over=false;Invalidate();}
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(Parent==null?WheelVisual.Background:Parent.BackColor);
   g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   float pulse=NeedsAttention?(float)(.5-.5*Math.Cos(Store.Now*2*Math.PI/2.6)):0;
   var fill=SelectedAccent?WheelVisual.Tint(WheelVisual.Raised,Accent,.14f):
    over&&Enabled?WheelVisual.Tint(WheelVisual.Raised,WheelVisual.Ink,.06f):WheelVisual.Raised;
   var border=SelectedAccent?Accent:Focused?WheelVisual.Edge:WheelVisual.Line;
   if(NeedsAttention) {
    fill=WheelVisual.Tint(fill,Accent,.06f+pulse*.15f);
    border=WheelVisual.Tint(border,Accent,.38f+pulse*.62f);
   }
   WheelVisual.Box(g,new RectangleF(1,1,Width-2,Height-2),
    fill,border,8,SelectedAccent||NeedsAttention?2:1);
   int textLeft=2;
   if(PowerIcon) {
    int y=(Height-18)/2;
    using(var line=new Pen(Accent,2.2f){StartCap=System.Drawing.Drawing2D.LineCap.Round,EndCap=System.Drawing.Drawing2D.LineCap.Round}) {
     g.DrawArc(line,16,y+2,16,16,-53,286);
     g.DrawLine(line,24,y,24,y+8);
    }
    textLeft=37;
   }
   if(Badge!=null) {
    var badge=new Rectangle(8,(Height-23)/2,24,23);
    if(ProgramImage!=null) {
     g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
     g.DrawImage(ProgramImage,new Rectangle(badge.Left,badge.Top,23,23));
    }else {
     WheelVisual.Box(g,badge,WheelVisual.Tint(WheelVisual.Background,Accent,.21f),WheelVisual.Tint(WheelVisual.Background,Accent,.38f),5);
     TextRenderer.DrawText(g,Badge,Font,badge,WheelVisual.AccentText(Accent),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);
    }
    textLeft=39;
   }
   TextRenderer.DrawText(g,Text,Font,new Rectangle(textLeft,0,Math.Max(1,Width-textLeft-4),Height),Enabled?ForeColor:WheelVisual.Muted,
    TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding|
    (Badge==null?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left));
  }
 }
}
