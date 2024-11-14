using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class TraceCamera : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _camera;
    private bool bFind = false;

    public void CameraTarget(Transform transform)
    {
        _camera.Follow = transform;
        _camera.LookAt = transform;
    }
}
