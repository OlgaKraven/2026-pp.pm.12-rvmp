using UnityEngine;

public class CollectibleVisual : MonoBehaviour
{
    [SerializeField, Min(0f)] private float rotationSpeed = 70f;
    [SerializeField, Range(0f, 0.25f)] private float pulseAmount = 0.08f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 4f;

    private Vector3 startScale;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = startScale * pulse;
    }

    private void OnDisable()
    {
        transform.localScale = startScale;
    }
}
