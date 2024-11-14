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

    // BGM ���
    public void PlayBGM(AudioClip clip)
    {
        bgmSource.clip = clip;
        bgmSource.Play();
    }

    // ȿ���� ���
    public void PlaySFX(string soundName)
    {
        if (soundDictionary.TryGetValue(soundName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // BGM ���� ����
    public void SetBGMVolume(float volume)
    {
        bgmSource.volume = volume;
    }

    // SFX ���� ����
    public void SetSFXVolume(float volume)
    {
        sfxSource.volume = volume;
    }
}
