using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class BananaItem : NetworkBehaviour
{
    private Rigidbody itemRB;
    private Collider itemCOL;
    private readonly SyncVar<bool> isLanded = new SyncVar<bool>(false);
    public float OffsetY = 0.1f;

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        isLanded.OnChange += OnLandedChanged;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!IsServerInitialized) return;

        if (collision.gameObject.CompareTag("Ground") && !isLanded.Value)
        {
            Debug.Log(gameObject.name + " landed on " + collision.gameObject.tag);
            StickToImpactPoint(collision);
        }
    }

    private void StickToImpactPoint(Collision collision)
    {
        ContactPoint cp = collision.contacts[0];
        transform.position = new Vector3(cp.point.x, cp.point.y + OffsetY, cp.point.z);
        transform.rotation = Quaternion.Euler(-90, 0, 0);

        itemRB = GetComponent<Rigidbody>();

        if (itemRB != null)
        {
            itemRB.linearVelocity = Vector3.zero;
            itemRB.angularVelocity = Vector3.zero;
            itemRB.isKinematic = true;
            itemRB.detectCollisions = true;
        }
        else
        {
            Debug.LogWarning("Could not find Rigidbody component on " + gameObject.name);
        }
        isLanded.Value = true;
    }

    private void OnLandedChanged(bool odlVal, bool newVal, bool asServer)
    {
        if(!newVal) return;

        itemRB = GetComponent<Rigidbody>();
        itemCOL = GetComponent<Collider>();

        if (itemRB != null)
        {
            itemRB.isKinematic = true;
            itemRB.detectCollisions = true;
        }
        if(itemCOL != null)
        {
            itemCOL.enabled = true;
            itemCOL.isTrigger = true;   
        }
        gameObject.layer = LayerMask.NameToLayer("GroundItem");
    }
}
