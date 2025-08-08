using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(AudioSource))]
public class AudioOcclusion : MonoBehaviour
{
    [SerializeField] private Transform playerTransform; // Reference to the AudioListener component
    [SerializeField] private LayerMask groundLayer; // Layer mask for ground objects
    [SerializeField] private float volumeDampeningFactor = 0.1f; // Factor to reduce volume when occluded

    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;


    private void Start()
    {
        if (!this.TryGetComponent<AudioSource>(out audioSource))
        {
            Debug.LogError("AudioSource missing on " + gameObject.name);
        }
        if (!this.TryGetComponent<AudioLowPassFilter>(out lowPassFilter))
        {
            Debug.LogWarning("AudioLowPassFilter missing on " + gameObject.name + ", will not apply muffled effect.");
        }
        if (playerTransform == null)
        {
            Debug.LogError("Player Transform is not assigned. Please assign the AudioListener's transform.");
        }
        if (groundLayer == 0)
        {
            Debug.LogError("Ground layer is not set. Please assign a layer mask for ground objects.");
        }
    }

    private void Update()
    {
        UpdateAudioOcclusion();
    }

    /// <summary>
    /// Calculates the total ground length between the source position and the current position by summing the lengths
    /// of intersected colliders.
    /// </summary>
    /// <param name="sourcePos">The starting position from which the ground length is measured.</param>
    /// <param name="hits">An array of <see cref="RaycastHit2D"/> objects representing the colliders intersected by the ray.</param>
    /// <returns>The total length of the ground covered by the intersected colliders.</returns>
    private float GetGroundLengthBetween2D(RaycastHit2D[] hits)
    {        
        float groundLength = 0f;

        foreach (var hit in hits)
        {
            Collider2D collider = hit.collider;
            if (collider is TilemapCollider2D box)
            {
                // Project the ray onto the box bounds to get entry/exit points
                Bounds bounds = box.bounds;
                // Find intersection points (entry/exit) with bounds
                // For simplicity, use bounds.min.x and bounds.max.x along the ray direction
                float minX = bounds.min.x;
                float maxX = bounds.max.x;
                float segmentLength = Mathf.Abs(maxX - minX);
                groundLength += segmentLength;
            }
            else
            {
                // For other collider types, use bounds size as an approximation
                groundLength += collider.bounds.size.x;
            }
        }

        return groundLength;
    }

    /// <summary>
    /// Updates the audio occlusion effect for the specified <see cref="AudioSource"/>.
    /// </summary>
    /// <remarks>This method calculates the occlusion of sound between the audio source and the listener by
    /// performing a raycast. If any obstacles are detected, the volume of the audio source is dampened, and a low-pass
    /// filter is applied to simulate a muffled effect. If no obstacles are detected, the audio source plays at full
    /// volume with no filter applied.</remarks>
    /// <param name="audioSource">The audio source whose occlusion effect is to be updated.</param>
    private void UpdateAudioOcclusion()
    {
        Vector3 sourcePos = transform.position;
        Vector3 listenerPos = (playerTransform.position + new Vector3(0f, 1f, 0f));
        Vector3 direction = listenerPos - sourcePos;
        float distance = direction.magnitude;

        // Raycast from source to listener
        RaycastHit2D[] hits = Physics2D.RaycastAll(sourcePos, direction.normalized, distance, groundLayer);

        // Dampen sound if there are any hits
        if (hits.Length > 0) {
            float groundLength = GetGroundLengthBetween2D(); // Calculate the total ground length between source and listener
            audioSource.volume = 1 - (volumeDampeningFactor * groundLength); // Dampen volume

            // Optionally, adjust low-pass filter for muffled effect
            if (lowPassFilter != null)
                lowPassFilter.cutoffFrequency = 500 / (groundLength / 2); // Muffle sound based on half the ground length
        }
        else {
            // Sound is not blocked
            audioSource.volume = 1.0f;
            if (lowPassFilter != null)
                lowPassFilter.cutoffFrequency = 22000; // Normal sound
        }
    }

    private float GetGroundLengthBetween2D(float sampleStep = 0.2f)
    {
        Vector2 direction = (playerTransform.position + new Vector3(0f, 1f, 0f)) - transform.position;
        float distance = direction.magnitude;
        Vector2 dirNorm = direction.normalized;

        float groundLength = 0f;
        for (float d = 0; d < distance; d += sampleStep)
        {
            Vector2 samplePoint = (Vector2)transform.position + dirNorm * d;
            Collider2D col = Physics2D.OverlapPoint(samplePoint, groundLayer);
            if (col != null)
            {
                groundLength += sampleStep;
            }
        }
        return groundLength;
    }
}
