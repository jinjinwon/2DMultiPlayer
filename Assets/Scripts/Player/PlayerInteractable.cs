using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerInteractable : MonoBehaviour
{
    public delegate void PlayerInteractableDel(Transform transform);

    private string _strMessage;
    private bool _isInteractable;

    public PlayerInteractableDel OnPlayerInteractionDel;
    public PlayerController playerController;

    public bool IsInteractable
    {
        get { return _isInteractable; }
        set
        {
            if (playerController.IsOwner == true)
            {
                Debug.Log($"_isInteractable {value}");

                if (_isInteractable == value)
                    return;

                _isInteractable = value;

                UIManager.Instance.OnInteraction(_isInteractable, ReplaceKey(), true);
            }
        }
    }
    public string StrMessage { get { return _strMessage; } set { _strMessage = value; } }


    private string ReplaceKey()
    {
        return $"Press Button";
    }


    private void Start()
    {
        OnPlayerInteractionDel += ActionDelay;
    }

    private void Update()
    {
        if (playerController.IsOwner)
        {
            if (IsInteractable == true)
            {
                if (UIManager.Instance.bInterButton)
                {
                    OnPlayerInteractionDel(this.transform);
                    if (playerController.OnClient()) playerController.SetInterActionServerRpc(true);
                    else if (playerController.IsHost) playerController.bInterAction.Value = true;
                    UIManager.Instance.bInterButton = false;
                }
            }
            else
            {
                if (UIManager.Instance.bInterButton)
                    UIManager.Instance.bInterButton = false;
            }
        }
    }

    private void ActionDelay(Transform transform)
    {
        Invoke("_ActionDelay", 1.5f);
    }

    private void _ActionDelay()
    {
        if (playerController.OnClient()) playerController.SetInterActionServerRpc(false);
        else if (playerController.IsHost) playerController.bInterAction.Value = false;
    }
}
