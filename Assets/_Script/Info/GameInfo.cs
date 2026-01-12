using UnityEngine;

public static class GameInfo
{
    // 씬이 넘어가도 기억하고 있을 닉네임 변수
    public static string myNickname = "Player";

    // 방 코드를 저장할 변수
    public static string currentLobbyCode = "";

    // 방 ID를 저장할 변수
    public static string CurrentLobbyId = "";

    // 싱글 플레이 모드인가?
    public static bool isSinglePlay = false;
}
