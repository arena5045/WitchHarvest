using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DialoguePortraitSlot : MonoBehaviour
{
    [SerializeField] private Image portraitImage;

    [Header("화자 강조 연출")]
    [SerializeField]
    private Vector2 speakingOffset =
        new Vector2(20f, 0f);

    [SerializeField, Min(1f)]
    private float speakingScale = 1.04f;

    [SerializeField] private Color speakingColor = Color.white;

    [SerializeField]
    private Color listeningColor =
        new Color(0.45f, 0.45f, 0.45f, 1f);

    [SerializeField, Min(0f)]
    private float transitionDuration = 0.18f;

    [SerializeField] private Ease transitionEase = Ease.OutQuad;

    private RectTransform portraitRect;
    private Vector2 basePosition;
    private Vector3 baseScale;
    private Sequence transition;
    private bool initialized;
    private bool speaking;

    private bool flipped;

    // 대화창이 처음부터 꺼져 있어도 재생기가 명시적으로 준비한다.
    public bool Initialize()
    {
        if (initialized)
            return true;

        if (portraitImage == null)
            return false;

        portraitRect = portraitImage.rectTransform;
        basePosition = portraitRect.anchoredPosition;
        baseScale = portraitRect.localScale;

        initialized = true;
        Clear();
        return true;
    }

    public void Show(Sprite sprite, bool flipHorizontal)
    {
        portraitImage.sprite = sprite;
        portraitImage.enabled = true;

        if (flipped == flipHorizontal)
            return;

        flipped = flipHorizontal;

        // 기존 확대 Tween이 반전값을 덮어쓰지 않도록 중단한다.
        StopTransition();

        // 현재 확대 크기를 유지하면서 좌우만 즉시 반전한다.
        Vector3 currentScale = portraitRect.localScale;
        currentScale.x *= -1f;
        portraitRect.localScale = currentScale;

        // 반전 방향을 포함한 현재 화자 상태로 연출을 이어간다.
        AnimateToCurrentState();
    }

    public void SetSpeaking(bool value)
    {
        // 같은 쪽의 연속 대사는 강조 연출을 다시 시작하지 않는다.
        if (!initialized || !portraitImage.enabled || speaking == value)
            return;

        speaking = value;
        AnimateToCurrentState();
    }

    private void AnimateToCurrentState()
    {
        StopTransition();

        Vector2 targetPosition =
            basePosition + (speaking ? speakingOffset : Vector2.zero);

        Vector3 targetScale =
            baseScale * (speaking ? speakingScale : 1f);

        // 확대 애니메이션의 목표값에도 반전 방향을 반영한다.
        if (flipped)
            targetScale.x *= -1f;

        Color targetColor =
            speaking ? speakingColor : listeningColor;

        if (transitionDuration <= 0f)
        {
            portraitRect.anchoredPosition = targetPosition;
            portraitRect.localScale = targetScale;
            portraitImage.color = targetColor;
            return;
        }

        transition = DOTween.Sequence();
        transition.SetUpdate(UpdateType.Normal, true);
        transition.SetAutoKill(true);

        transition.Join(
            portraitRect.DOAnchorPos(targetPosition, transitionDuration)
                .SetEase(transitionEase));

        transition.Join(
            portraitRect.DOScale(targetScale, transitionDuration)
                .SetEase(transitionEase));

        transition.Join(
            portraitImage.DOColor(targetColor, transitionDuration)
                .SetEase(transitionEase));

        transition.OnKill(() => transition = null);
    }

    public void Clear()
    {
        StopTransition();
        speaking = false;
        flipped = false;

        if (!initialized || portraitImage == null)
            return;

        portraitImage.enabled = false;
        portraitImage.sprite = null;
        portraitImage.color = listeningColor;

        portraitRect.anchoredPosition = basePosition;
        portraitRect.localScale = baseScale;
    }

    private void StopTransition()
    {
        // 다른 시스템의 Tween은 건드리지 않는다.
        transition?.Kill();
        transition = null;
    }

    private void OnDisable() => Clear();
    private void OnDestroy() => StopTransition();
}