#if UNITY_EDITOR
using System.Collections.Generic;
//using System.Reflection;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class ResidentialBuilding_Editor : MonoBehaviour
{
    private Transform[] floorTs;

    private GameObject frontBottom;
    private GameObject frontMiddle;
    private GameObject frontTop;
    private GameObject backMiddle;
    private GameObject backBottomSingle;
    private GameObject backTopSingle;
    //private GameObject backBottomS; private GameObject backBottomW; private GameObject backBottomN; private GameObject backBottomE; private GameObject backBottomFull;
    private GameObject backBottomSW; private GameObject backBottomNW; private GameObject backBottomNE; private GameObject backBottomSE;
    private GameObject backTopS; private GameObject backTopW; private GameObject backTopN; private GameObject backTopE; private GameObject backTopFull;
    private GameObject backTopSW; private GameObject backTopNW; private GameObject backTopNE; private GameObject backTopSE;

    //private List<GameObject> parts;

    private Vector3 frontSize, backSize;


    public int floors = 1; private int last_floors = 1;
    public int width = 1; private int last_width = 1;
    public int length = 1; private int last_length = 1;

    private bool update = false;

    public Material[] materials = new Material[2];


    void Awake()
    {
        Initialize();
    }
    private void Reset()
    {
        Initialize();
    }
    void Initialize()
    {
        FetchModels();
        frontSize = frontMiddle.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        frontSize = new Vector3(frontSize.x, frontSize.z, frontSize.y);
        backSize = backMiddle.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        backSize = new Vector3(backSize.x, backSize.z, backSize.y);

        //ClearChildren();
    }

    void OnValidate()
    {
        if (floors != last_floors || width != last_width || length != last_length)
        {
            if (floors < 1) { Debug.LogError("ERROR: Building cannot have less than 1 floor."); return; }
            else if (floors > 100) { Debug.LogWarning("WARNING: Blocked creation of more than 100 floors."); return; }
            if (length < 1) { Debug.LogError("ERROR: Building cannot have less than 1 length."); return; }
            else if (length > 50) { Debug.LogWarning("WARNING: Blocked creation of building with length over 50."); return; }
            if (width < 1) { Debug.LogError("ERROR: Building cannot have less than 1 width."); return; }
            else if (width > 50) { Debug.LogWarning("WARNING: Blocked creation of building with width over 50."); return; }

            last_floors = floors;
            last_width = width;
            last_length = length;
            update = true;
        }
    }
    private void Update()
    {
        if (!update) return;


        BuildAll();


        update = false;
    }

    void ClearChildren()
    {
        while (transform.childCount > 0) { DestroyImmediate(transform.GetChild(0).gameObject); }
    }



    void FetchModels()
    {
        string folderPath = "Assets/Models/Environment/ResidentialBuilding/Detailed/";
        string modelNamePrefix = "ResidentialBuilding_Detailed_";

        frontBottom = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "FrontBottom.blend");
        frontMiddle = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "FrontMid.blend");
        frontTop = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "FrontTop.prefab");

        backMiddle = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackMid.blend");
        backBottomSingle = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomSingle.blend");
        backTopSingle = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackTopSingle.blend");
        //backBottomS = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomS.blend");
        //backBottomW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomW.blend");
        //backBottomN = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomN.blend");
        //backBottomE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomE.blend");
        backBottomSW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomSW.blend");
        backBottomNW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomNW.blend");
        backBottomNE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomNE.blend");
        backBottomSE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomSE.blend");
        //backBottomFull = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + modelNamePrefix + "BackBottomFull.blend");

        backTopS = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopS.prefab");
        backTopW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopW.prefab");
        backTopN = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopN.prefab");
        backTopE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopE.prefab");
        backTopSW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopSW.prefab");
        backTopNW = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopNW.prefab");
        backTopNE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopNE.prefab");
        backTopSE = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopSE.prefab");
        backTopFull = AssetDatabase.LoadAssetAtPath<GameObject>(folderPath + "Prefabs/" + modelNamePrefix + "BackTopFull.prefab");

        //Debug.Log("Fetch done");
    }


    void CreateParents()
    {
        floorTs = new Transform[floors + 1];

        // Bottom
        Transform bottom = new GameObject("Bottom").transform; bottom.SetParent(transform, false);
        bottom.transform.localPosition = new Vector3(0, 0, 0); floorTs[0] = bottom;

        // Middle
        for (int i = 1; i <= floors - 1; ++i)
        {
            Transform middle = new GameObject("Floor " + i.ToString()).transform; middle.SetParent(transform, false);
            middle.transform.localPosition = new Vector3(0, 3.5f + ((i - 1) * (7.11f - 3.5f)), 0); floorTs[i] = middle;
        }

        // Top
        Transform top = new GameObject("Top").transform; top.SetParent(transform, false);
        top.transform.localPosition = new Vector3(0, 3.5f + ((floors - 1) * (7.11f - 3.5f)), 0); floorTs[floors] = top;
    }
    void BuildFront()
    {
        if (width == 1)
        {
            GameObject fb = Instantiate(frontBottom, floorTs[0]); //parts.Add(fb);
            fb.name = "FrontBottom"; fb.transform.localPosition = new Vector3(0, 0, 0);
            for (int f = 1; f <= floors - 1; ++f)
            {
                GameObject fm = Instantiate(frontMiddle, floorTs[f]); //parts.Add(fm);
                fm.name = "FrontMiddle"; fm.transform.localPosition = new Vector3(0, 0, 0);
            }
            GameObject ft = Instantiate(frontTop, floorTs[floors]); //parts.Add(ft);
            ft.name = "FrontTop"; ft.transform.localPosition = new Vector3(0, 0, 0);
        }
        else
        {
            float size_x = frontSize.x;
            float x_pos;
            if (width % 2 == 0) { x_pos = -(width / 2) * (size_x / 2.0f); }
            else { x_pos = (Mathf.FloorToInt(width / 2)) * -size_x; }

            for (int i = 0; i < width; ++i)
            {
                GameObject fb = Instantiate(frontBottom, floorTs[0]); //parts.Add(fb);
                fb.name = "FrontBottom_" + (i).ToString(); fb.transform.localPosition = new Vector3(x_pos, 0, 0);
                for (int f = 1; f <= floors - 1; ++f)
                {
                    GameObject fm = Instantiate(frontMiddle, floorTs[f]); //parts.Add(fm);
                    fm.name = "FrontMiddle_" + (i).ToString(); fm.transform.localPosition = new Vector3(x_pos, 0, 0);
                }
                GameObject ft = Instantiate(frontTop, floorTs[floors]); //parts.Add(ft);
                ft.name = "FrontTop_" + (i).ToString(); ft.transform.localPosition = new Vector3(x_pos, 0, 0);

                x_pos += size_x;
            }
        }
    }
    void BuildBack()
    {
        if (width == 1 && length == 1)
        {
            float z_delta = (frontSize.z / 2.0f) + (backSize.z / 2.0f) - 0.1f;
            GameObject bb = Instantiate(backBottomSingle, floorTs[0]); //parts.Add(bb);
            bb.name = "BackBottom"; bb.transform.localPosition = new Vector3(0, 0, z_delta);
            for (int f = 1; f <= floors - 1; ++f)
            {
                GameObject bm = Instantiate(backMiddle, floorTs[f]); //parts.Add(bm);
                bm.name = "BackMiddle"; bm.transform.localPosition = new Vector3(0, 0, z_delta);
            }
            GameObject bt = Instantiate(backTopSingle, floorTs[floors]); //parts.Add(bt);
            bt.name = "BackTop"; bt.transform.localPosition = new Vector3(0, 0, z_delta);
        }
        else
        {
            float start_pos_x;
            if (width % 2 == 0) { start_pos_x = -(width / 2) * (frontSize.x / 2.0f); }
            else { start_pos_x = (Mathf.FloorToInt(width / 2)) * -frontSize.x; }

            Vector2 pos = new Vector2(start_pos_x, (frontSize.z / 2.0f) + (backSize.z / 2.0f) - 0.1f);
            for (int l = 0; l < length; ++l)
            {
                for (int w = 0; w < width; ++w)
                {
                    GameObject prefabBot, prefabTop;

                    if (l == 0)
                    {
                        if (w == 0)
                        {
                            prefabBot = backBottomSW; prefabTop = backTopSW;
                        }
                        else if (w == width - 1)
                        {
                            prefabBot = backBottomSE; prefabTop = backTopSE;
                        }
                        else
                        {
                            prefabBot = backBottomSW; prefabTop = backTopS;
                        }
                    }
                    else if (l == length - 1)
                    {
                        if (w == 0)
                        {
                            prefabBot = backBottomNW; prefabTop = backTopNW;
                        }
                        else if (w == width - 1)
                        {
                            prefabBot = backBottomNE; prefabTop = backTopNE;
                        }
                        else
                        {
                            prefabBot = backBottomNW; prefabTop = backTopN;
                        }
                    }
                    else
                    {
                        if (w == 0)
                        {
                            prefabBot = backBottomNW; prefabTop = backTopW;
                        }
                        else if (w == width - 1)
                        {
                            prefabBot = backBottomNE; prefabTop = backTopE;
                        }
                        else
                        {
                            prefabBot = backBottomSingle; prefabTop = backTopFull;
                        }
                    }

                    GameObject bb = Instantiate(prefabBot, floorTs[0]); //parts.Add(bb);
                    bb.name = "BackBottom_" + (w).ToString() + (l).ToString(); bb.transform.localPosition = new Vector3(pos.x, 0, pos.y);
                    for (int f = 1; f <= floors - 1; ++f)
                    {
                        GameObject bm = Instantiate(backMiddle, floorTs[f]); //parts.Add(bm);
                        bm.name = "BackMiddle_" + (w).ToString() + (l).ToString(); bm.transform.localPosition = new Vector3(pos.x, 0, pos.y);
                    }
                    GameObject bt = Instantiate(prefabTop, floorTs[floors]); //parts.Add(bt);
                    bt.name = "BackTop_" + (w).ToString() + (l).ToString() + (w).ToString() + (l).ToString(); bt.transform.localPosition = new Vector3(pos.x, 0, pos.y);

                    pos.x += frontSize.x;
                }
                pos.y += (backSize.z / 2.0f);
                pos.x = start_pos_x;
            }
        }
    }
    void BuildAll()
    {
        // Delete past
        ClearChildren();

        // Create floor parents
        CreateParents();

        // Instantiate GameObjects
        BuildFront();
        BuildBack();

        // Set materials
        SetMaterials();
    }

    void SetMaterials_Recursive(Transform t)
    {
        if (t.GetComponent<MeshRenderer>())
        {
            if (t.name.Contains("Bottom"))
            {
                Material[] temp = new Material[2];
                temp[0] = materials[1]; temp[1] = materials[0];
                t.GetComponent<MeshRenderer>().sharedMaterials = temp;
            }
            else { t.GetComponent<MeshRenderer>().sharedMaterials = materials; }
        }
        if (t.childCount > 0)
        {
            foreach (Transform child in t) { SetMaterials_Recursive(child); }
        }
    }
    void SetMaterials()
    {
        SetMaterials_Recursive(transform);
    }
}
#endif