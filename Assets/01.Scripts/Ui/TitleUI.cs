using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    private const string FarmScene = "FarmPrototype";

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

    private bool leavingTitle;

    private void OnEnable()
    {
        // 액션의 활성화는 EventSystem이 담당한다. 여기서는 취소 입력만 구독한다.
        cancelAction.action.performed += HandleCancel;
    }

    private void OnDisable()
    {
        cancelAction.action.performed -= HandleCancel;
    }

    public void StartGame()
    {
        if (leavingTitle)
            return;

        leavingTitle = true;
        SceneManager.LoadScene(FarmScene, LoadSceneMode.Single);
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
        mainMenu.SetActive(false);
        panel.SetActive(true);
        backButton.Select();
    }

    public void ReturnToMenu()
    {
        if (mainMenu.activeSelf)
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
