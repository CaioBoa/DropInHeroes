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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        characterManager = GetComponent<CharacterManager>();
    }

    private void FixedUpdate()
    {
        // VERIFICAR SE PODE MOVER (via GameStateManager)
        if (GameStateManager.Instance != null && GameStateManager.Instance.CanPlayerMove())
        {
            rb.linearVelocity = moveInput * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
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
        // Apenas flipar se puder se mover
        if (GameStateManager.Instance == null || !GameStateManager.Instance.CanPlayerMove()) return;

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
        moveInput = value.Get<Vector2>();
    }

    public void OnSwitchCharacter(InputValue value)
    {
        if (characterManager != null)
        {
            characterManager.SwitchToNextCharacter();
        }
    }
}