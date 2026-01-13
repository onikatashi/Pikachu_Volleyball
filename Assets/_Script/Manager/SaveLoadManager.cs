using System;
using System.IO;
using UnityEngine;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance;

    public GameSettingData settingData;                                 // 게임 설정 데이터

    private readonly string settingDataFile = "settingData.json";       // 설정 데이터 파일명

    private string settingDataPath;                                     // 설정 데이터 자장 파일 경로

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
        }

        // 파일 경로 설정
        settingDataPath = Path.Combine(Application.persistentDataPath, settingDataFile);

        // 저장된 데이터 로드
        LoadSettingData();
    }

    // 설정 데이터 저장
    public void SaveSettingData()
    {
        try
        {
            string json = JsonUtility.ToJson(settingData, true);
            File.WriteAllText(settingDataPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"저장 실패: {e.Message}");
        }
    }

    // 설정 데이터 로드
    public bool LoadSettingData()
    {
        if (!File.Exists(settingDataPath))
        {
            return false;
        }

        try
        {
            string json = File.ReadAllText(settingDataPath);

            settingData = JsonUtility.FromJson<GameSettingData>(json);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"불러오기 실패: {e.Message}");
            return false;
        }
    }

    // 설정 저장 데이터 삭제
    public void DeleteSettingData()
    {
        if (File.Exists(settingDataPath))
        {
            File.Delete(settingDataPath);
        }
        settingData = new GameSettingData();
    }
}
