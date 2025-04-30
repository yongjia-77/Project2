using UnityEngine;
using System.Collections;

public class MushroomHoverController : MonoBehaviour
{
    [Header("References")]
    public SkinnedMeshRenderer capRenderer;
    public SkinnedMeshRenderer ringRenderer;
    public SkinnedMeshRenderer stemRenderer;

    [Header("ShapeKey Settings")]
    public string capShapeKey1 = "Key 1";
    public string capShapeKey2 = "Key 2";
    public string ringShapeKey = "Key 1";
    public string stemShapeKey = "Key 1";

    [Header("Animation Settings")]
    public float capAnimationDuration = 6.0f;
    public float ringAnimationDuration = 6.5f;
    public float stemAnimationDuration = 6.5f;

    [Header("Collider Settings")]
    public float colliderGrowthMultiplier = 1.5f;
    public float colliderHeightOffset = 0.5f;

    // Internal variables
    private BoxCollider boxCollider;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;
    private bool isHovering = false;
    private bool isAnimationStarted = false;
    private Coroutine animationCoroutine;

    // Blend shape values
    private float capBlendValue1 = 0;
    private float capBlendValue2 = 0;
    private float ringBlendValue = 0;
    private float stemBlendValue = 0;

    // Blend shape indices
    private int capShapeKeyIndex1 = -1;
    private int capShapeKeyIndex2 = -1;
    private int ringShapeKeyIndex = -1;
    private int stemShapeKeyIndex = -1;

    void OnDrawGizmos()
    {
        // Draw box to show collider in editor
        if (boxCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }

    void Start()
    {
        // Get BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1f, 1f, 1f); // Set reasonable default size
        }

        // Store original collider properties
        originalColliderSize = boxCollider.size;
        originalColliderCenter = boxCollider.center;

        // Auto-find renderers if not set
        if (capRenderer == null && transform.Find("Cap") != null)
        {
            capRenderer = transform.Find("Cap").GetComponent<SkinnedMeshRenderer>();
        }

        if (ringRenderer == null && transform.Find("Ring") != null)
        {
            ringRenderer = transform.Find("Ring").GetComponent<SkinnedMeshRenderer>();
        }

        if (stemRenderer == null && transform.Find("Stem") != null)
        {
            stemRenderer = transform.Find("Stem").GetComponent<SkinnedMeshRenderer>();
        }

        // Find shape key indices
        if (capRenderer != null)
        {
            capShapeKeyIndex1 = FindShapeKeyIndex(capRenderer, capShapeKey1);
            capShapeKeyIndex2 = FindShapeKeyIndex(capRenderer, capShapeKey2);
            Debug.Log($"Cap ShapeKey1 Index: {capShapeKeyIndex1}, ShapeKey2 Index: {capShapeKeyIndex2}");
        }

        if (ringRenderer != null)
        {
            ringShapeKeyIndex = FindShapeKeyIndex(ringRenderer, ringShapeKey);
            Debug.Log($"Ring ShapeKey Index: {ringShapeKeyIndex}");
        }

        if (stemRenderer != null)
        {
            stemShapeKeyIndex = FindShapeKeyIndex(stemRenderer, stemShapeKey);
            Debug.Log($"Stem ShapeKey Index: {stemShapeKeyIndex}");
        }

        // Log errors
        LogErrorIfNeeded(capRenderer, capShapeKeyIndex1, capShapeKey1, "Cap");
        LogErrorIfNeeded(capRenderer, capShapeKeyIndex2, capShapeKey2, "Cap");
        LogErrorIfNeeded(ringRenderer, ringShapeKeyIndex, ringShapeKey, "Ring");
        LogErrorIfNeeded(stemRenderer, stemShapeKeyIndex, stemShapeKey, "Stem");

        Debug.Log("MushroomHoverController initialized");
    }

    // Helper method to find shape key index by name
    private int FindShapeKeyIndex(SkinnedMeshRenderer renderer, string shapeKeyName)
    {
        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
        {
            string name = renderer.sharedMesh.GetBlendShapeName(i);
            Debug.Log($"Found blend shape: {name} on {renderer.name}");
            if (name == shapeKeyName)
                return i;
        }
        return -1;
    }

    // Helper method to log errors
    private void LogErrorIfNeeded(SkinnedMeshRenderer renderer, int index, string keyName, string partName)
    {
        if (renderer == null)
        {
            Debug.LogError($"{partName} SkinnedMeshRenderer not found!");
            return;
        }

        if (index == -1)
        {
            Debug.LogError($"ShapeKey '{keyName}' not found on {partName} renderer!");
        }
    }

    void OnMouseEnter()
    {
        isHovering = true;
        Debug.Log("Mouse entered collision area");

        if (!isAnimationStarted)
        {
            StartAnimation();
        }
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (isAnimationStarted)
        {
            StopAnimation();
        }
        Debug.Log("Mouse exited collision area");
    }

