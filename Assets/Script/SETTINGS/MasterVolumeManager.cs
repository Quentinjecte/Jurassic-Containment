using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MasterVolumeManager : MonoBehaviour
{
    public static MasterVolumeManager instance;

    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Volume[] volume = new Volume[4];

    [Serializable]
    private struct Volume{
        public string name;
        public float volume;

        public Volume(string name, float volume)
        {
            this.name = name;
            this.volume = volume;
        }
    }

    public AudioMixer GetAudioMixer() => mixer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        volume[0] = new Volume
        (
            "Master",
            100
        );
        volume[1] = new Volume
        (
            "SFX",
            100
        );
        volume[2] = new Volume
        (
            "Music",
            100
        );
        volume[3] = new Volume
        (
            "Env",
            100
        );
    }

    public void MasterVolum(Slider slider)
    {
        volume[0].volume = slider.value / 100;

        var txt = slider.GetComponentsInChildren<TextMeshProUGUI>(true).First();

        txt.text = $"{volume[0].volume * 100}%";
        SetAudioMixerVolume("Master");
    }
    public void EffectVolum(Slider slider)
    {
        volume[1].volume = slider.value / 100;

        var txt = slider.GetComponentsInChildren<TextMeshProUGUI>(true).First();

        txt.text = $"{volume[1].volume * 100}%";
        SetAudioMixerVolume("SFX");
    }
    public void MusicVolum(Slider slider)
    {
        volume[2].volume = slider.value / 100;

        var txt = slider.GetComponentsInChildren<TextMeshProUGUI>(true).First();

        txt.text = $"{volume[2].volume * 100}%";
        SetAudioMixerVolume("Music");
    }
    public void EnvironnementVolum(Slider slider)
    {
        volume[3].volume = slider.value / 100;

        var txt = slider.GetComponentsInChildren<TextMeshProUGUI>(true).First();

        txt.text = $"{volume[3].volume * 100}%";
        SetAudioMixerVolume("Env");
    }


    public void SetAudioMixerVolume(string targetMixer)
    {
        float val = Mathf.Clamp(GetFloatInVolumeArray(targetMixer), 0.0001f, 1f);
        mixer.SetFloat($"{targetMixer}Volume", Mathf.Log10(val) * 20);
    }
    private float GetFloatInVolumeArray(string nameOfVolume) => volume.FirstOrDefault(v =>  v.name == nameOfVolume).volume;
}
