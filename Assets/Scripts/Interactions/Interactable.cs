using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public abstract class Interactable : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] protected GameEvent[] gameEvents;

    [Header("Visual Indicator")]
    [SerializeField] private GameObject interactionIndicator;
    [SerializeField] private TextMeshPro interactionText;
    protected string interactPrompt = "Interagir";

    private bool isExecutingEvents = false;

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

    /// <summary>
    /// Executa todos os eventos sequencialmente
    /// </summary>
    public async void Interact()
    {
        // Prevenir múltiplas execuções simultâneas
        if (isExecutingEvents)
        {
            Debug.LogWarning($"[Interactable] Eventos já estão sendo executados em {gameObject.name}");
            return;
        }

        if (gameEvents == null || gameEvents.Length == 0)
        {
            Debug.LogWarning($"[Interactable] Nenhum evento configurado em {gameObject.name}");
            return;
        }

        isExecutingEvents = true;

        Debug.Log($"[Interactable] Iniciando cadeia de {gameEvents.Length} evento(s) em {gameObject.name}");

        // Executar eventos em sequência
        for (int i = 0; i < gameEvents.Length; i++)
        {
            GameEvent currentEvent = gameEvents[i];
            Debug.Log($"[Interactable] Executando evento {i + 1}/{gameEvents.Length}: {currentEvent.eventType} (ID: {currentEvent.eventId})");

            // Aguardar evento terminar
            EventResult result = await currentEvent.TriggerEvent(currentEvent.getEventType());

            // Verificar resultado
            switch (result)
            {
                case EventResult.Success:
                    Debug.Log($"[Interactable] Evento {i + 1} completado com sucesso");
                    // Continuar para próximo evento
                    break;

                case EventResult.Failed:
                    Debug.LogWarning($"[Interactable] Evento {i + 1} falhou. Interrompendo cadeia.");
                    isExecutingEvents = false;
                    return;

                case EventResult.Cancelled:
                    Debug.LogWarning($"[Interactable] Evento {i + 1} cancelado. Interrompendo cadeia.");
                    isExecutingEvents = false;
                    return;

                case EventResult.Error:
                    Debug.LogError($"[Interactable] Erro no evento {i + 1}. Interrompendo cadeia.");
                    isExecutingEvents = false;
                    return;
            }
        }

        Debug.Log($"[Interactable] Cadeia de eventos completa!");
        isExecutingEvents = false;
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