using UnityEngine;

[System.Serializable]
public class StatDefinition
{
    public string id;
    public string displayName;
    public bool hasMinValue;
    public float minValue;
    public bool hasMaxValue;
    public float maxValue;

    public float? MinValue => hasMinValue ? minValue : null;
    public float? MaxValue => hasMaxValue ? maxValue : null;
}
