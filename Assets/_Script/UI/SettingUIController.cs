using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SettingUIController : MonoBehaviour
{
    [Header("해상도 설정")]
    public TMP_Dropdown resolutionDropdown;         // 해상도
    public Toggle fullscreenToggle;                 // 전체 화면

    [Header("사운드 설정")]
    public Slider masterVolume;                     // 전체 볼륨
    public Slider bgmVolume;                        // bgm 볼륨
    public Slider sfxVolume;                        // sfx 볼륨
    public TextMeshProUGUI masterText;              // 전체 볼륨 수치
    public TextMeshProUGUI bgmText;                 // bgm 볼륨 수치
    public TextMeshProUGUI sfxText;                 // sfx 볼륨 수치

    private const float DEBOUNCE_DELAY = 0.5f;      // 디바운싱 지연 시간 (볼륨 저장 지연시간)
    private Coroutine currentCoroutine;             // 현재 진행 중인 저장 코루틴

    [Header("키 설정 버튼")]
    public Button keyLeft;                          // 왼쪽 키
    public Button keyRight;                         // 오른쪽 키
    public Button keyUp;                            // 위 키
    public Button keyDown;                          // 아래 키
    public Button keySpike;                         // 스파이크 키

    [Header("키 텍스트")]
    public TextMeshProUGUI txtLeft;                 // 왼쪽 키 텍스트
    public TextMeshProUGUI txtRight;                // 오른쪽 키 텍스트
    public TextMeshProUGUI txtUp;                   // 위 키 텍스트
    public TextMeshProUGUI txtDown;                 // 아래 키 텍스트
    public TextMeshProUGUI txtSpike;                // 스파이크 키 텍스트

    [Header("키 변경 패널")]
    public GameObject keyBindingPanel;              // 키 바인딩 패널
    public TextMeshProUGUI currentKey;              // 현재 키

    [Header("설정 종료 버튼")]
    public Button exitSettingBtn;                   // 설정 종료 버튼

    private string currentRebindingKeyName;         // 현재 바꾸려는 키 이름
    private bool isRebinding = false;               // 현재 키를 바꾸고 있는지 확인

    // 해상도 프리셋
    private readonly List<Vector2Int> resolutions = new List<Vector2Int>()
    {
        new Vector2Int(640, 360),
        new Vector2Int(960, 540),
        new Vector2Int(1280, 720),
        new Vector2Int(1600, 900),
        new Vector2Int(1920, 1080)
    };

    private void Start()
    {
        // UI 초기화
        InitResolutionUI();
        UpdateKeyTexts();

        // 해상도 이벤트 연결
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        // 사운드 슬라이더 초기화 및 연결
        masterVolume.value = SoundManager.Instance.masterVolume;
        bgmVolume.value = SoundManager.Instance.bgmVolume;
        sfxVolume.value = SoundManager.Instance.sfxVolume;
        UpdateVolumeText();

        masterVolume.onValueChanged.AddListener(OnMasterChanged);
        bgmVolume.onValueChanged.AddListener(OnBGMChanged);
        sfxVolume.onValueChanged.AddListener(OnSFXChanged);

        // 키 버튼 이벤트 연결
        keyLeft.onClick.AddListener(() => StartRebinding("Left"));
        keyRight.onClick.AddListener(() => StartRebinding("Right"));
        keyUp.onClick.AddListener(() => StartRebinding("Up"));
        keyDown.onClick.AddListener(() => StartRebinding("Down"));
        keySpike.onClick.AddListener(() => StartRebinding("Spike"));

        // 설정 패널 종료 버튼 연결
        exitSettingBtn.onClick.AddListener(ExitSettingPanel);

        keyBindingPanel.SetActive(false);
    }

    private void Update()
    {
        // 리바인딩 중일 때만 입력 감지
        if (isRebinding)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                foreach (Key key in Enum.GetValues(typeof(Key)))
                {
                    if (key == Key.None) continue;

                    if (Keyboard.current[key].wasPressedThisFrame)
                    {
                        ProcessRebind(key);
                        break;
                    }
                }
            }
        }
    }

    private void InitResolutionUI()
    {
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();

        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = $"{resolutions[i].x} x {resolutions[i].y}";
            options.Add(option);

            if (resolutions[i].x == Screen.width && resolutions[i].y == Screen.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = Screen.fullScreen;
    }

    // 해상도 변경 시 호출
    public void SetResolution(int index)
    {
        Debug.Log("index: " + index);
        ApplyScreenSetting(index, fullscreenToggle.isOn);
    }

    // 전체화면 변경 시 호출
    public void SetFullscreen(bool isFullscreen)
    {
        ApplyScreenSetting(resolutionDropdown.value, isFullscreen);
    }

    // 화면 적용 및 저장
    private void ApplyScreenSetting(int index, bool isFullscreen)
    {
        if (index < 0 || index >= resolutions.Count) return;

        Vector2Int res = resolutions[index];
        Screen.SetResolution(res.x, res.y, isFullscreen);
        Debug.Log("전: " + SaveLoadManager.Instance.settingData.resolutionIndex);
        SaveLoadManager.Instance.settingData.resolutionIndex = index;
        Debug.Log("후: " + SaveLoadManager.Instance.settingData.resolutionIndex);
        SaveLoadManager.Instance.settingData.isFullscreen = isFullscreen;

        SaveLoadManager.Instance.SaveSettingData();
    }

    void OnMasterChanged(float value)
    {
        SoundManager.Instance.SetMasterVolume(value);
        SaveLoadManager.Instance.settingData.masterVolume = value;
        UpdateVolumeText();

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // 코루틴 시작
        currentCoroutine = StartCoroutine(DebounceSaveCoroutine());
    }

    void OnBGMChanged(float value)
    {
        SoundManager.Instance.SetBGMVolume(value);
        SaveLoadManager.Instance.settingData.bgmVolume = value;
        UpdateVolumeText();

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // 코루틴 시작
        currentCoroutine = StartCoroutine(DebounceSaveCoroutine());
    }

    void OnSFXChanged(float value)
    {
        SoundManager.Instance.SetSFXVolume(value);
        SaveLoadManager.Instance.settingData.sfxVolume = value;
        UpdateVolumeText();

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        // 코루틴 시작
        currentCoroutine = StartCoroutine(DebounceSaveCoroutine());
    }

    // 볼륨 텍스트 업데이트
    void UpdateVolumeText()
    {
        masterText.text = $"{(masterVolume.value * 100):F0}%";
        bgmText.text = $"{(bgmVolume.value * 100):F0}%";
        sfxText.text = $"{(sfxVolume.value * 100):F0}%";
    }

    IEnumerator DebounceSaveCoroutine()
    {
        // 지연 시간 동안 대기
        yield return new WaitForSeconds(DEBOUNCE_DELAY);

        // 대기 시간 동안 취소되지 않았으면 저장
        SaveLoadManager.Instance.SaveSettingData();

        currentCoroutine = null;
    }

    private void StartRebinding(string keyName)
    {
        currentRebindingKeyName = keyName;
        isRebinding = true;
        keyBindingPanel.SetActive(true);

        var data = SaveLoadManager.Instance.settingData;
        switch (currentRebindingKeyName)
        {
            case "Left": currentKey.text = data.keyLeft.ToString(); break;
            case "Right": currentKey.text = data.keyRight.ToString(); break;
            case "Up": currentKey.text = data.keyUp.ToString(); break;
            case "Down": currentKey.text = data.keyDown.ToString(); break;
            case "Spike": currentKey.text = data.keySpike.ToString(); break;
        }
    }

    private void ProcessRebind(Key newKey)
    {
        if (newKey == Key.Escape)
        {
            EndRebinding();
            return;
        }
        
        var data = SaveLoadManager.Instance.settingData;

        // 이미 다른 기능에서 쓰고 있는 키라면 그 기능을 None으로 만듦
        if (data.keyLeft == newKey) data.keyLeft = Key.None;
        if (data.keyRight == newKey) data.keyRight = Key.None;
        if (data.keyUp == newKey) data.keyUp = Key.None;
        if (data.keyDown == newKey) data.keyDown = Key.None;
        if (data.keySpike == newKey) data.keySpike = Key.None;

        // 현재 선택한 기능에 새 키 할당
        switch (currentRebindingKeyName)
        {
            case "Left": data.keyLeft = newKey; break;
            case "Right": data.keyRight = newKey; break;
            case "Up":data.keyUp = newKey; break;
            case "Down": data.keyDown = newKey; break;
            case "Spike": data.keySpike = newKey; break;
        }

        // 저장 및 적용
        SaveLoadManager.Instance.SaveSettingData();
        InputManager.Instance.ApplySettings(data);

        EndRebinding();
    }

    private void EndRebinding()
    {
        isRebinding = false;
        keyBindingPanel.SetActive(false);
        UpdateKeyTexts();
    }

    private void UpdateKeyTexts()
    {
        GameSettingData data = SaveLoadManager.Instance.settingData;

        txtLeft.text = data.keyLeft.ToString();
        txtRight.text = data.keyRight.ToString();
        txtUp.text = data.keyUp.ToString();
        txtDown.text = data.keyDown.ToString();
        txtSpike.text = data.keySpike.ToString();
    }

    private void ExitSettingPanel()
    {
        gameObject.SetActive(false);
    }
}
