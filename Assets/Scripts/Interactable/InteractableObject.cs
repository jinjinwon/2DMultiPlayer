using Unity.Netcode;
using UnityEngine;

public abstract class InteractableObject : NetworkBehaviour
{
    public abstract void Interact(Transform player);
    public abstract void ReturnPool();
}
