using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class CardAnimation : MonoBehaviour
{
    public Image glowImage; // Assign an Image (UI) for glow, or leave empty to use FieldCard's image
    public Color hurtColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange
    public Color deathColor = new Color(1f, 0f, 0f, 0.5f); // Red
    public Transform shakeContainer; // Assign in inspector
    public Image overlayImage; // Overlay for full-card flash

    public void Awake()
    {
        // Only create overlay if it doesn't exist
        if (overlayImage == null)
        {
            // Find the root FieldCard RectTransform
            RectTransform rootRect = GetComponentInParent<FieldCard>()?.GetComponent<RectTransform>();
            if (rootRect == null)
                rootRect = GetComponent<RectTransform>();

            // Check if already present
            Transform existing = transform.Find("CardOverlay");
            if (existing != null)
            {
                overlayImage = existing.GetComponent<Image>();
            }
            else
            {
                // Create overlay
                GameObject overlayObj = new GameObject("CardOverlay");
                overlayObj.transform.SetParent(transform, false);
                overlayImage = overlayObj.AddComponent<Image>();
                overlayImage.color = new Color(1f, 0f, 0f, 0f); // Transparent red
                overlayImage.raycastTarget = false;

                // Set overlay size to match root card
                var rt = overlayObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = rootRect.rect.size;
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                overlayObj.transform.SetAsLastSibling();
            }
        }
    }

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

        // Force rerender of both fields after animation
        ForceFieldRerender();
    }

    IEnumerator DeathRoutine()
    {
        // Flash and scale at the same time
        float duration = 0.5f;
        Vector3 originalScale = transform.localScale;
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
        Color flashColor = deathColor;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // Scale down
            float scale = Mathf.Lerp(1f, 0f, t);
            transform.localScale = new Vector3(scale, scale, originalScale.z);
            // Flash red (fade out alpha)
            float alpha = Mathf.Lerp(flashColor.a, 0, t);
            targetImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure final state
        transform.localScale = new Vector3(0, 0, originalScale.z);
        targetImage.color = originalColor;
    }

    IEnumerator FlashAllComponents(Color flashColor, float duration)
    {
        // Find all UI components
        var images = GetComponentsInChildren<UnityEngine.UI.Image>(true);
        var texts = GetComponentsInChildren<UnityEngine.UI.Text>(true);

        // Store original colors
        var originalImageColors = new Dictionary<UnityEngine.UI.Image, Color>();
        var originalTextColors = new Dictionary<UnityEngine.UI.Text, Color>();

        foreach (var img in images)
            originalImageColors[img] = img.color;
        foreach (var txt in texts)
            originalTextColors[txt] = txt.color;

        // Set flash color
        foreach (var img in images)
            img.color = flashColor;
        foreach (var txt in texts)
            txt.color = flashColor;

        // Wait for duration
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore original colors
        foreach (var kvp in originalImageColors)
            kvp.Key.color = kvp.Value;
        foreach (var kvp in originalTextColors)
            kvp.Key.color = kvp.Value;
    }

    IEnumerator FadeGlow(Color color, float duration)
    {
        Image targetImage = glowImage;
        if (targetImage == null)
        {
            // Do not fade the main card art! Just skip the fade if no glowImage is set.
            yield break;
        }

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

    IEnumerator FadeOverlay(Color color, float duration)
    {
        if (overlayImage == null)
            yield break;
        Color startColor = new Color(color.r, color.g, color.b, color.a);
        Color endColor = new Color(color.r, color.g, color.b, 0f);
        overlayImage.color = startColor;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            overlayImage.color = Color.Lerp(startColor, endColor, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        overlayImage.color = endColor;
    }

    void ForceFieldRerender()
    {
        var gm = FindObjectOfType<GameManager>();
        if (gm != null)
        {
            if (gm.playerField != null && gm.playerField.content != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gm.playerField.content.GetComponent<RectTransform>());
            if (gm.enemyField != null && gm.enemyField.content != null)
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(gm.enemyField.content.GetComponent<RectTransform>());
        }
    }
}