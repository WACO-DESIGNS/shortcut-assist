using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FusionAssist {
 public class UiPalette {
  public bool Dark;public Color Background,Surface,Input,Ink,Muted,Line,Canvas,AccentText,Accent;
  public static readonly Color Orange=Color.FromArgb(255,122,24);
  public static Color ForApp(string id){switch(id){case "solidworks":return Color.FromArgb(239,83,80);case "photoshop":return Color.FromArgb(66,165,245);case "illustrator":return Color.FromArgb(255,154,0);default:return Orange;}}
  public static Color OnAccent(Color color){return color.GetBrightness()>.4?Color.FromArgb(25,25,25):Color.White;}
  public static UiPalette For(string mode,string appId=null) {
   bool dark=!String.Equals(mode,"Light",StringComparison.OrdinalIgnoreCase);
   Color accent=ForApp(appId);Color accentInk=dark?accent:ControlPaint.Dark(accent,.25f);
   return new UiPalette{Dark=dark,Background=dark?Color.FromArgb(29,30,32):Color.FromArgb(240,241,243),Surface=dark?Color.FromArgb(39,40,43):Color.White,
    Input=dark?Color.FromArgb(53,55,59):Color.FromArgb(244,245,247),Ink=dark?Color.FromArgb(242,243,245):Color.FromArgb(32,34,38),
    Muted=dark?Color.FromArgb(164,169,178):Color.FromArgb(103,109,120),Line=dark?Color.FromArgb(65,68,74):Color.FromArgb(217,221,227),Canvas=dark?Color.FromArgb(23,24,26):Color.FromArgb(231,234,238),Accent=accent,AccentText=accentInk};
  }
 }
 public class AccentButton:Button {
  public Color? IndicatorColor {get;set;}
  bool over;
  public AccentButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;UseVisualStyleBackColor=false;Cursor=Cursors.Hand;SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.Opaque,true);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;using(var path=Outline()){var old=Region;Region=new Region(path);if(old!=null)old.Dispose();}}
  GraphicsPath Outline(){var path=new GraphicsPath();int corner=Math.Min(16,Math.Max(4,Height/2));var r=new Rectangle(0,0,Width-1,Height-1);path.AddArc(r.Left,r.Top,corner,corner,180,90);path.AddArc(r.Right-corner,r.Top,corner,corner,270,90);path.AddArc(r.Right-corner,r.Bottom-corner,corner,corner,0,90);path.AddArc(r.Left,r.Bottom-corner,corner,corner,90,90);path.CloseFigure();return path;}
  protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);over=true;Invalidate();}
  protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);over=false;Invalidate();}
  protected override void OnPaintBackground(PaintEventArgs e){e.Graphics.Clear(Parent==null?BackColor:Parent.BackColor);}
  protected override void OnPaint(PaintEventArgs e) {
   var g=e.Graphics;g.Clear(Parent==null?SystemColors.Control:Parent.BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;
   using(var path=Outline()) {
    using(var brush=new SolidBrush(over?ControlPaint.Light(BackColor,.1f):BackColor))g.FillPath(brush,path);
    if(Focused)using(var pen=new Pen(ForeColor,1){DashStyle=DashStyle.Dot})g.DrawPath(pen,path);
   }
   var format=new StringFormat{Alignment=TextAlign==ContentAlignment.MiddleLeft?StringAlignment.Near:StringAlignment.Center,LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap};
   using(var brush=new SolidBrush(Enabled?ForeColor:Color.Gray))g.DrawString(Text,Font,brush,new RectangleF(TextAlign==ContentAlignment.MiddleLeft?15:4,1,Width-(TextAlign==ContentAlignment.MiddleLeft?20:8),Height-2),format);
   if(IndicatorColor.HasValue)using(var pen=new Pen(IndicatorColor.Value,2))g.DrawLine(pen,10,Height-3,Width-10,Height-3);
  }
 }
 public class ThemeSwitch:Control {
  public bool Dark;public Color Accent;
  public ThemeSwitch(){DoubleBuffered=true;Cursor=Cursors.Hand;TabStop=true;AccessibleRole=AccessibleRole.CheckButton;AccessibleName="Dark mode";}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;e.SuppressKeyPress=true;}base.OnKeyDown(e);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);var g=e.Graphics;g.Clear(BackColor);g.SmoothingMode=SmoothingMode.AntiAlias;int x=Width-56,y=(Height-26)/2;
   using(var path=new GraphicsPath()){path.AddArc(x,y,26,26,90,180);path.AddArc(x+28,y,26,26,270,180);path.CloseFigure();using(var b=new SolidBrush(Dark?Accent:Color.FromArgb(156,163,175)))g.FillPath(b,path);}
   using(var b=new SolidBrush(Color.White))g.FillEllipse(b,Dark?x+30:x+4,y+4,18,18);
   using(var f=new Font("Segoe UI",8,FontStyle.Bold))using(var b=new SolidBrush(ForeColor))g.DrawString(Dark?"DARK":"LIGHT",f,b,new RectangleF(0,0,x-7,Height),new StringFormat{Alignment=StringAlignment.Far,LineAlignment=StringAlignment.Center});
   if(Focused)ControlPaint.DrawFocusRectangle(g,ClientRectangle,ForeColor,BackColor);
  }
 }
 public class OrangeCheckBox:CheckBox {
  public Color AccentColor=UiPalette.Orange;
  public OrangeCheckBox(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);Cursor=Cursors.Hand;}
  protected override void OnPaint(PaintEventArgs e) {
   e.Graphics.Clear(BackColor);int y=(Height-15)/2;var rect=new Rectangle(1,y,15,15);
   using(var brush=new SolidBrush(Checked?AccentColor:Color.FromArgb(105,111,120)))e.Graphics.FillRectangle(brush,rect);
   if(Checked)using(var pen=new Pen(Color.FromArgb(25,25,25),2)){e.Graphics.DrawLines(pen,new[]{new Point(4,y+7),new Point(7,y+10),new Point(13,y+4)});}
   using(var brush=new SolidBrush(ForeColor))e.Graphics.DrawString(Text,Font,brush,new RectangleF(24,0,Width-25,Height),new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter});
  }
 }
 public class PageHost:TabControl {
  public PageHost(){Appearance=TabAppearance.FlatButtons;SizeMode=TabSizeMode.Fixed;ItemSize=new Size(0,1);Multiline=true;}
  protected override void WndProc(ref Message message){if(message.Msg==0x1328&&!DesignMode){message.Result=(IntPtr)1;return;}base.WndProc(ref message);}
  void ClipFrame(){if(!IsHandleCreated)return;var previous=Region;Region=new Region(DisplayRectangle);if(previous!=null)previous.Dispose();}
  protected override void OnHandleCreated(EventArgs e){base.OnHandleCreated(e);ClipFrame();}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);ClipFrame();}
 }
 public class BrandWheel:Control {
  public BrandWheel(){DoubleBuffered=true;BackColor=UiPalette.Orange;}
  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   int radius=Math.Min(Width/2-8,Height-8);var wheel=new HalfWheel(7,radius,(int)(radius*.38));
   e.Graphics.TranslateTransform((Width-wheel.Size.Width)/2f,4);
   for(int i=0;i<7;i++)using(var path=wheel.Path(i))using(var brush=new SolidBrush(i==2?Color.White:Color.FromArgb(35+i*4,35+i*4,35+i*4)))e.Graphics.FillPath(brush,path);
   using(var font=new Font("Segoe UI",10,FontStyle.Bold))using(var brush=new SolidBrush(UiPalette.Orange))e.Graphics.DrawString("F",font,brush,wheel.Center.X-6,wheel.Radius*.65f);
  }
 }
}
