using UnityEngine;

/// <summary>
/// Módulo que gerencia footprint circle da unidade
/// Responde a eventos globais de drag e detecção de swap
/// Gerencia collider usado para EventSystem e re-drag
/// </summary>
public class FootprintModule : IUnitModule
{
    private UnitController controller;
    private UnitFootprint footprint;
    private CircleCollider2D footprintCollider;

    // Lock de posição durante lerp
    private bool isPositionLocked = false;
    private Vector2 lockedWorldPosition;

    public void Initialize(UnitController unitController)
    {
        controller = unitController;
        footprint = controller.GetComponentInChildren<UnitFootprint>();

        if (footprint == null)
        {
            DebugManager.LogError("UnitFootprint não encontrado! É necessário ter child 'Footprint' no prefab.", DebugCategory.Drag);
            return;
        }

        // NOVO: Buscar collider no footprint
        footprintCollider = footprint.GetComponent<CircleCollider2D>();
        if (footprintCollider == null)
        {
            DebugManager.LogError("CircleCollider2D não encontrado no Footprint! Adicione manualmente na prefab.", DebugCategory.Drag);
            return;
        }

        // Configurar collider
        footprintCollider.isTrigger = true;
        footprintCollider.enabled = false; // Inicia desabilitado

        // Aplicar configurações globais do PreparationConfig
        if (PreparationManager.Instance != null && PreparationManager.Instance.Config != null)
        {
            ApplyConfig(PreparationManager.Instance.Config);
        }
        else
        {
            DebugManager.LogWarning("PreparationConfig não acessível! Footprint pode ter escala/offset incorreto.", DebugCategory.Drag);
        }

        // Inscrever em eventos globais
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGlobalDragStarted += HandleGlobalDragStarted;
            PreparationManager.Instance.OnGlobalDragEnded += HandleGlobalDragEnded;
        }
    }

    /// <summary>
    /// Aplica configurações globais do PreparationConfig ao footprint
    /// Deve ser chamado após Initialize()
    /// </summary>
    public void ApplyConfig(PreparationConfig config)
    {
        if (config == null)
        {
            DebugManager.LogWarning("PreparationConfig é null! Usando valores padrão.", DebugCategory.Drag);
            footprint?.SetScale(1.5f);
            footprint?.SetYOffset(-1.0f);
            return;
        }

        if (footprint != null)
        {
            footprint.SetScale(config.footprintScale);
            footprint.SetYOffset(config.footprintPlacedOffset);
        }
    }

    public void OnEnabled()
    {
        // Habilitar collider quando unidade está no board (permite re-drag)
        if (footprintCollider != null)
        {
            footprintCollider.enabled = true;
        }

        // Reinscrever em eventos globais
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGlobalDragStarted += HandleGlobalDragStarted;
            PreparationManager.Instance.OnGlobalDragEnded += HandleGlobalDragEnded;
        }
    }

    public void OnDisabled()
    {
        Debug.Log("FootprintModule OnDisabled called");
        // Desinscrever de eventos globais (impede resposta a drag)
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGlobalDragStarted -= HandleGlobalDragStarted;
            PreparationManager.Instance.OnGlobalDragEnded -= HandleGlobalDragEnded;
        }

        // Desabilitar collider
        if (footprintCollider != null)
        {
            footprintCollider.enabled = false;
        }

        // Esconder footprint
        if (footprint != null)
        {
            footprint.Hide();
        }
    }

    public void Cleanup()
    {
        // Desinscrever eventos
        if (PreparationManager.Instance != null)
        {
            PreparationManager.Instance.OnGlobalDragStarted -= HandleGlobalDragStarted;
            PreparationManager.Instance.OnGlobalDragEnded -= HandleGlobalDragEnded;
        }

        footprint = null;
        controller = null;
    }

    // === EVENT HANDLERS ===

    private void HandleGlobalDragStarted()
    {
        // Mostrar círculo quando QUALQUER drag começa
        if (footprint != null)
        {
            footprint.Show(UnitFootprint.FootprintState.Normal);
        }
    }

    private void HandleGlobalDragEnded()
    {
        // Esconder círculo quando drag termina
        if (footprint != null)
        {
            footprint.Hide();
        }
    }

    // === PUBLIC API ===

    public void SetState(UnitFootprint.FootprintState state)
    {
        if (footprint != null)
        {
            footprint.SetState(state);
        }
    }

    public void ShowSwapState()
    {
        SetState(UnitFootprint.FootprintState.Swap);
    }

    public void ShowNormalState()
    {
        SetState(UnitFootprint.FootprintState.Normal);
    }

    public void ShowValidState()
    {
        SetState(UnitFootprint.FootprintState.Valid);
    }

    public void ShowInvalidState()
    {
        SetState(UnitFootprint.FootprintState.Invalid);
    }

    public void ForceHide()
    {
        if (footprint != null)
        {
            footprint.Hide();
        }
    }

    public float GetFootprintRadius()
    {
        if (footprint != null)
        {
            return footprint.GetRadius();
        }
        return 0.75f; // Default baseado em scale 1.5
    }

    // === COLLIDER ACCESS ===

    public CircleCollider2D GetCollider()
    {
        return footprintCollider;
    }

    // === YOFFSET ACCESS ===

    public float GetYOffset()
    {
        if (footprint != null)
        {
            return footprint.GetYOffset();
        }
        return -1.0f; // Default
    }

    public void SetYOffset(float offset)
    {
        if (footprint != null)
        {
            footprint.SetYOffset(offset);
        }
    }

    // === FULL BOARD STATE ===

    public void ShowFullBoardState()
    {
        SetState(UnitFootprint.FootprintState.FullBoardNoSwap);
    }

    // === POSITION ACCESS ===

    /// <summary>
    /// Retorna a posição WORLD atual do footprint
    /// Esta é a posição de REFERÊNCIA para validação e placement
    /// </summary>
    public Vector2 GetCurrentWorldPosition()
    {
        if (footprint != null)
        {
            return footprint.transform.position;
        }
        return Vector2.zero;
    }

    // === LOCK DE POSIÇÃO (para lerp de queda) ===

    /// <summary>
    /// Fixa o footprint na posição world atual durante lerp de queda
    /// </summary>
    public void LockWorldPosition()
    {
        isPositionLocked = true;
        lockedWorldPosition = GetCurrentWorldPosition();
    }

    /// <summary>
    /// Deve ser chamado a cada frame durante o lerp para manter posição fixa
    /// </summary>
    public void MaintainLockedPosition()
    {
        if (!isPositionLocked || footprint == null || controller == null) return;

        Vector2 unitPos = controller.transform.position;
        Vector2 requiredLocalPos = lockedWorldPosition - unitPos;
        footprint.transform.localPosition = new Vector3(requiredLocalPos.x, requiredLocalPos.y, 0f);
    }

    /// <summary>
    /// Libera o lock e restaura offset normal
    /// </summary>
    public void UnlockPosition()
    {
        isPositionLocked = false;
        SetYOffset(0f);
    }
}
