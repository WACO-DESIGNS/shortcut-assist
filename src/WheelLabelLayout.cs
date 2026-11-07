using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace FusionAssist {
 sealed class WheelLabelLayout {
  internal readonly string Name,Shortcut;
  internal readonly float FontSize,KeySize,LineHeight;
  internal readonly string[] Lines;
  internal readonly PointF LabelTop;
  internal readonly RectangleF Keycap;

  WheelLabelLayout(string name,string shortcut,float fontSize,float keySize,float lineHeight,
   string[] lines,PointF labelTop,RectangleF keycap) {
   Name=name;Shortcut=shortcut;FontSize=fontSize;KeySize=keySize;LineHeight=lineHeight;
   Lines=lines;LabelTop=labelTop;Keycap=keycap;
  }

  internal static WheelLabelLayout Fit(Graphics graphics,HalfWheel wheel,int index,GraphicsPath wedge,
   string name,string shortcut) {
   float band=wheel.Radius-wheel.InnerRadius;
   double angle=(180-(index+.5)*180/wheel.Count)*Math.PI/180;
   double tangent=2*(wheel.InnerRadius+band*.59)*Math.Sin(Math.PI/(2*wheel.Count));
   int maxSize=(int)Math.Round(Math.Min(27,Math.Min(band*.27,tangent*.25)));
   maxSize=Math.Max(7,Math.Min(30,maxSize));
   string title=name.Trim();
   if(title.Length>120)title=title.Substring(0,119)+"…";
   var centers=new List<PointF>();
   foreach(float fraction in new[]{.59f,.51f,.67f,.43f,.75f}){
    float radius=wheel.InnerRadius+band*fraction;
    centers.Add(new PointF(wheel.Center.X+(float)Math.Cos(angle)*radius,wheel.Center.Y+(float)Math.Sin(angle)*radius));
   }
   using(var format=new StringFormat(StringFormat.GenericTypographic){FormatFlags=StringFormatFlags.NoWrap}) {
    for(int size=maxSize;size>=7;size--) {
     var result=TryFit(graphics,wheel,wedge,centers,name,shortcut,title,size,format);
     if(result!=null)return result;
    }

    // Keep the binding visible when a custom name is too long for a very thin wedge.
    for(int length=Math.Min(title.Length-1,28);length>=3;length--) {
     var result=TryFit(graphics,wheel,wedge,centers,name,shortcut,title.Substring(0,length).TrimEnd()+"…",7,format);
     if(result!=null)return result;
    }
    return TryFit(graphics,wheel,wedge,centers,name,shortcut,"",7,format);
   }
  }

  static WheelLabelLayout TryFit(Graphics g,HalfWheel wheel,GraphicsPath wedge,List<PointF> centers,string name,
   string shortcut,string title,int size,StringFormat format) {
   float keySize=Math.Max(5,Math.Min(size*.72f,(wheel.Radius-wheel.InnerRadius)*1.25f/Math.Max(1,shortcut.Length)));
   using(var label=new Font("Segoe UI",size,FontStyle.Bold,GraphicsUnit.Pixel))
   using(var key=new Font("Segoe UI",keySize,FontStyle.Bold,GraphicsUnit.Pixel)) {
    float lineHeight=(float)Math.Ceiling(label.GetHeight(g));
    float capHeight=shortcut.Length==0?0:(float)Math.Ceiling(key.GetHeight(g))+4;
    float capWidth=shortcut.Length==0?0:Math.Max(capHeight,g.MeasureString(shortcut,key,PointF.Empty,format).Width+7);
    float gap=capHeight==0||title.Length==0?0:Math.Max(2,size*.18f);
    foreach(var lines in LineOptions(title)) {
     var widths=new float[lines.Length];
     for(int i=0;i<lines.Length;i++)widths[i]=g.MeasureString(lines[i],label,PointF.Empty,format).Width;
     float height=lineHeight*lines.Length+gap+capHeight;
     foreach(var center in centers) {
      float top=center.Y-height/2;
      var cap=new RectangleF(center.X-capWidth/2,top+lines.Length*lineHeight+gap,capWidth,capHeight);
      if(capHeight>0&&!Fits(wheel,wedge,cap))continue;
      bool fits=true;
      for(int i=0;i<widths.Length&&fits;i++)
       fits=Fits(wheel,wedge,new RectangleF(center.X-widths[i]/2,top+i*lineHeight,widths[i],lineHeight));
      if(fits)return new WheelLabelLayout(name,shortcut,size,keySize,lineHeight,lines,new PointF(center.X,top),cap);
     }
    }
   }
   return null;
  }

  static IEnumerable<string[]> LineOptions(string title) {
   if(title.Length==0){yield return new string[0];yield break;}
   yield return new[]{title};
   string[] words=title.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
   for(int i=1;i<words.Length;i++){
    if(words[i]=="/")continue;
    yield return new[]{String.Join(" ",words,0,i),String.Join(" ",words,i,words.Length-i)};
   }
  }

  static bool Fits(HalfWheel wheel,GraphicsPath wedge,RectangleF box) {
   box.Inflate(2,1.5f);
   if(!wedge.IsVisible(box.Left,box.Top)||!wedge.IsVisible(box.Right,box.Top)||
    !wedge.IsVisible(box.Left,box.Bottom)||!wedge.IsVisible(box.Right,box.Bottom))return false;
   float x=Math.Max(box.Left,Math.Min(box.Right,wheel.Center.X))-wheel.Center.X;
   float y=Math.Max(box.Top,Math.Min(box.Bottom,wheel.Center.Y))-wheel.Center.Y;
   return x*x+y*y>=wheel.InnerRadius*wheel.InnerRadius;
  }
 }
}
