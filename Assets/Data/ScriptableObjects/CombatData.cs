using UnityEngine;

[CreateAssetMenu(fileName = "NewCombatData", menuName = "Game/Data/Combat Data")]
public class CombatData : ScriptableObject
{
    [Header("Combat Visual")]
    public RuntimeAnimatorController battleAnimator;

    [Header("Base Stats")]
    public int maxHP = 100;
    public int attack = 10;
    public int defense = 5;
    public int speed = 10;
    public int range = 5;
}