using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanel : MonoBehaviour
{
    public TextMeshProUGUI text_Volume;
    public Slider slider_Volume;

    private void Start()
    {
        // 현재 볼륨을 슬라이더에 반영합니다.
        slider_Volume.value = SoundManager.Instance.bgmSource.volume;
        text_Volume.text = $"{slider_Volume.value * 100} %";

        // 슬라이더의 값이 변경될 때 호출될 함수를 연결합니다.
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
