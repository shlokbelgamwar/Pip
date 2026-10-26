using System.Runtime.InteropServices;
using UnityEngine;
namespace Pip {
// Thin wrapper over UIKit feedback generators. Silent in the Editor and on other platforms.
public static class PipHaptics {
#if UNITY_IOS && !UNITY_EDITOR
 [DllImport("__Internal")] static extern void PipHapticImpact(int style);
 [DllImport("__Internal")] static extern void PipHapticNotify(int type);
 [DllImport("__Internal")] static extern void PipHapticSelection();
#endif
 static float last;
 // Coalesce bursts so a single interaction never buzzes twice.
 static bool Ready(){if(Time.unscaledTime-last<.06f)return false;last=Time.unscaledTime;return true;}
 static void Impact(int style){if(!Ready())return;
#if UNITY_IOS && !UNITY_EDITOR
  PipHapticImpact(style);
#endif
 }
 // UIImpactFeedbackStyle: 0 light, 1 medium, 3 soft.
 public static void Light()=>Impact(0);
 public static void Medium()=>Impact(1);
 public static void Soft()=>Impact(3);
 public static void Selection(){if(!Ready())return;
#if UNITY_IOS && !UNITY_EDITOR
  PipHapticSelection();
#endif
 }
 // UINotificationFeedbackType: 1 warning.
 public static void Warning(){if(!Ready())return;
#if UNITY_IOS && !UNITY_EDITOR
  PipHapticNotify(1);
#endif
 }
}
}
