using UnityEngine;
namespace Pip {
// A thin ring lying on the detected surface shows exactly where Pip will land, instead of a flat disc or screen text.
// It follows the placement marker's pose and reuses its material, so nothing extra is needed in the build.
public class PipSurfaceRing : MonoBehaviour {
 public Transform target;
 public PipPlacement placement;
 MeshRenderer ring;
 float shown;
 void Start() {
  var disc=target?target.GetComponent<Renderer>():null;
  var material=disc?new Material(disc.sharedMaterial):new Material(Shader.Find("Standard"));
  material.color=new Color(.62f,.88f,.76f);
  if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.15f);
  if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);
  if(disc)disc.enabled=false;
  ring=gameObject.AddComponent<MeshRenderer>();ring.sharedMaterial=material;ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
  gameObject.AddComponent<MeshFilter>().sharedMesh=Build(.06f,.0525f,.0055f,64);
 }
 // Annulus plus a small center dot facing up (clockwise from above), so lighting normals stay clean.
 static Mesh Build(float outer,float inner,float dot,int n) {
  var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
  for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;var d=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));v.Add(d*outer);v.Add(d*inner);}
  for(int i=0;i<n;i++){int a=i*2,b=((i+1)%n)*2;Quad(t,a,a+1,b+1,b);}
  int c=v.Count;v.Add(Vector3.zero);for(int i=0;i<24;i++){float a=i*Mathf.PI*2/24;v.Add(new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*dot);}
  for(int i=0;i<24;i++){int a=c+1+i,b=c+1+(i+1)%24;t.AddRange(new[]{c,b,a});}
  var m=new Mesh{name="Pip surface ring"};m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 static void Quad(System.Collections.Generic.List<int> t,int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
 void LateUpdate() {
  if(!ring)return;
  bool on=target&&target.gameObject.activeInHierarchy&&(!placement||placement.IsPlacing);
  shown=Mathf.MoveTowards(shown,on?1:0,Time.unscaledDeltaTime/.18f);
  ring.enabled=shown>.01f;
  if(!ring.enabled)return;
  if(on)transform.SetPositionAndRotation(target.position+target.up*.002f,target.rotation);
  // Settles in when a surface is found, then breathes gently so it reads as "ready".
  float e=1-(1-shown)*(1-shown);
  transform.localScale=Vector3.one*(.7f+.3f*e)*(1+.04f*Mathf.Sin(Time.unscaledTime*3));
 }
}
}
