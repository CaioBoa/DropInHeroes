using UnityEngine;

public enum EventType
{
    Dialogue,
    Battle,
    UnlockCharacter
}

[System.Serializable]
public class GameEvent
{
    public EventType eventType;
    public string eventId;  // ID do dado no Catalog

    public async void TriggerEvent(EventType type)
    {
        switch (type)
        {
            case EventType.Dialogue:
                DialogueData dialogue = DataManager.GetDialogue(eventId);
                if (dialogue != null)
                { 
                    if (DialogueManager.Instance.IsDialogueActive())
                    {
                        DialogueManager.Instance.AdvanceDialogue();
                        return;
                    }
                    DialogueManager.Instance.StartDialogue(dialogue);
                }
                else
                {
                    Debug.LogError($"[GameEvent] Diálogo com ID {eventId} não encontrado!");
                }
                break;

            case EventType.Battle:
                // ← IMPLEMENTAÇÃO COMPLETA
                BattleData battleData = DataManager.GetBattle(eventId);
                if (battleData != null)
                {
                    
                    if (BattleManager.Instance != null)
                    {
                        bool playerWon = await BattleManager.Instance.StartBattle(battleData);
                        
                        if (playerWon)
                        {
                            Debug.Log("[GameEvent] Vitória!");
                        }
                        else
                        {
                            Debug.Log("[GameEvent] Derrota...");
                        }
                    }
                    else
                    {
                        Debug.LogError("[GameEvent] BattleManager não encontrado!");
                    }
                }
                else
                {
                    Debug.LogError($"[GameEvent] Batalha com ID {eventId} não encontrada!");
                }
                break;
            default:
                Debug.LogWarning($"[GameEvent] Tipo de evento desconhecido: {type}");
                break;
        }
    }

    public EventType getEventType()
    {
        return eventType;
    }
}