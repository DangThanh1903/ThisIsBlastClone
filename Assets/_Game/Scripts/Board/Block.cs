using System;
using DG.Tweening;
using UnityEngine;

namespace ThisIsBlast.Gameplay
{
    public sealed class Block : MonoBehaviour
    {
        [SerializeField] private BlockView blockView;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float hitTiltAngle = 18f;
        [SerializeField] private float hitTiltDuration = 0.09f;
        [SerializeField] private float hitReboundDuration = 0.07f;
        [SerializeField] private float shrinkDuration = 0.16f;

        [SerializeField] private int x;
        [SerializeField] private int y;
        [SerializeField] private BlockColor color;

        private Vector3 targetWorldPosition;
        private Vector3 initialVisualScale;
        private Quaternion initialVisualRotation;
        private bool isMoving;
        private bool isShotted;
        private Sequence destroySequence;

        public int X => x;
        public int Y => y;
        public BlockColor Color => color;
        public BlockView View => blockView;
        public bool IsMoving => isMoving;
        public bool IsShotted => isShotted;

        private void Awake()
        {
            if (blockView == null)
            {
                blockView = GetComponent<BlockView>();
            }

            ResolveVisualRoot();
            targetWorldPosition = transform.position;
            CacheVisualTransform();
        }

        public void Init(int gridX, int gridY, BlockColor blockColor)
        {
            color = blockColor;
            isShotted = false;
            ResolveVisualRoot();
            ResetVisualTransform();
            SetGridPosition(gridX, gridY);

            if (blockView != null)
            {
                blockView.SetColor(color);
            }
        }

        public void SetGridPosition(int gridX, int gridY)
        {
            x = gridX;
            y = gridY;
        }

        public void MarkShotted()
        {
            isShotted = true;
        }

        public void ClearShotted()
        {
            isShotted = false;
        }

        public void PlayDestroyAnimation(Vector3 hitPosition, Vector3 impactDirection, Action completed)
        {
            isMoving = false;
            destroySequence?.Kill();
            ResolveVisualRoot();

            Vector3 flatImpactDirection = impactDirection;
            flatImpactDirection.y = 0f;

            if (flatImpactDirection.sqrMagnitude <= 0.0001f)
            {
                flatImpactDirection = transform.position - hitPosition;
                flatImpactDirection.y = 0f;
            }

            if (flatImpactDirection.sqrMagnitude <= 0.0001f)
            {
                flatImpactDirection = transform.forward;
            }

            Vector3 localImpactDirection = transform.InverseTransformDirection(flatImpactDirection.normalized);
            Vector3 tiltAxis = Vector3.Cross(localImpactDirection, Vector3.up);
            Quaternion baseRotation = initialVisualRotation;
            Quaternion hitTiltRotation = Quaternion.AngleAxis(hitTiltAngle, tiltAxis) * baseRotation;
            Quaternion reboundRotation = Quaternion.AngleAxis(-hitTiltAngle * 0.35f, tiltAxis) * baseRotation;

            destroySequence = DOTween.Sequence()
                .SetTarget(this)
                .Append(visualRoot.DOLocalRotate(hitTiltRotation.eulerAngles, hitTiltDuration, RotateMode.Fast).SetEase(Ease.OutQuad))
                .Append(visualRoot.DOLocalRotate(reboundRotation.eulerAngles, hitReboundDuration, RotateMode.Fast).SetEase(Ease.OutSine))
                .Append(visualRoot.DOScale(Vector3.zero, shrinkDuration).SetEase(Ease.InBack))
                .OnComplete(() =>
                {
                    destroySequence = null;
                    completed?.Invoke();
                });
        }

        private void OnDestroy()
        {
            destroySequence?.Kill();
            destroySequence = null;
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            visualRoot = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        private void CacheVisualTransform()
        {
            initialVisualScale = visualRoot.localScale;
            initialVisualRotation = visualRoot.localRotation;
        }

        private void ResetVisualTransform()
        {
            CacheVisualTransform();
            visualRoot.localScale = initialVisualScale;
            visualRoot.localRotation = initialVisualRotation;
        }

        public void BeginMove(Vector3 worldPosition)
        {
            targetWorldPosition = worldPosition;
            isMoving = true;
        }

        public void MoveStep(float maxDistance)
        {
            if (!isMoving)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetWorldPosition, maxDistance);

            if ((transform.position - targetWorldPosition).sqrMagnitude <= 0.0001f)
            {
                SnapToTarget();
            }
        }

        public void SnapToTarget()
        {
            transform.position = targetWorldPosition;
            isMoving = false;
        }

        public bool IsAtWorldPosition(Vector3 worldPosition)
        {
            return (transform.position - worldPosition).sqrMagnitude <= 0.0001f;
        }
    }
}
