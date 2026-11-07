using System;
using System.Collections.Generic;
using System.Linq;
namespace FusionAssist {
 public static class StarterTools {
  // Windows default bindings, checked against Adobe Help and the SOLIDWORKS vendor blog.
  public static List<Tool> For(string id){string[,] entries;
   switch(id){
    case "photoshop":entries=new string[,]{{"Undo","Ctrl+Z"},{"Redo","Ctrl+Shift+Z"},{"Move","V"},{"Brush","B"},{"Eraser","E"},{"Selection tools","W"},{"Crop","C"},{"Free Transform","Ctrl+T"},{"Deselect","Ctrl+D"},{"New Layer via Copy","Ctrl+J"},{"Lasso","L"},{"Zoom","Z"},{"Path Selection","A"}};break;
    case "illustrator":entries=new string[,]{{"Selection","V"},{"Direct Selection","A"},{"Pen","P"},{"Rectangle","M"},{"Shape Builder","Shift+M"},{"Type","T"},{"Ellipse","L"},{"Rotate","R"},{"Eyedropper","I"},{"Zoom","Z"},{"Undo","Ctrl+Z"},{"Redo","Ctrl+Shift+Z"}};break;
    case "solidworks":entries=new string[,]{{"Rebuild","Ctrl+B"},{"Zoom to Fit","F"},{"Normal To","Ctrl+8"},{"Isometric","Ctrl+7"},{"Shortcut Bar","S"},{"View Orientation","Space"},{"Line","L"},{"Force Rebuild","Ctrl+Q"},{"Front View","Ctrl+1"},{"Save","Ctrl+S"},{"Undo","Ctrl+Z"}};break;
    default:return new List<Tool>();
   }
   var result=new List<Tool>();for(int i=0;i<entries.GetLength(0);i++)result.Add(new Tool{Label=entries[i,0],Shortcut=entries[i,1],Priority=100-i,Command="Starter:"+id+":"+i});return result;
  }
  public static void Seed(AppProfile profile){if(profile.StarterToolsVersion>=1||profile.Id=="fusion")return;var s=profile.Preferences;WheelAssignments.Ensure(s);
   foreach(var tool in For(profile.Id)){
    // Preserve renamed, rebound and disabled existing tools; never duplicate a binding.
    var existing=s.Tools.FirstOrDefault(t=>String.Equals(t.Shortcut,tool.Shortcut,StringComparison.OrdinalIgnoreCase)||String.Equals(t.Label,tool.Label,StringComparison.OrdinalIgnoreCase));
    if(existing!=null)continue;s.Tools.Add(tool);
    int empty=s.WheelSlots.FindIndex(id=>id==null);if(empty>=0&&empty<s.Count)WheelAssignments.Assign(s,tool.Id,empty);
   }profile.StarterToolsVersion=1;
  }
  public static string Conflict(Settings s){if(s.Ctrl||s.Alt||s.Shift||s.Trigger==null)return null;var tool=For(s.AppId).FirstOrDefault(t=>String.Equals(t.Shortcut,s.Trigger,StringComparison.OrdinalIgnoreCase));return tool==null?null:s.Trigger+" is also "+tool.Label+" in this app. The trigger reaches the app too.";}
 }
}
