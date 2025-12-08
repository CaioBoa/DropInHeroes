using UnityEngine;
using TMPro;

public class InteractionIndicatorAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float bounceHeight = 0.3f;
    [SerializeField] private float bounceSpeed = 2f;
    
    [Header("References")]
    [SerializeField] private Transform InteractionIndicator;
    private Vector3 originalPosition;
    private float time;
    
    private void Start()
    {
        if (InteractionIndicator == null)
            InteractionIndicator = transform;
            
        originalPosition = InteractionIndicator.localPosition;
    }
    
    private void Update()
    {
        // Animação de bounce usando seno
        time += Time.deltaTime * bounceSpeed;
        float yOffset = Mathf.Abs(Mathf.Sin(time)) * bounceHeight;
        
        InteractionIndicator.localPosition = originalPosition + new Vector3(0, yOffset, 0);
    }
}