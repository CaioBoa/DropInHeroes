using UnityEngine;
using System.Threading.Tasks;
using System;

public enum EventType
{
    Dialogue,
    Battle,
    UnlockCharacter
}

public enum EventResult
{
    Success,    // Evento completado com sucesso
    Failed,     // Evento falhou (ex: derrota em batalha)
    Cancelled,  // Evento foi cancelado
    Error       // Erro durante execução
}

[System.Serializable]
public class GameEvent
{
    public EventType eventType;
    public string eventId;  // ID do dado no Catalog

    /// <summary>
    /// Executa o evento e aguarda sua conclusão
    /// </summary>
    public async Task<EventResult> TriggerEvent(EventType type)
    {
        switch (type)
        {
            case EventType.Dialogue:
                return await TriggerDialogue();

            case EventType.Battle:
                return await TriggerBattle();

            case EventType.UnlockCharacter:
                return TriggerUnlockCharacter();

            default:
                Debug.LogWarning($"[GameEvent] Tipo de evento desconhecido: {type}");
                return EventResult.Error;
        }
    }

    private async Task<EventResult> TriggerDialogue()
    {
        DialogueData dialogue = DataManager.GetDialogue(eventId);

        if (dialogue == null)
        {
            Debug.LogError($"[GameEvent] Diálogo com ID '{eventId}' não encontrado!");
            return EventResult.Error;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[GameEvent] DialogueManager não encontrado!");
            return EventResult.Error;
        }

        // Usar TaskCompletionSource para aguardar diálogo terminar
        var tcs = new TaskCompletionSource<EventResult>();

        void OnDialogueEnd()
        {
            DialogueManager.Instance.OnDialogueEnd -= OnDialogueEnd;
            tcs.SetResult(EventResult.Success);
        }

        // Inscrever ao evento de fim
        DialogueManager.Instance.OnDialogueEnd += OnDialogueEnd;

        // Iniciar diálogo
        DialogueManager.Instance.StartDialogue(dialogue);

        // Aguardar até terminar
        return await tcs.Task;
    }

    private async Task<EventResult> TriggerBattle()
    {
        BattleData battleData = DataManager.GetBattle(eventId);

        // Executar batalha
        bool playerWon = await BattleManager.Instance.StartBattle(battleData);

        if (playerWon)
        {
            Debug.Log("[GameEvent] Vitória!");
            return EventResult.Success;
        }
        else
        {
            Debug.Log("[GameEvent] Derrota...");
            return EventResult.Failed;
        }
    }

    private EventResult TriggerUnlockCharacter()
    {
        // TODO: Implementar unlock de personagem
        Debug.LogWarning($"[GameEvent] UnlockCharacter ainda não implementado para '{eventId}'");
        return EventResult.Success;
    }

    public EventType getEventType()
    {
        return eventType;
    }
}