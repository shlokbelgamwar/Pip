using UnityEngine;
using UnityEngine.InputSystem.XR;
namespace Pip {
// This prototype is portrait-only. On the tested iOS/ARKit package combination,
// CameraRotation retains the landscape sensor axes while projection/background are portrait.
// Correct the camera basis, not the creature or its world-space anchor.
public class PipARCameraPoseDriver : TrackedPoseDriver {
 protected override void SetLocalTransform(Vector3 position,Quaternion rotation) {
#if UNITY_IOS && !UNITY_EDITOR
  if(Screen.orientation==ScreenOrientation.Portrait)
   rotation=rotation*Quaternion.AngleAxis(90,Vector3.forward);
#endif
  base.SetLocalTransform(position,rotation);
 }
}
}
