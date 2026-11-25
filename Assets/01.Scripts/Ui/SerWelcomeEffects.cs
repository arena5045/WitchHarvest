using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SerWelcomeEffects : MonoBehaviour
{
    [SerializeField] private TMP_Text preparingText;
    [SerializeField] private RectTransform palmSeal;
    [SerializeField] private RectTransform cardJewel;
    [SerializeField] private Image[] sparks;

    private float openedAt;

    private void OnEnable()
    {
        openedAt = Time.unscaledTime;
        Animate(0f);
    }

    private void Update()
    {
        Animate(Time.unscaledTime - openedAt);
    }

    private void Animate(float elapsed)
    {
        // 세 점의 자리까지 미리 조판하고 보이는 글자 수만 바꿔 문구를 고정한다.
        int dots = 1 + Mathf.FloorToInt(elapsed / 0.30f) % 3;
        preparingText.maxVisibleCharacters = preparingText.text.Length - 3 + dots;
        palmSeal.localRotation = Quaternion.Euler(0f, 0f, 12f - elapsed * 24f);

        // 손과 카드가 서로 다른 앵커를 사용하므로 두 끝점을 입자 레이어 좌표로 옮긴다.
        Vector3 start = transform.InverseTransformPoint(palmSeal.position);
        Vector3 end = transform.InverseTransformPoint(cardJewel.position);
        float lift = Mathf.Clamp(Vector3.Distance(start, end) * 0.16f, 20f, 56f);

        for (int i = 0; i < sparks.Length; i++)
        {
            float t = Mathf.Repeat(elapsed * 0.84f + (float)i / sparks.Length, 1f);
            float arc = Mathf.Sin(t * Mathf.PI);
            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += arc * (lift + 6f * Mathf.Sin(2f * Mathf.PI * t + i));
            sparks[i].rectTransform.localPosition = position;
            sparks[i].canvasRenderer.SetAlpha(arc);
        }
    }
}
