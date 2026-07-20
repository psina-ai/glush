using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Glush.Dialogue
{
    /// <summary>
    /// Изолированный прототип физики Dialogue Wheel на тестовых строках.
    /// </summary>
    public class DialogueWheelPrototype : MonoBehaviour, IScrollHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public enum FollowMode
        {
            FollowingLive,
            Detached
        }

        [Header("Wheel Setup")]
        [SerializeField] private RectTransform _wheelContainer;
        [SerializeField] private TMP_Text _itemTemplate;
        [SerializeField] private WheelRuleView _ruleTemplate;

        [Header("Input & Scroll")]
        [SerializeField] private float _scrollSensitivity = 1f;
        [SerializeField] private float _dragPixelsPerItem = 90f;
        [SerializeField] private float _releaseDetectionTime = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _wheelClickClip;
        [SerializeField] private int _maxSimultaneousClicks = 6;

        [Header("Test Data")]
        [SerializeField] private float _newLineSpawnInterval = 3f;
        [SerializeField] private int _testLineCount = 12;

        private readonly List<string> _testLines = new()
        {
            "Первая строка диалога",
            "Вторая реплика персонажа",
            "Третья фраза в конце",
            "Четвёртая строка, подлиннее для теста",
            "Пятая важная фраза",
            "Шестая рефлексия героя",
            "Седьмая из двенадцати строк",
            "Восьмая часть тестовых данных",
            "Девятая линия диалога здесь",
            "Десятая строка почти конца",
            "Одиннадцатая и предпоследняя фраза",
            "Двенадцатая финальная строка"
        };

        private readonly List<WheelItemView> _itemViews = new();
        private readonly List<WheelRuleView> _ruleViews = new();
        private readonly Queue<float> _activeClickEndTimes = new();

        private WheelItemView _itemProjectionTemplate;

        private WheelMotionController _motionController;
        private Canvas _canvas;
        private bool _isDragging;
        private float _wheelPosition = 0.5f;
        private FollowMode _followMode = FollowMode.FollowingLive;
        private float _liveTargetCenter = 0.5f;
        private float _lastScrollTime;
        private float _nextSpawnTime;
        private int _spawnedLineCount;

        private int TargetLineCount => Mathf.Clamp(_testLineCount, 1, _testLines.Count);

        private void Awake()
        {
            if (_wheelContainer == null)
            {
                Debug.LogError(
                    "DialogueWheelPrototype: WheelContainer не назначен. Компонент отключён.",
                    this);
                enabled = false;
                return;
            }

            if (_itemTemplate == null || _ruleTemplate == null)
            {
                Debug.LogError(
                    "DialogueWheelPrototype: не назначен ItemTemplate или RuleTemplate. " +
                    "Компонент отключён.",
                    this);
                enabled = false;
                return;
            }

            _itemProjectionTemplate = _itemTemplate.GetComponent<WheelItemView>();
            if (_itemProjectionTemplate == null)
            {
                Debug.LogError(
                    "DialogueWheelPrototype: на ItemTemplate отсутствует WheelItemView.",
                    this);
                enabled = false;
                return;
            }

            // Шаблоны служат только источниками для копий и не попадают на экран.
            _itemTemplate.gameObject.SetActive(false);
            _ruleTemplate.gameObject.SetActive(false);

            _motionController = GetComponent<WheelMotionController>();
            if (_motionController == null)
            {
                _motionController = gameObject.AddComponent<WheelMotionController>();
            }

            _canvas = GetComponentInParent<Canvas>();
        }

        private void Start()
        {
            AddRule(0f, isDouble: true);
            AddNewLine(_testLines[0]);
            _nextSpawnTime = Time.unscaledTime + _newLineSpawnInterval;
        }

        private void Update()
        {
            UpdateWheelPosition();
            UpdateFollowMode();
            UpdateViews();
            TrySpawnNextLine();
            TryStartSnap();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_motionController == null || _isDragging)
            {
                return;
            }

            float normalizedScroll = Mathf.Clamp(eventData.scrollDelta.y, -1f, 1f);
            if (Mathf.Approximately(normalizedScroll, 0f))
            {
                return;
            }

            DetachFromLive();
            _motionController.ApplyScrollImpulse(
                -normalizedScroll * _scrollSensitivity);
            _lastScrollTime = Time.unscaledTime;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left ||
                _motionController == null)
            {
                return;
            }

            _isDragging = true;
            DetachFromLive();
            _motionController.BeginDrag();
            _lastScrollTime = Time.unscaledTime;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            float canvasScale = _canvas != null
                ? Mathf.Max(0.0001f, _canvas.scaleFactor)
                : 1f;
            float localDeltaY = eventData.delta.y / canvasScale;
            float wheelDelta = localDeltaY / Mathf.Max(1f, _dragPixelsPerItem);

            _motionController.ApplyDragDelta(wheelDelta);
            _lastScrollTime = Time.unscaledTime;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _isDragging = false;
            _motionController.EndDrag();
            _lastScrollTime = Time.unscaledTime;
        }

        private void DetachFromLive()
        {
            _followMode = FollowMode.Detached;
            _motionController.StopAutoMove();
        }

        private void UpdateWheelPosition()
        {
            float oldPosition = _wheelPosition;
            float motionDelta = _motionController.UpdateMotion(_wheelPosition);

            float requestedPosition = oldPosition + motionDelta;
            float clampedPosition = ClampWheelPosition(requestedPosition);

            if (!Mathf.Approximately(requestedPosition, clampedPosition))
            {
                // На физической границе не оставляем скорость, которая продолжит давить наружу.
                _motionController.ZeroVelocity();
            }

            DetectBoundaryCrossings(oldPosition, clampedPosition);
            _wheelPosition = clampedPosition;
        }

        private void UpdateFollowMode()
        {
            float distanceToLive = Mathf.Abs(_wheelPosition - _liveTargetCenter);

            if (_followMode == FollowMode.FollowingLive)
            {
                if (_motionController.Phase == WheelMotionController.MotionPhase.Idle &&
                    distanceToLive > _motionController.CenterTolerance)
                {
                    // Live target мог измениться, пока предыдущий AutoMove ещё завершался.
                    _motionController.StartAutoMove(_liveTargetCenter);
                }

                return;
            }

            if (_motionController.Phase == WheelMotionController.MotionPhase.Idle &&
                distanceToLive <= _motionController.CenterTolerance)
            {
                _followMode = FollowMode.FollowingLive;
            }
        }

        private void UpdateViews()
        {
            foreach (WheelItemView view in _itemViews)
            {
                view.UpdateProjection(_wheelPosition);
            }

            foreach (WheelRuleView rule in _ruleViews)
            {
                rule.UpdateProjection(_wheelPosition);
            }
        }

        private void TrySpawnNextLine()
        {
            if (Time.unscaledTime < _nextSpawnTime ||
                _spawnedLineCount >= TargetLineCount)
            {
                return;
            }

            int nextIndex = _spawnedLineCount;
            AddNewLine(_testLines[nextIndex]);
            _nextSpawnTime = Time.unscaledTime + _newLineSpawnInterval;

            if (_followMode == FollowMode.FollowingLive)
            {
                // Обновляет цель даже если предыдущий AutoMove ещё не успел закончиться.
                _motionController.StartAutoMove(_liveTargetCenter);
            }
        }

        private void TryStartSnap()
        {
            if (Time.unscaledTime - _lastScrollTime <= _releaseDetectionTime ||
                _motionController.Phase != WheelMotionController.MotionPhase.Inertia ||
                Mathf.Abs(_motionController.Velocity) >= _motionController.VelocityThreshold)
            {
                return;
            }

            _motionController.StartSnap(FindNearestCenter(_wheelPosition));
        }

        private void AddNewLine(string text)
        {
            TMP_Text newItem = Instantiate(_itemTemplate, _wheelContainer);
            newItem.text = text;

            WheelItemView view = newItem.GetComponent<WheelItemView>();
            if (view == null)
            {
                view = newItem.gameObject.AddComponent<WheelItemView>();
            }

            float centerPosition = _itemViews.Count + 0.5f;
            view.SetItemCenterPosition(centerPosition);
            _itemViews.Add(view);

            _spawnedLineCount++;
            _liveTargetCenter = centerPosition;

            // Копия наследует неактивное состояние шаблона, поэтому включаем только её.
            newItem.gameObject.SetActive(true);
            view.UpdateProjection(_wheelPosition);

            bool isBlockEnd = _spawnedLineCount >= TargetLineCount;
            AddRule(_itemViews.Count, isBlockEnd);
        }

        private void AddRule(float boundaryPosition, bool isDouble)
        {
            WheelRuleView rule = Instantiate(_ruleTemplate, _wheelContainer);
            rule.CopyProjectionSettingsFrom(_itemProjectionTemplate);
            rule.Configure(boundaryPosition, isDouble);
            _ruleViews.Add(rule);

            rule.gameObject.SetActive(true);
            rule.UpdateProjection(_wheelPosition);
        }

        private void DetectBoundaryCrossings(float oldPosition, float newPosition)
        {
            if (newPosition > oldPosition)
            {
                int firstBoundary = Mathf.FloorToInt(oldPosition) + 1;
                int lastBoundary = Mathf.FloorToInt(newPosition);

                for (int boundary = firstBoundary; boundary <= lastBoundary; boundary++)
                {
                    PlayWheelClick();
                }
            }
            else if (newPosition < oldPosition)
            {
                int firstBoundary = Mathf.CeilToInt(oldPosition) - 1;
                int lastBoundary = Mathf.CeilToInt(newPosition);

                for (int boundary = firstBoundary; boundary >= lastBoundary; boundary--)
                {
                    PlayWheelClick();
                }
            }
        }

        private void PlayWheelClick()
        {
            if (_audioSource == null || _wheelClickClip == null)
            {
                return;
            }

            float now = Time.unscaledTime;
            while (_activeClickEndTimes.Count > 0 &&
                   _activeClickEndTimes.Peek() <= now)
            {
                _activeClickEndTimes.Dequeue();
            }

            if (_activeClickEndTimes.Count >= _maxSimultaneousClicks)
            {
                return;
            }

            _audioSource.PlayOneShot(_wheelClickClip);

            float effectivePitch = Mathf.Max(0.01f, Mathf.Abs(_audioSource.pitch));
            float clickDuration = _wheelClickClip.length / effectivePitch;
            _activeClickEndTimes.Enqueue(now + clickDuration);
        }

        private static float FindNearestCenter(float position)
        {
            return Mathf.Floor(position) + 0.5f;
        }

        private float ClampWheelPosition(float position)
        {
            if (_itemViews.Count == 0)
            {
                return 0.5f;
            }

            float minPosition = 0.5f;
            float maxPosition = _itemViews.Count - 0.5f;
            return Mathf.Clamp(position, minPosition, maxPosition);
        }

        private void OnDisable()
        {
            _isDragging = false;
            _activeClickEndTimes.Clear();

            if (_motionController != null)
            {
                _motionController.ZeroVelocity();
                _motionController.SetPhase(WheelMotionController.MotionPhase.Idle);
            }
        }

        private void OnValidate()
        {
            _scrollSensitivity = Mathf.Max(0.01f, _scrollSensitivity);
            _dragPixelsPerItem = Mathf.Max(1f, _dragPixelsPerItem);
            _releaseDetectionTime = Mathf.Max(0f, _releaseDetectionTime);
            _maxSimultaneousClicks = Mathf.Max(1, _maxSimultaneousClicks);
            _newLineSpawnInterval = Mathf.Max(0.01f, _newLineSpawnInterval);
            _testLineCount = Mathf.Max(1, _testLineCount);
        }
    }
}