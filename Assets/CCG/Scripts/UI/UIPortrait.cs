using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public partial class UIPortrait : MonoBehaviour
{
    public GameObject panel;
    public Image portrait;
    public Text username;
    public Text deckAmount;
    public Text graveyardAmount;
    public Text handAmount;
    public Text health;
    public Text mana;
    public PlayerType playerType;

    // Animation fields
    public Image glowImage; // Assign in inspector or dynamically
    public Color hurtColor = new Color(1f, 0.5f, 0f, 0.5f); // Orange
    private Vector3 originalPanelPos;
    private bool isAnimating = false;

    private PlayerInfo enemyInfo;

    void Awake()
    {
        if (panel != null)
            originalPanelPos = panel.transform.localPosition;
    }

    void Update()
    {
        Player player = Player.localPlayer;
        if (player && player.hasEnemy) enemyInfo = player.enemyInfo;

        if (player && playerType == PlayerType.PLAYER)
        {
            panel.SetActive(true);
            player.transform.position = portrait.transform.position;
            portrait.sprite = player.portrait;
            username.text = player.username;
            deckAmount.text = player.deck.deckList.Count.ToString();
            graveyardAmount.text = player.deck.graveyard.Count.ToString();
            handAmount.text = player.deck.hand.Count.ToString();
            health.text = player.health.ToString();
            mana.text = player.mana.ToString();
            player.spawnOffset = portrait.transform;
        }
        else if (player && player.hasEnemy && playerType == PlayerType.ENEMY)
        {
            panel.SetActive(true);
            enemyInfo.player.transform.position = portrait.transform.position;
            portrait.sprite = enemyInfo.portrait;
            username.text = enemyInfo.username;
            deckAmount.text = enemyInfo.deckCount.ToString();
            graveyardAmount.text = enemyInfo.graveCount.ToString();
            handAmount.text = enemyInfo.handCount.ToString();
            health.text = enemyInfo.health.ToString();
            mana.text = enemyInfo.mana.ToString();
            enemyInfo.data.spawnOffset = portrait.transform;
        }
        else
        {
            panel.SetActive(false);
        }
    }

    // Call this method when the player takes damage
    public void PlayHurtAnimationUI()
    {
        if (!isAnimating && panel != null)
            StartCoroutine(HurtRoutineUI());
    }

    private IEnumerator HurtRoutineUI()
    {
        isAnimating = true;
        // Shake
        float shakeDuration = 0.3f;
        float shakeMagnitude = 10f;
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            panel.transform.localPosition = originalPanelPos + new Vector3(x, y, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        panel.transform.localPosition = originalPanelPos;

        // Glow
        yield return StartCoroutine(FadeGlowUI(hurtColor, 1f));
        isAnimating = false;
    }

    private IEnumerator FadeGlowUI(Color color, float duration)
    {
        if (glowImage == null) yield break;
        Color originalColor = glowImage.color;
        glowImage.color = color;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alpha = Mathf.Lerp(color.a, 0, elapsed / duration);
            glowImage.color = new Color(color.r, color.g, color.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        glowImage.color = originalColor;
    }
}