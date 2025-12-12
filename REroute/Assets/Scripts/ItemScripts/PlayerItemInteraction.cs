using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class PlayerItemInteraction : NetworkBehaviour
{
    [SerializeField] Transform itemHoldPoint;
    [SerializeField] List<GameObject> itemPrefabs;
    [SerializeField] List<GameObject> originalItems;
    private GameObject pickedUpItem;
    //private GameObject heldItemInstance;
    //bool hasEmptyHand;

    [Header("Throwing Settings")]
    float throwCharege = 0f;
    [SerializeField]
    float minThrowCharge = 5f;
    [SerializeField]
    float maxThrowCharge = 20f;

    [Space(20)]
    [Header("DEBUG")]
    public bool hasEmptyHand;
    public GameObject heldItemInstance;
    InputAction throwOrPourItemAction;
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
           gameObject.GetComponent<PlayerItemInteraction>().enabled = false;
           return; 
        }  

        hasEmptyHand = true;
        
        throwOrPourItemAction = InputSystem.actions.FindAction("Throw_pour");
        if(throwOrPourItemAction != null)
        {
            throwOrPourItemAction.Enable();
        }
        else
        {
            Debug.LogError("Throw_pour action not found!");
        }
    }

    private void OnDisable()
    {
        if(throwOrPourItemAction != null) throwOrPourItemAction.Disable();
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            if (hasEmptyHand)
            {
                Debug.Log("Player picked up " + other.gameObject);
                pickedUpItem = other.gameObject;
            }
            else
            {
                Debug.Log("Would pick up " + other.gameObject + "but hand is full!");
            }
        }
    }

    public void Update()
    {
        if(pickedUpItem != null) PickUp();

        if (!hasEmptyHand && heldItemInstance != null)
        {
            HandleItemInteraction();
        }
    }

    void PickUp()
    {
        if (hasEmptyHand)
        {
            // instantiate a picked up item prefab in player's right hand
            GameObject foundPrefab = itemPrefabs.Find(item => item.CompareTag(pickedUpItem.tag));
            if(foundPrefab != null)
            {
                DespawnItemServer(pickedUpItem);
                SetObjectInHandServer(foundPrefab);
                hasEmptyHand = false;
            } 
            else
            {
                Debug.Log("Can't find prefab!");  
            }
        }
    }

    void HandleItemInteraction()
    {
        if (throwOrPourItemAction.IsPressed())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                throwCharege += Time.deltaTime * 10f;
                throwCharege = Mathf.Clamp(throwCharege, minThrowCharge, maxThrowCharge);
                Debug.Log("Charging throw: " + throwCharege);
            }else if (heldItemInstance.CompareTag("OilCanItem"))
            {
                PourOil();
            }
        }
        if (throwOrPourItemAction.WasReleasedThisFrame())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                heldItemInstance.transform.position = new Vector3(heldItemInstance.transform.parent.position.x + 0.236626789f,heldItemInstance.transform.parent.position.y -0.301602364f, heldItemInstance.transform.parent.position.z + 0.936370909f);
                heldItemInstance.transform.rotation = Quaternion.Euler(42.458744f,337.735229f,345.082825f);
                Debug.Log("Throwing banana with charge: " + throwCharege);
                ThrowBananaServerRpc(throwCharege);
                throwCharege = 0f;
            }
        }
    }

    // ------------ Networking Section ------------
    [ServerRpc(RequireOwnership = false)]
    void DespawnItemServer(GameObject item)
    {
        if (item != null) 
        {
            // Data for respawn
            //Vector3 odlItemPos = item.transform.position;
            //Quaternion oldRot = item.transform.rotation;
            //string itemTag = item.tag;

            pickedUpItem = null;
            Despawn(item);
            StartCoroutine(ResapwnItemWithDelay());
        }

        System.Collections.IEnumerator ResapwnItemWithDelay()
        {
            yield return new WaitForSeconds(2f);
            // GameObject originalItem = originalItems.Find(og => og.CompareTag(tag));
            // GameObject respawnedItem = Instantiate(originalItem, pos, rot);
            Spawn(item);

        }
    }
    [ServerRpc(RequireOwnership = false)]
    void SetObjectInHandServer(GameObject item)
    {
        heldItemInstance = Instantiate(item, itemHoldPoint.position, itemHoldPoint.rotation);
        Spawn(heldItemInstance, Owner);
        SetObjectInHandObserver(heldItemInstance);
    }

    [ObserversRpc]
    void SetObjectInHandObserver(GameObject item)
    {
        item.transform.parent = itemHoldPoint;
        item.transform.position = itemHoldPoint.position;
        item.transform.rotation = itemHoldPoint.rotation;
        
        // item.transform.localPosition = Vector3.zero;
        // item.transform.localRotation = Quaternion.identity;
    }

    [ServerRpc(RequireOwnership = false)]
    void ThrowBananaServerRpc(float throwForce)
    {
        if(heldItemInstance != null)
        {
            heldItemInstance.transform.parent = null;

            heldItemInstance.AddComponent<BoxCollider>();
            heldItemInstance.AddComponent<Rigidbody>();
            Rigidbody heldItemRB = heldItemInstance.GetComponent<Rigidbody>();
            if(heldItemRB != null)
            {
                heldItemRB.mass = 0.5f;
                heldItemRB.isKinematic = false;
                heldItemRB.AddForce(itemHoldPoint.forward * throwForce, ForceMode.VelocityChange);
            }
            heldItemInstance.GetComponent<NetworkObject>().RemoveOwnership();
            heldItemInstance = null;
            SetHandEmptyObserversRpc();
        }
    }

    [ObserversRpc]
    void SetHandEmptyObserversRpc()
    {
        if(IsOwner) hasEmptyHand = true;
        heldItemInstance = null;
    }

    private void PourOil()
    {
        // simulate pouring oil (could be a particle effect or similar)
        if(heldItemInstance != null)
        {
            Debug.Log("Pouring oil...");
            // After pouring, we can assume the oil can is empty and remove it
            // Destroy(itemPrefab);
            // hasEmptyHand = true;
            // itemPrefab = null;
        }
    }
}
