using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BuffIcon : MonoBehaviour
{
    public Image Image_CoolDown;
    private float fMaxTimer;
    private float fTimer;

    Action DelAction = null;

    public void BuffAction(float fTime, Action exitAction)
    {
        if (fTimer > 0) 
        {
            StopCoroutine(IE_BuffAction());
        }

        fMaxTimer = fTime;
        fTimer = fTime;
        DelAction = exitAction;
        StartCoroutine(IE_BuffAction());
    }

    private IEnumerator IE_BuffAction()
    {
        while (fTimer > 0f)
        {
            fTimer = Mathf.Max(fTimer - Time.deltaTime, 0);
            UIUpdate();
            yield return null;
        }
        DelAction?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        fMaxTimer = 0f;
        fTimer = 0f;
        DelAction = null;
    }

    private void UIUpdate()
    {
        Image_CoolDown.fillAmount = fTimer / fMaxTimer;
    }
}
