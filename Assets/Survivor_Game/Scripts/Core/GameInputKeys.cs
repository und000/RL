using UnityEngine.InputSystem;

/// <summary>
/// 게임 전체가 함께 쓰는 조작 키. 받침대·드롭처럼 "다가가서 누르는" 것들이
/// 서로 다른 키를 요구하면 안 되므로 여기 한 곳에서만 정한다.
/// </summary>
public static class GameInputKeys
{
    /// <summary>
    /// 상호작용 키. 보상 받침대와 바닥 드롭이 모두 이 키를 쓴다.
    /// 전부 다른 키로 바꾸려면 이 한 줄만 고치면 되고, 화면에 뜨는 안내 문구도
    /// 여기서 만들어 쓰므로 함께 따라간다.
    /// </summary>
    public const Key Interact = Key.F;

    /// <summary>"[F]"처럼 안내에 끼워 넣을 키 표기.</summary>
    public static string InteractPrompt => "[" + Interact + "]";
}
