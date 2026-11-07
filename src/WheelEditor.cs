using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace FusionAssist {
 sealed class ToolChoice {
  internal readonly string Id,Label,Shortcut;
  internal ToolChoice(string id,string label,string shortcut){Id=id;Label=label;Shortcut=shortcut;}
  internal string PresetKey{get{return PresetIdentity(Label,Shortcut);}}
  internal static string PresetIdentity(string label,string shortcut){return label+"\u001f"+shortcut;}
  public override string ToString(){return Label+"  ·  "+Shortcut;}
 }

 sealed class SlotDrag {
  internal readonly Settings Profile;
  internal readonly ToolChoice Choice;
  internal readonly int SourceSlot;
  internal SlotDrag(Settings profile,ToolChoice choice,int sourceSlot=-1){Profile=profile;Choice=choice;SourceSlot=sourceSlot;}
 }

 sealed class WheelEditor:Control {
  readonly Func<Settings> values;
  int dragSlot=-1,pressedSlot=-1;
  Point press;
  internal int SelectedSlot{get;private set;}
  internal event Action<int> SlotSelected;
  internal event Action<SlotDrag,int> ToolDropped;

  internal WheelEditor(Func<Settings> get) {
   values=get;SelectedSlot=-1;DoubleBuffered=true;ResizeRedraw=true;AllowDrop=true;TabStop=true;
   AccessibleName="Wheel slots. Use the arrow keys to select a slot.";
  }

  HalfWheel Geometry() {
   var s=values();
   int radius=Math.Max(80,Math.Min((Width-46)/2,Height-24));
   return new HalfWheel(s.Count,radius){Gap=0};
  }
  Point Offset(HalfWheel wheel){return new Point((Width-wheel.Size.Width)/2,(Height-wheel.Size.Height)/2);}
  internal int Hit(Point location) {
   var wheel=Geometry();var offset=Offset(wheel);
   return wheel.Hit(new Point(location.X-offset.X,location.Y-offset.Y));
  }
  internal void SelectSlot(int slot) {
   SelectedSlot=slot>=0&&slot<values().Count?slot:-1;
   Invalidate();
   if(SlotSelected!=null)SlotSelected(SelectedSlot);
  }

  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   var wheel=Geometry();var offset=Offset(wheel);
   var state=e.Graphics.Save();
   e.Graphics.TranslateTransform(offset.X,offset.Y);
   var prefs=values();
   WheelVisual.Draw(e.Graphics,wheel,Ranking.Wheel(prefs),prefs.AppId,dragSlot>=0?dragSlot:SelectedSlot,true);
   e.Graphics.Restore(state);
  }
  protected override bool IsInputKey(Keys keyData) {
   return keyData==Keys.Left||keyData==Keys.Right||base.IsInputKey(keyData);
  }
  protected override void OnKeyDown(KeyEventArgs e) {
   if(e.KeyCode==Keys.Left||e.KeyCode==Keys.Right) {
    SelectSlot(Math.Max(0,Math.Min(values().Count-1,SelectedSlot+(e.KeyCode==Keys.Left?-1:1))));
    e.Handled=true;
   }
   base.OnKeyDown(e);
  }
  protected override void OnMouseDown(MouseEventArgs e) {
   base.OnMouseDown(e);
   if(e.Button!=MouseButtons.Left)return;
   press=e.Location;pressedSlot=Hit(press);
   if(pressedSlot>=0){Focus();SelectSlot(pressedSlot);}
  }
  protected override void OnMouseMove(MouseEventArgs e) {
   base.OnMouseMove(e);
   Cursor=Hit(e.Location)>=0?Cursors.Hand:Cursors.Default;
   if(e.Button!=MouseButtons.Left||pressedSlot<0||!Moved(press,e.Location))return;
   int source=pressedSlot;pressedSlot=-1;
   var s=values();var tool=Ranking.Wheel(s)[source];
   if(tool.Enabled)DoDragDrop(new SlotDrag(s,new ToolChoice(tool.Id,tool.Label,tool.Shortcut),source),DragDropEffects.Move);
  }
  protected override void OnMouseUp(MouseEventArgs e){pressedSlot=-1;base.OnMouseUp(e);}
  internal static bool Moved(Point from,Point to) {
   return Math.Abs(from.X-to.X)>SystemInformation.DragSize.Width/2||Math.Abs(from.Y-to.Y)>SystemInformation.DragSize.Height/2;
  }

  SlotDrag Payload(DragEventArgs e){return e.Data.GetData(typeof(SlotDrag)) as SlotDrag;}
  protected override void OnDragEnter(DragEventArgs e){OnDragOver(e);}
  protected override void OnDragOver(DragEventArgs e) {
   var payload=Payload(e);
   dragSlot=payload!=null&&payload.Profile==values()?Hit(PointToClient(new Point(e.X,e.Y))):-1;
   e.Effect=dragSlot>=0?DragDropEffects.Move:DragDropEffects.None;Invalidate();
  }
  protected override void OnDragLeave(EventArgs e){dragSlot=-1;Invalidate();base.OnDragLeave(e);}
  protected override void OnDragDrop(DragEventArgs e) {
   var payload=Payload(e);
   int slot=Hit(PointToClient(new Point(e.X,e.Y)));
   dragSlot=-1;Invalidate();
   if(payload!=null&&payload.Profile==values()&&slot>=0&&ToolDropped!=null)ToolDropped(payload,slot);
  }
 }

 sealed class ToolList:ListBox {
  readonly Func<Settings> values;
  readonly Font keyFont=new Font("Segoe UI",9,FontStyle.Bold);
  readonly ToolTip shortcutHint=new ToolTip();
  Point press;
  int pressed=-1,draggingRow=-1;
  bool dragged,refreshing;
  internal string AssignedId;
  internal bool DeleteMode;

  internal event Action<ToolChoice> ToolClicked;
  internal event Action<ToolChoice> ToolDeleted;

  internal ToolList(Func<Settings> get) {
   values=get;DrawMode=DrawMode.OwnerDrawFixed;IntegralHeight=false;ItemHeight=55;BorderStyle=BorderStyle.None;
   BackColor=WheelVisual.Surface;ForeColor=WheelVisual.Ink;Font=new Font("Segoe UI",11,FontStyle.Bold);
   AccessibleName="Tool library";
  }
  internal void ReplaceRows(IEnumerable<ToolChoice> rows) {
   shortcutHint.SetToolTip(this,"");
   refreshing=true;BeginUpdate();
   try{Items.Clear();Items.AddRange(rows.Cast<object>().ToArray());}
   finally{EndUpdate();refreshing=false;}
  }
  protected override void OnDrawItem(DrawItemEventArgs e) {
   using(var background=new SolidBrush(BackColor))e.Graphics.FillRectangle(background,e.Bounds);
   if(e.Index<0||e.Index>=Items.Count)return;
   var row=(ToolChoice)Items[e.Index];
   var accent=UiPalette.ForApp(values().AppId);
   bool assigned=e.Index==draggingRow||(AssignedId!=null&&AssignedId==row.Id);
   bool focused=(e.State&DrawItemState.Focus)!=0&&Focused;
   var bounds=Rectangle.Inflate(e.Bounds,-2,-4);
   e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
   WheelVisual.Box(e.Graphics,bounds,assigned?WheelVisual.Tint(WheelVisual.Raised,accent,.18f):WheelVisual.Raised,assigned?accent:focused?WheelVisual.Edge:WheelVisual.Line,7,assigned?2:1);
   string badge=ShortcutSequence.Badge(row.Shortcut);
   float measured=TextRenderer.MeasureText(badge,keyFont).Width;
   int keyWidth=Math.Min(bounds.Width/2,Math.Max(30,(int)measured+8));
   var key=new Rectangle(bounds.Right-keyWidth-(DeleteMode?9:30),bounds.Y+(bounds.Height-26)/2,keyWidth,26);
   if(measured>keyWidth-10) {
    using(var compact=new Font(keyFont.FontFamily,Math.Max(5.5f,keyFont.Size*(keyWidth-10)/measured),keyFont.Style))
     WheelVisual.Keycap(e.Graphics,badge,compact,WheelVisual.AccentText(accent),key);
   }else WheelVisual.Keycap(e.Graphics,badge,keyFont,WheelVisual.AccentText(accent),key);
   int labelLeft=bounds.Left+(DeleteMode?44:12);
   TextRenderer.DrawText(e.Graphics,row.Label,Font,new Rectangle(labelLeft,bounds.Y,Math.Max(1,key.Left-labelLeft-8),bounds.Height),
    ForeColor,TextFormatFlags.NoPrefix|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis);
   if(DeleteMode) {
    var remove=DeleteBounds(e.Bounds);
    WheelVisual.Box(e.Graphics,remove,WheelVisual.Tint(WheelVisual.Raised,UiPalette.Orange,.15f),UiPalette.Orange,5);
    TextRenderer.DrawText(e.Graphics,"−",Font,remove,WheelVisual.Ink,
     TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
   }else using(var dot=new SolidBrush(WheelVisual.Muted))
    for(int i=0;i<3;i++)for(int j=0;j<2;j++)e.Graphics.FillEllipse(dot,bounds.Right-21+j*6,bounds.Y+bounds.Height/2-7+i*6,3,3);
  }
  static Rectangle DeleteBounds(Rectangle item){return new Rectangle(item.Left+11,item.Top+(item.Height-24)/2,24,24);}
  protected override void OnMouseDown(MouseEventArgs e) {
   press=e.Location;pressed=e.Button==MouseButtons.Left?IndexFromPoint(press):-1;dragged=false;
   base.OnMouseDown(e);
  }
  protected override void OnMouseMove(MouseEventArgs e) {
   base.OnMouseMove(e);
   if(e.Button==MouseButtons.None) {
    int index=IndexFromPoint(e.Location);
    string hint=index>=0?((ToolChoice)Items[index]).Shortcut:"";
    if(shortcutHint.GetToolTip(this)!=hint)shortcutHint.SetToolTip(this,hint);
   }
   if(DeleteMode||e.Button!=MouseButtons.Left||pressed<0||pressed>=Items.Count||!WheelEditor.Moved(press,e.Location))return;
   var row=(ToolChoice)Items[pressed];draggingRow=pressed;pressed=-1;dragged=true;
   Invalidate();
   try{DoDragDrop(new SlotDrag(values(),row),DragDropEffects.Move);}
   finally{draggingRow=-1;Invalidate();}
  }
  protected override void OnMouseUp(MouseEventArgs e) {
   int index=IndexFromPoint(e.Location);
   bool clicked=e.Button==MouseButtons.Left&&!dragged&&index>=0&&index==pressed;
   pressed=-1;base.OnMouseUp(e);
   if(clicked&&(!DeleteMode||(DeleteBounds(GetItemRectangle(index)).Contains(press)&&DeleteBounds(GetItemRectangle(index)).Contains(e.Location))))Choose(index);
  }
  protected override void OnKeyDown(KeyEventArgs e) {
   if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space||DeleteMode&&e.KeyCode==Keys.Delete){Choose(SelectedIndex);e.Handled=true;e.SuppressKeyPress=true;}
   base.OnKeyDown(e);
  }
  void Choose(int index) {
   if(refreshing||index<0||index>=Items.Count)return;
   var action=DeleteMode?ToolDeleted:ToolClicked;
   if(action!=null)action((ToolChoice)Items[index]);
  }
  protected override void Dispose(bool disposing){
   if(disposing){shortcutHint.Dispose();keyFont.Dispose();}
   base.Dispose(disposing);
  }
 }
}
