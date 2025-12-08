using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private CharacterManager characterManager;
    private bool canMove = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterManager = GetComponent<CharacterManager>();
    }

    private void OnEnable()
    {
        // Inscrever nos eventos do DialogueManager
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart += DisableMovement;
            DialogueManager.Instance.OnDialogueEnd += EnableMovement;
        }
    }

    private void OnDisable()
    {
        // Desinscrever dos eventos
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueStart -= DisableMovement;
            DialogueManager.Instance.OnDialogueEnd -= EnableMovement;
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    private void Update()
    {
        UpdateAnimation();
        FlipSprite();        
    }

    private void UpdateAnimation()
    {
        bool isMoving = moveInput.magnitude > 0.01f;
        animator.SetBool("isMoving", isMoving);
    } 

    private void FlipSprite()
    {
        if (!canMove) return;

        if (moveInput.x > 0.01f)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x < -0.01f)
        {
            spriteRenderer.flipX = false;
        }
    }

    // Chamado automaticamente pelo PlayerInput component
    public void OnMovement(InputValue value)
    {
        if (canMove)
        {
            moveInput = value.Get<Vector2>();
        }
        else
        {
            moveInput = Vector2.zero;
        }
    }

    public void OnSwitchCharacter(InputValue value)
    {
        if (characterManager != null)
        {
            characterManager.SwitchToNextCharacter();
        }
    }
    
    private void DisableMovement()
    {
        canMove = false;
        moveInput = Vector2.zero;  // Limpar input
        rb.linearVelocity = Vector2.zero;  // Parar imediatamente
        animator.SetBool("isMoving", false);  // Voltar para idle
        Debug.Log("[PlayerController] Movimento bloqueado");
    }

    private void EnableMovement()
    {
        canMove = true;
        Debug.Log("[PlayerController] Movimento liberado");
    }
}