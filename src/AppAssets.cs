using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.Windows.Forms;

namespace FusionAssist {
 public static class AppAssets {
  public static readonly Bitmap MarkImage=LoadMark();
  public static readonly Icon AppIcon=LoadIcon();
  public static bool HasResources{get{return MarkImage!=null&&Array.IndexOf(Assembly.GetExecutingAssembly().GetManifestResourceNames(),"FusionAssist.AppIcon.ico")>=0;}}
  static Bitmap LoadMark(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FusionAssist.BrandMark.png")){if(stream==null)return null;using(var image=new Bitmap(stream))return new Bitmap(image);}}
  static Icon LoadIcon(){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("FusionAssist.AppIcon.ico")){if(stream==null)return (Icon)SystemIcons.Application.Clone();using(var icon=new Icon(stream,new Size(32,32)))return (Icon)icon.Clone();}}
  public static void DrawMark(Graphics g,Rectangle bounds,Color tint){if(MarkImage==null)return;using(var attrs=new System.Drawing.Imaging.ImageAttributes()){
   var matrix=new System.Drawing.Imaging.ColorMatrix(new float[][]{new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,1,0},new float[]{tint.R/255f,tint.G/255f,tint.B/255f,0,1}});attrs.SetColorMatrix(matrix);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(MarkImage,bounds,0,0,MarkImage.Width,MarkImage.Height,GraphicsUnit.Pixel,attrs);}}
  public static Icon StatusIcon(Color status) {
   using(var bitmap=new Bitmap(32,32))using(var g=Graphics.FromImage(bitmap)) {
    g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.SmoothingMode=SmoothingMode.AntiAlias;
    if(MarkImage!=null)g.DrawImage(MarkImage,0,0,32,32);else g.DrawIcon(AppIcon,new Rectangle(0,0,32,32));
    using(var brush=new SolidBrush(status))g.FillEllipse(brush,21,21,10,10);
    using(var pen=new Pen(Color.White,1))g.DrawEllipse(pen,21,21,10,10);
    IntPtr handle=bitmap.GetHicon();var result=(Icon)Icon.FromHandle(handle).Clone();Native.DestroyIcon(handle);return result;
   }
  }
  public static void DrawFace(Graphics g,Rectangle bounds,Color tint,Color background,bool wink){DrawMark(g,bounds,tint);if(!wink)return;g.SmoothingMode=SmoothingMode.AntiAlias;float x=bounds.X,y=bounds.Y,size=bounds.Width;using(var b=new SolidBrush(background))g.FillRectangle(b,x+size*.53f,y+size*.34f,size*.17f,size*.32f);using(var pen=new Pen(tint,Math.Max(2,size*.035f)){StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawArc(pen,x+size*.563f,y+size*.46f,size*.115f,size*.055f,0,180);}
 }
 public class BrandMark:Control {
  readonly Timer winkTimer=new Timer();public Color Tint=Color.FromArgb(54,56,59);public bool IsWinking{get;private set;}
  public BrandMark(){SetStyle(ControlStyles.StandardDoubleClick,false);DoubleBuffered=true;BackColor=UiPalette.Orange;Cursor=Cursors.Hand;TabStop=true;AccessibleName="Shortcut Assist mascot. Click to wink.";winkTimer.Interval=380;winkTimer.Tick+=(s,e)=>{winkTimer.Stop();IsWinking=false;Invalidate();};}
  public void Wink(){IsWinking=true;winkTimer.Stop();winkTimer.Start();Invalidate();}
  protected override void OnClick(EventArgs e){base.OnClick(e);Wink();}
  protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
  protected override void Dispose(bool disposing){if(disposing)winkTimer.Dispose();base.Dispose(disposing);}
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(AppAssets.MarkImage==null)return;int size=Math.Min(Width,Height)-8,x=(Width-size)/2,y=(Height-size)/2;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   AppAssets.DrawFace(e.Graphics,new Rectangle(x,y,size,size),Tint,BackColor,IsWinking);
  }
 }
}
