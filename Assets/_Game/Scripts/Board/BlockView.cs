using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlockView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] targetRenderers;

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private void Awake()
        {
            CacheRenderersIfNeeded();
        }

        public void SetColor(BlockColor color)
        {
            CacheRenderersIfNeeded();

            Color unityColor = GetUnityColor(color);
            propertyBlock.SetColor(ColorId, unityColor);
            propertyBlock.SetColor(BaseColorId, unityColor);

            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void CacheRenderersIfNeeded()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                return;
            }

            targetRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public static Color GetUnityColor(BlockColor color)
        {
            switch (color)
            {
                case BlockColor.Yellow:
                    return new Color(1f, 0.86f, 0.18f);
                case BlockColor.Red:
                    return new Color(0.95f, 0.18f, 0.16f);
                case BlockColor.Blue:
                    return new Color(0.16f, 0.49f, 0.95f);
                case BlockColor.Green:
                    return new Color(0.22f, 0.75f, 0.32f);
                case BlockColor.Purple:
                    return new Color(0.6f, 0.25f, 0.9f);
                default:
                    return Color.white;
            }
        }
    }
}
