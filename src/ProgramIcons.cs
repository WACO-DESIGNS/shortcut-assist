using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace FusionAssist {
 sealed class ProgramIcons:IDisposable {
  readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>(StringComparer.OrdinalIgnoreCase);
  readonly Dictionary<string,string> observedPaths=new Dictionary<string,string>();

  internal Bitmap Get(AppProfile profile) {
   var image=Load(profile.ExecutablePath);
   if(image!=null)return image;
   var app=SupportedApps.ById(profile.Id);
   if(app==null)return null;
   string observed;
   if(observedPaths.TryGetValue(app.Id,out observed)) {
    image=Load(observed);
    if(image!=null)return image;
   }
   return Load(RegisteredPath(app.Process+".exe"))??Load(CommonPath(app));
  }

  internal Bitmap Observe(string id,uint processId) {
   if(processId==0||SupportedApps.ById(id)==null)return null;
   try {
    using(var process=Process.GetProcessById((int)processId)) {
     var path=process.MainModule.FileName;
     if(SupportedApps.ByProcess(Path.GetFileName(path))!=SupportedApps.ById(id))return null;
     var image=Load(path);
     if(image!=null)observedPaths[id]=path;
     return image;
    }
   }catch(ArgumentException){return null;}
    catch(InvalidOperationException){return null;}
    catch(System.ComponentModel.Win32Exception){return null;}
  }

  Bitmap Load(string path) {
   if(!Native.IsLocalDrivePath(path)||!File.Exists(path))return null;
   Bitmap image;
   if(images.TryGetValue(path,out image))return image;
   try {
    using(var icon=Icon.ExtractAssociatedIcon(path)) {
     if(icon==null)return null;
     image=icon.ToBitmap();
     images.Add(path,image);
     return image;
    }
   }catch(ArgumentException){return null;}
    catch(ExternalException){return null;}
  }

  static string RegisteredPath(string fileName) {
   string keyName=@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\"+fileName;
   foreach(var root in new[]{Registry.CurrentUser,Registry.LocalMachine}) {
    try {
     using(var key=root.OpenSubKey(keyName)) {
      var value=key==null?null:key.GetValue("") as string;
      if(value!=null) {
       var path=value.Trim('"');
       if(Native.IsLocalDrivePath(path)&&File.Exists(path))return path;
      }
     }
    }catch(UnauthorizedAccessException){}
     catch(System.Security.SecurityException){}
   }
   return null;
  }

  static string CommonPath(AppDefinition app) {
   string programFiles=Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
   if(app.Id=="solidworks")return Path.Combine(programFiles,"SOLIDWORKS Corp","SOLIDWORKS","SLDWORKS.exe");
   if(app.Id!="fusion")return null;

   string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
   string userLocal=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"AppData","Local");
   foreach(string root in new[]{local,userLocal,programFiles}.Distinct(StringComparer.OrdinalIgnoreCase)) {
    string production=Path.Combine(root,"Autodesk","webdeploy","production");
    if(!Native.IsLocalDrivePath(production)||!Directory.Exists(production))continue;
    try {
     var executable=Directory.EnumerateDirectories(production)
      .Select(folder=>new FileInfo(Path.Combine(folder,"Fusion360.exe"))).Where(file=>file.Exists)
      .OrderByDescending(file=>file.LastWriteTimeUtc).FirstOrDefault();
     if(executable!=null)return executable.FullName;
    }catch(IOException){}
     catch(UnauthorizedAccessException){}
   }
   return null;
  }

  public void Dispose() {
   foreach(var image in images.Values)image.Dispose();
   images.Clear();
  }
 }
}
