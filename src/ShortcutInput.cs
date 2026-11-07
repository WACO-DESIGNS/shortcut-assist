using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
namespace FusionAssist {
 public class ShortcutPreset {
  public string Name,Chord;public ShortcutPreset(string name,string chord){Name=name;Chord=chord;}
  public override string ToString(){return Name+"  ·  "+Chord;}
  public static readonly ShortcutPreset[] All={new ShortcutPreset("Undo","Ctrl+Z"),new ShortcutPreset("Redo","Ctrl+Y"),new ShortcutPreset("Redo (alternate)","Ctrl+Shift+Z"),new ShortcutPreset("Undo (alternate)","Ctrl+Alt+Z"),new ShortcutPreset("Copy","Ctrl+C"),new ShortcutPreset("Paste","Ctrl+V"),new ShortcutPreset("Cut","Ctrl+X"),new ShortcutPreset("Save","Ctrl+S"),new ShortcutPreset("Select all","Ctrl+A")};
  public static ShortcutPreset[] ForApp(string id){bool adobe=id=="photoshop"||id=="illustrator";return All.Select(p=>adobe&&p.Name=="Redo"?new ShortcutPreset("Redo","Ctrl+Shift+Z"):adobe&&p.Name=="Redo (alternate)"?new ShortcutPreset("Redo (alternate)","Ctrl+Y"):p).Concat(StarterTools.For(id).Select(t=>new ShortcutPreset(t.Label,t.Shortcut))).GroupBy(p=>p.Chord).Select(g=>g.First()).ToArray();}
 }
 public class ShortcutInput:TextBox,IMessageFilter {
  Keys pendingModifier;
  readonly List<string> recordedSteps=new List<string>();
  internal bool RecordSequence;
  public bool Recording{get;private set;}public bool AllowMouse;public event EventHandler RecordingChanged;
  public static string MouseName(MouseButtons button){switch(button){case MouseButtons.Middle:return "Mouse middle";case MouseButtons.XButton1:return "Mouse side 1";case MouseButtons.XButton2:return "Mouse side 2";case MouseButtons.Left:return "Mouse left";case MouseButtons.Right:return "Mouse right";default:return null;}}
  protected override void OnMouseDown(MouseEventArgs e){if(Recording&&AllowMouse){string name=MouseName(e.Button);if(name!=null){Text=((ModifierKeys&Keys.Control)!=0?"Ctrl+":"")+((ModifierKeys&Keys.Alt)!=0?"Alt+":"")+((ModifierKeys&Keys.Shift)!=0?"Shift+":"")+name;SetRecording(false);return;}}base.OnMouseDown(e);}
  public void SetRecording(bool value){if(Recording==value)return;Recording=value;pendingModifier=Keys.None;if(value){recordedSteps.Clear();Application.AddMessageFilter(this);}else Application.RemoveMessageFilter(this);if(RecordingChanged!=null)RecordingChanged(this,EventArgs.Empty);}
  public bool PreFilterMessage(ref Message msg){if(!Recording||!Focused)return false;
   if(msg.HWnd==Handle){
    var key=(Keys)msg.WParam.ToInt32();
    if(msg.Msg==0x100||msg.Msg==0x104){
     if(!RecordSequence||(msg.LParam.ToInt64()&(1L<<30))==0)CaptureChord(key|ModifierKeys);
     return true;
    }
    if(msg.Msg==0x101||msg.Msg==0x105){ReleaseModifier(key);return true;}
   }
   if(!AllowMouse)return false;
   MouseButtons button;
   switch(msg.Msg){case 0x204:button=MouseButtons.Right;break;case 0x207:button=MouseButtons.Middle;break;case 0x20B:long code=(msg.WParam.ToInt64()>>16)&0xffff;button=code==1?MouseButtons.XButton1:code==2?MouseButtons.XButton2:MouseButtons.None;break;default:return false;}
   string name=MouseName(button);if(name==null)return false;RecordMouse(name);return true;
  }
  void RecordMouse(string name){Text=((ModifierKeys&Keys.Control)!=0?"Ctrl+":"")+((ModifierKeys&Keys.Alt)!=0?"Alt+":"")+((ModifierKeys&Keys.Shift)!=0?"Shift+":"")+name;SetRecording(false);}
  protected override void Dispose(bool disposing){if(disposing)Application.RemoveMessageFilter(this);base.Dispose(disposing);}
  public static string Chord(Keys keys){Keys code=keys&Keys.KeyCode;var parts=new List<string>();if((keys&Keys.Control)!=0)parts.Add("Ctrl");if((keys&Keys.Alt)!=0)parts.Add("Alt");if((keys&Keys.Shift)!=0)parts.Add("Shift");parts.Add(KeyName(code));string result=String.Join("+",parts);return ShortcutSpec.Parse(result)==null?null:result;}
  protected override bool IsInputKey(Keys keyData){return Recording||base.IsInputKey(keyData);}
  protected override bool ProcessCmdKey(ref Message msg,Keys keyData){if(Recording){CaptureChord(keyData);return true;}return base.ProcessCmdKey(ref msg,keyData);}
  protected override void OnKeyDown(KeyEventArgs e){if(Recording){CaptureChord(e.KeyData);e.Handled=true;e.SuppressKeyPress=true;return;}base.OnKeyDown(e);}
  internal static string KeyName(Keys key){
   switch(key){
    case Keys.ControlKey:return "Ctrl";
    case Keys.Menu:return "Alt";
    case Keys.ShiftKey:return "Shift";
    case Keys.Capital:return "CapsLock";
    case Keys.Oemtilde:return "`";
    default:return key.ToString();
   }
  }

  void CaptureChord(Keys keys){
   Keys code=keys&Keys.KeyCode;
   if(code==Keys.Escape&&!RecordSequence){SetRecording(false);return;}
   if(code==Keys.ControlKey||code==Keys.Menu||code==Keys.ShiftKey){
    Keys own=code==Keys.ControlKey?Keys.Control:code==Keys.Menu?Keys.Alt:Keys.Shift;
    pendingModifier=(keys&Keys.Modifiers&~own)==Keys.None?code:Keys.None;
    return;
   }
   pendingModifier=Keys.None;
   string value=Chord(keys);
   if(value!=null&&RecordSequence&&code>=Keys.D0&&code<=Keys.D9)value=value.Remove(value.Length-2,1);
   if(value!=null)FinishRecording(value);
  }

  void ReleaseModifier(Keys key){
   if(pendingModifier!=Keys.None&&key==pendingModifier)FinishRecording(KeyName(key));
  }

  protected override void OnKeyUp(KeyEventArgs e){
   if(Recording){ReleaseModifier(e.KeyCode);e.Handled=true;return;}
   base.OnKeyUp(e);
  }

  void FinishRecording(string value){
   if(RecordSequence)recordedSteps.Add(value);
   Text=RecordSequence?String.Join(", ",recordedSteps):value;
   SelectionStart=Text.Length;
   if(!RecordSequence||recordedSteps.Count==ShortcutSequence.MaximumSteps)SetRecording(false);
  }
  protected override void OnLostFocus(EventArgs e){if(!RecordSequence)SetRecording(false);base.OnLostFocus(e);}
 }
}

