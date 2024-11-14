using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoSingleton<UIManager>
{
    #region Actions
    public delegate void Interaction(bool bActive, string strMessage, bool bImageActive);
    public delegate void AttackAction(ActionType type);
    public delegate void WeaponChange(WeaponType type);
    #endregion

    #region Interaction Variable
    public TextMeshProUGUI text_Interaction;
    public Interaction OnInteraction;
    public Image image_Interaction;
    public bool bInterButton = false;
    #endregion

    #region Attack Variable
    public AttackAction OnAttackAction;
    #endregion

    #region Joystick Variable
    public bool bJoystick = false;
    public JoyStickDirection JoyStickDirection;
    #endregion

    #region Minimap Variable
    public MinimapMarker minimapMarker;
    #endregion

    #region HP Variable
    public Image image_Hp;
    public TextMeshProUGUI text_Hp;
    private Coroutine hpBarCoroutine;
    #endregion

    #region Button Variable
    public Button button_Tip;
    public TipPanel tipPanel;

    public Button button_Setting;
    public SettingPanel settingPanel;
    #endregion

    #region Camera Variable
    public TraceCamera traceCamera;
    #endregion

    #region Death Variable
    public DeathUI deathUI;
    #endregion

    #region WeaponPanel
    public WeaponSelectPanel weapon;
    public WeaponChange OnWepaonChange;
    #endregion


    private void Awake()
    {
        OnInteraction += InteractionMessage;
    }

    #region Interaction Function
    private void InteractionMessage(bool bActive, string strMessage, bool bImageActive = false)
    {
        if(bActive)
        {
            text_Interaction.gameObject.SetActive(bActive);
            text_Interaction.text = strMessage;
            image_Interaction.gameObject.SetActive(bImageActive);
        }
        else
        {
            text_Interaction.gameObject.SetActive(bActive);
            text_Interaction.text = "";
            image_Interaction.gameObject.SetActive(false);
        }
    }
    #endregion

    #region HP Function
    public void OnHPChanged(int maxHp,int hp)
    {
        text_Hp.text = $"{hp} %";

        // 기존에 실행 중인 Coroutine이 있다면 중지
        if (hpBarCoroutine != null)
        {
            StopCoroutine(hpBarCoroutine);
        }

        // 새로운 Coroutine 시작
        hpBarCoroutine = StartCoroutine(UpdateHpBar((float)hp / maxHp));
    }

    private IEnumerator UpdateHpBar(float targetFillAmount)
    {
        float currentFillAmount = image_Hp.fillAmount;
        float duration = 1f;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            image_Hp.fillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, time / duration);
            yield return null;
        }

        image_Hp.fillAmount = targetFillAmount;
        hpBarCoroutine = null;
    }
    #endregion

    #region Attack,JoyStick Function
    public void OnAttack()
    {
        if (GameManage.Instance.GameEnd)
            return;

        OnAttackAction(ActionType.Attack);
    }

    public void OnInteract()
    {
        if (GameManage.Instance.GameEnd)
            return;

        bInterButton = true;
    }
    #endregion

    #region Button Function
    public void OnClickTip()
    {
        if (GameManage.Instance.GameEnd)
            return;

        tipPanel.gameObject.SetActive(!tipPanel.gameObject.activeSelf);
    }

    public void OnClickSetting()
    {
        if (GameManage.Instance.GameEnd)
            return;

        settingPanel.gameObject.SetActive(!settingPanel.gameObject.activeSelf);
    }

    #endregion

    #region Camera Function
    public void CameraFind(Transform trans)
    {
        traceCamera.CameraTarget(trans);
    }
    #endregion

    #region Death Function
    public void OnDeathUI()
    {
        deathUI.OnDeath();
    }
    #endregion

    #region WeaponSelect Function

    #endregion
}
