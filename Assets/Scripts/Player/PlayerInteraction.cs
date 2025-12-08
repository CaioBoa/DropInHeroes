using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    private Interactable currentInteractable;

    private void Update()
    {
        DetectInteractables();
    }

    private void DetectInteractables()
    {
        // Buscar todos interactables na área
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius, interactableLayer);

        Interactable closestInteractable = null;
        float closestDistance = float.MaxValue;

        // Encontrar o interactable mais próximo
        foreach (var hit in hits)
        {
            Interactable interactable = hit.GetComponent<Interactable>();
            
            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, hit.transform.position);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
        }

        // Atualizar referência atual
        UpdateCurrentInteractable(closestInteractable);
    }

    private void UpdateCurrentInteractable(Interactable newInteractable)
    {
        // Se mudou de interactable
        if (newInteractable != currentInteractable)
        {
            // Esconder indicador do anterior
            if (currentInteractable != null)
            {
                currentInteractable.HideIndicator();
            }

            // Mostrar indicador do novo
            if (newInteractable != null)
            {
                newInteractable.ShowIndicator();
            }

            currentInteractable = newInteractable;
        }
    }

    // === INPUT CALLBACK ===

    public void OnInteract(InputValue value)
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
        else
        {
            Debug.Log("[PlayerInteraction] Nenhum objeto interagível próximo");
        }
    }

    // === GETTERS ===

    public Interactable GetCurrentInteractable() => currentInteractable;
    public bool HasInteractable() => currentInteractable != null;

    // === DEBUG ===

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Raio de detecção
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        // Linha para o interactable atual
        if (currentInteractable != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentInteractable.transform.position);
        }
    }

    // === CLEANUP ===

    private void OnDisable()
    {
        // Esconder indicador ao desabilitar player
        if (currentInteractable != null)
        {
            currentInteractable.HideIndicator();
        }
    }
}