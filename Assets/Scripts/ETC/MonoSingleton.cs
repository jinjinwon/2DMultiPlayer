using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    private static bool isQuitting;

    public static T Instance
    {
        get
        {
            // Find에 FindObjectsInactive.Include를 인자로 주어 active가 꺼진 객체라도 찾아옴
            if (instance == null && !isQuitting)
            {
               instance = FindFirstObjectByType<T>(FindObjectsInactive.Include) ?? new GameObject(typeof(T).Name).AddComponent<T>();
            }
            return instance;
        }
    }

    protected virtual void DontDestoryObject() => DontDestroyOnLoad(instance);

    protected virtual void OnApplicationQuit() => isQuitting = true;
}
