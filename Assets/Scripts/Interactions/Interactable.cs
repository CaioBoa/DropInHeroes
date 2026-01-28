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
            DebugManager.LogWarning($"Eventos já estão sendo executados em {gameObject.name}", DebugCategory.Interaction);
            return;
        }

        if (gameEvents == null || gameEvents.Length == 0)
        {
            DebugManager.LogWarning($"Nenhum evento configurado em {gameObject.name}", DebugCategory.Interaction);
            return;
        }

        isExecutingEvents = true;

        DebugManager.Log($"Iniciando cadeia de {gameEvents.Length} evento(s) em {gameObject.name}", DebugCategory.Interaction);

        // Executar eventos em sequência
        for (int i = 0; i < gameEvents.Length; i++)
        {
            GameEvent currentEvent = gameEvents[i];
            DebugManager.Log($"Executando evento {i + 1}/{gameEvents.Length}: {currentEvent.eventType} (ID: {currentEvent.eventId})", DebugCategory.Interaction);

            // Aguardar evento terminar
            EventResult result = await currentEvent.TriggerEvent(currentEvent.getEventType());

            // Verificar resultado
            switch (result)
            {
                case EventResult.Success:
                    DebugManager.Log($"Evento {i + 1} completado com sucesso", DebugCategory.Interaction);
                    break;

                case EventResult.Failed:
                    DebugManager.LogWarning($"Evento {i + 1} falhou. Interrompendo cadeia.", DebugCategory.Interaction);
                    isExecutingEvents = false;
                    return;

                case EventResult.Cancelled:
                    DebugManager.LogWarning($"Evento {i + 1} cancelado. Interrompendo cadeia.", DebugCategory.Interaction);
                    isExecutingEvents = false;
                    return;

                case EventResult.Error:
                    DebugManager.LogError($"Erro no evento {i + 1}. Interrompendo cadeia.", DebugCategory.Interaction);
                    isExecutingEvents = false;
                    return;
            }
        }

        DebugManager.Log("Cadeia de eventos completa!", DebugCategory.Interaction);
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