using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FusionAssist {
 static class WheelVisual {
  static readonly Color[] dark={Color.FromArgb(20,23,28),Color.FromArgb(29,34,41),
   Color.FromArgb(35,41,48),Color.FromArgb(65,73,84),Color.FromArgb(246,247,249),
   Color.FromArgb(176,184,195),Color.FromArgb(123,133,145)};
  static readonly Color[] light={Color.FromArgb(236,239,243),Color.White,
   Color.FromArgb(247,248,250),Color.FromArgb(201,207,216),Color.FromArgb(25,32,42),
   Color.FromArgb(82,93,108),Color.FromArgb(112,125,143)};
  internal static bool Light{get;set;}
  static Color[] Colors{get{return Light?light:dark;}}
  internal static Color Background{get{return Colors[0];}}
  internal static Color Surface{get{return Colors[1];}}
  internal static Color Raised{get{return Colors[2];}}
  internal static Color Line{get{return Colors[3];}}
  internal static Color Ink{get{return Colors[4];}}
  internal static Color Muted{get{return Colors[5];}}
  internal static Color Edge{get{return Colors[6];}}

  internal static void Recolor(Control root) {
   var previous=Light?dark:light;
   int background=Array.IndexOf(previous,root.BackColor);
   int foreground=Array.IndexOf(previous,root.ForeColor);
   if(background>=0)root.BackColor=Colors[background];
   if(foreground>=0)root.ForeColor=Colors[foreground];
   if(root is Form&&root.IsHandleCreated)Native.TitleTheme(root.Handle,Light?"Light":"Dark");
   foreach(Control child in root.Controls)Recolor(child);
   root.Invalidate();
  }

  internal static Color AccentText(Color accent){return Light?Tint(accent,Ink,.49f):accent;}

  internal static Color Tint(Color background,Color accent,float ratio) {
   return Color.FromArgb((int)(background.R+(accent.R-background.R)*ratio),
    (int)(background.G+(accent.G-background.G)*ratio),(int)(background.B+(accent.B-background.B)*ratio));
  }

  internal static void Box(Graphics g,RectangleF r,Color fill,Color outline,float radius=8,float stroke=1) {
   if(r.Width<=1||r.Height<=1)return;
   float d=Math.Min(radius*2,Math.Min(r.Width,r.Height));
   using(var path=new GraphicsPath())using(var brush=new SolidBrush(fill))using(var pen=new Pen(outline,stroke)) {
    path.AddArc(r.Left,r.Top,d,d,180,90);path.AddArc(r.Right-d,r.Top,d,d,270,90);
    path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.Left,r.Bottom-d,d,d,90,90);
    path.CloseFigure();g.FillPath(brush,path);g.DrawPath(pen,path);
   }
  }

  internal static Rectangle Keycap(Graphics g,string text,Font font,Color accent,Rectangle area) {
   int width=Math.Min(area.Width,Math.Max(area.Height,TextRenderer.MeasureText(g,text,font,Size.Empty,TextFormatFlags.NoPadding).Width+14));
   var key=new Rectangle(area.X+(area.Width-width)/2,area.Y,width,area.Height);
   Box(g,new RectangleF(key.X+.5f,key.Y+.5f,key.Width-1,key.Height-1),Background,Edge,5);
   TextRenderer.DrawText(g,text,font,key,accent,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
   return key;
  }

  internal static void Draw(Graphics g,HalfWheel wheel,List<Tool> tools,string appId,int selected,bool editor) {
   g.SmoothingMode=editor?SmoothingMode.AntiAlias:SmoothingMode.None;
   var accent=UiPalette.ForApp(appId);
   var fill=Raised;
   var ink=Ink;
   var keys=AccentText(accent);

   for(int i=0;i<tools.Count;i++) {
    if(!tools[i].Enabled&&!editor)continue;
    var wedgeState=g.Save();
    using(var path=wheel.Path(i))using(var brush=new SolidBrush(i==selected?Tint(fill,accent,.16f):fill))
    using(var line=new Pen(i==selected?accent:Edge,i==selected?2.5f:editor?1.5f:1f)) {
     g.FillPath(brush,path);
     g.DrawPath(line,path);
     g.SetClip(path,CombineMode.Intersect);
     DrawLabel(g,wheel,i,path,tools[i],ink,keys);
    }
    g.Restore(wedgeState);
   }
  }

  static void DrawLabel(Graphics g,HalfWheel wheel,int index,GraphicsPath path,Tool tool,Color ink,Color accent) {
   string name=tool.Enabled?(tool.Label??""):"+";
   string shortcut=tool.Enabled?(ShortcutSequence.Badge(tool.Shortcut)??""):"";
   g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
   var layout=wheel.Labels[index];
   if(layout==null||layout.Name!=name||layout.Shortcut!=shortcut) {
    layout=WheelLabelLayout.Fit(g,wheel,index,path,name,shortcut);
    wheel.Labels[index]=layout;
   }
   if(layout==null)return;

   using(var format=new StringFormat(StringFormat.GenericTypographic){Alignment=StringAlignment.Center,FormatFlags=StringFormatFlags.NoWrap})
   using(var label=new Font("Segoe UI",layout.FontSize,FontStyle.Bold,GraphicsUnit.Pixel))
   using(var text=new SolidBrush(ink)) {
    for(int i=0;i<layout.Lines.Length;i++)
     g.DrawString(layout.Lines[i],label,text,new PointF(layout.LabelTop.X,layout.LabelTop.Y+i*layout.LineHeight),format);
    if(layout.Keycap.IsEmpty)return;
    Box(g,layout.Keycap,Background,Edge,4);
    format.LineAlignment=StringAlignment.Center;
    using(var key=new Font("Segoe UI",layout.KeySize,FontStyle.Bold,GraphicsUnit.Pixel))
    using(var keyColor=new SolidBrush(accent))g.DrawString(shortcut,key,keyColor,layout.Keycap,format);
   }
  }
 }
}
