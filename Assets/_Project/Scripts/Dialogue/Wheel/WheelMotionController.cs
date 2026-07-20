using UnityEngine;

namespace Glush.Dialogue
{
    /// <summary>
    /// Управляет автоматическим движением, ручным перетаскиванием,
    /// инерцией и магнитом барабана.
    /// </summary>
    public class WheelMotionController : MonoBehaviour
    {
        public enum MotionPhase
        {
            Idle,
            AutoMove,
            Dragging,
            Inertia,
            Snap
        }

        [Header("Motion Phases")]
        [SerializeField] private float _autoMoveSpeed = 1.5f;
        [SerializeField] private float _inertiaFriction = 0.95f;
        [SerializeField] private float _snapSpringStrength = 5f;
        [SerializeField] private float _velocityThreshold = 0.01f;
        [SerializeField] private float _centerTolerance = 0.02f;

        private MotionPhase _phase = MotionPhase.Idle;
        private float _velocity;
        private float _autoMoveTarget = 0.5f;
        private float _snapTarget = 0.5f;
        private float _pendingDragDelta;
        private bool _finishDragAfterUpdate;

        public MotionPhase Phase => _phase;
        public float Velocity => _velocity;
        public float VelocityThreshold => _velocityThreshold;
        public float CenterTolerance => _centerTolerance;
        public bool IsInMotion => _phase != MotionPhase.Idle;

        public void ApplyScrollImpulse(float deltaScroll)
        {
            _velocity = deltaScroll;
            _phase = MotionPhase.Inertia;
        }

        public void BeginDrag()
        {
            _pendingDragDelta = 0f;
            _finishDragAfterUpdate = false;
            _velocity = 0f;
            _phase = MotionPhase.Dragging;
        }

        public void ApplyDragDelta(float deltaPosition)
        {
            if (_phase != MotionPhase.Dragging)
            {
                BeginDrag();
            }

            _pendingDragDelta += deltaPosition;
            _velocity = 0f;
        }

        public void EndDrag()
        {
            if (_phase == MotionPhase.Dragging)
            {
                // ЛКМ двигает барабан напрямую: после отпускания он сразу останавливается.
                // Последний drag-delta всё равно будет применён в ближайшем UpdateMotion.
                _velocity = 0f;
                _finishDragAfterUpdate = true;
            }
        }

        public void StartAutoMove(float targetCenter)
        {
            _autoMoveTarget = targetCenter;
            ResetDragState();
            _velocity = 0f;
            _phase = MotionPhase.AutoMove;
        }

        public void StopAutoMove()
        {
            if (_phase == MotionPhase.AutoMove)
            {
                _phase = MotionPhase.Idle;
            }
        }

        public void StartSnap(float targetCenter)
        {
            _snapTarget = targetCenter;
            ResetDragState();
            _velocity = 0f;
            _phase = MotionPhase.Snap;
        }

        public float UpdateMotion(float currentWheelPosition)
        {
            float deltaTime = Time.unscaledDeltaTime;

            switch (_phase)
            {
                case MotionPhase.Idle:
                    return 0f;

                case MotionPhase.AutoMove:
                    return UpdateAutoMove(currentWheelPosition, deltaTime);

                case MotionPhase.Dragging:
                    return UpdateDragging();

                case MotionPhase.Inertia:
                    return UpdateInertia(deltaTime);

                case MotionPhase.Snap:
                    return UpdateSnap(currentWheelPosition, deltaTime);

                default:
                    return 0f;
            }
        }

        public void SetPhase(MotionPhase newPhase)
        {
            _phase = newPhase;
        }

        public void ZeroVelocity()
        {
            _velocity = 0f;
        }

        private float UpdateAutoMove(float currentWheelPosition, float deltaTime)
        {
            float nextPosition = Mathf.MoveTowards(
                currentWheelPosition,
                _autoMoveTarget,
                _autoMoveSpeed * deltaTime);

            if (Mathf.Abs(nextPosition - _autoMoveTarget) <= _centerTolerance)
            {
                nextPosition = _autoMoveTarget;
                _phase = MotionPhase.Idle;
            }

            return nextPosition - currentWheelPosition;
        }

        private float UpdateDragging()
        {
            float deltaPosition = _pendingDragDelta;
            _pendingDragDelta = 0f;

            if (_finishDragAfterUpdate)
            {
                _finishDragAfterUpdate = false;
                _phase = MotionPhase.Inertia;
            }

            return deltaPosition;
        }

        private float UpdateInertia(float deltaTime)
        {
            float deltaPosition = _velocity * deltaTime;
            _velocity *= Mathf.Pow(_inertiaFriction, deltaTime * 60f);

            // Переход в Snap выполняет внешний контроллер после задержки ввода.
            if (Mathf.Abs(_velocity) < _velocityThreshold)
            {
                _velocity = 0f;
            }

            return deltaPosition;
        }

        private float UpdateSnap(float currentWheelPosition, float deltaTime)
        {
            float distance = _snapTarget - currentWheelPosition;

            if (Mathf.Abs(distance) <= _centerTolerance)
            {
                _phase = MotionPhase.Idle;
                return distance;
            }

            float interpolation = 1f - Mathf.Exp(-_snapSpringStrength * deltaTime);
            return distance * interpolation;
        }

        private void ResetDragState()
        {
            _pendingDragDelta = 0f;
            _finishDragAfterUpdate = false;
        }

        private void OnValidate()
        {
            _autoMoveSpeed = Mathf.Max(0.01f, _autoMoveSpeed);
            _inertiaFriction = Mathf.Clamp(_inertiaFriction, 0.01f, 0.999f);
            _snapSpringStrength = Mathf.Max(0.01f, _snapSpringStrength);
            _velocityThreshold = Mathf.Max(0.0001f, _velocityThreshold);
            _centerTolerance = Mathf.Max(0.0001f, _centerTolerance);
        }
    }
}