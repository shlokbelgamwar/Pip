using System.Runtime.InteropServices;
namespace Pip {
// Add-only Photos permission is requested only after the user chooses Save to Photos.
public static class PipPhotos {
#if UNITY_IOS && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void PipSaveMemoryToPhotos(string path,string receiver);
 public static bool Supported=>true;
 public static void Save(string path,string receiver){PipSaveMemoryToPhotos(path,receiver);}
#else
 public static bool Supported=>false;
 public static void Save(string path,string receiver){}
#endif
}
}