    void StartAnimation()
    {
        isAnimationStarted = true;

        // Stop previous animation if running
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        // Start new animation
        animationCoroutine = StartCoroutine(AnimateShapeKeys());
        Debug.Log("Starting animation");
    }

    void StopAnimation()
    {
        isAnimationStarted = false;

        // Stop animation coroutine
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        Debug.Log("Animation stopped");
    }

    IEnumerator AnimateShapeKeys()
    {
        float startCapValue1 = capBlendValue1;
        float startCapValue2 = capBlendValue2;
        float startRingValue = ringBlendValue;
        float startStemValue = stemBlendValue;
        float startTime = Time.time;

        bool allComplete = false;

        while (isAnimationStarted && !allComplete)
        {
            float elapsedTime = Time.time - startTime;

            // Animate Cap blend shapes
            if (capRenderer != null)
            {
                // Animate Cap Key 1
                if (capBlendValue1 < 100 && capShapeKeyIndex1 != -1)
                {
                    float progress = Mathf.Clamp01(elapsedTime / capAnimationDuration);
                    capBlendValue1 = Mathf.Lerp(startCapValue1, 100, progress);
                    capRenderer.SetBlendShapeWeight(capShapeKeyIndex1, capBlendValue1);
                }

                // Animate Cap Key 2
                if (capBlendValue2 < 100 && capShapeKeyIndex2 != -1)
                {
                    float progress = Mathf.Clamp01(elapsedTime / capAnimationDuration);
                    capBlendValue2 = Mathf.Lerp(startCapValue2, 100, progress);
                    capRenderer.SetBlendShapeWeight(capShapeKeyIndex2, capBlendValue2);
                }
            }

            // Animate Ring blend shape
            if (ringBlendValue < 100 && ringShapeKeyIndex != -1 && ringRenderer != null)
            {
                float progress = Mathf.Clamp01(elapsedTime / ringAnimationDuration);
                ringBlendValue = Mathf.Lerp(startRingValue, 100, progress);
                ringRenderer.SetBlendShapeWeight(ringShapeKeyIndex, ringBlendValue);
            }

            // Animate Stem blend shape
            if (stemBlendValue < 100 && stemShapeKeyIndex != -1 && stemRenderer != null)
            {
                float progress = Mathf.Clamp01(elapsedTime / stemAnimationDuration);
                stemBlendValue = Mathf.Lerp(startStemValue, 100, progress);
                stemRenderer.SetBlendShapeWeight(stemShapeKeyIndex, stemBlendValue);
            }

            // Determine animation progress for collider (use average of all animations)
            float averageProgress = 0;
            int progressCount = 0;

            if (capShapeKeyIndex1 != -1) { averageProgress += capBlendValue1; progressCount++; }
            if (capShapeKeyIndex2 != -1) { averageProgress += capBlendValue2; progressCount++; }
            if (ringShapeKeyIndex != -1) { averageProgress += ringBlendValue; progressCount++; }
            if (stemShapeKeyIndex != -1) { averageProgress += stemBlendValue; progressCount++; }

            if (progressCount > 0)
            {
                averageProgress /= (progressCount * 100f); // Normalize to 0-1 range

                // Update collider size and position
                float scaleFactor = 1f + (colliderGrowthMultiplier - 1f) * averageProgress;
                boxCollider.size = originalColliderSize * scaleFactor;

                // Move collider up as animation progresses
                Vector3 newCenter = originalColliderCenter;
                newCenter.y += colliderHeightOffset * averageProgress;
                boxCollider.center = newCenter;
            }

            // Check if all animations are complete
            allComplete = (capBlendValue1 >= 100 || capShapeKeyIndex1 == -1) &&
                          (capBlendValue2 >= 100 || capShapeKeyIndex2 == -1) &&
                          (ringBlendValue >= 100 || ringShapeKeyIndex == -1) &&
                          (stemBlendValue >= 100 || stemShapeKeyIndex == -1);

            yield return null;
        }
    }

    // Reset animation function if needed
    public void ResetAnimation()
    {
        capBlendValue1 = 0;
        capBlendValue2 = 0;
        ringBlendValue = 0;
        stemBlendValue = 0;

        if (capRenderer != null)
        {
            if (capShapeKeyIndex1 != -1)
                capRenderer.SetBlendShapeWeight(capShapeKeyIndex1, 0);
            if (capShapeKeyIndex2 != -1)
                capRenderer.SetBlendShapeWeight(capShapeKeyIndex2, 0);
        }

        if (ringRenderer != null && ringShapeKeyIndex != -1)
            ringRenderer.SetBlendShapeWeight(ringShapeKeyIndex, 0);

        if (stemRenderer != null && stemShapeKeyIndex != -1)
            stemRenderer.SetBlendShapeWeight(stemShapeKeyIndex, 0);

        boxCollider.size = originalColliderSize;
        boxCollider.center = originalColliderCenter;
    }
}