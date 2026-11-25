using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    private const string FarmScene = "FarmPrototype";
    private const float FadeInSeconds = 0.16f;
    private const float WelcomeSeconds = 1.2f;
    private const float FadeOutSeconds = 0.16f;

    private static readonly string[] Greetings =
    {
        "어서 오십시오, 아가씨.",
        "별장의 등불을 밝혀 두었습니다.",
        "도망무도 깨워 두었지요.",
        "찻잔을 하나 더 꺼내지요."
    };

    [Header("메뉴와 패널")]
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject memoirPanel;

    [Header("패널 전환 후 선택할 버튼")]
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button memoirButton;
    [SerializeField] private Button optionsBackButton;
    [SerializeField] private Button memoirBackButton;

    [Header("UI 입력")]
    [SerializeField] private InputActionReference cancelAction;

    [Header("세르의 별장 맞이")]
    [SerializeField] private CanvasGroup welcomeOverlay;
    [SerializeField] private TMP_Text greetingText;
    [SerializeField] private Image closingShade;

    private bool leavingTitle;
    private AsyncOperation farmLoad;

    private void OnEnable()
    {
        // 액션의 활성화는 EventSystem이 담당한다. 여기서는 취소 입력만 구독한다.
        cancelAction.action.performed += HandleCancel;
    }

    private void OnDisable()
    {
        cancelAction.action.performed -= HandleCancel;
        // 타이틀 Canvas가 도중에 꺼져도 농장 씬이 활성화 대기에 묶이지 않게 한다.
        if (farmLoad != null)
            farmLoad.allowSceneActivation = true;
    }

    public void StartGame()
    {
        if (leavingTitle)
            return;

        if (!Application.CanStreamedLevelBeLoaded(FarmScene))
        {
            Debug.LogError("빌드 씬 목록에서 FarmPrototype을 확인해 주세요.", this);
            return;
        }

        leavingTitle = true;
        StartCoroutine(WelcomeToFarm());
    }

    private IEnumerator WelcomeToFarm()
    {
        mainMenu.SetActive(false);
        greetingText.text = Greetings[Random.Range(0, Greetings.Length)];
        welcomeOverlay.gameObject.SetActive(true);
        welcomeOverlay.alpha = 0.12f;
        float shownAt = Time.unscaledTime;

        // 먼저 인사를 한 프레임 그린 후, 짧은 연출과 농장 로드를 함께 진행한다.
        yield return null;
        farmLoad = SceneManager.LoadSceneAsync(FarmScene, LoadSceneMode.Single);
        farmLoad.allowSceneActivation = false;
        float elapsed;
        do
        {
            elapsed = Time.unscaledTime - shownAt;
            welcomeOverlay.alpha = Mathf.SmoothStep(0.12f, 1f, elapsed / FadeInSeconds);
            yield return null;
        }
        while (elapsed < WelcomeSeconds || farmLoad.progress < 0.9f);

        // 0.9는 Unity가 씬 활성화를 기다리는 지점이다. 암전 후 농장에 넘긴다.
        closingShade.gameObject.SetActive(true);
        closingShade.canvasRenderer.SetAlpha(0f);
        closingShade.CrossFadeAlpha(1f, FadeOutSeconds, true);
        yield return new WaitForSecondsRealtime(FadeOutSeconds);
        farmLoad.allowSceneActivation = true;
    }

    public void OpenOptions()
    {
        ShowPanel(optionsPanel, optionsBackButton);
    }

    public void OpenMemoir()
    {
        ShowPanel(memoirPanel, memoirBackButton);
    }

    private void ShowPanel(GameObject panel, Button backButton)
    {
        if (leavingTitle)
            return;

        mainMenu.SetActive(false);
        panel.SetActive(true);
        backButton.Select();
    }

    public void ReturnToMenu()
    {
        if (leavingTitle || mainMenu.activeSelf)
            return;

        // 패널을 열었던 버튼으로 포커스를 돌려 키보드 탐색을 이어간다.
        Button returnFocus = optionsPanel.activeSelf ? optionsButton : memoirButton;
        optionsPanel.SetActive(false);
        memoirPanel.SetActive(false);
        mainMenu.SetActive(true);
        returnFocus.Select();
    }

    private void HandleCancel(InputAction.CallbackContext context)
    {
        ReturnToMenu();
    }

    public void QuitGame()
    {
        if (leavingTitle)
            return;

        leavingTitle = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
