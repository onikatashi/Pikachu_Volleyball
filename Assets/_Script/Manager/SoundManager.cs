using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    public SoundPack bgmPacks;          // BGM 사운드팩
    public SoundPack sfxPacks;          // SFX 사운드팩

    // 사운드 검색을 위한 딕셔너리
    Dictionary<string, SoundData> soundDictionary = new Dictionary<string, SoundData>();

    AudioSource bgmSource;
    AudioSource sfxSource;
    string currentBGM = "";

    [Header("볼륨 설정")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;
    [Range(0f, 1f)]
    public float bgmVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeSources();
        InitializeSoundDictionary();
    }

    private void Start()
    { 
        masterVolume = SaveLoadManager.Instance.settingData.masterVolume;
        bgmVolume = SaveLoadManager.Instance.settingData.bgmVolume;
        sfxVolume = SaveLoadManager.Instance.settingData.sfxVolume;

        RefreshSourceVolume();
    }

    void RefreshSourceVolume()
    {
        bgmSource.volume = masterVolume * bgmVolume;
        sfxSource.volume = masterVolume * sfxVolume;
    }

    private void InitializeSources()
    {
        bgmSource = CreatePlayer("BGM_Player", true);
        sfxSource = CreatePlayer("SFX_Player", false);
    }

    private AudioSource CreatePlayer(string name, bool loop)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(transform);
        AudioSource source = obj.AddComponent<AudioSource>();
        source.loop = loop;
        return source;
    }

    // SoundDictionary 초기화
    private void InitializeSoundDictionary()
    {

        foreach (var bgmData in bgmPacks.soundList)
        {
            if (!soundDictionary.ContainsKey(bgmData.soundName))
            {
                soundDictionary.Add(bgmData.soundName, bgmData);
            }
        }

        // BGM 프리로드
        StartCoroutine(PreloadBGMs());

        foreach (var sfxData in sfxPacks.soundList)
        {
            if (!soundDictionary.ContainsKey(sfxData.soundName))
            {
                soundDictionary.Add(sfxData.soundName, sfxData);
            }
        }
    }


    public IEnumerator PreloadBGMs()
    {
        foreach (SoundData bgm in bgmPacks.soundList)
        {
            // AudioClip이 로드되었는지 확인
            if (bgm.clip.loadState != AudioDataLoadState.Loaded)
            {
                // 강제 로드 요청
                // 비동기로 진행됨
                bgm.clip.LoadAudioData();

                // 로드 완료될 때까지 대기
                while (bgm.clip.loadState == AudioDataLoadState.Loading)
                {
                    yield return null;
                }
            }
        }
    }

    // 효과음 재생
    public void PlaySFX(string name, float volumeScale = 1f)
    {
        // 딕셔너리에서 사운드 찾기
        if (!soundDictionary.TryGetValue(name, out SoundData sound))
        {
            Debug.LogError($"Effect {name} 딕셔너리에 없음");
            return;
        }

        // 최종 볼륨 계산 및 설정 (마스터 * SFX * 사운드 * 추가배율)
        float finalVolume = masterVolume * sfxVolume * sound.volume * volumeScale;

        // 효과음의 피치를 살짝 랜덤화 하여 반복적인 효과음의 지루함을 해소
        //sfxSource.pitch = sound.pitch + Random.Range(-0.05f, 0.05f);

        // 재생
        sfxSource.PlayOneShot(sound.clip, finalVolume);
    }


    // 배경 음악(BGM)을 재생합니다.
    public void PlayBGM(string name, float volumScale = 1f)
    {
        if (!soundDictionary.TryGetValue(name, out SoundData bgm))
        {
            Debug.LogError($"BGM {name} 딕셔너리에 없음");
            return;
        }

        // 현재 재생 중인 BGM과 같으면 재생하지 않고 종료
        if (currentBGM == name && bgmSource.isPlaying)
        {
            Debug.Log($"BGM {name}이미 재생 중.");
            return;
        }

        // BGM 전용 AudioSource에 클립 설정
        bgmSource.clip = bgm.clip;

        // 최종 볼륨 설정 (마스터 * BGM * 사운드 * 추가배율)
        bgmSource.volume = masterVolume * bgmVolume * bgm.volume * volumScale;
        bgmSource.Play();

        currentBGM = name;
    }

    // BGM 정지
    public void StopBGM()
    {
        bgmSource.Stop();
        currentBGM = "";
    }

    // 마스터 볼륨 설정
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);

        // BGM 볼륨 즉시 갱신
        if (bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM))
        {
            SoundData bgm = soundDictionary[currentBGM];
            bgmSource.volume = masterVolume * bgmVolume * bgm.volume;
        }
        // SFX 볼륨은 다음 재생 시 자동으로 적용됨
    }

    // BGM 볼륨 설정
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        // BGM 볼륨 즉시 갱신
        if (bgmSource.isPlaying && !string.IsNullOrEmpty(currentBGM))
        {
            SoundData bgm = soundDictionary[currentBGM];
            bgmSource.volume = masterVolume * bgmVolume * bgm.volume;
        }
    }

    // 효과음 볼륨 설정
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        // 효과음은 재생 시 볼륨이 적용되므로 별도로 갱신할 필요 없음
    }

    // BGM 일시 정지
    public void PauseBGM()
    {
        if (bgmSource.isPlaying)
        {
            bgmSource.Pause();
        }
    }

    // BGM 다시 재생
    public void ResumeBGM()
    {
        if (!bgmSource.isPlaying)
        {
            bgmSource.UnPause();
        }
    }

    // 현재 BGM 클립 이름 반환
    public string GetCurrentBGMTitle()
    {
        return bgmSource.clip.name;
    }

    public SoundData GetSoundData(string name)
    {
        return soundDictionary[name];
    }
}
