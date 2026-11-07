namespace FusionAssist {
 public class SharedActivation {
  public string Trigger {get;set;}public bool Ctrl {get;set;}public bool Alt {get;set;}public bool Shift {get;set;}
  public static SharedActivation From(Settings s){return new SharedActivation{Trigger=s.Trigger,Ctrl=s.Ctrl,Alt=s.Alt,Shift=s.Shift};}
  public static SharedActivation Parse(string text){if(string.IsNullOrWhiteSpace(text))return null;string[] parts=text.Split('+');string last=parts[parts.Length-1].Trim();bool mouse=last=="Mouse middle"||last=="Mouse side 1"||last=="Mouse side 2"||last=="Mouse left"||last=="Mouse right";string chord=mouse?text.Substring(0,text.LastIndexOf(last))+"F8":text;var spec=ShortcutSpec.Parse(chord);if(spec==null)return null;return new SharedActivation{Trigger=mouse?last:ShortcutInput.KeyName((System.Windows.Forms.Keys)spec.Key),Ctrl=spec.Modifiers.Contains(17),Alt=spec.Modifiers.Contains(18),Shift=spec.Modifiers.Contains(16)};}
  public void Apply(Settings s) {
   bool assigned=!string.IsNullOrWhiteSpace(Trigger);
   s.Trigger=assigned?(Native.TriggerCode(Trigger)==0?"F8":Trigger):"";
   s.Ctrl=assigned&&Ctrl;
   s.Alt=assigned&&Alt;
   s.Shift=assigned&&Shift;
  }
 }

}

