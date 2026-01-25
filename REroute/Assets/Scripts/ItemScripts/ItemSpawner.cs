using UnityEngine;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;

public class ItemSpawner : NetworkBehaviour
{
    public static ItemSpawner Instance { get; private set; }

    [System.Serializable]
    public class SpawnPoint
    {
        public string itemTag;
        public Vector3 position;
        public Quaternion rotation;
        [HideInInspector] public GameObject spawnedItem;
        [HideInInspector] public bool isAvailable = true;
    }

    [Header("Spawn Configuration")]
    [SerializeField] List<GameObject> itemPickupPrefabs;
    [SerializeField] List<SpawnPoint> spawnPoints = new List<SpawnPoint>();
    [SerializeField] float respawnDelay = 2f;

    [Header("Auto-Spawn on Start")]
    [SerializeField] bool spawnItemsOnStart = true;

    [Header("Debug")]
    [SerializeField] bool showGizmos = true;
    [SerializeField] Color availableColor = Color.green;
    [SerializeField] Color unavailableColor = Color.red;

    private Dictionary<int, SpawnPoint> spawnedItemsMap = new Dictionary<int, SpawnPoint>();
    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple ItemSpawners detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        // Only server spawns items
        if (IsServerInitialized && spawnItemsOnStart)
        {
            SpawnAllItems();
        }
        
        Debug.Log($"ItemSpawner: OnStartNetwork - IsServer: {IsServerInitialized}, IsClient: {IsClientInitialized}");
    }

    [Server]
    public void SpawnAllItems()
    {
        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.isAvailable)
            {
                SpawnItemAtPoint(spawnPoint);
            }
        }
    }

    [Server]
    void SpawnItemAtPoint(SpawnPoint spawnPoint)
    {
        GameObject prefab = itemPickupPrefabs.Find(p => p.CompareTag(spawnPoint.itemTag));
        if (prefab == null)
        {
            Debug.LogError($"ItemSpawner: No prefab found with tag '{spawnPoint.itemTag}'");
            return;
        }

        GameObject item = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        
        item.layer = LayerMask.NameToLayer("Pickup");
        
        Collider itemCollider = item.GetComponent<Collider>();
        if (itemCollider != null)
        {
            itemCollider.enabled = true;
            itemCollider.isTrigger = true;
        }
        
        Rigidbody itemRB = item.GetComponent<Rigidbody>();
        if (itemRB != null)
        {
            itemRB.isKinematic = true;
            itemRB.useGravity = false;
        }

        NetworkObject netObj = item.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Spawn(item);
            
            spawnPoint.spawnedItem = item;
            spawnPoint.isAvailable = false;
            spawnedItemsMap[netObj.ObjectId] = spawnPoint;
            
            Debug.Log($"ItemSpawner: Spawned {spawnPoint.itemTag} at {spawnPoint.position}");
        }
        else
        {
            Debug.LogError($"ItemSpawner: Prefab {prefab.name} missing NetworkObject component!");
            Destroy(item);
        }
    }

    [Server]
    public void OnItemPickedUp(int itemObjectId)
    {
        if (!spawnedItemsMap.ContainsKey(itemObjectId))
        {
            Debug.LogWarning($"ItemSpawner: Picked up item {itemObjectId} not tracked by spawner");
            return;
        }

        SpawnPoint spawnPoint = spawnedItemsMap[itemObjectId];
        GameObject item = spawnPoint.spawnedItem;
        
        if (item != null)
        {
            Debug.Log($"ItemSpawner: Despawning {item.name}, will respawn in {respawnDelay}s");
            
            Despawn(item);
            Debug.Log("Despawning picked up item");
            
            spawnPoint.spawnedItem = null;
            spawnedItemsMap.Remove(itemObjectId);
            
            StartCoroutine(RespawnAfterDelay(spawnPoint));
        }
    }

    [Server]
    IEnumerator RespawnAfterDelay(SpawnPoint spawnPoint)
    {
        yield return new WaitForSeconds(respawnDelay);
        
        spawnPoint.isAvailable = true;
        SpawnItemAtPoint(spawnPoint);
    }

    public void AddSpawnPoint(string itemTag, Vector3 position, Quaternion rotation)
    {
        SpawnPoint newPoint = new SpawnPoint
        {
            itemTag = itemTag,
            position = position,
            rotation = rotation,
            isAvailable = true
        };
        spawnPoints.Add(newPoint);
        
        if (IsServerInitialized)
        {
            SpawnItemAtPoint(newPoint);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        foreach (SpawnPoint spawnPoint in spawnPoints)
        {
            Gizmos.color = spawnPoint.isAvailable ? availableColor : unavailableColor;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.3f);
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + (spawnPoint.rotation * Vector3.forward * 0.5f));
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(spawnPoint.position + Vector3.up * 0.5f, spawnPoint.itemTag);
            #endif
        }
    }

    [ContextMenu("Generate Spawn Points from Scene Items")]
    void GenerateSpawnPointsFromScene()
    {
        spawnPoints.Clear();
        
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == LayerMask.NameToLayer("Pickup"))
            {
                SpawnPoint newPoint = new SpawnPoint
                {
                    itemTag = obj.tag,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    isAvailable = true
                };
                spawnPoints.Add(newPoint);
                Debug.Log($"Added spawn point for {obj.name} at {obj.transform.position}");
            }
        }
        
        Debug.Log($"Generated {spawnPoints.Count} spawn points");
    }
}