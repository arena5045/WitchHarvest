using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePlayer : MonoBehaviour
{
    [Header("대화창")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private DialoguePortraitSlot leftPortrait;
    [SerializeField] private DialoguePortraitSlot rightPortrait;

    [Header("대화 중 입력을 막을 로비")]
    [SerializeField] private CanvasGroup lobbyGroup;

    public bool IsPlaying { get; private set; }

    // 대화가 정상적으로 시작되었을 때만 발생한다.
    public event Action<DialogueData> DialogueStarted;


    private DayManager dayManager;
    private DialogueData currentDialogue;
    private int lineIndex;
    private bool initialized;
    private bool bound;
    private bool previousInteractable;
    private bool previousBlocksRaycasts;

    [Header("타이핑 설정")]
    [Tooltip("1초에 표시할 글자 수")]
    [SerializeField, Min(1f)]
    private float charactersPerSecond = 35f;

    private Tween typingTween;
    private bool isTyping;
    private int totalCharacters;


    private void Start()
    {
        dayManager = DayManager.Instance;

        if (dayManager == null || dialogueRoot == null ||
            speakerNameText == null || bodyText == null ||
            nextButton == null || skipButton == null ||
            nextButton == skipButton ||
            leftPortrait == null || rightPortrait == null ||
            leftPortrait == rightPortrait ||
            lobbyGroup == null)
        {
            Debug.LogError(
                "DialoguePlayer의 필수 참조와 좌우 슬롯을 확인해주세요.",
                this);

            enabled = false;
            return;
        }

        if (transform.IsChildOf(dialogueRoot.transform) ||
            dialogueRoot.transform.IsChildOf(lobbyGroup.transform))
        {
            Debug.LogError(
                "재생기는 대화창 밖에, 대화창은 로비 CanvasGroup 밖에 배치해주세요.",
                this);

            enabled = false;
            return;
        }

        if (!leftPortrait.Initialize() || !rightPortrait.Initialize())
        {
            Debug.LogError(
                "좌우 DialoguePortraitSlot의 Image를 연결해주세요.",
                this);

            enabled = false;
            return;
        }

        dialogueRoot.SetActive(false);
        initialized = true;
        BindUI();
    }

    private void OnEnable()
    {
        if (initialized)
            BindUI();
    }

    private void BindUI()
    {
        if (bound)
            return;

        bound = true;

        nextButton.onClick.AddListener(Next);
        skipButton.onClick.AddListener(Skip);
        dayManager.StateChanged += HandleDayStateChanged;
    }

    public bool TryPlay(DialogueData dialogue)
    {
        if (!initialized || !isActiveAndEnabled || IsPlaying)
            return false;

        // 잘못된 대본 때문에 로비만 잠기는 일이 없도록,
        // 대화창을 열기 전에 모든 줄을 검사한다.
        if (!ValidateDialogue(dialogue))
            return false;

        previousInteractable = lobbyGroup.interactable;
        previousBlocksRaycasts = lobbyGroup.blocksRaycasts;

        lobbyGroup.interactable = false;
        lobbyGroup.blocksRaycasts = false;

        currentDialogue = dialogue;
        lineIndex = 0;
        IsPlaying = true;

        leftPortrait.Clear();
        rightPortrait.Clear();

        dialogueRoot.transform.SetAsLastSibling();
        dialogueRoot.SetActive(true);

        ShowCurrentLine();
        nextButton.Select();

        // 재생에 성공한 대화를 회고록 시스템에 알린다.
        DialogueStarted?.Invoke(dialogue);

        return true;
    }

    public void Next()
    {
        if (!IsPlaying)
            return;

        lineIndex++;

        if (lineIndex >= currentDialogue.Lines.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    public void Skip() => EndDialogue();

    private void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue.Lines[lineIndex];

        // 대본은 재생 전에 검사했으며, 재생 중에는 SO를 수정하지 않는다.
        line.Character.TryGetPortrait(
            line.ExpressionId,
            out Sprite sprite);

        DialoguePortraitSlot speaker =
            line.Side == DialogueSide.Left
                ? leftPortrait
                : rightPortrait;

        DialoguePortraitSlot listener =
            line.Side == DialogueSide.Left
                ? rightPortrait
                : leftPortrait;

        // 이번 대사의 스프라이트와 좌우 반전 설정을 함께 전달한다.
        speaker.Show(sprite, line.FlipHorizontal);
        speaker.SetSpeaking(true);

        // 듣는 쪽의 스프라이트는 교체하지 않고 마지막 표정을 유지한다.
        listener.SetSpeaking(false);

        speakerNameText.text = line.Character.DisplayName;

        // 본문은 즉시 출력하지 않고 타이핑 효과로 표시한다.
        StartTyping(line.Text);
    }
    // 새로운 대사의 타이핑을 시작한다.
    private void StartTyping(string text)
    {
        StopTyping();

        // 전체 문장을 먼저 설정해서 줄바꿈과 배치를 확정한다.
        bodyText.text = text;
        bodyText.ForceMeshUpdate();

        totalCharacters = bodyText.textInfo.characterCount;
        bodyText.maxVisibleCharacters = 0;

        if (totalCharacters == 0)
            return;

        isTyping = true;

        typingTween = DOTween.To(
                () => bodyText.maxVisibleCharacters,
                value => bodyText.maxVisibleCharacters = value,
                totalCharacters,
                totalCharacters / charactersPerSecond)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                bodyText.maxVisibleCharacters = totalCharacters;
                isTyping = false;
                typingTween = null;
            });
    }

    // 실행 중인 타이핑을 정리한다.
    private void StopTyping()
    {
        typingTween?.Kill();
        typingTween = null;
        isTyping = false;
    }

    private void EndDialogue()
    {
        if (!IsPlaying)
            return;

        // 스킵하거나 대화가 끝나면 진행 중인 타이핑을 중단한다.
        StopTyping();

        // 먼저 종료 상태로 바꿔 중복 종료를 막는다.
        IsPlaying = false;
        currentDialogue = null;
        lineIndex = 0;

        if (leftPortrait != null)
            leftPortrait.Clear();

        if (rightPortrait != null)
            rightPortrait.Clear();

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        // 무조건 true가 아니라, 대화 시작 전 설정으로 복구한다.
        if (lobbyGroup != null)
        {
            lobbyGroup.interactable = previousInteractable;
            lobbyGroup.blocksRaycasts = previousBlocksRaycasts;
        }
    }

    private bool ValidateDialogue(DialogueData dialogue)
    {
        if (dialogue == null ||
            dialogue.Lines == null ||
            dialogue.Lines.Count == 0)
        {
            Debug.LogError("재생할 대화 SO 또는 대사가 없습니다.", this);
            return false;
        }

        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            DialogueLine line = dialogue.Lines[i];

            if (line == null || line.Character == null ||
                string.IsNullOrWhiteSpace(line.Character.DisplayName) ||
                string.IsNullOrWhiteSpace(line.Text) ||
                (line.Side != DialogueSide.Left &&
                 line.Side != DialogueSide.Right) ||
                !line.Character.TryGetPortrait(line.ExpressionId, out _))
            {
                Debug.LogError(
                    $"{dialogue.name}: {i + 1}번째 줄의 캐릭터/표정/스프라이트/대사를 확인해주세요.",
                    this);

                return false;
            }
        }

        return true;
    }

    private void HandleDayStateChanged(DayState state)
    {
        // 외부 코드로 로비를 벗어나더라도
        // 대화창과 입력 잠금을 남기지 않는다.
        if (state != DayState.Settlement)
            EndDialogue();
    }

    private void OnDisable()
    {
        if (bound)
        {
            bound = false;

            if (nextButton != null)
                nextButton.onClick.RemoveListener(Next);

            if (skipButton != null)
                skipButton.onClick.RemoveListener(Skip);

            if (dayManager != null)
                dayManager.StateChanged -= HandleDayStateChanged;
        }

        EndDialogue();
    }

#if UNITY_EDITOR
    [ButtonAttribute("테스트: 대화 재생"), DisableInEditorMode]
    private void TestPlay(DialogueData dialogue)
    {
        if (!TryPlay(dialogue))
        {
            Debug.LogWarning(
                "로비인지, 이미 재생 중인지, 대본 설정이 맞는지 확인해주세요.",
                this);
        }
    }
#endif
}