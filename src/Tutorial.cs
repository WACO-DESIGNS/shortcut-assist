using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FusionAssist {
 public partial class MainForm {
  void ShowTutorial() {
   var steps=new List<TutorialStep>();
   string activation=SharedKey.Length==0
    ?"Click Set key to record a key or mouse button. With a configured program in front, use that binding to open the wheel and select a filled slot to run its shortcut."
    :"Click the activation key button to change the binding. With a configured program in front, press "+SharedKey+
     " to open the wheel at the pointer, then select a filled slot.";
   steps.Add(new TutorialStep("Set the activation key",activation,activationPanel));
   var tabs=appTabs.Where(tab=>tab.Visible).Cast<Control>().ToArray();
   if(tabs.Length>0)
    steps.Add(new TutorialStep("Choose a program",
     "Select a tab to edit that program’s wheel. Each program has its own slots and shortcuts.",tabs));
   steps.Add(new TutorialStep("Manage your programs",
    "Open the three-dot menu to find more tabs or choose Manage programs. Add a program by selecting its Windows executable.",programsButton));

   if(wheelEditor.Visible) {
    steps.Add(new TutorialStep("Find a tool",
     "Search by tool name or shortcut key. The tool list shows the shortcuts available for this program.",searchField,toolList));
    steps.Add(new TutorialStep("Build your wheel",
     "Drag a tool onto a slot, or select a slot and click a tool. Drag filled slots to reorder them. Use Clear slot to empty the selected slot.",wheelPanel));
    steps.Add(new TutorialStep("Change the slot count",
     "Use + or − to resize this program’s wheel. Removing slots hides their shortcuts. Add the slots back to restore them.",removeSlot,slotCount,addSlot));
    steps.Add(new TutorialStep("Add a custom shortcut",
     "Type or record a shortcut or a sequence of keys. Select a slot first to assign it immediately. Use the − button to enter delete mode.",addCustom,deleteTools));
   }

   steps.Add(new TutorialStep("Make it yours",
    "Open Settings to switch between light and dark. In Advanced, change the diameter of the wheel that appears at your pointer.",gear));
   steps.Add(new TutorialStep("Mini HUD",
    "The Mini HUD sits in a bottom corner of the desktop and shows the active program or wheel status. Click it to open Shortcut Assist. In Settings, turn it on or off and choose its corner under Advanced.",gear){ShowsMiniHud=true});
   steps.Add(new TutorialStep("Exit Shortcut Assist",
    "Use Exit app to close Shortcut Assist completely. The window’s X hides the editor and leaves the shortcut wheel running.",exitButton));
   using(var tutorial=new TutorialDialog(this,steps))tutorial.ShowDialog(this);
  }
 }

 sealed class TutorialStep {
  internal readonly string Title,Description;
  internal readonly Rectangle Target;
  internal bool ShowsMiniHud;

  internal TutorialStep(string title,string description,params Control[] controls) {
   Title=title;
   Description=description;
   Target=controls.Select(control=>control.RectangleToScreen(control.ClientRectangle)).Aggregate(Rectangle.Union);
  }
 }

 sealed class TutorialDialog:Form {
  static readonly Color Cutout=Color.Magenta;
  readonly IList<TutorialStep> steps;
  readonly Panel card;
  readonly Label progress,heading,body;
  readonly Panel hudPreview;
  readonly WheelButton back,next,skip;
  Rectangle highlight;
  int index;

  internal TutorialDialog(Form owner,IList<TutorialStep> steps) {
   this.steps=steps;
   Text="Shortcut Assist tutorial";
   FormBorderStyle=FormBorderStyle.None;
   StartPosition=FormStartPosition.Manual;
   ShowInTaskbar=false;
   BackColor=TransparencyKey=Cutout;
   DoubleBuffered=true;
   var ownerBounds=owner.RectangleToScreen(owner.ClientRectangle);
   var visibleBounds=Rectangle.Intersect(ownerBounds,Screen.FromControl(owner).WorkingArea);
   Bounds=visibleBounds.IsEmpty?ownerBounds:visibleBounds;

   card=new TutorialCard();
   progress=new Label{Font=new Font("Segoe UI",9,FontStyle.Bold),ForeColor=WheelVisual.AccentText(UiPalette.Orange)};
   heading=new Label{Font=new Font("Segoe UI",13,FontStyle.Bold),ForeColor=WheelVisual.Ink};
   body=new Label{Font=new Font("Segoe UI",10),ForeColor=WheelVisual.Ink,UseMnemonic=false};
   hudPreview=new Panel{AccessibleName="Mini HUD preview showing Fusion 360"};
   hudPreview.Paint+=(s,e)=>MiniHud.DrawPreview(e.Graphics,hudPreview.ClientSize,
    UiPalette.For(WheelVisual.Light?"Light":"Dark","fusion"),true,true,"Fusion 360");
   back=new WheelButton("Back");
   next=new WheelButton("Next"){SelectedAccent=true};
   skip=new WheelButton("Skip tutorial"){DialogResult=DialogResult.Cancel};
   back.Click+=(s,e)=>ShowStep(index-1);
   next.Click+=(s,e)=>{
    if(index+1==steps.Count){DialogResult=DialogResult.OK;return;}
    ShowStep(index+1);
   };
   card.Controls.AddRange(new Control[]{progress,heading,body,hudPreview,skip,back,next});
   Controls.Add(card);
   AcceptButton=next;
   CancelButton=skip;
   Shown+=(s,e)=>next.Select();
   ShowStep(0);
  }

  void ShowStep(int stepIndex) {
   index=stepIndex;
   var step=steps[index];
   var target=RectangleToClient(step.Target);
   target.Inflate(4,4);
   highlight=Rectangle.Intersect(Rectangle.Inflate(ClientRectangle,-3,-3),target);

   progress.Text=(index+1)+" of "+steps.Count;
   heading.Text=step.Title;
   body.Text=step.Description;
   back.Enabled=index>0;
   next.Text=index+1==steps.Count?"Done":"Next";

   const int width=300,padding=16;
   int innerWidth=width-2*padding;
   int progressHeight=Math.Max(18,progress.PreferredHeight);
   int headingHeight=Math.Max(26,heading.PreferredHeight);
   int bodyHeight=body.GetPreferredSize(new Size(innerWidth,0)).Height+4;
   int headingTop=13+progressHeight+3,bodyTop=headingTop+headingHeight+5;
   int previewTop=bodyTop+bodyHeight+12;
   int footerTop=(step.ShowsMiniHud?previewTop+40:bodyTop+bodyHeight)+18;
   card.SuspendLayout();
   card.Bounds=PlaceCard(new Size(width,footerTop+46));
   progress.SetBounds(padding,13,innerWidth,progressHeight);
   heading.SetBounds(padding,headingTop,innerWidth,headingHeight);
   body.SetBounds(padding,bodyTop,innerWidth,bodyHeight);
   hudPreview.Visible=step.ShowsMiniHud;
   hudPreview.SetBounds(padding,previewTop,innerWidth,40);
   skip.SetBounds(padding,footerTop,110,30);
   back.SetBounds(width-padding-146,footerTop,68,30);
   next.SetBounds(width-padding-70,footerTop,70,30);
   card.ResumeLayout(false);
   Invalidate(true);
  }

  Rectangle PlaceCard(Size size) {
   const int margin=12,gap=14;
   var area=Rectangle.Inflate(ClientRectangle,-margin,-margin);
   int x=Math.Max(area.Left,Math.Min(highlight.Left,area.Right-size.Width));
   int y=Math.Max(area.Top,Math.Min(highlight.Top,area.Bottom-size.Height));
   var candidates=new[]{
    new Rectangle(x,highlight.Bottom+gap,size.Width,size.Height),
    new Rectangle(x,highlight.Top-gap-size.Height,size.Width,size.Height),
    new Rectangle(highlight.Right+gap,y,size.Width,size.Height),
    new Rectangle(highlight.Left-gap-size.Width,y,size.Width,size.Height)
   };
   foreach(var candidate in candidates)if(area.Contains(candidate))return candidate;
   return new Rectangle(x,area.Bottom-size.Height,size.Width,size.Height);
  }

  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   if(highlight.Width<=0||highlight.Height<=0)return;
   using(var pen=new Pen(UiPalette.Orange,3))e.Graphics.DrawRectangle(pen,highlight);

   var bubble=card.Bounds;
   int x=Math.Max(bubble.Left+16,Math.Min(highlight.Left+highlight.Width/2,bubble.Right-16));
   int y=Math.Max(bubble.Top+16,Math.Min(highlight.Top+highlight.Height/2,bubble.Bottom-16));
   Point[] pointer;
   if(bubble.Top>highlight.Bottom)
    pointer=new[]{new Point(x-7,bubble.Top),new Point(x,bubble.Top-8),new Point(x+7,bubble.Top)};
   else if(bubble.Bottom<highlight.Top)
    pointer=new[]{new Point(x-7,bubble.Bottom-1),new Point(x,bubble.Bottom+7),new Point(x+7,bubble.Bottom-1)};
   else if(bubble.Left>highlight.Right)
    pointer=new[]{new Point(bubble.Left,y-7),new Point(bubble.Left-8,y),new Point(bubble.Left,y+7)};
   else if(bubble.Right<highlight.Left)
    pointer=new[]{new Point(bubble.Right-1,y-7),new Point(bubble.Right+7,y),new Point(bubble.Right-1,y+7)};
   else return;
   using(var brush=new SolidBrush(UiPalette.Orange))e.Graphics.FillPolygon(brush,pointer);
  }
 }

 sealed class TutorialCard:Panel {
  internal TutorialCard() {
   BackColor=WheelVisual.Surface;
   DoubleBuffered=true;
   ResizeRedraw=true;
  }

  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   using(var border=new Pen(UiPalette.Orange,2))e.Graphics.DrawRectangle(border,1,1,Width-2,Height-2);
  }
 }
}
