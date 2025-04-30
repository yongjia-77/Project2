using UnityEngine;
using System.Collections;

public class HoverShapeKeyController : MonoBehaviour
{
    [Header("References")]
    public SkinnedMeshRenderer mirrorRenderer;
    public SkinnedMeshRenderer treeRenderer;

    [Header("ShapeKey Settings")]
    public string treeShapeKeyName = "Key 1";
    public string mirrorShapeKeyName = "Key 1";

    [Header("Animation Settings")]
    public float hoverDelay = 1.0f;
    public float treeAnimationDuration = 3.0f;
    public float mirrorAnimationDuration = 6.0f;
    public float colliderGrowthMultiplier = 1.5f;

    // Internal variables
    private BoxCollider boxCollider;
    private Vector3 originalColliderSize;
    private bool isHovering = false;
    private bool isAnimationStarted = false;
    private float hoverTimer = 0;
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

        // Get shape key indices
        if (treeRenderer != null)
            treeShapeKeyIndex = FindShapeKeyIndex(treeRenderer, treeShapeKeyName);

        if (mirrorRenderer != null)
            mirrorShapeKeyIndex = FindShapeKeyIndex(mirrorRenderer, mirrorShapeKeyName);
    }

    private int FindShapeKeyIndex(SkinnedMeshRenderer renderer, string shapeKeyName)
    {
        for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
        {
            string name = renderer.sharedMesh.GetBlendShapeName(i);
            if (name == shapeKeyName)
                return i;
        }
        return -1;
    }

    void OnMouseEnter()
    {
        isHovering = true;
        hoverTimer = 0;
    }

    void OnMouseExit()
    {
        isHovering = false;
        if (isAnimationStarted)
        {
            StopAnimation();
        }
    }

    void Update()
    {
        if (isHovering && !isAnimationStarted)
        {
            hoverTimer += Time.deltaTime;

            if (hoverTimer >= hoverDelay)
            {
                StartAnimation();
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
            float scaleFactor = 1f + (colliderGrowthMultiplier - 1f) * colliderProgress;
            boxCollider.size = originalColliderSize * scaleFactor;

            yield return null;
        }
    }
}
