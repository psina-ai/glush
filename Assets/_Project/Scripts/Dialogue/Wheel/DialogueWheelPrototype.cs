using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

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

        [System.Serializable]
        private struct TestBlockSettings
        {
            [SerializeField] private int _lineCount;
            [SerializeField] private float _pauseAfter;
            [SerializeField] private int _gapCount;

            public int LineCount => Mathf.Max(1, _lineCount);
            public float PauseAfter => Mathf.Max(0f, _pauseAfter);
            public int GapCount => Mathf.Max(0, _gapCount);

            public TestBlockSettings(int lineCount, float pauseAfter, int gapCount)
            {
                _lineCount = lineCount;
                _pauseAfter = pauseAfter;
                _gapCount = gapCount;
            }
        }

        [Header("Wheel Setup")]
        [SerializeField] private RectTransform _wheelContainer;
        [SerializeField] private TMP_Text _itemTemplate;
        [SerializeField] private WheelRuleView _ruleTemplate;

        [Header("Input & Scroll")]
        [SerializeField] private float _scrollSensitivity = 1f;
        [SerializeField] private float _dragPixelsPerItem = 90f;
        [SerializeField] private float _releaseDetectionTime = 0.5f;
        [SerializeField] private TMP_Text _advanceIndicator;
        [SerializeField] private string _advanceIndicatorText = "Нажмите Пробел";

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _wheelClickClip;
        [SerializeField] private AudioClip _textEndClickClip;
        [SerializeField] private int _maxSimultaneousClicks = 6;

        [Header("Test Presentation")]
        [SerializeField] private float _wordFadeDuration = 0.12f;
        [SerializeField] private float _delayBetweenWords = 0.04f;
        [SerializeField] private float _ruleFadeDuration = 0.2f;
        [SerializeField] private float _delayBetweenLines = 0.15f;
        [SerializeField] private float _spaceAutoMoveSpeed = 4.5f;
        [SerializeField] private bool _lockHistoryUntilBlockComplete;
        [SerializeField] private TestBlockSettings[] _testBlocks =
        {
            new TestBlockSettings(3, 0f, 3),
            new TestBlockSettings(4, 1.5f, 2),
            new TestBlockSettings(5, 0f, 3)
        };

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
        private InputAction _advanceAction;
        private WordFadeAnimator _activeWordFade;
        private bool _isDragging;
        private bool _isWritingBlock;
        private bool _isWaitingForAdvance;
        private bool _advanceRequested;
        private bool _carryFastTransitionIntoNextLine;
        private float _wheelPosition = 0.5f;
        private FollowMode _followMode = FollowMode.FollowingLive;
        private float _liveTargetCenter = 0.5f;
        private float _lastScrollTime;

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
            _advanceAction = new InputAction(
                "DialogueWheelAdvance",
                InputActionType.Button,
                "<Keyboard>/space");

            if (_advanceIndicator != null)
            {
                _advanceIndicator.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (_advanceAction == null)
            {
                return;
            }

            _advanceAction.performed += HandleAdvancePerformed;
            _advanceAction.Enable();
        }

        private void Start()
        {
            StartCoroutine(RunTestPresentation());
        }

        private void Update()
        {
            UpdateWheelPosition();
            UpdateFollowMode();
            UpdateViews();
            TryStartSnap();
            UpdateAdvanceIndicator();
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (_motionController == null || _isDragging || !CanBrowseHistoryNow())
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
                _motionController == null ||
                !CanBrowseHistoryNow())
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
                eventData.button != PointerEventData.InputButton.Left ||
                !CanBrowseHistoryNow())
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

        private void HandleAdvancePerformed(InputAction.CallbackContext context)
        {
            if (_motionController == null)
            {
                return;
            }

            bool isAtLivePosition =
                Mathf.Abs(_wheelPosition - _liveTargetCenter) <=
                _motionController.CenterTolerance;

            // На нижней границе колесо может уже стоять физически, но ещё числиться
            // в Inertia/Snap. Такое состояние не должно съедать первое нажатие.
            if (!isAtLivePosition)
            {
                _followMode = FollowMode.FollowingLive;
                _motionController.StartAutoMove(
                    _liveTargetCenter,
                    _spaceAutoMoveSpeed);
                return;
            }

            _isDragging = false;
            _followMode = FollowMode.FollowingLive;
            _motionController.ZeroVelocity();
            _motionController.SetPhase(WheelMotionController.MotionPhase.Idle);

            if (_activeWordFade != null && _activeWordFade.IsRevealing)
            {
                _activeWordFade.CompleteImmediately();
                return;
            }

            if (_isWaitingForAdvance)
            {
                _advanceRequested = true;
            }
        }

        private void UpdateAdvanceIndicator()
        {
            if (_advanceIndicator == null || _motionController == null)
            {
                return;
            }

            bool isAtLivePosition =
                Mathf.Abs(_wheelPosition - _liveTargetCenter) <=
                _motionController.CenterTolerance;
            bool shouldShow = _isWaitingForAdvance && isAtLivePosition;

            if (shouldShow)
            {
                _advanceIndicator.text = _advanceIndicatorText;
            }

            if (_advanceIndicator.gameObject.activeSelf != shouldShow)
            {
                _advanceIndicator.gameObject.SetActive(shouldShow);
            }
        }

        private bool CanBrowseHistoryNow()
        {
            return !_lockHistoryUntilBlockComplete || !_isWritingBlock;
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

            DetectCenterCrossings(oldPosition, clampedPosition);
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

        private IEnumerator RunTestPresentation()
        {
            int lineCursor = 0;

            if (_testBlocks != null)
            {
                foreach (TestBlockSettings block in _testBlocks)
                {
                    if (lineCursor >= _testLines.Count)
                    {
                        yield break;
                    }

                    int lineCount = Mathf.Min(
                        block.LineCount,
                        _testLines.Count - lineCursor);

                    yield return RunTestBlock(lineCursor, lineCount);
                    lineCursor += lineCount;
                    yield return RunBlockTransition(
                        block,
                        lineCursor < _testLines.Count);
                }
            }

            if (lineCursor < _testLines.Count)
            {
                TestBlockSettings fallback = new TestBlockSettings(
                    _testLines.Count - lineCursor,
                    0f,
                    3);

                yield return RunTestBlock(
                    lineCursor,
                    _testLines.Count - lineCursor);
                yield return RunBlockTransition(fallback, hasMoreContent: false);
            }
        }

        private IEnumerator RunTestBlock(int startLine, int lineCount)
        {
            _isWritingBlock = true;

            if (_lockHistoryUntilBlockComplete)
            {
                _isDragging = false;
                _followMode = FollowMode.FollowingLive;
            }

            WheelRuleView startRule = AddRule(_itemViews.Count, isDouble: true);
            StartCoroutine(startRule.FadeIn(_ruleFadeDuration));

            for (int localIndex = 0; localIndex < lineCount; localIndex++)
            {
                if (localIndex > 0)
                {
                    WheelRuleView separator = AddRule(
                        _itemViews.Count,
                        isDouble: false);
                    StartCoroutine(separator.FadeIn(_ruleFadeDuration));
                }

                WordFadeAnimator wordFade = AddNewLine(
                    _testLines[startLine + localIndex]);

                bool useFastTransition =
                    localIndex == 0 && _carryFastTransitionIntoNextLine;
                _carryFastTransitionIntoNextLine = false;

                if (_followMode == FollowMode.FollowingLive)
                {
                    if (useFastTransition)
                    {
                        _motionController.StartAutoMove(
                            _liveTargetCenter,
                            _spaceAutoMoveSpeed);
                    }
                    else
                    {
                        _motionController.StartAutoMove(_liveTargetCenter);
                    }

                    yield return WaitForLiveCenterOrDetach();
                }

                _activeWordFade = wordFade;
                yield return wordFade.Reveal(
                    _wordFadeDuration,
                    _delayBetweenWords);
                _activeWordFade = null;

                if (_delayBetweenLines > 0f && localIndex < lineCount - 1)
                {
                    yield return new WaitForSecondsRealtime(_delayBetweenLines);
                }
            }

            WheelRuleView endRule = AddRule(_itemViews.Count, isDouble: true);
            yield return endRule.FadeIn(_ruleFadeDuration);
            PlayTextEndClick();
            _isWritingBlock = false;
        }

        private IEnumerator RunBlockTransition(
            TestBlockSettings block,
            bool hasMoreContent)
        {
            if (block.PauseAfter > 0f)
            {
                yield return new WaitForSecondsRealtime(block.PauseAfter);
            }

            _isWaitingForAdvance = true;
            _advanceRequested = false;

            while (!_advanceRequested)
            {
                yield return null;
            }

            _isWaitingForAdvance = false;
            _advanceRequested = false;

            for (int i = 0; i < block.GapCount; i++)
            {
                AddSpacer();
            }

            if (_followMode == FollowMode.FollowingLive)
            {
                _carryFastTransitionIntoNextLine = hasMoreContent;

                if (block.GapCount > 0)
                {
                    _motionController.StartAutoMove(
                        _liveTargetCenter,
                        _spaceAutoMoveSpeed);

                    if (!hasMoreContent)
                    {
                        yield return WaitForLiveCenterOrDetach();
                    }
                }
            }
        }

        private IEnumerator WaitForLiveCenterOrDetach()
        {
            while (_followMode == FollowMode.FollowingLive &&
                   Mathf.Abs(_wheelPosition - _liveTargetCenter) >
                   _motionController.CenterTolerance)
            {
                yield return null;
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

        private WordFadeAnimator AddNewLine(string text)
        {
            TMP_Text newItem = Instantiate(_itemTemplate, _wheelContainer);
            newItem.text = text;

            WheelItemView view = newItem.GetComponent<WheelItemView>();
            if (view == null)
            {
                view = newItem.gameObject.AddComponent<WheelItemView>();
            }

            WordFadeAnimator wordFade = newItem.GetComponent<WordFadeAnimator>();
            if (wordFade == null)
            {
                wordFade = newItem.gameObject.AddComponent<WordFadeAnimator>();
            }

            float centerPosition = _itemViews.Count + 0.5f;
            view.SetItemCenterPosition(centerPosition);
            _itemViews.Add(view);

            _liveTargetCenter = centerPosition;

            // Копия наследует неактивное состояние шаблона, поэтому включаем только её.
            newItem.gameObject.SetActive(true);
            view.UpdateProjection(_wheelPosition);
            wordFade.PrepareHidden();

            return wordFade;
        }

        private void AddSpacer()
        {
            TMP_Text spacerItem = Instantiate(_itemTemplate, _wheelContainer);
            spacerItem.text = string.Empty;

            WheelItemView view = spacerItem.GetComponent<WheelItemView>();
            if (view == null)
            {
                view = spacerItem.gameObject.AddComponent<WheelItemView>();
            }

            float centerPosition = _itemViews.Count + 0.5f;
            view.SetItemCenterPosition(centerPosition);
            _itemViews.Add(view);
            _liveTargetCenter = centerPosition;

            spacerItem.gameObject.SetActive(true);
            view.UpdateProjection(_wheelPosition);
        }

        private WheelRuleView AddRule(float boundaryPosition, bool isDouble)
        {
            WheelRuleView rule = Instantiate(_ruleTemplate, _wheelContainer);
            rule.CopyProjectionSettingsFrom(_itemProjectionTemplate);
            rule.Configure(boundaryPosition, isDouble);
            _ruleViews.Add(rule);

            rule.gameObject.SetActive(true);
            rule.UpdateProjection(_wheelPosition);
            return rule;
        }

        private void DetectCenterCrossings(float oldPosition, float newPosition)
        {
            // После сдвига на 0.5 центры 0.5, 1.5, 2.5 превращаются в целые числа.
            float oldShifted = oldPosition - 0.5f;
            float newShifted = newPosition - 0.5f;

            if (newShifted > oldShifted)
            {
                int firstCenter = Mathf.FloorToInt(oldShifted) + 1;
                int lastCenter = Mathf.FloorToInt(newShifted);

                for (int center = firstCenter; center <= lastCenter; center++)
                {
                    PlayWheelClick();
                }
            }
            else if (newShifted < oldShifted)
            {
                int firstCenter = Mathf.CeilToInt(oldShifted) - 1;
                int lastCenter = Mathf.CeilToInt(newShifted);

                for (int center = firstCenter; center >= lastCenter; center--)
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

        private void PlayTextEndClick()
        {
            if (_audioSource != null && _textEndClickClip != null)
            {
                _audioSource.PlayOneShot(_textEndClickClip);
            }
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
            _isWritingBlock = false;
            _isWaitingForAdvance = false;
            _advanceRequested = false;
            _activeWordFade = null;
            _activeClickEndTimes.Clear();

            if (_advanceAction != null)
            {
                _advanceAction.performed -= HandleAdvancePerformed;
                _advanceAction.Disable();
            }

            if (_advanceIndicator != null)
            {
                _advanceIndicator.gameObject.SetActive(false);
            }

            if (_motionController != null)
            {
                _motionController.ZeroVelocity();
                _motionController.SetPhase(WheelMotionController.MotionPhase.Idle);
            }
        }

        private void OnDestroy()
        {
            _advanceAction?.Dispose();
        }

        private void OnValidate()
        {
            _scrollSensitivity = Mathf.Max(0.01f, _scrollSensitivity);
            _dragPixelsPerItem = Mathf.Max(1f, _dragPixelsPerItem);
            _releaseDetectionTime = Mathf.Max(0f, _releaseDetectionTime);
            _maxSimultaneousClicks = Mathf.Max(1, _maxSimultaneousClicks);
            _wordFadeDuration = Mathf.Max(0f, _wordFadeDuration);
            _delayBetweenWords = Mathf.Max(0f, _delayBetweenWords);
            _ruleFadeDuration = Mathf.Max(0f, _ruleFadeDuration);
            _delayBetweenLines = Mathf.Max(0f, _delayBetweenLines);
            _spaceAutoMoveSpeed = Mathf.Max(0.01f, _spaceAutoMoveSpeed);
        }
    }
}