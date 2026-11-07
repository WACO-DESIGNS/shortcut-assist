namespace FusionAssist {
 public class SharedWheel {
  // Count is retained for reading older catalogs. Each program owns its slot count.
  public int Count,Radius;

  public static SharedWheel From(Settings settings) {
   return new SharedWheel{Count=settings.Count,Radius=settings.Radius};
  }

  public void Apply(Settings settings) {
   settings.Radius=System.Math.Max(80,System.Math.Min(400,Radius));
  }
 }
}
