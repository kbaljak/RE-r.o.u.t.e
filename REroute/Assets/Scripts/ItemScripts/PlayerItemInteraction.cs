using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using FishNet.Object.Synchronizing;

public class PlayerItemInteraction : NetworkBehaviour
{
    [SerializeField] Transform itemHoldPoint;
    [SerializeField] List<GameObject> itemPrefabs;
    private GameObject pickedUpItem;
    private GameObject heldItemInstance;
    //private int heldItemObjectId = -1;
    private readonly SyncVar<int> _heldItemObjectId = new SyncVar<int>(-1);
    private bool hasEmptyHand;
    private Vector3 throwDirection;
    InputAction throwOrPourItemAction;

    [Header("Throwing Settings")]
    float throwCharge = 0f;
    [SerializeField]
    float minThrowCharge = 5f;
    [SerializeField]
    float maxThrowCharge = 20f;

    [Space(20)]
    [Header("DEBUG")]
    public bool debugHasEmptyHand;
    public GameObject debugHeldItemInstance;

    private void Awake()
    {
        _heldItemObjectId.OnChange += OnHeldItemChanged;
    }
    private void OnHeldItemChanged(int oldVal, int newVal, bool asServer)
    {
        Debug.Log($"OnHeldItemChanged: {oldVal} -> {newVal}, AsServer: {asServer}, IsOwner: {IsOwner}");

        if (!asServer && IsOwner)
        {
            if (newVal == -1)
            {
                heldItemInstance = null;
                hasEmptyHand = true;
            }
            else
            {
                NetworkObject itemNetObj;
                if (ClientManager.Objects.Spawned.TryGetValue(newVal, out itemNetObj))
                {
                    heldItemInstance = itemNetObj.gameObject;
                    hasEmptyHand = false;
                    Debug.Log("Client: SyncVar updated held item to " + heldItemInstance.name);   
                }
            }
        }
    }
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

