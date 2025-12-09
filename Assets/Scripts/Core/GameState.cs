using UnityEngine;

public enum GameState
{
    Gameplay,      
    Dialogue,       
}

[System.Serializable]
public class GameStateConfig
{
    public GameState state;

    [Header("Permissões")]
    public bool allowPlayerMovement = true;
    public bool allowPlayerInteraction = true;
    public bool allowPause = true;
    public bool showInteractionIndicators = true;
    public bool detectInteractables = true;
    public bool freezeTime = false;

    [Header("UI")]
    public bool showHUD = true;
    public bool allowUIInput = true;
}
