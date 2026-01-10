using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Ledge))]
public class Ledge_Editor : MonoBehaviour
{
    private bool update = false;

    [SerializeField]
    public float width;
    private float lastWidth;

    void Awake()
    {
        float lossyScale = transform.lossyScale.x;
        if (lossyScale > 1.01f || lossyScale < 0.99f)
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            SetWidthCorrectly(width); //lossyScale);
        }
        //width = lossyScale; lastWidth = width;
    }

    void OnValidate()
    {
        if (width != lastWidth)
        {
            update = true;
            //SetWidthCorrectly(width);
            lastWidth = width;
        }
    }
    private void Update()
    {
        if (!Application.isPlaying && update) {

            SetWidthCorrectly(width);
            update = false;
        }
    }


    void SetWidthCorrectly(float width)
    {
        GetComponent<BoxCollider>().size = new Vector3(width, GetComponent<BoxCollider>().size.y, GetComponent<BoxCollider>().size.z);
        Transform spriteParent = transform.GetChild(0);
        foreach (SpriteRenderer s in spriteParent.GetComponentsInChildren<SpriteRenderer>())
        {
            s.size = new Vector2(width, s.size.y);
        }
    }
}
