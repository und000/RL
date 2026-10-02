using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFontManager : MonoBehaviour
{
    private const string FontResourcePath = "Fonts/NanumMyeongjo";

    private static TMP_FontAsset gameFontAsset;
    private static GameFontManager instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateManager()
    {
        if (instance != null)
        {
            return;
        }

        GameObject managerObject = new GameObject("GameFontManager");
        instance = managerObject.AddComponent<GameFontManager>();
        DontDestroyOnLoad(managerObject);
    }

    private void Awake()
    {
        LoadFont();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplyFontToAllText();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyFontToAllText();
    }

    private static void LoadFont()
    {
        if (gameFontAsset != null)
        {
            return;
        }

        gameFontAsset = Resources.Load<TMP_FontAsset>("Fonts/NanumMyeongjoTMP");
        if (gameFontAsset != null)
        {
            gameFontAsset.isMultiAtlasTexturesEnabled = true;
            return;
        }

        Font sourceFont = Resources.Load<Font>(FontResourcePath);

        if (sourceFont == null)
        {
            Debug.LogError("나눔명조 폰트를 Resources/Fonts에서 찾을 수 없습니다.");
            return;
        }

        gameFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        gameFontAsset.isMultiAtlasTexturesEnabled = true;
        gameFontAsset.name = "NanumMyeongjo Runtime TMP Font";
    }

    public static void ApplyFont(TMP_Text targetText)
    {
        if (targetText == null)
        {
            return;
        }

        LoadFont();

        if (gameFontAsset != null)
        {
            if (targetText.font != gameFontAsset)
            {
                targetText.font = gameFontAsset;
                targetText.fontSharedMaterial = gameFontAsset.material;
            }
        }
    }

    private static void ApplyFontToAllText()
    {
        TMP_Text[] allText = FindObjectsByType<TMP_Text>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (TMP_Text targetText in allText)
        {
            ApplyFont(targetText);
        }
    }
}
