using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MirrorGrowController : MonoBehaviour
{
    [Header("References")]
    public SkinnedMeshRenderer mirrorRenderer;
    public SkinnedMeshRenderer treeRenderer;
    public Camera vrCamera; // Reference to the VR camera

    [Header("ShapeKey Settings")]
    public string treeShapeKeyName = "Key 1";
    public string mirrorShapeKeyName = "Key 1";

    [Header("Animation Settings")]
    public float gazeDelay = 1.0f;
    public float treeAnimationDuration = 3.0f;
    public float mirrorAnimationDuration = 6.0f;
    public float colliderGrowthMultiplier = 1.5f;
    public float colliderHeightMultiplier = 5.0f;  // Height multiplier (y-axis)
    public float colliderElevationDistance = 1.0f; // How high the collider will rise

    // Internal variables
    private BoxCollider boxCollider;
    private Vector3 originalColliderSize;
    private Vector3 originalColliderCenter;
    private bool isGazing = false;
    private bool isAnimationStarted = false;
    private float gazeTimer = 0;
    private Coroutine animationCoroutine;
    private float treeBlendValue = 0;
    private float mirrorBlendValue = 0;
    private int treeShapeKeyIndex = -1;
    private int mirrorShapeKeyIndex = -1;

    void OnDrawGizmos()
    {
        // Draw collider area in the editor
        if (boxCollider != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }

    public void ResetAnimation()
    {
        treeBlendValue = 0;
        mirrorBlendValue = 0;

        if (treeRenderer != null && treeShapeKeyIndex != -1)
            treeRenderer.SetBlendShapeWeight(treeShapeKeyIndex, 0);

        if (mirrorRenderer != null && mirrorShapeKeyIndex != -1)
            mirrorRenderer.SetBlendShapeWeight(mirrorShapeKeyIndex, 0);

        boxCollider.size = originalColliderSize;
        boxCollider.center = originalColliderCenter;
    }

    void Start()
    {
        // Add or get BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1f, 1f, 1f); // Default size
        }

        originalColliderSize = boxCollider.size;
        originalColliderCenter = boxCollider.center;

        // Get shape key indices
        if (treeRenderer != null)
            treeShapeKeyIndex = FindShapeKeyIndex(treeRenderer, treeShapeKeyName);

        if (mirrorRenderer != null)
            mirrorShapeKeyIndex = FindShapeKeyIndex(mirrorRenderer, mirrorShapeKeyName);

        // If VR camera not assigned, try to find main camera
        if (vrCamera == null)
        {
            vrCamera = Camera.main;
            Debug.LogWarning("VR Camera not assigned, using Main Camera instead");
        }
    }

    private int FindShapeKeyIndex(SkinnedMeshRenderer renderer, string shapeKeyName)
    {
        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
        {
            string name = renderer.sharedMesh.GetBlendShapeName(i);
            if (name == shapeKeyName)
                return i;
        }
        Debug.LogWarning("Shape key '" + shapeKeyName + "' not found");
        return -1;
    }

    void Update()
    {
        if (vrCamera == null)
            return;

        // Cast a ray from the VR camera center
        Ray gazeRay = new Ray(vrCamera.transform.position, vrCamera.transform.forward);
        RaycastHit hit;

        // Check if the ray hits this object's collider
        if (boxCollider.Raycast(gazeRay, out hit, 100f))
        {
            if (!isGazing)
            {
                isGazing = true;
                gazeTimer = 0;
            }

            // Increment timer when gazing
            gazeTimer += Time.deltaTime;

            if (gazeTimer >= gazeDelay && !isAnimationStarted)
            {
                StartAnimation();
            }
        }
        else
        {
            // Not gazing at object
            isGazing = false;
            if (isAnimationStarted)
            {
                StopAnimation();
            }
        }
    }

    void StartAnimation()
    {
        isAnimationStarted = true;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        animationCoroutine = StartCoroutine(AnimateShapeKeys());
    }

    void StopAnimation()
    {
        isAnimationStarted = false;

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }

    IEnumerator AnimateShapeKeys()
    {
        float startTreeValue = treeBlendValue;
        float startMirrorValue = mirrorBlendValue;
        float startTime = Time.time;

        while (isAnimationStarted && (treeBlendValue < 100 || mirrorBlendValue < 100))
        {
            float elapsedTime = Time.time - startTime;

            if (treeBlendValue < 100 && treeShapeKeyIndex != -1 && treeRenderer != null)
            {
                float treeProgress = Mathf.Clamp01(elapsedTime / treeAnimationDuration);
                treeBlendValue = Mathf.Lerp(startTreeValue, 100, treeProgress);
                treeRenderer.SetBlendShapeWeight(treeShapeKeyIndex, treeBlendValue);
            }

            if (mirrorBlendValue < 100 && mirrorShapeKeyIndex != -1 && mirrorRenderer != null)
            {
                float mirrorProgress = Mathf.Clamp01(elapsedTime / mirrorAnimationDuration);
                mirrorBlendValue = Mathf.Lerp(startMirrorValue, 100, mirrorProgress);
                mirrorRenderer.SetBlendShapeWeight(mirrorShapeKeyIndex, mirrorBlendValue);
            }

            float colliderProgress = treeBlendValue / 100f;

            // Calculate new size with custom height multiplier
            float widthScaleFactor = 1f + (colliderGrowthMultiplier - 1f) * colliderProgress;
            float heightScaleFactor = 1f + (colliderHeightMultiplier - 1f) * colliderProgress;

            Vector3 newSize = new Vector3(
                originalColliderSize.x * widthScaleFactor,
                originalColliderSize.y * heightScaleFactor,
                originalColliderSize.z * widthScaleFactor
            );

            // Calculate new elevation position
            float elevationAmount = colliderElevationDistance * colliderProgress;
            Vector3 newCenter = new Vector3(
                originalColliderCenter.x,
                originalColliderCenter.y + elevationAmount,
                originalColliderCenter.z
            );

            // Apply new size and position
            boxCollider.size = newSize;
            boxCollider.center = newCenter;

            yield return null;
        }
    }

    // Optional: Add visual feedback for debugging
    void OnGUI()
    {
        if (isGazing)
        {
            GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 50, 200, 30),
                "Gazing at object: " + gameObject.name + " (" + gazeTimer.ToString("F1") + "s)");
        }
    }
}