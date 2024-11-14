using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoSingleton<SoundManager>
{
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    [SerializeField] private List<Sound> soundList;

    private Dictionary<string, AudioClip> soundDictionary = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        foreach (var sound in soundList)
        {
            soundDictionary[sound.name] = sound.clip;
        }
    }

    // BGM 재생
    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // 효과음 재생
    public void PlaySFX(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // BGM 볼륨 조절
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    // SFX 볼륨 조절
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}
