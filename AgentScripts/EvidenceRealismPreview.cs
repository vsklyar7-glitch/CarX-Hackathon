using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EvidenceRealismPreview
{
    public static string Render()
    {
        if(Application.isPlaying)throw new Exception("Stop Play Mode first");
        Directory.CreateDirectory("output/evidence-realism");
        var scene=EditorSceneManager.NewPreviewScene();
        var originals=new List<GameObject>();
        try
        {
            var cameraObject=new GameObject("Evidence preview camera",typeof(Camera));SceneManager.MoveGameObjectToScene(cameraObject,scene);
            var camera=cameraObject.GetComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=.225f;camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.30f,.27f,.22f);camera.transform.position=new Vector3(.14f,.48f,-.22f);camera.transform.LookAt(new Vector3(0,.012f,0));
            Light(scene,"Warm key",Quaternion.Euler(50,-25,0),2.2f,new Color(1,.88f,.70f));
            Light(scene,"Cool fill",Quaternion.Euler(55,155,0),1.4f,new Color(.68f,.78f,1));
            foreach(string path in Directory.GetFiles("Assets/InteractionContent/Prefabs","*.prefab"))
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var evidence=prefab.GetComponent<EvidenceItem>();if(evidence==null)continue;
                var instance=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(instance,scene);originals.Add(instance);
                Save(camera,900,900,"output/evidence-realism/"+evidence.evidenceId+".png");
                if(evidence.evidenceId!="family_photo")
                {
                    Color old=camera.backgroundColor;camera.backgroundColor=new Color(.83f,.76f,.60f);
                    camera.transform.position=new Vector3(0,.6f,-.1f);camera.transform.LookAt(Vector3.zero);
                    FitIllustration(camera,instance);
                    Save(camera,512,512,"Assets/Resources/NotebookArt/"+evidence.evidenceId+".png");
                    camera.orthographicSize=.225f;camera.backgroundColor=old;camera.transform.position=new Vector3(.14f,.48f,-.22f);camera.transform.LookAt(new Vector3(0,.012f,0));
                }
                instance.SetActive(false);
            }
            for(int i=0;i<originals.Count;i++)
            {
                originals[i].SetActive(true);originals[i].transform.position=new Vector3((i%4-1.5f)*.37f,0,(i/4-.5f)*.40f);
            }
            camera.orthographicSize=.53f;camera.transform.position=new Vector3(0,1.7f,-.85f);camera.transform.LookAt(Vector3.zero);
            Save(camera,1600,1000,"output/evidence-realism/overview.png");
            AssetDatabase.Refresh();
            return "Rendered 8 model close-ups, overview and updated notebook thumbnails";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    // Fit visible geometry, not the deliberately generous interaction collider.
    private static void FitIllustration(Camera camera,GameObject instance)
    {
        foreach(var text in instance.GetComponentsInChildren<TMP_Text>())text.ForceMeshUpdate(true,true);
        var renderers=instance.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
        if(renderers.Length==0)throw new Exception("Evidence has no visible geometry");
        Bounds bounds=renderers[0].bounds;
        foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
        Vector3 direction=camera.transform.forward;
        camera.transform.position=bounds.center-direction*.6f;
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        foreach(var renderer in renderers)
        {
            Bounds b=renderer.bounds;
            for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            {
                Vector3 p=camera.transform.InverseTransformPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                min=Vector2.Min(min,new Vector2(p.x,p.y));max=Vector2.Max(max,new Vector2(p.x,p.y));
            }
        }
        Vector2 middle=(min+max)*.5f;
        camera.transform.position+=camera.transform.right*middle.x+camera.transform.up*middle.y;
        // Square 512px image; the longest dimension occupies 90% of the frame.
        camera.orthographicSize=Mathf.Max(max.x-min.x,max.y-min.y)*.5f/.9f;
    }
    private static void Light(Scene scene,string name,Quaternion rotation,float intensity,Color color)
    {
        var go=new GameObject(name,typeof(Light));SceneManager.MoveGameObjectToScene(go,scene);go.transform.rotation=rotation;
        var light=go.GetComponent<Light>();light.type=LightType.Directional;light.intensity=intensity;light.color=color;
    }
    private static void Save(Camera camera,int width,int height,string path)
    {
        var rt=new RenderTexture(width,height,24);var texture=new Texture2D(width,height,TextureFormat.RGB24,false);var old=RenderTexture.active;
        try
        {
            camera.targetTexture=rt;camera.Render();camera.Render();RenderTexture.active=rt;
            texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());
        }
        finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
    }
}
