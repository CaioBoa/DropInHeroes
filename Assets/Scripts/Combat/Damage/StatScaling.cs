[System.Serializable]
public struct StatScaling
{
    public StatType stat;
    public float multiplier;

    public StatScaling(StatType stat, float multiplier)
    {
        this.stat = stat;
        this.multiplier = multiplier;
    }
}
