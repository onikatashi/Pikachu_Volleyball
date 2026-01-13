using UnityEngine;
using UnityEngine.InputSystem;

public class GameSettingData
{
    // 해상도, 화면
    public int resolutionIndex = 0;
    public bool isFullscreen = true;

    // 사운드
    public float masterVolume = 0.7f;
    public float bgmVolume = 0.5f;
    public float sfxVolume = 0.5f;

    // 키 바인딩
    public Key keyUp = Key.UpArrow;
    public Key keyDown = Key.DownArrow;
    public Key keyLeft = Key.LeftArrow;
    public Key keyRight = Key.RightArrow;
    public Key keySpike = Key.Enter;
}
