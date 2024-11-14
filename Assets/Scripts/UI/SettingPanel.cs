using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    public TextMeshProUGUI text_Volume;
    public Slider slider_Volume;

    private void Start()
    {
        // ���� ������ �����̴��� �ݿ��մϴ�.
        slider_Volume.value = SoundManager.Instance.bgmSource.volume;
        text_Volume.text = $"{slider_Volume.value * 100} %";

        // �����̴��� ���� ����� �� ȣ��� �Լ��� �����մϴ�.
        slider_Volume.onValueChanged.AddListener(SetVolume);
    }

    private void SetVolume(float volume)
    {
        slider_Volume.value = volume;
        text_Volume.text = $"{(slider_Volume.value * 100).ToString("F0")} %";


        SoundManager.Instance.SetBGMVolume(volume);
        SoundManager.Instance.SetSFXVolume(volume);
    }

    public void OnClickEscape()
    {

    }
}
