using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EvidenceRealismBuilder
{
    private const string Root = "Assets/EvidenceRealism";
    private static Material paper, ivory, leather, wood, brass, steel, ink, red, photo;
    private static TMP_FontAsset font;
    private static readonly HashSet<string> OriginalParts = new HashSet<string> { "Cover", "Page", "Writing", "Mark", "PortraitHead", "PortraitBody", "DrawnDoor", "DrawnHead", "DrawnBody", "DrawnArms", "KeyRing", "Shaft", "Tooth", "Tag17", "RealisticVisual" };

    public static string Build()
    {
        if (EditorApplication.isPlaying) throw new Exception("Stop Play Mode before updating evidence assets.");
        if (SceneManager.GetActiveScene().isDirty) throw new Exception("Save the active scene first; no scene changes have been discarded.");
        Directory.CreateDirectory(Root + "/Materials");
        Directory.CreateDirectory(Root + "/Meshes");
        Directory.CreateDirectory(Root + "/Textures");
        AssetDatabase.Refresh();
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/InteractionContent/RussianFont.asset");
        SetupMaterials();
        int prefabs = 0, sceneItems = 0;
        foreach (string path in Directory.GetFiles("Assets/InteractionContent/Prefabs", "*.prefab"))
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EvidenceItem>() == null) continue;
            var contents = PrefabUtility.LoadPrefabContents(path);
            try { Upgrade(contents.GetComponent<EvidenceItem>()); PrefabUtility.SaveAsPrefabAsset(contents, path); prefabs++; }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        Scene previous = SceneManager.GetActiveScene();
        foreach (string path in new[] { "Assets/InteractionContent/InteractionDemo.unity", "Assets/Scenes/Day1.unity", "Assets/Scenes/Day2.unity", "Assets/Scenes/Day3.unity", "Assets/Scenes/Scene1.unity", "Assets/Scenes/Scene2.unity", "Assets/Scenes/SampleScene.unity" })
        {
            if (!File.Exists(path)) continue;
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new Exception("Scene has unsaved edits: " + path);
                var items = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EvidenceItem>(true)).ToArray();
                foreach (var item in items) { Upgrade(item); sceneItems++; }
                if (items.Length > 0) EditorSceneManager.SaveScene(scene);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        SceneManager.SetActiveScene(previous);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("output/evidence-realism");
        string report = "Updated " + prefabs + " evidence prefabs and " + sceneItems + " scene instances. IDs, root transforms, colliders, descriptions and story cross-outs preserved.";
        File.WriteAllText("output/evidence-realism/build.txt", report);
        return report;
    }

    private static void SetupMaterials()
    {
        paper = Material("WornPaper", new Color(.76f,.70f,.56f), .03f, 0, PaperTexture());
        ivory = Material("PageEdges", new Color(.73f,.68f,.55f), .08f);
        string source = "Assets/Prefabs/apartments/textures/generated/";
        leather = Material("OxBloodLeather", new Color(.29f,.16f,.12f), .28f, 0, AssetDatabase.LoadAssetAtPath<Texture2D>(source + "gen_leather_black.png"));
        wood = Material("DarkWoodFrame", new Color(.45f,.32f,.22f), .36f, 0, AssetDatabase.LoadAssetAtPath<Texture2D>(source + "gen_wood_brown_hi.png"));
        brass = Material("TarnishedBrass", new Color(.46f,.33f,.16f), .43f, .72f, PaperTexture());
        steel = Material("PaperclipSteel", new Color(.4f,.42f,.41f), .5f, .8f);
        ink = Material("FadedInk", new Color(.11f,.10f,.085f), .04f);
        red = Material("FadedRed", new Color(.32f,.065f,.045f), .03f);
        photo = Material("FamilyPhotograph", Color.white, .32f, 0, AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/NotebookArt/family_photo.png"));
    }
    private static Texture2D PaperTexture()
    {
        string path = Root + "/Textures/PaperGrainV2.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(512,512,TextureFormat.RGB24,false);
            var pixels = new Color[512*512];
            for (int y=0;y<512;y++) for(int x=0;x<512;x++)
            {
                float edge = Mathf.Pow(Mathf.Max(Mathf.Abs(x-255.5f),Mathf.Abs(y-255.5f))/256,7);
                float grain = Mathf.PerlinNoise(x*.8f,y*.8f)*.025f + Mathf.PerlinNoise(x*.027f,y*.027f)*.015f;
                pixels[y*512+x] = new Color(.93f-grain-edge*.07f,.91f-grain-edge*.09f,.84f-grain-edge*.10f);
            }
            texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path,texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Material Material(string name, Color color, float smoothness, float metallic=0, Texture2D texture=null)
    {
        string path=Root+"/Materials/"+name+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(result==null) { result=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(result,path); }
        result.SetColor("_BaseColor",color); result.SetFloat("_Smoothness",smoothness); result.SetFloat("_Metallic",metallic);
        if(texture!=null) result.SetTexture("_BaseMap",texture);
        result.SetFloat("_Cull",0); EditorUtility.SetDirty(result); return result;
    }

    private static void Upgrade(EvidenceItem evidence)
    {
        var root = evidence.transform;
        foreach (Transform child in root.Cast<Transform>().ToArray())
            if(OriginalParts.Contains(child.name)) UnityEngine.Object.DestroyImmediate(child.gameObject);
        var visual=new GameObject("RealisticVisual"); visual.transform.SetParent(root,false);
        Transform t=visual.transform;
        switch(evidence.evidenceId)
        {
            case "case_file": Folder(t); break;
            case "family_photo": Photograph(t); break;
            case "electricity_receipt": Receipt(t); break;
            case "basement_key_17": Key(t); break;
            case "dmitry_diary": Diary(t); break;
            case "ivan_letter": Letter(t, false); break;
            case "child_drawing": Drawing(t); break;
            case "father_note": Letter(t, true); break;
            default: throw new Exception("Unknown evidence ID: "+evidence.evidenceId);
        }
        foreach(Transform child in root)
            if(child.name.StartsWith("Crossed-out Alexei"))
            {
                child.localPosition=new Vector3(.038f,.021f,.025f);
                child.localScale=new Vector3(.052f,.0006f,.003f);
            }
        CombineParts(t,evidence.evidenceId);
    }

    private static void CombineParts(Transform root,string id)
    {
        var parts=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponentInParent<TMP_Text>()==null).ToArray();
        foreach(var group in parts.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
        {
            string path=Root+"/Meshes/Combined_"+id+"_"+group.Key.name+".asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(mesh==null){mesh=new Mesh{name="Combined_"+id+"_"+group.Key.name};AssetDatabase.CreateAsset(mesh,path);}
            mesh.Clear();
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            EditorUtility.SetDirty(mesh);
            MeshObject(root,"Detail_"+group.Key.name,mesh,Vector3.zero,group.Key);
        }
        foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.gameObject);
    }

    private static GameObject MeshObject(Transform parent,string name,Mesh mesh,Vector3 position,Material material)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
        go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<Renderer>().sharedMaterial=material; return go;
    }
    private static Mesh SaveMesh(string name, List<Vector3> vertices,List<int> triangles,List<Vector2> uv)
    {
        string path=Root+"/Meshes/"+name+".asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(mesh!=null)return mesh;
        mesh=new Mesh{name=name}; mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.SetUVs(0,uv); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
    }
    // Rounded outline extruded with bevel rings; no sharp primitive-cube silhouette.
    private static GameObject Slab(Transform t,string name,Vector3 pos,Vector3 size,float radius,Material material)
    {
        string key="Bevel_"+size.x+"_"+size.y+"_"+size.z+"_"+radius;
        var v=new List<Vector3>(); var tri=new List<int>(); var uv=new List<Vector2>(); int n=32;
        for(int ring=0;ring<4;ring++)
        {
            float inset=(ring==0||ring==3)?Mathf.Min(radius*.4f,Mathf.Min(.0012f,size.y*.3f)):0;
            float y=(ring<2?-1:1)*(size.y*.5f-((ring==1||ring==2)?Mathf.Min(.0012f,size.y*.3f):0));
            for(int i=0;i<n;i++)
            {
                int corner=i/8; float a=(corner*90+(i%8)*90f/7)*Mathf.Deg2Rad;
                float x=(corner==0||corner==3?1:-1)*(size.x*.5f-radius)+Mathf.Cos(a)*(radius-inset);
                float z=(corner<2?1:-1)*(size.z*.5f-radius)+Mathf.Sin(a)*(radius-inset);
                v.Add(new Vector3(x,y,z));uv.Add(new Vector2(x/size.x+.5f,z/size.z+.5f));
            }
        }
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n;tri.AddRange(new[]{a,a+n,b,b,a+n,b+n});}
        for(int i=1;i<n-1;i++){tri.AddRange(new[]{0,i,i+1});tri.AddRange(new[]{3*n,3*n+i+1,3*n+i});}
        return MeshObject(t,name,SaveMesh(key,v,tri,uv),pos,material);
    }
    private static GameObject Sheet(Transform t,string name,float width,float depth,float y,float curl,Material material)
    {
        var v=new List<Vector3>();var tri=new List<int>();var uv=new List<Vector2>();int nx=12,nz=18;
        for(int z=0;z<=nz;z++)for(int x=0;x<=nx;x++)
        {
            float u=x/(float)nx,w=z/(float)nz;
            float lift=curl*(Mathf.Pow(u,8)*Mathf.Pow(w,6)+.35f*Mathf.Pow(1-u,9)*Mathf.Pow(1-w,6));
            v.Add(new Vector3((u-.5f)*width,lift,(w-.5f)*depth));uv.Add(new Vector2(u,w));
            if(x<nx&&z<nz){int a=z*(nx+1)+x;tri.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});}
        }
        return MeshObject(t,name,SaveMesh("Sheet_"+width+"_"+depth+"_"+curl,v,tri,uv),new Vector3(0,y,0),material);
    }
    private static void Print(Transform t,string name,string content,Vector3 pos,Vector2 size,float fontSize=1.1f,bool italic=false)
    {
        var go=new GameObject(name,typeof(TextMeshPro));go.transform.SetParent(t,false);go.transform.localPosition=pos;go.transform.localRotation=Quaternion.Euler(90,0,0);
        var text=go.GetComponent<TextMeshPro>();text.font=font;text.text=content;text.fontSize=fontSize;text.color=new Color(.14f,.12f,.095f);text.alignment=TextAlignmentOptions.TopLeft;
        text.rectTransform.sizeDelta=size;text.enableAutoSizing=true;text.fontSizeMin=.015f;text.fontSizeMax=fontSize*.1f;
        text.overflowMode=TextOverflowModes.Truncate;
        if(italic)text.fontStyle=FontStyles.Italic;
        text.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        text.ForceMeshUpdate(true,true);
    }
    private static void Line(Transform t,string name,Vector3 a,Vector3 b,float thickness,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.name=name;go.transform.SetParent(t,false);
        go.transform.localPosition=(a+b)*.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);go.transform.localScale=new Vector3(thickness,(b-a).magnitude*.5f,thickness);go.GetComponent<Renderer>().sharedMaterial=material;
    }
    private static void Folder(Transform t)
    {
        Slab(t,"Manila folder",new Vector3(0,.006f,0),new Vector3(.25f,.003f,.33f),.004f,paper);
        Slab(t,"Index tab",new Vector3(.07f,.007f,.165f),new Vector3(.07f,.002f,.025f),.003f,paper);
        for(int i=0;i<6;i++) Slab(t,"Loose sheet",new Vector3((i%2)*.001f,.009f+i*.00065f,0),new Vector3(.222f,.0005f,.297f),.001f,ivory);
        Sheet(t,"Case report",.22f,.295f,.014f,.004f,paper);
        Print(t,"Police form","POLICE DEPARTMENT\nMISSING PERSON REPORT\n____________________________\n\nNAME: FYODOR KARAMAZOV\nSTATUS: UNRESOLVED\nLAST SEEN: 1991\n\nCASE REVIEW — 1999\n\nAddress verified. Family members\nto be interviewed. Original file\nretained for investigation.",new Vector3(0,.0148f,.015f),new Vector2(.19f,.25f),1.4f);
        Line(t,"Steel clip",new Vector3(-.08f,.016f,.105f),new Vector3(-.08f,.016f,.145f),.0015f,steel);
        Line(t,"Clip return",new Vector3(-.074f,.016f,.105f),new Vector3(-.074f,.016f,.14f),.0015f,steel);
    }
    private static void Photograph(Transform t)
    {
        Slab(t,"Wood backing",new Vector3(0,.009f,0),new Vector3(.255f,.015f,.275f),.005f,wood);
        Slab(t,"Ivory photo mat",new Vector3(0,.017f,0),new Vector3(.237f,.001f,.257f),.001f,ivory);
        Sheet(t,"Printed silver photograph",.215f,.215f,.018f,0,photo);
        for(int i=-1;i<=1;i+=2)
        {
            Slab(t,"Frame side",new Vector3(i*.123f,.019f,0),new Vector3(.012f,.009f,.275f),.002f,wood);
            Slab(t,"Frame end",new Vector3(0,.019f,i*.133f),new Vector3(.24f,.009f,.012f),.002f,wood);
        }
        Print(t,"Photo date","SUMMER 1991",new Vector3(.03f,.0182f,-.12f),new Vector2(.13f,.015f),.7f);
    }
    private static void Receipt(Transform t)
    {
        Sheet(t,"Curled utility receipt",.13f,.28f,.009f,.012f,paper);
        Print(t,"Thermal receipt","CITY ELECTRIC SERVICE\nACCOUNT 0017-91\n--------------------------\nSERVICE STATEMENT\n11 / 08 / 1994\n\nPREVIOUS BALANCE    $0.00\nENERGY CHARGE      $36.40\nSERVICE FEE        $4.00\n--------------------------\nTOTAL PAID        $40.40\n\nPAYMENT RECEIVED\nTHANK YOU\n\nKeep for your records.",new Vector3(0,.0102f,.005f),new Vector2(.112f,.24f),1.0f);
    }
    private static void Diary(Transform t)
    {
        Slab(t,"Lower leather cover",new Vector3(0,.006f,0),new Vector3(.175f,.006f,.24f),.005f,leather);
        Slab(t,"Page block",new Vector3(.003f,.017f,0),new Vector3(.162f,.018f,.224f),.003f,ivory);
        for(int i=0;i<9;i++) Line(t,"Page edge",new Vector3(.085f,.009f+i*.0017f,-.108f),new Vector3(.085f,.009f+i*.0017f,.108f),.00035f,paper);
        Slab(t,"Upper leather cover",new Vector3(0,.029f,0),new Vector3(.175f,.007f,.24f),.005f,leather);
        Slab(t,"Rounded spine",new Vector3(-.084f,.018f,0),new Vector3(.012f,.029f,.235f),.004f,leather);
        Print(t,"Embossed title","ДМИТРИЙ\n\nЛИЧНЫЕ ЗАПИСИ\n1991",new Vector3(.009f,.033f,.03f),new Vector2(.12f,.12f),1.25f);
        t.Find("Embossed title").GetComponent<TextMeshPro>().color=new Color(.62f,.48f,.27f);
        Line(t,"Bookmark ribbon",new Vector3(.04f,.016f,-.10f),new Vector3(.04f,.016f,-.14f),.004f,red);
        for(int side=-1;side<=1;side+=2) for(int i=0;i<22;i++) Line(t,"Cover stitching",new Vector3(side*.075f,.033f,-.1f+i*.009f),new Vector3(side*.075f,.033f,-.097f+i*.009f),.00045f,ivory);
    }
    private static void Letter(Transform t,bool father)
    {
        Sheet(t,"Folded letter",.21f,.27f,.009f,.006f,paper);
        // Crease strips stay flush with the paper, with low contrast rather than black rods.
        Line(t,"Fold crease",new Vector3(-.101f,.0095f,-.035f),new Vector3(.101f,.0095f,-.035f),.0006f,ivory);
        string text=father ? "Алёша,\n\nНе вини себя. Ты был ребёнком.\nТы выполнил мою просьбу,\nне понимая, что случится.\n\nЯ сам решил остаться\nза этой дверью.\n\nПрости меня.\n\nОтец" : "Алёша,\n\nЯ знал, что он скрывает\nв подвале. Мы слишком долго\nделали вид, что ничего\nне происходит.\n\nНе открывай дверь.\n\nМы должны были сказать\nтебе правду раньше.\n\nИван";
        Print(t,"Handwritten message",text,new Vector3(0,.0102f,.005f),new Vector2(.18f,.235f),1.25f,true);
        if(!father)
        {
            Slab(t,"Envelope beneath letter",new Vector3(.025f,.005f,-.015f),new Vector3(.19f,.002f,.12f),.002f,ivory);
            Line(t,"Envelope fold",new Vector3(-.07f,.0063f,-.07f),new Vector3(.02f,.0063f,.025f),.0006f,paper);
        }
    }
    private static void Drawing(Transform t)
    {
        Sheet(t,"Old drawing paper",.21f,.28f,.009f,.008f,paper);
        Slab(t,"Drawn basement doorway",new Vector3(.055f,.010f,.015f),new Vector3(.065f,.00025f,.14f),.001f,ink);
        for(int person=0;person<2;person++)
        {
            float x=-.065f+person*.055f;float h=person==0?.08f:.11f;
            Ring(t,"Crayon head",new Vector3(x,.0105f,h*.5f),.014f,.0012f,red);
            Line(t,"Crayon body",new Vector3(x,.0105f,h*.5f-.015f),new Vector3(x,.0105f,-.04f),.0014f,red);
            Line(t,"Crayon arms",new Vector3(x-.02f,.0105f,.005f),new Vector3(x+.02f,.0105f,.014f),.0014f,red);
            Line(t,"Crayon leg",new Vector3(x,.0105f,-.04f),new Vector3(x-.017f,.0105f,-.075f),.0014f,red);
            Line(t,"Crayon leg",new Vector3(x,.0105f,-.04f),new Vector3(x+.017f,.0105f,-.075f),.0014f,red);
        }
        Print(t,"Child caption","ПАПА  И  Я",new Vector3(-.02f,.0107f,-.112f),new Vector2(.16f,.025f),1.25f);
    }
    private static void Ring(Transform t,string name,Vector3 center,float radius,float wire,Material material)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
        for(int i=0;i<=48;i++)for(int j=0;j<=8;j++)
        {
            float a=i*Mathf.PI*2/48,b=j*Mathf.PI*2/8;
            float r=radius+Mathf.Cos(b)*wire*.5f;
            vertices.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(b)*wire*.5f,Mathf.Sin(a)*r));uv.Add(new Vector2(i/48f,j/8f));
            if(i<48&&j<8){int n=i*9+j;triangles.AddRange(new[]{n,n+1,n+9,n+1,n+10,n+9});}
        }
        MeshObject(t,name,SaveMesh("Ring_"+radius+"_"+wire,vertices,triangles,uv),center,material);
    }
    private static void Key(Transform t)
    {
        // Real key proportions: about 11 cm, not an oversized segmented ring.
        Ring(t,"Brass bow",new Vector3(0,.011f,.035f),.018f,.005f,brass);
        Slab(t,"Key blade",new Vector3(0,.011f,-.013f),new Vector3(.009f,.004f,.068f),.001f,brass);
        for(int i=0;i<4;i++) Slab(t,"Cut tooth",new Vector3(.007f,.011f,-.021f-i*.007f),new Vector3(.011f-i%2*.004f,.004f,.0045f),.0007f,brass);
        for(int i=-1;i<=1;i+=2) Line(t,"Blade groove",new Vector3(i*.002f,.0133f,.012f),new Vector3(i*.002f,.0133f,-.042f),.0007f,ink);
        Ring(t,"Steel split ring",new Vector3(-.022f,.011f,.04f),.015f,.0014f,steel);
        Slab(t,"Worn paper key tag",new Vector3(-.05f,.009f,.023f),new Vector3(.034f,.001f,.05f),.004f,paper);
        Print(t,"Room number 17","17",new Vector3(-.05f,.010f,.022f),new Vector2(.025f,.032f),2.0f);
        Line(t,"Tag string",new Vector3(-.035f,.010f,.043f),new Vector3(-.023f,.011f,.04f),.0008f,ivory);
    }
}
