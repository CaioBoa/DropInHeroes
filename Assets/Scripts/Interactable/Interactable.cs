using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] protected GameEvent[] gameEvents;

    [Header("Visual Indicator")]
    [SerializeField] private GameObject interactionIndicator;
    [SerializeField] private TextMeshPro interactionText;
    protected string interactPrompt = "Interagir";

    protected virtual void Start()
    {
        // Preparar indicador mas mantê-lo escondido
        if (interactionIndicator != null)
        {
            interactionText.text = interactPrompt;
        }
        HideIndicator();
    }

    public void ShowIndicator()
    {
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(true);
        }
    }

    public void HideIndicator()
    {
        if (interactionIndicator != null)
        {
            interactionIndicator.SetActive(false);
        }
    }

    // === INTERAÇÃO ===

    public void Interact()
    {
        for (int i = 0; i < gameEvents.Length; i++)
        {
            gameEvents[i].TriggerEvent(gameEvents[i].getEventType());
        }
    }

    // === GETTERS ===

    public string GetInteractPrompt() => interactPrompt;

    public void setInteractPrompt(string prompt)
    {
        interactPrompt = prompt;
    }

    // === CLEANUP ===

    protected virtual void OnDestroy()
    {
        if (interactionIndicator != null && interactionIndicator.transform.parent == transform)
        {
            Destroy(interactionIndicator);
        }
    }
}