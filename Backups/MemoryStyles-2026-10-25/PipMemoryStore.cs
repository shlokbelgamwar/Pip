using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace Pip {
// App-private local gallery. This store never reads credentials or sends network requests.
public sealed class PipMemoryStore {
 [Serializable] public sealed class Entry {
  public string id,createdUtc,subject,remark;
  public int width,height;
  public bool withPipOverlay,hidePipOverlay;
 }
 public readonly string Folder;
 public readonly List<Entry> Entries=new List<Entry>();
 public PipMemoryStore(string folder){Folder=folder;Reload();}
 public void Reload(){
  Entries.Clear();if(!Directory.Exists(Folder))return;
  foreach(var file in Directory.GetFiles(Folder,"*.json")){
   try{var entry=JsonUtility.FromJson<Entry>(File.ReadAllText(file));
    if(entry==null||!Valid(entry.id)||Path.GetFileNameWithoutExtension(file)!=entry.id||!File.Exists(ImagePath(entry))||!File.Exists(ThumbnailPath(entry)))continue;
    Entries.Add(entry);
   }catch(Exception){/* Incomplete/corrupt records do not prevent opening other memories. */}
  }
  Entries.Sort((a,b)=>string.CompareOrdinal(b.createdUtc,a.createdUtc));
 }
 static bool Valid(string id){Guid value;return Guid.TryParseExact(id,"N",out value);}
 public string ImagePath(Entry entry){if(entry==null||!Valid(entry.id))throw new ArgumentException("Invalid memory");return Path.Combine(Folder,entry.id+".jpg");}
 public string ThumbnailPath(Entry entry){if(entry==null||!Valid(entry.id))throw new ArgumentException("Invalid memory");return Path.Combine(Folder,entry.id+"-thumb.jpg");}
 public string CleanImagePath(Entry entry){if(entry==null||!Valid(entry.id))throw new ArgumentException("Invalid memory");return Path.Combine(Folder,entry.id+"-clean.jpg");}
 public string CleanThumbnailPath(Entry entry){if(entry==null||!Valid(entry.id))throw new ArgumentException("Invalid memory");return Path.Combine(Folder,entry.id+"-clean-thumb.jpg");}
 public bool HasClean(Entry entry)=>entry!=null&&entry.withPipOverlay&&File.Exists(CleanImagePath(entry))&&File.Exists(CleanThumbnailPath(entry));
 public string DisplayPath(Entry entry,bool thumbnail=false)=>entry.hidePipOverlay&&HasClean(entry)?(thumbnail?CleanThumbnailPath(entry):CleanImagePath(entry)):(thumbnail?ThumbnailPath(entry):ImagePath(entry));
 public void SetOverlay(Entry entry,bool enabled){
  if(!enabled&&!HasClean(entry))throw new InvalidOperationException("Original version unavailable");
  bool previous=entry.hidePipOverlay;entry.hidePipOverlay=!enabled;
  string record=Path.Combine(Folder,entry.id+".json"),temporary=record+".tmp";
  try{File.WriteAllText(temporary,JsonUtility.ToJson(entry));File.Replace(temporary,record,null);}
  catch{entry.hidePipOverlay=previous;try{if(File.Exists(temporary))File.Delete(temporary);}catch(Exception){}throw;}
 }
 public Entry Save(byte[] jpeg,byte[] thumbnail,int width,int height,string subject,string remark,bool withPipOverlay=false,byte[] cleanJpeg=null,byte[] cleanThumbnail=null){
  if(jpeg==null||thumbnail==null||jpeg.Length<4||thumbnail.Length<4||width<=0||height<=0)throw new ArgumentException("Missing captured image");
  Directory.CreateDirectory(Folder);
  var e=new Entry{id=Guid.NewGuid().ToString("N"),createdUtc=DateTime.UtcNow.ToString("O"),subject=subject??"",remark=remark??"",width=width,height=height,withPipOverlay=withPipOverlay};
  string image=ImagePath(e),thumb=ThumbnailPath(e),record=Path.Combine(Folder,e.id+".json");
  try{
   File.WriteAllBytes(image,jpeg);File.WriteAllBytes(thumb,thumbnail);
   if(cleanJpeg!=null&&cleanThumbnail!=null){File.WriteAllBytes(CleanImagePath(e),cleanJpeg);File.WriteAllBytes(CleanThumbnailPath(e),cleanThumbnail);}
   File.WriteAllText(record+".tmp",JsonUtility.ToJson(e));File.Move(record+".tmp",record);
   Entries.Insert(0,e);return e;
  }catch{
   foreach(var path in new[]{image,thumb,CleanImagePath(e),CleanThumbnailPath(e),record+".tmp"})try{if(File.Exists(path))File.Delete(path);}catch(Exception){}
   throw;
  }
 }
}
}
