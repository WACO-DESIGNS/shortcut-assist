using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace FusionAssist {
 public class HudPlacement {
  public double X {get;set;} public double Y {get;set;}
  public string Anchor {get;set;}
  public HudPlacement(){X=0;Y=1;Anchor="Bottom right";}
 }
 public class HalfWheel {
  public int Count,Radius,InnerRadius;public double Gap;public Size Size;public PointF Center;
  internal readonly WheelLabelLayout[] Labels;
  public HalfWheel(int count,int radius,int innerRadius=-1) {
   Count=Math.Max(1,Math.Min(12,count));Radius=Math.Max(80,radius);InnerRadius=innerRadius<0?(int)Math.Round(Radius*53.0/190):Math.Max(0,Math.Min(Radius-15,innerRadius));
   Labels=new WheelLabelLayout[Count];
   Gap=Math.Min(1.2,180.0/Count*.08);Size=new Size(Radius*2+4,Radius+4);Center=new PointF(Radius+2,2);
  }
  public System.Drawing.Drawing2D.GraphicsPath Path(int index) {
   double step=180.0/Count;float start=(float)(180-(index+1)*step+Gap/2),sweep=(float)(step-Gap);
   var path=new System.Drawing.Drawing2D.GraphicsPath();
   path.AddArc(Center.X-Radius,Center.Y-Radius,Radius*2,Radius*2,start,sweep);
   if(InnerRadius>0)path.AddArc(Center.X-InnerRadius,Center.Y-InnerRadius,InnerRadius*2,InnerRadius*2,start+sweep,-sweep);else path.AddLine(path.GetLastPoint(),Center);
   path.CloseFigure();return path;
  }
  public int Hit(Point point) {
   double dx=point.X-Center.X,dy=point.Y-Center.Y,r=Math.Sqrt(dx*dx+dy*dy);
   if(dy<0||r<=InnerRadius||r>=Radius)return -1;
   double angle=Math.Atan2(dy,dx)*180/Math.PI,step=180.0/Count;
   int fromRight=(int)(angle/step);if(fromRight>=Count)return -1;
   double within=angle-fromRight*step;if(within<Gap/2||within>step-Gap/2)return -1;
   return Count-1-fromRight;
  }
 }
 public static class OverlayLayout {
  public static Point DesktopHud(Rectangle workArea,Size size,string anchor) {
   return new Point(anchor=="Bottom left"?workArea.Left+12:Math.Max(workArea.Left,workArea.Right-size.Width-12),Math.Max(workArea.Top,workArea.Bottom-size.Height-10));
  }
  static double Unit(double value){return Double.IsNaN(value)||Double.IsInfinity(value)?0:Math.Max(0,Math.Min(1,value));}
  public static Point HudPoint(Rectangle bounds,Size size,HudPlacement placement) {
   int width=Math.Max(0,bounds.Width-size.Width),height=Math.Max(0,bounds.Height-size.Height);
   return new Point(bounds.Left+(int)Math.Round(width*Unit(placement.X)),bounds.Top+(int)Math.Round(height*Unit(placement.Y)));
  }
  public static HudPlacement HudPosition(Rectangle bounds,Size size,Point location) {
   return new HudPlacement {X=Unit((double)(location.X-bounds.Left)/Math.Max(1,bounds.Width-size.Width)),Y=Unit((double)(location.Y-bounds.Top)/Math.Max(1,bounds.Height-size.Height))};
  }
  public static Point? Below(Rectangle bounds,Size size,Point cursor) {
   if(size.Width>bounds.Width||cursor.Y+12+size.Height>bounds.Bottom)return null;
   return new Point(Math.Max(bounds.Left,Math.Min(bounds.Right-size.Width,cursor.X-size.Width/2)),Math.Max(bounds.Top,cursor.Y+12));
  }
 }
}
