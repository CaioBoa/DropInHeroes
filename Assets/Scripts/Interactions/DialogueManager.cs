using UnityEngine;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private DialogueUI dialogueUI;
    
    private DialogueData currentDialogue;
    private int currentLineIndex = 0;
    private bool isDialogueActive = false;
    
    // Eventos
    public event Action OnDialogueStart;
    public event Action OnDialogueEnd;
    public event Action<DialogueLine> OnLineChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines.Length == 0)
        {
            DebugManager.LogWarning("DialogueData vazio ou inválido!", DebugCategory.Interaction);
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;
        isDialogueActive = true;

        // === MUDAR ESTADO GLOBAL ===
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PushState(GameState.Dialogue);
        }

        // Mostrar UI
        if (dialogueUI != null)
        {
            dialogueUI.Show();
        }

        OnDialogueStart?.Invoke();
        DisplayCurrentLine();
    }

    public void AdvanceDialogue()
    {
        if (!isDialogueActive) return;

        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        if (dialogueUI != null && dialogueUI.IsTyping())
        {
            dialogueUI.SkipTypewriter(line);
            return;
        }

        currentLineIndex++;
        
        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
        }
        else
        {
            DisplayCurrentLine();
        }
    }

    private void DisplayCurrentLine()
    {
        if (currentDialogue == null || currentLineIndex >= currentDialogue.lines.Length) return;
        
        DialogueLine line = currentDialogue.lines[currentLineIndex];
        
        // Atualizar UI
        if (dialogueUI != null)
        {
            dialogueUI.DisplayLine(line);
        }
        
        OnLineChanged?.Invoke(line);
    }

    private void EndDialogue()
    {
        isDialogueActive = false;

        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.PopState();
        }

        // Esconder UI
        if (dialogueUI != null)
        {
            dialogueUI.Hide();
        }

        OnDialogueEnd?.Invoke();

        currentDialogue = null;
        currentLineIndex = 0;
    }

    public bool IsDialogueActive() => isDialogueActive;
}