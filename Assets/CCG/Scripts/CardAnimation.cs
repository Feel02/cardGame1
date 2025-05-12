using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardAnimation : MonoBehaviour
{
    public Image glowImage; // Assign an Image (UI) for glow, or leave empty to use FieldCard's image
    public Color hurtColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange
    public Color deathColor = new Color(1f, 0f, 0f, 0.5f); // Red
    public Transform shakeContainer; // Assign in inspector

    public void PlayHurtAnimation()
    {
        StartCoroutine(HurtRoutine());
    }

    public void PlayDeathAnimation()
    {
        StartCoroutine(DeathRoutine());
    }

    IEnumerator HurtRoutine()
    {
        if (shakeContainer == null)
        {
            Debug.LogWarning($"[CardAnimation] shakeContainer not assigned on {gameObject.name}, using self transform.");
            shakeContainer = this.transform;
        }
        Vector3 originalPos = shakeContainer.localPosition;
        float shakeDuration = 0.3f;
        float shakeMagnitude = 10f;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            shakeContainer.localPosition = originalPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        shakeContainer.localPosition = originalPos;

        // Orange glow fade
        yield return StartCoroutine(FadeGlow(hurtColor, 1f));
    }

    IEnumerator DeathRoutine()
    {
        // Red glow fade
        yield return StartCoroutine(FadeGlow(deathColor, 1f));

        // Split animation (simple example: scale X to 0)
        float splitDuration = 0.5f;
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < splitDuration)
        {
            float t = elapsed / splitDuration;
            transform.localScale = new Vector3(Mathf.Lerp(originalScale.x, 0, t), originalScale.y, originalScale.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Optionally destroy or disable the card here
    }

    IEnumerator FadeGlow(Color color, float duration)
    {
        Image targetImage = glowImage;
        if (targetImage == null)
        {
            // Try to get FieldCard's image
            var fieldCard = GetComponent<FieldCard>();
            if (fieldCard != null)
                targetImage = fieldCard.image;
        }
        if (targetImage == null)
            yield break;

        Color originalColor = targetImage.color;
        targetImage.color = color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(color.a, 0, elapsed / duration);
            targetImage.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        targetImage.color = originalColor;
    }
}