using UnityEngine;
using FishNet.Object;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using FishNet.Object.Synchronizing;
using UnityEngine.UI;

public class PlayerItemInteraction : NetworkBehaviour
{
    private PlayerUIController plScoreCont;
    [SerializeField] Transform itemHandHoldPoint;
    [SerializeField] Transform itemBackHoldPoint;
    [SerializeField] List<GameObject> itemPrefabs;
    private GameObject pickedUpItem;
    private GameObject heldItemInstance;
    private readonly SyncVar<int> _heldItemObjectId = new SyncVar<int>(-1);
    private bool hasEmptyHand;
    private Vector3 throwDirection;
    private bool canApplyOilToLedge = false;
    private int climbedLedgeNetObjId = -1;
    private float oilApplicationTimer = 0f;
    public float OIL_APPLICATION_TIME_WINDOW = 1f;

    InputAction throwItemAction;
    InputAction applyOilAction;

    [Header("Throwing Settings")]
    public float throwCharge = 0f;
    [SerializeField]
    public float minThrowCharge = 5f;
    [SerializeField]
    public float maxThrowCharge = 20f;

    //UI
    private GameObject throwChargeMeter;
    private GameObject applyOilPrompt;
    
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
        //Debug.Log($"OnHeldItemChanged: {oldVal} -> {newVal}, AsServer: {asServer}, IsOwner: {IsOwner}");

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
                    //Debug.Log("Client: SyncVar updated held item to " + heldItemInstance.name);
                }
            }
        }
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
        {
           // gameObject.GetComponent<PlayerItemInteraction>().enabled = false;
           enabled = false;
           return;
        }
        hasEmptyHand = true;

        plScoreCont = transform.parent.GetComponent<PlayerUIController>();
        //if (plScoreCont == null) { Debug.LogError("Could now find PlayerScoreController!"); }
    
        throwItemAction = InputSystem.actions.FindAction("Throw");
        if(throwItemAction != null){ throwItemAction.Enable(); }
        else{ Debug.LogError("Throw action not found!"); }

        applyOilAction = InputSystem.actions.FindAction("UseItem");
        if(applyOilAction != null) { applyOilAction.Enable(); }
        else { Debug.LogError("UseItem action not found!"); }

        if (ItemSpawner.Instance == null) { Debug.LogError("PlayerItemInteraction: No ItemSpawner found in scene!");}
        else{ Debug.Log($"PlayerItemInteraction: Found ItemSpawner.Instance"); }

        throwChargeMeter = UI.Instance.throwChargeMeter;
        if (throwChargeMeter == null) { Debug.LogError("Could not find throwChargeMeter slider in scene hierarchy!"); }

        applyOilPrompt = UI.Instance.applyOilPrompt;
        if (applyOilPrompt == null) {Debug.LogError("Could not find ApplyOilPrompt in scene hierarchy");}
        //else{ applyOilPrompt.SetActive(false); }

        itemHandHoldPoint = GameObject.Find("ItemHandHoldPoint").transform;
        if (itemHandHoldPoint == null){Debug.LogError("Could not find item hand hold point on player prefab");}

        itemBackHoldPoint = GameObject.Find("ItemBackHoldPoint").transform;
        if (itemHandHoldPoint == null){Debug.LogError("Could not find item back hold point on player prefab");}
    }

    private void OnDisable()
    {
        if (throwItemAction != null) throwItemAction.Disable();
        if (applyOilAction != null) applyOilAction.Disable();
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
                //Debug.Log("Player picked up " + other.gameObject + "with ID: " + other.GetComponent<NetworkObject>().ObjectId);
                pickedUpItem = other.gameObject;
            }
            else { /*Debug.Log("Would pick up " + other.gameObject + "but hand is full!");*/return;}
        }

        if(other.gameObject.CompareTag("BananaItem") && other.gameObject.layer == LayerMask.NameToLayer("GroundItem"))
        {
            Debug.LogWarning("Player stepped on a banana. He should FLIP HIS PANTS!!!");
        }
    }

    public void Update()
    {
        if (!IsOwner) return;

        debugHasEmptyHand = hasEmptyHand;
        debugHeldItemInstance = heldItemInstance;

        if (pickedUpItem != null) PickUp();

        if (canApplyOilToLedge) { HandleOilApplicationToLedge(); }

        if (!hasEmptyHand && heldItemInstance != null){ HandleItemInteraction(); }
        //if (!hasEmptyHand && heldItemInstance.CompareTag("BananaItem")){ HandleItemInteraction(); }
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

    void HandleOilApplicationToLedge()
    {
        //Debug.Log("Oil application window started - press E to apply oil!");
        applyOilPrompt.SetActive(true);   
        oilApplicationTimer -= Time.deltaTime;
        if (oilApplicationTimer <= 0f)
        {
            canApplyOilToLedge = false;
            climbedLedgeNetObjId = -1;
            oilApplicationTimer = 0f;
            applyOilPrompt.SetActive(false);
            //Debug.Log("Oil application window expired");
        }
        else if (applyOilAction.WasPressedThisFrame())
        {
            if (climbedLedgeNetObjId != -1)
            {
                //Debug.Log("Oil applied to ledge(" + climbedLedgeNetObjId + ")!");
                RequestApplyOilToLedgeServerRPC(climbedLedgeNetObjId);
                canApplyOilToLedge = false;
                climbedLedgeNetObjId = -1;
                applyOilPrompt.SetActive(false);
            }
            oilApplicationTimer = 0f;

            plScoreCont.OnOilAppliedScore();
        }
    }
    void HandleItemInteraction()
    {
        if (throwItemAction.IsPressed())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                throwChargeMeter.SetActive(true);
                throwCharge += Time.deltaTime * 10f;
                throwCharge = Mathf.Clamp(throwCharge, minThrowCharge, maxThrowCharge);
                //Debug.Log("Charging throw: " + throwCharge);
                UpdateThrowChargeSlider(throwCharge);
            }
            else if (heldItemInstance.CompareTag("OilCanItem"))
            {
                //Debug.Log("This will remove oil can from back and sync with other clients!");
                RequestRemoveOilCanFromBackRpc();
            }
        }
        if (throwItemAction.WasReleasedThisFrame())
        {
            if (heldItemInstance.CompareTag("BananaItem"))
            {
                //Debug.Log($"Client {(IsServerInitialized ? "Host" : "Client")}: Releasing throw with charge: {throwCharge:F1}");
                
                GameObject camera = GameObject.FindWithTag("TPCamera");
                throwDirection = camera != null ? camera.transform.forward : itemHandHoldPoint.forward;
                //ThrowBananaServerRpc(throwCharge, throwDirection.normalized);
                RequestThrowBananaServerRPC(throwCharge, throwDirection.normalized);
                throwCharge = 0f;
                throwChargeMeter.SetActive(false);
                UpdateThrowChargeSlider(throwCharge);

                plScoreCont.OnBananaThrownScore();
            }
        }
    }

    private void UpdateThrowChargeSlider(float throwCharge)
    {
        Slider throwMeter = throwChargeMeter.GetComponent<Slider>();
        if (throwMeter == null) Debug.LogError("Could not find Slider component on ThrowChargeMeter GameObject!");
        if (throwCharge > 0f) { throwMeter.value = Mathf.Lerp(throwMeter.value, throwCharge,Time.deltaTime * 10f); }
        else { throwMeter.value = 0f; }
    }

    public void StartOilApplicationTimeWindow(int ledgeNetObjId)
    {
        if (heldItemInstance != null && heldItemInstance.CompareTag("OilCanItem"))
        {
            //Debug.Log("Player just climbed a ledge(" + ledgeNetObjId + "), there is a " + OIL_APPLICATION_TIME_WINDOW + " window in which he can apply oil to it");
            // set all needed flags
            canApplyOilToLedge = true;
            climbedLedgeNetObjId = ledgeNetObjId;
            oilApplicationTimer = OIL_APPLICATION_TIME_WINDOW;    
        }else { /*Debug.Log("Player just climbed a ledge(" + ledgeNetObjId + "), but does not have an oil can!");*/ return;}
        
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

            GameObject heldItem = Instantiate(pickedUpObjectHandPrefab, itemHandHoldPoint.transform.position, Quaternion.identity);
            NetworkObject heldItemNetObj = heldItem.GetComponent<NetworkObject>();
            if (heldItemNetObj != null)
            {
                //Debug.Log("Spawning " + heldItem + "and the owner is: " + Owner.ClientId);
                Spawn(heldItem, Owner);
                _heldItemObjectId.Value = heldItemNetObj.ObjectId;
                SetItemInHandObserversRPC(heldItemNetObj.ObjectId, true);
                //Debug.Log($"Server: Player {Owner.ClientId} picked up {pickedUpNetObj.gameObject.name}, ObjectId: {heldItemNetObj.ObjectId}");
            }
        }
    }

    [ServerRpc]
    void RequestApplyOilToLedgeServerRPC(int ledgeObjectId)
    {
        NetworkObject ledgeNetObj;
        if (ServerManager.Objects.Spawned.TryGetValue(ledgeObjectId, out ledgeNetObj))
        {
            Ledge ledgeToOilUp = ledgeNetObj.GetComponent<Ledge>();
            //ledgeToOilUp.ApplyOilToLedge();

            NetworkObject oilCanNetObj;
            if (ServerManager.Objects.Spawned.TryGetValue(_heldItemObjectId.Value, out oilCanNetObj))
            {
                _heldItemObjectId.Value = -1;

                oilCanNetObj.RemoveOwnership();
                SetItemInHandObserversRPC(oilCanNetObj.ObjectId, false);
                Despawn(oilCanNetObj.gameObject);
            }
            else
            {
                Debug.LogError($"Server: Could not find held item with ObjectId {_heldItemObjectId.Value} for player {Owner.ClientId}");
                _heldItemObjectId.Value = -1;
            }
        }
    }

    [ServerRpc]
    public void RequestRemoveOilFromLedgeRpc(int ledgeNetObjId)
    {
        NetworkObject ledgeNetObj;
        if (ServerManager.Objects.Spawned.TryGetValue(ledgeNetObjId, out ledgeNetObj))
        {
            Ledge ledge = ledgeNetObj.GetComponent<Ledge>();
            //ledge.RemoveOilFromLedge();
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
            //Debug.Log($"Server: Player {Owner.ClientId} throwing {heldItem.name} with force {throwForce}");

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

    [ServerRpc]
    void RequestRemoveOilCanFromBackRpc()
    {
        NetworkObject oilCanNetObj;
        if (ServerManager.Objects.Spawned.TryGetValue(_heldItemObjectId.Value, out oilCanNetObj))
        {
            oilCanNetObj.RemoveOwnership();
            SetItemInHandObserversRPC(oilCanNetObj.ObjectId, false);
            Despawn(oilCanNetObj.gameObject);
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
        if (itemInHand.CompareTag("BananaItem"))
        {
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
                
                itemInHand.transform.SetParent(itemHandHoldPoint);
                itemInHand.transform.position = itemHandHoldPoint.position;
                itemInHand.transform.rotation = itemHandHoldPoint.rotation;

                if (IsOwner && IsServerInitialized)
                {
                    heldItemInstance = itemInHand;
                    hasEmptyHand = false;
                    //Debug.Log("Host: Now holding " + itemInHand.name);
                }
            }
            else
            {
                itemInHand.transform.SetParent(null);

                if (IsOwner && IsServerInitialized)
                {
                    heldItemInstance = null;
                    hasEmptyHand = true;
                    //Debug.Log("Host: Released " + itemInHand.name);
                }
            }
        }
        else if (itemInHand.CompareTag("OilCanItem"))
        {
            if (isHeld)
            {
                itemInHand.transform.SetParent(itemBackHoldPoint);
                itemInHand.transform.position = itemBackHoldPoint.position;
                itemInHand.transform.rotation = itemBackHoldPoint.rotation;

                if (IsOwner && IsServerInitialized)
                {
                    heldItemInstance = itemInHand;
                    hasEmptyHand = false;
                    //Debug.Log("Host: Now holding " + itemInHand.name);
                }
            }
            else
            {
                itemInHand.transform.SetParent(null);

                if (IsOwner && IsServerInitialized)
                {
                    heldItemInstance = null;
                    hasEmptyHand = true;
                    //Debug.Log("Host: Released " + itemInHand.name);
                }
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

                itemToThrow.transform.position = new Vector3(itemHandHoldPoint.transform.parent.position.x + 0.064000003f, itemHandHoldPoint.transform.parent.position.y - 0.270999998f, itemHandHoldPoint.transform.parent.position.z + 0.855000019f);
                itemToThrow.transform.rotation = Quaternion.Euler(42.458744f,337.735229f,345.082825f);
                itemRB.AddForce(throwDirection * throwForce, ForceMode.VelocityChange);
            }

            if (itemCollider != null) { itemCollider.enabled = true;}
        }
    }
}