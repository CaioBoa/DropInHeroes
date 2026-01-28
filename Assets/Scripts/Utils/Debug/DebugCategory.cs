/// <summary>
/// Categorias de debug para filtragem no console
/// </summary>
public enum DebugCategory
{
    Initialization,   // Setup e carregamento de sistemas
    State,            // Transições de estado (GameState, UnitState)
    Character,        // Carregamento de dados de personagem
    Interaction,      // Sistema de eventos e diálogos
    Drag,             // Sistema de drag and drop
    Combat,           // Batalha e unidades
    UI,               // Interface do usuário
    Pool              // Object pooling
}
