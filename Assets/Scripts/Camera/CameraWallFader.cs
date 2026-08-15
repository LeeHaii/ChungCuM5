using System.Collections.Generic;
using UnityEngine;

public class CameraWallFader : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private sealed class WallState
    {
        public Renderer Renderer;
        public Transform Transform;
        public int ColorPropertyId;
        public Color BaseColor;
        public float LastAlpha = float.NaN;
        public bool LastDarkened;
    }

    [Header("Fade Settings")]
    public string wallTag = "Wall";

    [Tooltip("Distance where the wall becomes completely invisible (0% opacity).")]
    public float fullTransparentDistance = 3f;

    [Tooltip("Distance where the wall becomes completely solid (100% opacity).")]
    public float fullSolidDistance = 8f;

    [SerializeField, Min(0f)] private float movementEpsilon = 0.001f;
    [SerializeField, Min(0f)] private float alphaEpsilon = 0.001f;

    private readonly List<WallState> walls = new List<WallState>(128);
    private MaterialPropertyBlock propertyBlock;
    private Vector3 lastCameraPosition;
    private Renderer lastHoveredRenderer;
    private bool hasCameraPosition;

    private void Start()
    {
        propertyBlock = new MaterialPropertyBlock();
        GameObject[] wallObjects = GameObject.FindGameObjectsWithTag(wallTag);
        walls.Capacity = Mathf.Max(walls.Capacity, wallObjects.Length);

        for (int i = 0; i < wallObjects.Length; i++)
        {
            Renderer renderer = wallObjects[i].GetComponent<Renderer>();
            Material material = renderer != null ? renderer.sharedMaterial : null;
            if (renderer == null || material == null) continue;

            int propertyId;
            Color baseColor;
            if (material.HasProperty(BaseColorId))
            {
                propertyId = BaseColorId;
                baseColor = material.GetColor(BaseColorId);
            }
            else if (material.HasProperty(ColorId))
            {
                propertyId = ColorId;
                baseColor = material.GetColor(ColorId);
            }
            else
            {
                continue;
            }

            walls.Add(new WallState
            {
                Renderer = renderer,
                Transform = renderer.transform,
                ColorPropertyId = propertyId,
                BaseColor = baseColor
            });
        }
    }

    private void LateUpdate()
    {
        Vector3 cameraPosition = transform.position;
        Renderer hoveredRenderer = HoverManager.Instance != null
            ? HoverManager.Instance.CurrentHoveredRenderer
            : null;

        float movementEpsilonSqr = movementEpsilon * movementEpsilon;
        bool cameraMoved = !hasCameraPosition
            || (cameraPosition - lastCameraPosition).sqrMagnitude > movementEpsilonSqr;
        bool hoverChanged = hoveredRenderer != lastHoveredRenderer;
        if (!cameraMoved && !hoverChanged) return;

        lastCameraPosition = cameraPosition;
        lastHoveredRenderer = hoveredRenderer;
        hasCameraPosition = true;

        float transparentDistance = Mathf.Min(fullTransparentDistance, fullSolidDistance);
        float solidDistance = Mathf.Max(fullTransparentDistance, fullSolidDistance);
        float transparentDistanceSqr = transparentDistance * transparentDistance;
        float solidDistanceSqr = solidDistance * solidDistance;

        for (int i = 0; i < walls.Count; i++)
        {
            WallState wall = walls[i];
            if (wall.Renderer == null || wall.Transform == null) continue;

            float distanceSqr = (cameraPosition - wall.Transform.position).sqrMagnitude;
            float targetAlpha;
            if (distanceSqr <= transparentDistanceSqr)
            {
                targetAlpha = 0f;
            }
            else if (distanceSqr >= solidDistanceSqr)
            {
                targetAlpha = 1f;
            }
            else
            {
                targetAlpha = Mathf.InverseLerp(
                    transparentDistance,
                    solidDistance,
                    Mathf.Sqrt(distanceSqr));
            }

            bool darkened = hoveredRenderer == wall.Renderer;
            if (!float.IsNaN(wall.LastAlpha)
                && Mathf.Abs(wall.LastAlpha - targetAlpha) <= alphaEpsilon
                && wall.LastDarkened == darkened)
            {
                continue;
            }

            wall.Renderer.GetPropertyBlock(propertyBlock);
            Color color = wall.BaseColor;
            if (darkened && HoverManager.Instance != null)
            {
                float multiplier = HoverManager.Instance.DarkenMultiplier;
                color.r *= multiplier;
                color.g *= multiplier;
                color.b *= multiplier;
            }

            color.a = targetAlpha;
            propertyBlock.SetColor(wall.ColorPropertyId, color);
            wall.Renderer.SetPropertyBlock(propertyBlock);
            wall.LastAlpha = targetAlpha;
            wall.LastDarkened = darkened;
        }
    }

    private void OnDisable()
    {
        ResetWalls();
    }

    public void ResetWalls()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < walls.Count; i++)
        {
            WallState wall = walls[i];
            if (wall.Renderer == null) continue;

            wall.Renderer.GetPropertyBlock(propertyBlock);
            Color color = wall.BaseColor;
            color.a = 1f;
            propertyBlock.SetColor(wall.ColorPropertyId, color);
            wall.Renderer.SetPropertyBlock(propertyBlock);
            wall.LastAlpha = float.NaN;
            wall.LastDarkened = false;
        }

        hasCameraPosition = false;
        lastHoveredRenderer = null;
    }
}
