using System;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class ProjectileController : MonoBehaviour
    {
        private const float HitDistanceSqr = 0.0004f;

        private Block targetBlock;
        private LevelController levelController;
        private Action<Block, bool> onFinished;
        private float speed;

        public static ProjectileController Create(
            BlockColor color,
            Vector3 startPosition,
            float projectileScale,
            float projectileSpeed,
            Block target,
            LevelController level,
            Action<Block, bool> finishedCallback)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = $"Projectile_{color}";
            projectile.transform.position = startPosition;
            projectile.transform.localScale = Vector3.one * projectileScale;

            Renderer renderer = projectile.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"))
                {
                    color = BlockView.GetUnityColor(color)
                };
            }

            Collider collider = projectile.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            ProjectileController controller = projectile.AddComponent<ProjectileController>();
            controller.Configure(projectileSpeed, target, level, finishedCallback);
            return controller;
        }

        private void Configure(
            float projectileSpeed,
            Block target,
            LevelController level,
            Action<Block, bool> finishedCallback)
        {
            speed = projectileSpeed;
            targetBlock = target;
            levelController = level;
            onFinished = finishedCallback;
        }

        private void Update()
        {
            if (targetBlock == null || levelController == null || levelController.State != GameState.Playing)
            {
                Finish(false);
                return;
            }

            Vector3 targetPosition = targetBlock.transform.position;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime);

            if ((transform.position - targetPosition).sqrMagnitude <= HitDistanceSqr)
            {
                Finish(true);
            }
        }

        private void Finish(bool hitTarget)
        {
            Block finishedTarget = targetBlock;
            Action<Block, bool> finishedCallback = onFinished;

            targetBlock = null;
            onFinished = null;

            finishedCallback?.Invoke(finishedTarget, hitTarget);
            Destroy(gameObject);
        }
    }
}
