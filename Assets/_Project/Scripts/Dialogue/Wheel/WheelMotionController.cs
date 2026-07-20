using UnityEngine;

namespace Glush.Dialogue
{
    /// <summary>
    /// Контроллер физических фаз движения барабана.
    /// Управляет Idle, AutoMove, Inertia, Snap на основе wheelPosition.
    /// </summary>
    public class WheelMotionController : MonoBehaviour
    {
        public enum MotionPhase
        {
            Idle,
            AutoMove,
            Inertia,
            Snap
        }

        [Header("Motion Phases")]
        [SerializeField] private float _autoMoveSpeed = 1.5f;      // единиц в секунду к целевому центру
        [SerializeField] private float _inertiaFriction = 0.95f;   // коэффициент сохранения скорости на 60 FPS; применять через Mathf.Pow для независимости от FPS
        [SerializeField] private float _snapSpringStrength = 15f;  // жёсткость пружины для snap
        [SerializeField] private float _velocityThreshold = 0.01f; // минимальная скорость перед snap
        [SerializeField] private float _centerTolerance = 0.02f;   // расстояние до центра считается "достигнут"

        private MotionPhase _phase = MotionPhase.Idle;
        private float _velocity = 0f;
        private float _autoMoveTarget = 0.5f;  // целевой центр при AutoMove
        private float _snapTarget = 0.5f;      // целевой центр при Snap

        public MotionPhase Phase => _phase;
        public float Velocity => _velocity;
        public float CenterTolerance => _centerTolerance;

        /// <summary>
        /// Инициировать пользовательский scroll-импульс (немедленно -> Inertia).
        /// </summary>
        public void ApplyScrollImpulse(float deltaScroll)
        {
            _velocity = deltaScroll;
            _phase = MotionPhase.Inertia;
        }

        /// <summary>
        /// Начать AutoMove к новому live target.
        /// </summary>
        public void StartAutoMove(float targetCenter)
        {
            _autoMoveTarget = targetCenter;
            _phase = MotionPhase.AutoMove;
            _velocity = 0f;
        }

        /// <summary>
        /// Прекратить AutoMove (обычно из-за пользовательского scroll).
        /// </summary>
        public void StopAutoMove()
        {
            if (_phase == MotionPhase.AutoMove)
                _phase = MotionPhase.Idle;
        }

        /// <summary>
        /// Начать Snap к ближайшему N+0.5.
        /// </summary>
        public void StartSnap(float snapTargetCenter)
        {
            _snapTarget = snapTargetCenter;
            _phase = MotionPhase.Snap;
            _velocity = 0f;
        }

        /// <summary>
        /// Обновить фазу движения, вернуть дельту для wheelPosition.
        /// </summary>
        public float UpdateMotion(float currentWheelPosition, int itemCount)
        {
            float deltaPosition = 0f;

            switch (_phase)
            {
                case MotionPhase.Idle:
                    // Нет движения
                    break;

                case MotionPhase.AutoMove:
                    // Движение к целевому центру
                    float distance = _autoMoveTarget - currentWheelPosition;
                    
                    // Защита от "оборота" через границы
                    if (Mathf.Abs(distance) > itemCount * 0.5f)
                    {
                        if (distance > 0)
                            distance -= itemCount;
                        else
                            distance += itemCount;
                    }

                    if (Mathf.Abs(distance) <= _centerTolerance)
                    {
                        deltaPosition = 0f;
                        _phase = MotionPhase.Idle;
                    }
                    else
                    {
                        deltaPosition = Mathf.Sign(distance) * _autoMoveSpeed * Time.unscaledDeltaTime;
                    }
                    break;

                case MotionPhase.Inertia:
                    // Движение с затуханием, скорректировано для независимости от FPS
                    deltaPosition = _velocity * Time.unscaledDeltaTime;
                    
                    // Применить затухание через Pow для FPS-независимости:
                    // inertiaFriction 0.95 = сохранение 95% скорости за 1 кадр на 60 FPS
                    _velocity *= Mathf.Pow(_inertiaFriction, Time.unscaledDeltaTime * 60f);

                    if (Mathf.Abs(_velocity) < _velocityThreshold)
                    {
                        // Инерция закончилась, переходим в Snap
                        float nearest = FindNearestCenter(currentWheelPosition + deltaPosition);
                        StartSnap(nearest);
                        _velocity = 0f;
                    }
                    break;

                case MotionPhase.Snap:
                    // Пружина к целевому центру
                    float snapDistance = _snapTarget - currentWheelPosition;

                    // Защита от оборота
                    if (Mathf.Abs(snapDistance) > itemCount * 0.5f)
                    {
                        if (snapDistance > 0)
                            snapDistance -= itemCount;
                        else
                            snapDistance += itemCount;
                    }

                    if (Mathf.Abs(snapDistance) <= _centerTolerance)
                    {
                        // Точно установить целевой центр и завершить
                        deltaPosition = _snapTarget - currentWheelPosition;
                        _velocity = 0f;
                        _phase = MotionPhase.Idle;
                    }
                    else
                    {
                        // Пружинный закон: применить силу и вычислить смещение
                        float springForce = snapDistance * _snapSpringStrength;
                        deltaPosition = springForce * Time.unscaledDeltaTime;
                    }
                    break;
            }

            return deltaPosition;
        }

        /// <summary>
        /// Найти ближайший центр N+0.5 к текущей позиции.
        /// </summary>
        private float FindNearestCenter(float position)
        {
            float floor = Mathf.Floor(position);
            float center1 = floor + 0.5f;
            float center2 = center1 + 1f;

            return Mathf.Abs(position - center1) < Mathf.Abs(position - center2)
                ? center1
                : center2;
        }

        public bool IsInMotion => _phase != MotionPhase.Idle;

        public void SetPhase(MotionPhase newPhase) => _phase = newPhase;

        /// <summary>
        /// Обнулить скорость (используется при зажатии wheelPosition границей).
        /// </summary>
        public void ZeroVelocity() => _velocity = 0f;
    }
}
