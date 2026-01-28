using UnityEngine;

public class Stat
{
    public StatType Type { get; }
    public string DisplayName { get; }

    private float baseValue;
    private float hypotheticalValue;
    private float? minValue;
    private float? maxValue;

    public float CurrentValue => ClampValue(hypotheticalValue);
    public float HypotheticalValue => hypotheticalValue;
    public float BaseValue => baseValue;

    public Stat(StatType type, float baseValue, StatDefinition definition)
    {
        Type = type;
        this.baseValue = baseValue;
        hypotheticalValue = baseValue;
        minValue = definition?.MinValue;
        maxValue = definition?.MaxValue;
        DisplayName = definition?.displayName ?? type.ToString();
    }

    public void Modify(float amount) => hypotheticalValue += amount;

    public void SetValue(float value) => hypotheticalValue = value;

    public void SetBaseValue(float value)
    {
        baseValue = value;
        hypotheticalValue = value;
    }

    public void Reset() => hypotheticalValue = baseValue;

    private float ClampValue(float value)
    {
        if (minValue.HasValue) value = Mathf.Max(value, minValue.Value);
        if (maxValue.HasValue) value = Mathf.Min(value, maxValue.Value);
        return value;
    }
}
