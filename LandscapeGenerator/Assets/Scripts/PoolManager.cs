using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public Transform spawner = null; 

    Dictionary<string, List<GameObject>> _pool; 
    Transform _poolParent;

    private void Awake()
    {
        _pool = new Dictionary<string, List<GameObject>>();
        _poolParent = transform; 
    }

    // Carga un cierto número de copias del prefab en el pool
    public void Load(GameObject prefab, int quantity = 1)
    {
        var goName = prefab.name;
        if (!_pool.ContainsKey(goName)) // Si no existe el objeto a spawnear se agrega
        {
            _pool[goName] = new List<GameObject>();
        }
        for (int i = 0; i < quantity; i++) // Se cargan todas las instancias requeridas
        {
            var go = Instantiate(prefab, _poolParent, true);
            go.name = prefab.name;
            // go.SetActive(false);
            go.layer = 3;
            _pool[goName].Add(go);
        }
    }

    // Spawnea un objeto del pool
    public GameObject Spawn(GameObject prefab)
    {
        if (!_pool.ContainsKey(prefab.name) || _pool[prefab.name].Count == 0)
        {
            // Debug.LogWarning($"Tried to spawn {prefab} but there were no copies available in the pool");
            Load(prefab);
        }
        var l = _pool[prefab.name];
        var go = l[0];
        l.RemoveAt(0);
        // go.SetActive(true);
        go.layer = 0;
        go.transform.SetParent(spawner);
        return go;
    }

    // Spawnea un objeto del pool en una posición y con rotación específicas
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        var go = Spawn(prefab);
        var t = go.transform;
        t.position = position;
        t.rotation = rotation;
        return go;
    }
    
    // Devuelve un objeto al pool
    public void Despawn(GameObject go)
    {
        if (!_pool.ContainsKey(go.name))
        {
            _pool[go.name] = new List<GameObject>();
        }
        // go.SetActive(false);
        go.layer = 3;
        go.transform.SetParent(_poolParent);
        _pool[go.name].Add(go);
    }
}