            if (ItemSpawner.Instance == null)
            {
                Debug.LogError("PlayerItemInteraction: No ItemSpawner found in scene!");
            }
            else
            {
                Debug.Log($"PlayerItemInteraction: Found ItemSpawner.Instance");
            }
        }
    }

    private void OnDisable()
    {
        if(throwOrPourItemAction != null) throwOrPourItemAction.Disable();
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsServerInitialized && _heldItemObjectId.Value != -1)
        {
            NetworkObject heldNetObj;
            if(ServerManager.Objects.Spawned.TryGetValue(_heldItemObjectId.Value, out heldNetObj))
            {
                Despawn(heldNetObj.gameObject);
            }
            _heldItemObjectId.Value = -1;
            Debug.Log("Server: Cleaned up held item for disconnected player " + Owner.ClientId);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsOwner) return;

        //Debug.Log($"Trigger detected: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}, HasEmptyHand: {hasEmptyHand}");

        if(other.gameObject.layer == LayerMask.NameToLayer("Pickup"))
        {
            if (hasEmptyHand)
            {
                Debug.Log("Player picked up " + other.gameObject + "with ID: " + other.GetComponent<NetworkObject>().ObjectId);
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
        if (!IsOwner) return;

        debugHasEmptyHand = hasEmptyHand;
        debugHeldItemInstance = heldItemInstance;

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
            if (foundPrefab != null)
            {
                RequestPickUpServerRPC(pickedUpItem.GetComponent<NetworkObject>().ObjectId, pickedUpItem.tag);
                pickedUpItem = null;
            }
            else
            {
                Debug.Log("Could not find any prefab with tag: " + pickedUpItem.tag);
                pickedUpItem = null;
            }
        }
    }

    void HandleItemInteraction()
    {
        if (throwOrPourItemAction.IsPressed())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                throwCharge += Time.deltaTime * 10f;
                throwCharge = Mathf.Clamp(throwCharge, minThrowCharge, maxThrowCharge);
                Debug.Log("Charging throw: " + throwCharge);
            }
            else if (heldItemInstance.CompareTag("OilCanItem"))
            {
                PourOil();
            }
        }
        if (throwOrPourItemAction.WasReleasedThisFrame())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                Debug.Log($"Client {(IsServerInitialized ? "Host" : "Client")}: Releasing throw with charge: {throwCharge:F1}");
                
                GameObject camera = GameObject.FindWithTag("TPCamera");
                throwDirection = camera != null ? camera.transform.forward : itemHoldPoint.forward;

                //ThrowBananaServerRpc(throwCharge, throwDirection.normalized);
                RequestThrowBananaServerRPC(throwCharge, throwDirection.normalized);
                throwCharge = 0f;
            }
        }
    }

    // ------------ Networking Section ------------
    // ##### SERVER RPCs #####
    [ServerRpc]
    void RequestPickUpServerRPC(int pickedUpObjId, string pickedUpObjTag)
    {
        NetworkObject pickedUpNetObj = null;
        if (ServerManager.Objects.Spawned.TryGetValue(pickedUpObjId, out pickedUpNetObj))
        {
            GameObject pickedUpObjectHandPrefab = itemPrefabs.Find(p => p.CompareTag(pickedUpObjTag));
            if (pickedUpObjectHandPrefab == null)
            {
                Debug.Log("itemPrefabs list doesn't have a prefab with tag: " + pickedUpObjTag);
            }

            if (ItemSpawner.Instance != null)
            {
                ItemSpawner.Instance.OnItemPickedUp(pickedUpObjId);
            }
            else
            {
                Debug.LogWarning("No ItemSpawner Instance found! Item won't respawn.");
                Despawn(pickedUpNetObj.gameObject);
            }

            GameObject heldItem = Instantiate(pickedUpObjectHandPrefab, itemHoldPoint.transform.position, Quaternion.identity);
            NetworkObject heldItemNetObj = heldItem.GetComponent<NetworkObject>();
            if (heldItemNetObj != null)
            {
                Debug.Log("Spawning " + heldItem + "and the owner is: " + Owner.ClientId);
                Spawn(heldItem, Owner);
                _heldItemObjectId.Value = heldItemNetObj.ObjectId;
                SetItemInHandObserversRPC(heldItemNetObj.ObjectId, true);
                Debug.Log($"Server: Player {Owner.ClientId} picked up {pickedUpNetObj.gameObject.name}, ObjectId: {heldItemNetObj.ObjectId}");
            }
        }
    }

    [ServerRpc]
    void RequestThrowBananaServerRPC(float throwForce, Vector3 normalizedThrowDirection)
    {
         if (_heldItemObjectId.Value == -1)
        {
            Debug.LogWarning($"Server: Player {Owner.ClientId} tried to throw but heldItemObjectId is -1");
            return;
        }
        NetworkObject heldNetObj;
        if (ServerManager.Objects.Spawned.TryGetValue(_heldItemObjectId.Value, out heldNetObj))
        {
            GameObject heldItem = heldNetObj.gameObject;
            Debug.Log($"Server: Player {Owner.ClientId} throwing {heldItem.name} with force {throwForce}");

            _heldItemObjectId.Value = -1;

            heldNetObj.RemoveOwnership();
            ThrowBananaObserversRPC(heldNetObj.ObjectId, throwForce, normalizedThrowDirection);
        }
        else
        {
            Debug.LogError($"Server: Could not find held item with ObjectId {_heldItemObjectId.Value} for player {Owner.ClientId}");
            _heldItemObjectId.Value = -1;
        }
    }

    // ##### OBSERVER RPCs #####
    [ObserversRpc]
    void SetItemInHandObserversRPC(int heldItemNetObjId, bool isHeld)
    {
        NetworkObject heldItemNetObj;
        if (!ServerManager.Objects.Spawned.TryGetValue(heldItemNetObjId, out heldItemNetObj) && !ClientManager.Objects.Spawned.TryGetValue(heldItemNetObjId, out heldItemNetObj))
        {
            Debug.Log("Couldn't find network object with id: " + heldItemNetObjId);
        }

        GameObject itemInHand = heldItemNetObj.gameObject;
        Rigidbody itemRB = itemInHand.GetComponent<Rigidbody>();
        Collider itemCollider = itemInHand.GetComponent<Collider>();

        if (isHeld)
        {
            if(itemRB != null)
            {
                itemRB.isKinematic = true;
                itemRB.detectCollisions = false;
            }
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }
            
            itemInHand.transform.SetParent(itemHoldPoint);
            itemInHand.transform.position = itemHoldPoint.position;
            itemInHand.transform.rotation = itemHoldPoint.rotation;

            if (IsOwner && IsServerInitialized)
            {
                heldItemInstance = itemInHand;
                hasEmptyHand = false;
                Debug.Log("Host: Now holding " + itemInHand.name);
            }
        }
        else
        {
            itemInHand.transform.SetParent(null);

            if (IsOwner && IsServerInitialized)
            {
                heldItemInstance = null;
                hasEmptyHand = true;
                Debug.Log("Host: Released " + itemInHand.name);
            }
        }
    }

    [ObserversRpc]
    void ThrowBananaObserversRPC(int itemNetObjId, float throwForce, Vector3 throwDirection)
    {
        NetworkObject heldItemNetObj;
        if (!ServerManager.Objects.Spawned.TryGetValue(itemNetObjId, out heldItemNetObj) && !ClientManager.Objects.Spawned.TryGetValue(itemNetObjId, out heldItemNetObj))
        {
            Debug.Log("Couldn't find network object with id: " + itemNetObjId);
        }

        GameObject itemToThrow = heldItemNetObj.gameObject;
        Rigidbody itemRB = itemToThrow.GetComponent<Rigidbody>();
        Collider itemCollider = itemToThrow.GetComponent<Collider>();
        
        SetItemInHandObserversRPC(itemNetObjId, false);

        if (IsServerInitialized)
        {
            if (itemRB != null)
            {
                itemRB.isKinematic = false;
                itemRB.detectCollisions = true;
                itemRB.useGravity = true;

                itemToThrow.transform.position = new Vector3(itemHoldPoint.transform.parent.position.x + 0.064000003f, itemHoldPoint.transform.parent.position.y - 0.270999998f, itemHoldPoint.transform.parent.position.z + 0.855000019f);
                itemToThrow.transform.rotation = Quaternion.Euler(42.458744f,337.735229f,345.082825f);
                itemRB.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            }

            if (itemCollider != null)
            {
                itemCollider.enabled = true;
            }
        }
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
