
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class CropVisual : MonoBehaviour
{
    [Header("피격 크기 연출")]
    [SerializeField, Min(0f)] private float punchPower = 0.15f;
    [SerializeField, Min(0.01f)] private float hitDuration = 0.18f;

    [Header("피격 색상 연출")]
    [SerializeField]
    private Color hitColor =
        new Color(1f, 0.65f, 0.65f, 1f);

    [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;


    [Header("수확 연출")]
    [SerializeField, Min(1f)] private float harvestPopScale = 1.15f;
    [SerializeField, Min(0.01f)] private float harvestPopDuration = 0.06f;
    [SerializeField, Min(0.01f)] private float harvestShrinkDuration = 0.14f;



    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;

    private Tween scaleTween;
    private Tween colorTween;

    private Sequence harvestTween;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 원래 크기와 색상을 저장해 피격 효과가 누적되지 않도록 한다.
        originalScale = transform.localScale;
        originalColor = spriteRenderer.color;
    }

    public void PlayHit()
    {
        // 공격속도가 빨라져도 이전 피격 효과가 중첩되지 않도록 정리한다.
        scaleTween?.Kill();
        colorTween?.Kill();

        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;

        // 작물 이미지에만 크기 변화 적용 (콜라이더에는 영향 없음).
        scaleTween = transform.DOPunchScale(
            new Vector3(punchPower, punchPower, 0f),
            hitDuration,
            vibrato: 2,
            elasticity: 0.5f
        );

        // 피격 색상으로 변했다가 원래 색상으로 복귀한다.
        colorTween = spriteRenderer
            .DOColor(hitColor, flashDuration * 0.5f)
            .SetLoops(2, LoopType.Yoyo);
    }


    public void PlayHarvest(System.Action onComplete)
    {
        // 남아 있는 피격 연출을 중단해 수확 연출과 충돌하지 않게 한다.
        scaleTween?.Kill();
        colorTween?.Kill();

        transform.localScale = originalScale;
        spriteRenderer.color = originalColor;

        // 살짝 커진 다음, 작아지면서 투명해지는 수확 연출.
        harvestTween = DOTween.Sequence()
            .Append(
                transform.DOScale(
                    originalScale * harvestPopScale,
                    harvestPopDuration
                ).SetEase(Ease.OutQuad)
            )
            .Append(
                transform.DOScale(
                    Vector3.zero,
                    harvestShrinkDuration
                ).SetEase(Ease.InBack)
            )
            .Join(
                spriteRenderer.DOFade(0f, harvestShrinkDuration)
            )
            // 연출이 끝난 다음에만 실제 작물 오브젝트를 제거한다.
            .OnComplete(() => onComplete?.Invoke());
    }



    private void OnDestroy()
    {
        scaleTween?.Kill();
        colorTween?.Kill();
        harvestTween?.Kill();
    }
}
