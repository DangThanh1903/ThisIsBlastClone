using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    [RequireComponent(typeof(Collider))]
    public sealed class BlastDragController : MonoBehaviour
    {
        [SerializeField] private BlastItem blastItem;
        [SerializeField] private BlastItemSpawner itemSpawner;
        [SerializeField] private LevelController levelController;

        private void Awake()
        {
            if (blastItem == null)
            {
                blastItem = GetComponent<BlastItem>();
            }
        }

        public void Configure(BlastItemSpawner spawner, LevelController level)
        {
            itemSpawner = spawner;
            levelController = level;
        }

        private void OnMouseDown()
        {
            if (!CanActivate())
            {
                return;
            }

            itemSpawner.ActivateItem(blastItem);
        }

        private bool CanActivate()
        {
            return blastItem != null
                && itemSpawner != null
                && levelController != null
                && levelController.State == GameState.Playing;
        }
    }
}
