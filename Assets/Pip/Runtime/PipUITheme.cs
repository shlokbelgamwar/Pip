using UnityEngine;
namespace Pip {
// Native font fallback and small vector-like masks; no image assets or extra UI packages.
public sealed class PipUITheme {
 public readonly Color Ink=new Color(.15f,.14f,.13f,.84f),Glass=new Color(.15f,.14f,.13f,.68f),Control=new Color(.13f,.13f,.12f,.48f);
 public readonly Color Paper=new Color(.97f,.96f,.93f,.98f),Dark=new Color(.16f,.20f,.17f),Mint=new Color(.71f,.91f,.80f),White=new Color(.98f,.97f,.94f);
 public readonly Font Font;
 public readonly Texture2D Tail,Play;
 public PipUITheme() {
  Font=UnityEngine.Font.CreateDynamicFontFromOSFont(new[]{"Helvetica Neue","Arial"},36);
  Tail=Triangle(32,22,new Vector2(0,0),new Vector2(32,0),new Vector2(22,22));
  Play=Triangle(24,28,new Vector2(2,1),new Vector2(23,14),new Vector2(2,27));
 }
 static float Edge(Vector2 a,Vector2 b,Vector2 p)=>(p.x-a.x)*(b.y-a.y)-(p.y-a.y)*(b.x-a.x);
 static Texture2D Triangle(int w,int h,Vector2 a,Vector2 b,Vector2 c) {
  var t=new Texture2D(w,h,TextureFormat.RGBA32,false){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};var pixels=new Color[w*h];
  for(int y=0;y<h;y++)for(int x=0;x<w;x++){int hits=0;for(int sy=0;sy<4;sy++)for(int sx=0;sx<4;sx++){var p=new Vector2(x+(sx+.5f)/4,h-y-(sy+.5f)/4);float e1=Edge(a,b,p),e2=Edge(b,c,p),e3=Edge(c,a,p);if((e1<=0&&e2<=0&&e3<=0)||(e1>=0&&e2>=0&&e3>=0))hits++;}pixels[y*w+x]=new Color(1,1,1,hits/16f);}
  t.SetPixels(pixels);t.Apply();return t;
 }
 public void Dispose(){if(Font)Object.Destroy(Font);Object.Destroy(Tail);Object.Destroy(Play);}
}
}
