using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FusionAssist {
 static class IntroMotion {
  internal const double FadeStart=1.45;
  internal const double Duration=2.0;

  internal static bool Winking(double elapsed){return elapsed>=.55&&elapsed<1.05;}

  internal static double Opacity(double elapsed) {
   double progress=Math.Max(0,Math.Min(1,(elapsed-FadeStart)/(Duration-FadeStart)));
   return 1-progress*progress*(3-2*progress);
  }
 }

 sealed class LaunchContext:ApplicationContext {
  readonly MainForm app;
  readonly StartupIntro intro;

  internal LaunchContext():this(CreateMainForm()) {}

  internal LaunchContext(MainForm app) {
   this.app=app;
   MainForm=app;
   intro=new StartupIntro(app.Bounds);
   intro.FadeStarted+=()=>{
    app.Show();
    app.Update();
   };
   intro.Finished+=()=>{
    app.FinishStartup();
    intro.Close();
   };
   intro.FormClosed+=(s,e)=>{if(app.StartupPending)app.Shutdown();};
   intro.Show();
  }

  static MainForm CreateMainForm() {
   var app=new MainForm(true){StartPosition=FormStartPosition.Manual};
   var area=Screen.PrimaryScreen.WorkingArea;
   app.Location=new Point(area.Left+Math.Max(0,(area.Width-app.Width)/2),
    area.Top+Math.Max(0,(area.Height-app.Height)/2));
   return app;
  }

  protected override void Dispose(bool disposing) {
   if(disposing) {
    intro.Dispose();
    app.Dispose();
   }
   base.Dispose(disposing);
  }
 }

 sealed class StartupIntro:Form {
  static readonly Color Background=Color.FromArgb(70,73,78);
  readonly Timer timer=new Timer{Interval=15};
  readonly Stopwatch clock=new Stopwatch();
  bool fading,finished;

  internal event Action FadeStarted;
  internal event Action Finished;

  internal StartupIntro(Rectangle bounds) {
   Text=MainForm.WindowTitle;
   FormBorderStyle=FormBorderStyle.None;
   AllowTransparency=true;
   ShowInTaskbar=false;
   TopMost=true;
   StartPosition=FormStartPosition.Manual;
   Bounds=bounds;
   BackColor=Background;
   DoubleBuffered=true;
   timer.Tick+=(s,e)=>Advance(clock.Elapsed.TotalSeconds);
   Shown+=(s,e)=>{
    clock.Start();
    timer.Start();
   };
  }

  void Advance(double elapsed) {
   if(finished)return;
   if(elapsed<IntroMotion.FadeStart){Invalidate();return;}
   if(!fading) {
    fading=true;
    if(FadeStarted!=null)FadeStarted();
   }
   Opacity=IntroMotion.Opacity(elapsed);
   if(elapsed<IntroMotion.Duration)return;
   finished=true;
   timer.Stop();
   if(Finished!=null)Finished();
  }

  protected override void OnPaint(PaintEventArgs e) {
   base.OnPaint(e);
   double elapsed=clock.Elapsed.TotalSeconds;
   const int logoSize=200,titleHeight=52;
   int top=(ClientSize.Height-logoSize-titleHeight)/2;
   var logo=new Rectangle((ClientSize.Width-logoSize)/2,top,logoSize,logoSize);
   AppAssets.DrawFace(e.Graphics,logo,UiPalette.Orange,Background,IntroMotion.Winking(elapsed));
   using(var font=new Font("Segoe UI",26,FontStyle.Bold))
   using(var brush=new SolidBrush(Color.White))
   using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center}) {
    e.Graphics.DrawString("Shortcut Assist",font,brush,new RectangleF(0,logo.Bottom,ClientSize.Width,titleHeight),format);
   }
  }

  protected override void Dispose(bool disposing) {
   if(disposing)timer.Dispose();
   base.Dispose(disposing);
  }
 }

 public partial class MainForm {
  internal const string WindowTitle="Shortcut Assist  /  v1.5";
  public bool StartupPending{get;private set;}

  internal void FinishStartup() {
   StartupPending=false;
   Text=WindowTitle;
   tray.Visible=true;
   OpenSettings();
  }
 }
}
