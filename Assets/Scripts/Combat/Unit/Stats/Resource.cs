using UnityEngine;
using System;

public class Resource
{
    public ResourceType Type { get; }
    public string DisplayName { get; }

    private float currentValue;
    private Stat maxStat;
    private float minValue;

    public float CurrentValue => currentValue;
    public float MaxValue => maxStat.CurrentValue;
    public float Percentage => MaxValue > 0 ? currentValue / MaxValue : 0;
    public bool IsEmpty => currentValue <= minValue;
    public bool IsFull => currentValue >= MaxValue;

    public event Action<float> OnValueChanged;
    public event Action OnDepleted;

    public Resource(ResourceType type, Stat maxStat, StatDefinition definition)
    {
        Type = type;
        this.maxStat = maxStat;
        minValue = definition?.MinValue ?? 0f;
        DisplayName = definition?.displayName ?? type.ToString();
        currentValue = maxStat.CurrentValue;
    }

    public void Add(float amount)
    {
        float oldValue = currentValue;
        currentValue = Mathf.Clamp(currentValue + amount, minValue, MaxValue);
        if (currentValue != oldValue) OnValueChanged?.Invoke(currentValue);
    }

    public void Remove(float amount)
    {
        float oldValue = currentValue;
        currentValue = Mathf.Max(currentValue - amount, minValue);
        if (currentValue != oldValue) OnValueChanged?.Invoke(currentValue);
        if (IsEmpty) OnDepleted?.Invoke();
    }

    public void SetToMax() => currentValue = MaxValue;

    public void SetToMin() => currentValue = minValue;
}
