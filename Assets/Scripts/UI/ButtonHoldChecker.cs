using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHoldChecker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private JoyStickDirection _direction;

    public void OnPointerDown(PointerEventData eventData)
    {
        UIManager.Instance.bJoystick = true;
        UIManager.Instance.JoyStickDirection = _direction;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        UIManager.Instance.bJoystick = false;
    }
}