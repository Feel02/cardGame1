using UnityEngine;
using UnityEngine.UI;

public class AISelectionPanel : MonoBehaviour
{
    [Header("References")]
    public Button basicAIButton;
    public Button hardAIButton;
    public OfflineStartGameButtonScript offlineStart;

    void Start()
    {
        basicAIButton.onClick.AddListener(() => OnAISelected(false));
        hardAIButton.onClick.AddListener(() => OnAISelected(true));
    }

    private void OnAISelected(bool useRLAgent)
    {
        offlineStart.StartGame(useRLAgent);
        //gameObject.SetActive(false);
    }
}