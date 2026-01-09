using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Ledge))]
public class Ledge_Editor : MonoBehaviour
{
    [SerializeField]
    public float width;
    private float lastWidth;

    void Awake()
    {
        float lossyScale = transform.lossyScale.x;
        if (lossyScale != 1.0f)
        {
            transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            SetWidthCorrectly(lossyScale);
        }
        width = lossyScale; lastWidth = width;
    }

    void OnValidate()
    {
        if (width != lastWidth)
        {
            SetWidthCorrectly(width);
            lastWidth = width;
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
