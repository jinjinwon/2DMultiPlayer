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
            // Find�� FindObjectsInactive.Include�� ���ڷ� �־� active�� ���� ��ü�� ã�ƿ�
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
