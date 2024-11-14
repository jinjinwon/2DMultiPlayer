using Unity.Netcode;
using UnityEngine;

public class TransformData : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    // 기본 생성자
    public TransformData() { }

    // Transform으로부터 데이터를 생성하는 생성자
    public TransformData(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    // INetworkSerializable 구현
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // position, rotation, scale을 직렬화
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
        serializer.SerializeValue(ref scale);
    }

    // 역직렬화해서 Transform을 업데이트하는 메서드
    public void ApplyToTransform(ulong parentID ,Transform transform,bool bIsServer)
    {
        if (bIsServer)
        {
            // 부모 설정
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentID, out NetworkObject parentObject))
            {
                transform.SetParent(parentObject.transform);
            }
            transform.localPosition = Vector3.zero;
        }
    }
}
