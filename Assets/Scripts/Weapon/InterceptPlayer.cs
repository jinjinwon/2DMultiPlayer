using UnityEngine;

public class InterceptPlayer : MonoBehaviour
{
    public PlayerController controller;

    public void CallPlayerFunction_Arrow()
    {
        StartCoroutine(controller.FireArrow());
    }

    public void CallPlayerFunction_Bullet()
    {
        StartCoroutine(controller.FireProjectile());
    }
}
