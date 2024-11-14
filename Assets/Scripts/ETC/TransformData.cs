using Unity.Netcode;
using UnityEngine;

public class TransformData : INetworkSerializable
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    // �⺻ ������
    public TransformData() { }

    // Transform���κ��� �����͸� �����ϴ� ������
    public TransformData(Transform transform)
    {
        position = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    // INetworkSerializable ����
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // position, rotation, scale�� ����ȭ
        serializer.SerializeValue(ref position);
        serializer.SerializeValue(ref rotation);
        serializer.SerializeValue(ref scale);
    }

    // ������ȭ�ؼ� Transform�� ������Ʈ�ϴ� �޼���
    public void ApplyToTransform(ulong parentID ,Transform transform,bool bIsServer)
    {
        if (bIsServer)
        {
            // �θ� ����
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(parentID, out NetworkObject parentObject))
            {
                transform.SetParent(parentObject.transform);
            }
            transform.localPosition = Vector3.zero;
        }
    }
}
