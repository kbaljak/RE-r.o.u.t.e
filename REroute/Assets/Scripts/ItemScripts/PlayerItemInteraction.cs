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
    private Vector3 throwDirection;

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
        }
        else
        {
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
        if (pickedUpItem != null) PickUp();

        if (!hasEmptyHand && heldItemInstance != null)
        {
            HandleItemInteraction();
        }
    }

    void PickUp()
    {
        if (hasEmptyHand)
        {
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

                
                GameObject camera = GameObject.FindWithTag("TPCamera");
                if (camera != null) throwDirection = camera.transform.forward;
                else
                {
                    throwDirection = itemHoldPoint.forward;
                    Debug.Log("Camera was null!");
                } 
                ThrowBananaServerRpc(throwCharege, throwDirection.normalized);
                throwCharege = 0f;
            }
        }
    }

    // ------------ Networking Section ------------
    [ServerRpc]
    void DespawnItemServer(GameObject item)
    {
        if (item != null) 
        {
            pickedUpItem = null;
            Despawn(item);
            StartCoroutine(ResapwnItemWithDelay());
        }

        System.Collections.IEnumerator ResapwnItemWithDelay()
        {
            yield return new WaitForSeconds(2f);
            Spawn(item);

        }
    }
    [ServerRpc]
    void SetObjectInHandServer(GameObject item)
    {
        heldItemInstance = Instantiate(item, itemHoldPoint.position, itemHoldPoint.rotation);
        Spawn(heldItemInstance, Owner);
        SetObjectInHandObserver(heldItemInstance, true);
    }

    [ObserversRpc]
    void SetObjectInHandObserver(GameObject item, bool isHeld)
    {
        Rigidbody itemRB = item.GetComponent<Rigidbody>();
        Collider itemCollider = item.GetComponent<Collider>();

        if (isHeld)
        {
            if (itemRB != null)
            {
                itemRB.isKinematic = true;
                itemRB.detectCollisions = false;
            }
            item.transform.SetParent(itemHoldPoint);
            item.transform.position = itemHoldPoint.position;
            item.transform.rotation = itemHoldPoint.rotation;
        }
        else
        {
            item.transform.SetParent(null);
            itemCollider.enabled = true;

            if (itemRB != null)
            {
                itemRB.isKinematic = false;
                itemRB.detectCollisions = true;
                itemRB.useGravity = true;
            }
        }
    }

    [ServerRpc]
    void ThrowBananaServerRpc(float throwForce, Vector3 noramlizedThrowDirection)
    {
        if(heldItemInstance != null)
        {
            heldItemInstance.transform.parent = null;

            Rigidbody heldItemRB = heldItemInstance.GetComponent<Rigidbody>();

            SetObjectInHandObserver(heldItemInstance, false);

            if(heldItemRB != null)
            {
                heldItemRB.isKinematic = false;
                heldItemRB.detectCollisions = true;
                heldItemRB.AddForce(noramlizedThrowDirection * throwForce, ForceMode.VelocityChange);
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
