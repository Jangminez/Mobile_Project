using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

public class NetworkObjectPool : NetworkBehaviour
{
    static NetworkObjectPool _instance;
    public static NetworkObjectPool Instance {get {return _instance;} }

    [SerializeField]
    List<PoolConfigObject> PooledPrefabsList;
    HashSet<GameObject> prefabs = new HashSet<GameObject>();
    Dictionary<GameObject, Queue<NetworkObject>> pooledObjects = new Dictionary<GameObject, Queue<NetworkObject>>();

    bool m_HashIntialized = false;

    public void Awake()
    {
        if(_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        InitializePool();
    }

    public override void OnNetworkDespawn()
    {
        ClearPool();
    }

    public void OnValidate()
    {
        for (var i = 0; i < PooledPrefabsList.Count; i++)
        {
            var prefab = PooledPrefabsList[i].prefab;
            if(prefab != null)
            {
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
            }
        }
    }

    public NetworkObject GetNetworkObject(GameObject prefab)
    {
        return GetNetworkObjectInternal(prefab, Vector3.zero, Quaternion.identity);
    }

    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        return GetNetworkObjectInternal(prefab, position, rotation);
    }

    public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
    {
        var go = networkObject.gameObject;
        go.SetActive(false);
        pooledObjects[prefab].Enqueue(networkObject);
    }

    public void AddPrefab(GameObject prefab, int prewarmCount = 0)
    {
        var networkObject = prefab.GetComponent<NetworkObject>();

        Assert.IsNotNull(networkObject, $"{nameof(prefab)} must have {nameof(networkObject)} component.");
        Assert.IsFalse(prefabs.Contains(prefab), $"Prefab {prefab.name} is already registered in the pool.");
 
        RegisterPrefabInternal(prefab, prewarmCount);
    }

    private void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
    {
        prefabs.Add(prefab);

        var prefabQueue = new Queue<NetworkObject>();
        pooledObjects[prefab] = prefabQueue;
        for (int i = 0; i < prewarmCount; i++)
        {
            var go = CreateInstance(prefab);
            ReturnNetworkObject(go.GetComponent<NetworkObject>(), prefab);
        }

        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private GameObject CreateInstance(GameObject prefab)
    {
        return Instantiate(prefab);
    }

    private NetworkObject GetNetworkObjectInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var queue = pooledObjects[prefab];

        NetworkObject networkObject;
        if(queue.Count > 0)
        {
            networkObject = queue.Dequeue();
        }
        else
        {
            networkObject = CreateInstance(prefab).GetComponent<NetworkObject>();
        }

        var go = networkObject.gameObject;
        go.SetActive(true);

        go.transform.position = position;
        go.transform.rotation = rotation;

        return networkObject;
    }

    public void InitializePool()
    {
        if(m_HashIntialized) return;
        foreach (var configObject in PooledPrefabsList)
        {
            RegisterPrefabInternal(configObject.prefab, configObject.prewarmCount);
        }
        m_HashIntialized = true;
    }

    public void ClearPool()
    {
        foreach (var prefab in prefabs)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
        }
        pooledObjects.Clear();
    }
}

[Serializable]
struct PoolConfigObject
{
    public GameObject prefab;
    public int prewarmCount;
}

class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    GameObject m_Prefab;
    NetworkObjectPool m_Pool;

    public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
    {
        m_Prefab = prefab;
        m_Pool = pool;
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, UnityEngine.Vector3 position, UnityEngine.Quaternion rotation)
    {
        var networkObject = m_Pool.GetNetworkObject(m_Prefab, position, rotation);
        return networkObject;
    }

    void INetworkPrefabInstanceHandler.Destroy(Unity.Netcode.NetworkObject networkObject)
    {
        m_Pool.ReturnNetworkObject(networkObject, m_Prefab);
    }
}

