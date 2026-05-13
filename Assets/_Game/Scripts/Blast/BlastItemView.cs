using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class BlastItemView : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private TextMesh powerText;

        private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

        private void Awake()
        {
            CacheRenderersIfNeeded();
            
            if (powerText == null)
            {
                powerText = GetComponentInChildren<TextMesh>();
            }
        }

        public void SetData(BlockColor color, int power)
        {
            CacheRenderersIfNeeded();

            Color unityColor = BlockView.GetUnityColor(color);
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

            if (powerText != null)
            {
                powerText.text = power.ToString();
            }
        }

        private void CacheRenderersIfNeeded()
        {
            if (targetRenderers != null && targetRenderers.Length > 0)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            int writeIndex = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].GetComponent<TextMesh>() == null)
                {
                    renderers[writeIndex] = renderers[i];
                    writeIndex++;
                }
            }

            targetRenderers = new Renderer[writeIndex];
            for (int i = 0; i < writeIndex; i++)
            {
                targetRenderers[i] = renderers[i];
            }
        }
    }
}
