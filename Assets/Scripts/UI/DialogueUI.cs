using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI leftCharacterNameText;
    [SerializeField] private TextMeshProUGUI rightCharacterNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image leftCharacterPortrait;
    [SerializeField] private Image rightCharacterPortrait;
    [SerializeField] private Image leftCharacterNameBackground;
    [SerializeField] private Image rightCharacterNameBackground;

    [Header("Animation (Opcional)")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private bool useTypewriterEffect = true;

    [Header("Speaker Emphasis Settings")]
    [SerializeField] private float inactiveAlpha = 0.8f;           // Transparência do inativo
    [SerializeField] private float activeAlpha = 1f;               // Transparência do ativo
    [SerializeField] private float inactiveScale = 0.95f;          // Escala do inativo
    [SerializeField] private float activeScale = 1.05f;            // Escala do ativo (destaque)
    [SerializeField] private Color inactiveTint = Color.gray;      // Cor do inativo (escurecido)
    [SerializeField] private Color activeTint = Color.white;       // Cor do ativo (brilhante)
    [SerializeField] private float emphasisTransitionSpeed = 5f;   // Velocidade da transição

    [Header("Name Emphasis Settings")]
    [SerializeField] private Color activeNameColor = Color.white;                      // Cor do texto do nome ativo
    [SerializeField] private Color inactiveNameColor = new Color(0.6f, 0.6f, 0.6f, 1f); // Cor do texto do nome inativo
    [SerializeField] private Color activeNameBgColor = Color.white;                    // Cor do fundo do nome ativo
    [SerializeField] private Color inactiveNameBgColor = new Color(0.5f, 0.5f, 0.5f, 0.8f); // Cor do fundo do nome inativo
    [SerializeField] private float activeNameBgAlpha = 1f;                             // Alpha do background ativo
    [SerializeField] private float inactiveNameBgAlpha = 0.6f;                         // Alpha do background inativo

    private bool isTyping = false;
    private Coroutine typewriterCoroutine;
    private Coroutine emphasisCoroutine;

    private CharacterData leftCharacter;
    private CharacterData rightCharacter;

    private void Awake()
    {
        // Esconder no início
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    public void Show()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // Parar typewriter se estiver rodando
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Parar animação de ênfase
        if (emphasisCoroutine != null)
        {
            StopCoroutine(emphasisCoroutine);
        }
    }

    public void DisplayLine(DialogueLine line)
    {
        if (line == null) return;

        if (line.isSingleDialogue)
        {
            rightCharacterPortrait.gameObject.SetActive(false);
            rightCharacterNameText.gameObject.SetActive(false);
            if (rightCharacterNameBackground != null)
            {
                rightCharacterNameBackground.gameObject.SetActive(false);
            }
        }
        else
        {
            rightCharacterPortrait.gameObject.SetActive(true);
            rightCharacterNameText.gameObject.SetActive(true);
            if (rightCharacterNameBackground != null)
            {
                rightCharacterNameBackground.gameObject.SetActive(true);
            }
        }

        if (line.leftCharacterId != null)
        {
            leftCharacter = DataManager.GetCharacter(line.leftCharacterId);
            leftCharacterNameText.text = leftCharacter.displayName;
            leftCharacterPortrait.sprite = leftCharacter.dialoguePortrait;
        }

        if (line.rightCharacterId != null && !line.isSingleDialogue)
        {
            rightCharacter = DataManager.GetCharacter(line.rightCharacterId);
            rightCharacterNameText.text = rightCharacter.displayName;
            rightCharacterPortrait.sprite = rightCharacter.dialoguePortrait;
        }

        // === APLICAR ÊNFASE EM QUEM ESTÁ FALANDO ===
        SetSpeakerEmphasis(line.isLeftSpeaking);

        // Parar typewriter anterior
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
        }

        // Mostrar texto
        if (useTypewriterEffect)
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(line.text));
        }
        else
        {
            dialogueText.text = line.text;
        }
    }

    /// <summary>
    /// Define qual personagem está falando e aplica efeitos visuais de ênfase
    /// </summary>
    private void SetSpeakerEmphasis(bool isLeftSpeaking)
    {
        // Parar animação anterior
        if (emphasisCoroutine != null)
        {
            StopCoroutine(emphasisCoroutine);
        }

        // Iniciar nova animação de transição
        emphasisCoroutine = StartCoroutine(AnimateEmphasis(isLeftSpeaking));
    }

    /// <summary>
    /// Anima suavemente a transição de ênfase entre personagens
    /// </summary>
    private IEnumerator AnimateEmphasis(bool isLeftSpeaking)
    {
        float elapsedTime = 0f;
        float duration = 1f / emphasisTransitionSpeed;

        // Valores iniciais (atuais)
        Color leftPortraitColor = leftCharacterPortrait.color;
        Color rightPortraitColor = rightCharacterPortrait.color;
        Color leftNameColor = leftCharacterNameText.color;
        Color rightNameColor = rightCharacterNameText.color;
        Vector3 leftScale = leftCharacterPortrait.rectTransform.localScale;
        Vector3 rightScale = rightCharacterPortrait.rectTransform.localScale;

        // Backgrounds dos nomes (verificar se existem)
        Color leftNameBgColor = leftCharacterNameBackground != null ? leftCharacterNameBackground.color : Color.white;
        Color rightNameBgColor = rightCharacterNameBackground != null ? rightCharacterNameBackground.color : Color.white;

        // Valores alvo - PORTRAITS
        Color leftTargetColor = isLeftSpeaking ? activeTint : inactiveTint;
        Color rightTargetColor = isLeftSpeaking ? inactiveTint : activeTint;
        float leftTargetAlpha = isLeftSpeaking ? activeAlpha : inactiveAlpha;
        float rightTargetAlpha = isLeftSpeaking ? inactiveAlpha : activeAlpha;
        float leftTargetScale = isLeftSpeaking ? activeScale : inactiveScale;
        float rightTargetScale = isLeftSpeaking ? inactiveScale : activeScale;

        // Valores alvo - NOMES (texto)
        Color leftNameTargetColor = isLeftSpeaking ? activeNameColor : inactiveNameColor;
        Color rightNameTargetColor = isLeftSpeaking ? inactiveNameColor : activeNameColor;

        // Valores alvo - BACKGROUNDS dos nomes
        Color leftNameBgTargetColor = isLeftSpeaking ? activeNameBgColor : inactiveNameBgColor;
        Color rightNameBgTargetColor = isLeftSpeaking ? inactiveNameBgColor : activeNameBgColor;
        float leftNameBgTargetAlpha = isLeftSpeaking ? activeNameBgAlpha : inactiveNameBgAlpha;
        float rightNameBgTargetAlpha = isLeftSpeaking ? inactiveNameBgAlpha : activeNameBgAlpha;

        // Animar transição
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // === LERP PORTRAITS ===
            Color newLeftColor = Color.Lerp(leftPortraitColor, leftTargetColor, t);
            newLeftColor.a = Mathf.Lerp(leftPortraitColor.a, leftTargetAlpha, t);
            leftCharacterPortrait.color = newLeftColor;

            Color newRightColor = Color.Lerp(rightPortraitColor, rightTargetColor, t);
            newRightColor.a = Mathf.Lerp(rightPortraitColor.a, rightTargetAlpha, t);
            rightCharacterPortrait.color = newRightColor;

            // === LERP TEXTO DOS NOMES ===
            Color newLeftNameColor = Color.Lerp(leftNameColor, leftNameTargetColor, t);
            leftCharacterNameText.color = newLeftNameColor;

            Color newRightNameColor = Color.Lerp(rightNameColor, rightNameTargetColor, t);
            rightCharacterNameText.color = newRightNameColor;

            // === LERP BACKGROUNDS DOS NOMES ===
            if (leftCharacterNameBackground != null)
            {
                Color newLeftBgColor = Color.Lerp(leftNameBgColor, leftNameBgTargetColor, t);
                newLeftBgColor.a = Mathf.Lerp(leftNameBgColor.a, leftNameBgTargetAlpha, t);
                leftCharacterNameBackground.color = newLeftBgColor;
            }

            if (rightCharacterNameBackground != null)
            {
                Color newRightBgColor = Color.Lerp(rightNameBgColor, rightNameBgTargetColor, t);
                newRightBgColor.a = Mathf.Lerp(rightNameBgColor.a, rightNameBgTargetAlpha, t);
                rightCharacterNameBackground.color = newRightBgColor;
            }

            // === LERP ESCALA ===
            Vector3 newLeftScale = leftScale;
            newLeftScale.x = Mathf.Lerp(Mathf.Abs(leftScale.x), leftTargetScale, t);
            newLeftScale.y = Mathf.Lerp(leftScale.y, leftTargetScale, t);
            // Esquerda possui flip horizontal
            newLeftScale.x = -newLeftScale.x;
            leftCharacterPortrait.rectTransform.localScale = newLeftScale;

            Vector3 newRightScale = rightScale;
            newRightScale.x = Mathf.Lerp(Mathf.Abs(rightScale.x), rightTargetScale, t);
            newRightScale.y = Mathf.Lerp(rightScale.y, rightTargetScale, t);
            rightCharacterPortrait.rectTransform.localScale = newRightScale;

            yield return null;
        }

        // === GARANTIR VALORES FINAIS EXATOS ===

        // Portraits
        leftTargetColor.a = leftTargetAlpha;
        rightTargetColor.a = rightTargetAlpha;
        leftCharacterPortrait.color = leftTargetColor;
        rightCharacterPortrait.color = rightTargetColor;

        // Nomes
        leftCharacterNameText.color = leftNameTargetColor;
        rightCharacterNameText.color = rightNameTargetColor;

        // Backgrounds dos nomes
        if (leftCharacterNameBackground != null)
        {
            leftNameBgTargetColor.a = leftNameBgTargetAlpha;
            leftCharacterNameBackground.color = leftNameBgTargetColor;
        }

        if (rightCharacterNameBackground != null)
        {
            rightNameBgTargetColor.a = rightNameBgTargetAlpha;
            rightCharacterNameBackground.color = rightNameBgTargetColor;
        }
    }

    private IEnumerator TypewriterEffect(string text)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
    }

    public bool IsTyping() => isTyping;
    
    // Se clicar enquanto está digitando, completa instantaneamente
    public void SkipTypewriter(DialogueLine line)
    {
        if (isTyping && typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            isTyping = false;
        }
        dialogueText.text = line.text;
    }
}