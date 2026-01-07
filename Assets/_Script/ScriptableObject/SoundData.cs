using UnityEngine;

[CreateAssetMenu(fileName = "SoundData", menuName = "Scriptable Objects/SoundData")]
public class SoundData : ScriptableObject
{
    public string soundName;        // 사운드 이름(키 값)
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;       // 0 ~ 1 사이의 볼륨
    [Range(0f, 1f)]
    public float pitch = 1f;        // 피치 (고음 / 저음)

    public bool loop = false;       // 반복 재생 여부
    public bool isBGM = false;      // BGM인지 SFX인지 구분
}
