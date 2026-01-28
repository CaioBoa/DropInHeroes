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
                DebugManager.LogWarning($"Tipo de evento desconhecido: {type}", DebugCategory.Interaction);
                return EventResult.Error;
        }
    }

    private async Task<EventResult> TriggerDialogue()
    {
        DialogueData dialogue = DataManager.GetDialogue(eventId);

        if (dialogue == null)
        {
            DebugManager.LogError($"Diálogo com ID '{eventId}' não encontrado!", DebugCategory.Interaction);
            return EventResult.Error;
        }

        if (DialogueManager.Instance == null)
        {
            DebugManager.LogError("DialogueManager não encontrado!", DebugCategory.Interaction);
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
            DebugManager.Log("Vitória!", DebugCategory.Interaction);
            return EventResult.Success;
        }
        else
        {
            DebugManager.Log("Derrota...", DebugCategory.Interaction);
            return EventResult.Failed;
        }
    }

    private EventResult TriggerUnlockCharacter()
    {
        // TODO: Implementar unlock de personagem
        DebugManager.LogWarning($"UnlockCharacter ainda não implementado para '{eventId}'", DebugCategory.Interaction);
        return EventResult.Success;
    }

    public EventType getEventType()
    {
        return eventType;
    }
}