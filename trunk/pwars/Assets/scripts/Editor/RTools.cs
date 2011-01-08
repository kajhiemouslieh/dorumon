using UnityEditor;
using System;
using UnityEngine;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;
using GUI = UnityEngine.GUILayout;
using System.IO;
using System.Collections;
using AstarClasses;
using System.Text.RegularExpressions;
using Object = UnityEngine.Object;
[ExecuteInEditMode]
public partial class RTools : InspectorSearch
{
    string file;
    string cspath = @"C:\Users\igolevoc\Documents\PhysxWars\Assets\scripts\GUI\";
    public bool bake;
    protected override void Awake()
    {
        base.Awake();
    }
    protected override void OnGUI()
    {
        BuildButtons();
             
        GUI.BeginHorizontal();
        bake = GUI.Toggle(bake, "Bake");
        if (GUI.Button("SetupLevel"))
        {
            var l = GameObject.Find("level");
            var p = l.transform.parent;
            DestroyImmediate(l);
            string path = EditorApplication.currentScene.Split('.')[0] + "/";
            path = path.Substring("Assets/".Length);
            Debug.Log("setup level: " + path);
            l = (GameObject)Editor.Instantiate(GetAssets<GameObject>(path, "*.FBX").FirstOrDefault());
            l.transform.parent = p;            
            l.name = "level";
            Selection.activeGameObject = l;
            SetupLevel();
            Inits(cspath);
            _TimerA.AddMethod(delegate
            {
                if (bake)
                {
                    var old = RenderSettings.ambientLight;
                    RenderSettings.ambientLight = Color.white * .05f;
                    Lightmapping.Bake();
                    //_TimerA.AddMethod(() => (!Lightmapping.isRunning), delegate
                    {
                        foreach (var a in LightmapSettings.lightmaps)
                        {
                            var t = a.lightmapFar;
                            for (int x = 0; x < t.width; x++)
                            {
                                for (int y = 0; y < t.height; y++)
                                {
                                    var c = t.GetPixel(x, y);
                                    var alf = Math.Min(.1999f, c.a);
                                    t.SetPixel(x, y, new Color(c.r, c.g, c.b, alf));
                                }
                            }
                            t.Apply();
                        }
                    }//);
                    RenderSettings.ambientLight = old;
                }
            });
        }


        if (GUI.Button("Init"))
        {
            Undo.RegisterSceneUndo("SceneInit");
            if (Selection.activeGameObject != null)
                Inits(cspath);
        }

        GUI.EndHorizontal();

        BuildGUI();
        base.OnGUI();   
    }
    private void BuildButtons()
    {
        if (GUILayout.Button("Build"))
        {
            Build();
            return;
        }

        GUI.BeginHorizontal();
        if (GUILayout.Button("Server Editor"))
        {
            _Loader.mapSettings.host = true;
            new SerializedObject(_Loader).ApplyModifiedProperties();
            EditorApplication.isPlaying = true;
        }
        if (GUILayout.Button("Server App"))
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/" + file, "server");
        GUI.EndHorizontal();
        GUI.BeginHorizontal();
        if (GUILayout.Button("Client App"))
            System.Diagnostics.Process.Start(Directory.GetCurrentDirectory() + "/" + file, "client");
        if (GUILayout.Button("Client Editor"))
        {
            _Loader.mapSettings.host = false;
            new SerializedObject(_Loader).ApplyModifiedProperties();
            EditorApplication.isPlaying = true;
        }
        GUI.EndHorizontal();
        if (GUILayout.Button("Open Project Folder"))
        {
            System.Diagnostics.Process.Start(@"C:\Users\igolevoc\Documents\PhysxWars");
        }
    }
    //[MenuItem("RTools/Materials")]
    private static void SetupMaterials()
    {
        var ago = Selection.activeGameObject;
        if (ago != null)
            foreach (var m in ago.GetComponentsInChildren<Renderer>().Where(a => a != null).SelectMany(a => a.sharedMaterials).Where(a => a != null).Distinct())
            {
                var norm = .8f;
                var spec = .8f;
                var n = m.shader.name;
                bool isSpec = (n == "Specular" || n == "Parallax Specular");
                if (n == "Diffuse" || isSpec)
                {
                    m.color = new Color(norm, norm, norm, .2f);
                    if (isSpec)
                        m.SetColor("_SpecColor", new Color(spec, spec, spec, .3f));
                }
            }
    }
    IEnumerable<Transform> GetTransforms(Transform ts)
    {
        foreach (Transform t in ts)
        {
            foreach (var t2 in GetTransforms(t))
                yield return t2;
            yield return t;
        }

    }
    private void Inits(string cspath)
    {
        foreach (var go in Selection.gameObjects)
        {
            go.active = true;
            foreach (Transform t in GetTransforms(go.transform.root))
            {
                t.gameObject.active = true;
            }
        }

        _TimerA.AddMethod(delegate()
        {
            foreach (var go in Selection.gameObjects)
            {
                foreach (var scr in go.GetComponentsInChildren<Base2>())
                {                    
                    foreach (var pf in scr.GetType().GetFields())
                    {
                        InitLoadPath(scr, pf);
                        CreateEnum(cspath, scr, pf);
                        PathFind(scr, pf);
                    }
                    if (scr.networkView != null && scr.networkView.observed == null)
                        scr.networkView.stateSynchronization = NetworkStateSynchronization.Off;
                    scr.Init();
                }
            }
        });

        _TimerA.AddMethod(delegate()
        {
            foreach (var au in Selection.activeGameObject.GetComponentsInChildren<AudioSource>())
                au.minDistance = 10;
        });



        foreach (Transform t in Selection.activeGameObject.GetComponentsInChildren<Transform>())
        {
            if (t.gameObject.animation == null || t.gameObject.animation.clip == null)
            {
                DestroyImmediate(t.gameObject.animation);
            }
            else
            {
            }
            t.gameObject.isStatic = true;
        }

        //var agos = Selection.activeGameObject.GetComponentsInChildren<Animation>().Select(a=>a.gameObject);
        //var ie = agos.GetEnumerator();
        //ie.MoveNext();
        //Next(ie);            
    }
    void Next(IEnumerator<GameObject> ie)
    {
        var ago = ie.Current; 
        Selection.activeGameObject = ago;
        AnimationUtility.StartAnimationMode(new[] { ago });
        _TimerA.AddMethod(500, delegate
        {
            var p = ago.transform.position;
            _TimerA.AddMethod(500, delegate
            {
                ago.transform.position = p;
                var c = ie.Current ;
                ie.MoveNext();
                if (c == ie.Current) return;
                Next(ie);
            });
            AnimationUtility.StopAnimationMode();
        });

    }
    protected override void SetupLevel()
    {
        List<GameObject> destroy = new List<GameObject>();
        foreach (Transform t in Selection.activeGameObject.GetComponentsInChildren<Transform>())
            t.gameObject.layer = LayerMask.NameToLayer("Level");
        base.SetupLevel();

        
        foreach (Transform t in Selection.activeGameObject.transform)
        {

            GameObject g = t.gameObject;
            if (Selection.activeGameObject.transform.parent.Find(t.name) != null)
            {
                Debug.Log("found parent " + g.name);
                DestroyImmediate(g);
            }
        }
        foreach (Transform t in Selection.activeGameObject.transform)
        {
            GameObject g = t.gameObject;
            string[] param = g.name.Split(',');
        
            if (param[0] == ("coll"))
            {
                g.AddOrGet<Box>();
            }
            var items = GetAssets<GameObject>("/Items/", "*.Prefab");
            foreach (var itemPrefab in items)
            {
                if (param[0].ToLower() == itemPrefab.name.ToLower() && g.GetComponent<MonoBehaviour>() == null)
                {
                    GameObject item = ((GameObject)Instantiate(itemPrefab));
                    if (ParseRotation(g.name) != Vector3.zero)
                        item.transform.rotation = Quaternion.LookRotation(ParseRotation(g.name));
                    item.transform.position = t.position;
                    item.transform.parent = t.parent;
                    t.parent = item.transform;
                    item.name = g.name;
                    if (!item.name.StartsWith("lamp"))
                    {
                        destroy.Add(t.gameObject);
                    }
                    else
                    {
                        RaycastHit h;
                        Ray r = new Ray(item.transform.position + item.transform.rotation * Vector3.forward, item.transform.rotation * Vector3.forward);
                        Debug.DrawRay(r.origin, r.direction*1000, Color.red);
                        if (Physics.Raycast(r, out h, 1000, 1 << LayerMask.NameToLayer("Level")))
                            item.light.range = h.distance * 2;
                    }

                }
            }
            if (param[0].ToLower() == "zombiespawn")
            {
                g.renderer.enabled = false;
                g.collider.isTrigger = true;
                g.tag = "SpawnZombie";
            }
            if (g.name == "path")
            {
                Debug.Log("founded path");
                destroy.Add(g);
            }
            
            foreach (string s in Enum.GetNames(typeof(MapItemType)))
            {
                if (param[0].ToLower() == "i" + s.ToLower() && g.GetComponent<MapItem>() == null)
                {
                    g.AddComponent<MapItem>();
                }
            }

        }

        foreach (var a in destroy)
            DestroyImmediate(a);
        _TimerA.AddMethod(delegate
        {
            foreach (Transform t in Selection.activeGameObject.GetComponentsInChildren<Transform>())
                if (t.name.StartsWith("hide"))
                {
                    DestroyImmediate(t.gameObject);
                }
        });
    }
    private static void PathFind(Base2 scr, FieldInfo pf)
    {
        PathFind atr = (PathFind)pf.GetCustomAttributes(true).FirstOrDefault(a => a is PathFind);
        if (atr != null)
        {
            try
            {
                GameObject g = atr.scene ? GameObject.Find(atr.name).gameObject : scr.transform.Find(atr.name).gameObject;
                if (pf.FieldType == typeof(GameObject))
                    pf.SetValue(scr, g);
                else
                    pf.SetValue(scr, g.GetComponent(pf.FieldType));
            }
            catch { Debug.Log("cound not find path " + scr.name + "+" + atr.name); }
        }
    }
    private static void InitLoadPath(Base2 scr, FieldInfo pf)
    {
        LoadPath ap = (LoadPath)pf.GetCustomAttributes(true).FirstOrDefault(a => a is LoadPath);
        if (ap != null)
        {
            object value = pf.GetValue(scr);
            if (value is Array)
            {
                var o = Base2.GetFiles().Where(a => a.Contains(ap.name))
                                    .Select(a => UnityEditor.AssetDatabase.LoadAssetAtPath(a, pf.FieldType))
                                    .Where(a => a != null).Cast<AudioClip>().ToArray();
                if (o.Length == 0)
                    Debug.Log("could not find audioSources " + ap.name);

                pf.SetValue(scr, o);
            }
            else
                if ((value == null || value.Equals(null)))
                {                                        
                    pf.SetValue(scr, Base2.FindAsset(ap.name, pf.FieldType));
                }
        }
    }
    //[MenuItem("RTools/SetupTextures")]
    private static void SetupTextures()
    {
        foreach (var o in Selection.objects)
        {
            if (o is Texture2D)
            {
                TextureImporter ti = ((TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(o)));
                int max = 256;
                if (!ti.lightmap)
                {
                    TextureImporterSettings tis = new TextureImporterSettings();
                    ti.ReadTextureSettings(tis);
                    tis.maxTextureSize = max;
                    ti.SetTextureSettings(tis);

                    AssetDatabase.ImportAsset(ti.assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }
    }
    private void BuildGUI()
    {
        GUI.Space(10);
        if (Application.isPlaying && Base2._Game!=null)
        {
            foreach (Player p in Base2._Game.players)
                if (p != null)
                    if (GUI.Button(p.name + ":" + p.OwnerID))
                        Selection.activeObject = p;
        }        
       
        if (_Loader != null)
        {
            
            _Loader.build = GUI.Toggle(_Loader.build, "build");
            _Loader.disablePathFinding = GUI.Toggle(_Loader.disablePathFinding, "disable path finding");
            _Loader.dontcheckwin = GUI.Toggle(_Loader.dontcheckwin, "dont check win");
        }

    }
    private static void CreateEnum(string cspath, Base2 g, FieldInfo f)
    {
        GenerateEnums ge = (GenerateEnums)f.GetCustomAttributes(true).FirstOrDefault(a => a is GenerateEnums);
        if (ge != null)
        {
            string cs = "";
            Debug.Log("Found!" + ge.name);
            cs += "public enum " + ge.name + ":int{none = -1,";
            var ie = (IEnumerable)f.GetValue(g);
            foreach (object o in ie)
                cs += o + ",";
            cs = cs.Trim(new[] { ',' });
            cs += "}";
            Debug.Log("geneerated:" + cs);
            File.WriteAllText(cspath + ge.name + ".cs", cs);
        }
    }
    private void Build()
    {
        file = "Builds/" + DateTime.Now.ToFileTime() + "/";
        Directory.CreateDirectory(file);
        BuildPipeline.BuildPlayer(new[] { EditorApplication.currentScene }, (file = file + "Game.Exe"), BuildTarget.StandaloneWindows, BuildOptions.Development);
    }
    protected override void Update()
    {        
        
        base.Update();
    }
    private static Loader _Loader
    {
        get
        {
            Loader l = (Loader)GameObject.FindObjectsOfTypeIncludingAssets(typeof(Loader)).FirstOrDefault();
            return l;
        }
    }    
    public static Vector3 ParseRotation(string name)
    {
        Match m;
        if ((m = Regex.Match(name, ",(-?(?:y|x|z))(?:$|,)")).Success)
        {
            string s = m.Groups[1].Value;
            Vector3 v = new Vector3();
            switch (s)
            {
                case "x":
                    v = (new Vector3(-1, 0, 0));
                    break;
                case "-x":
                    v = (new Vector3(1, 0, 0));
                    break;
                case "-z":
                    v = (new Vector3(0, -1, 0));
                    break;
                case "z":
                    v = (new Vector3(0, 1, 0));
                    break;
                case "y":
                    v = (new Vector3(0, 0, -1));
                    break;
                case "-y":
                    v = (new Vector3(0, 0, 1));
                    break;
            };
            return v;
        }
        return new Vector3();
    }
    IEnumerable<T> GetAssets<T>(string path, string pattern) where T : Object
    {
        foreach (string f2 in Directory.GetFiles("Assets/" + path, pattern, SearchOption.AllDirectories))
        {
            string f = f2.Replace(@"\", "/").Replace("//", "/");
            var a = (T)AssetDatabase.LoadAssetAtPath(f, typeof(T));
            if (a != null)
                yield return a;
        }
    }
    
}
