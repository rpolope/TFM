using System.Collections.Generic;
using UnityEngine;

public class ObjectPool<T> where T : class, new()
{
    private readonly Stack<T> _availableObjects = new Stack<T>();
    private readonly HashSet<T> _inUseObjects = new HashSet<T>();

    public T Get() {
        if (_availableObjects.Count > 0) {
            T obj = _availableObjects.Pop();
            _inUseObjects.Add(obj);
            return obj;
        } else {
            T newObj = new T();
            _inUseObjects.Add(newObj);
            return newObj;
        }
    }

    public void Release(T obj) {
        if (_inUseObjects.Contains(obj)) {
            _inUseObjects.Remove(obj);
            _availableObjects.Push(obj);
        } else {
            Debug.LogWarning("Trying to release an object that was not in use.");
        }
    }

    public int Count => _availableObjects.Count + _inUseObjects.Count;
}