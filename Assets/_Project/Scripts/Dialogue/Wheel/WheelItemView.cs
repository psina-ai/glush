using UnityEngine;

namespace Glush.Dialogue
{
    /// <summary>
    /// Отображает один элемент на передней поверхности виртуального барабана.
    /// </summary>
    public class WheelItemView : MonoBehaviour
    {
        [Header("Projection Settings")]
        [SerializeField] private float _angleStep = 36f;
        [SerializeField] private float _cylinderRadius = 100f;
        [SerializeField] private float _edgeHorizontalScale = 0.85f;
        [SerializeField] private AnimationCurve _alphaByDepth =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private float _alphaDepthPower = 1.5f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private bool _isInitialized;
        private float _itemCenterPosition;
        private float _visibilityMultiplier = 1f;

        private void OnEnable()
        {
            EnsureInitialized();
        }

        public void SetItemCenterPosition(float centerPosition)
        {
            _itemCenterPosition = centerPosition;
        }

        public void SetVisibility(float visibility)
        {
            _visibilityMultiplier = Mathf.Clamp01(visibility);
        }

        public void CopyProjectionSettingsFrom(WheelItemView source)
        {
            if (source == null)
            {
                return;
            }

            _angleStep = source._angleStep;
            _cylinderRadius = source._cylinderRadius;
            _edgeHorizontalScale = source._edgeHorizontalScale;
            _alphaDepthPower = source._alphaDepthPower;
            _alphaByDepth = source._alphaByDepth != null
                ? new AnimationCurve(source._alphaByDepth.keys)
                : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }

        public void UpdateProjection(float wheelPosition)
        {
            EnsureInitialized();

            float angleDegrees =
                (_itemCenterPosition - wheelPosition) * _angleStep;

            // Список линейный: элементы за передней полуокружностью не должны
            // появляться снова после полного оборота синуса и косинуса.
            if (Mathf.Abs(angleDegrees) >= 90f)
            {
                _canvasGroup.alpha = 0f;
                return;
            }

            float angle = angleDegrees * Mathf.Deg2Rad;
            float normalizedDepth = Mathf.Clamp01(Mathf.Cos(angle));
            // Новые строки входят снизу, а прочитанные уходят вверх.
            float yOffset = -Mathf.Sin(angle) * _cylinderRadius;
            float scaleY = normalizedDepth;
            float scaleX = Mathf.Lerp(
                _edgeHorizontalScale,
                1f,
                normalizedDepth);

            // Глубина 0 соответствует прозрачному краю, глубина 1 — видимому центру.
            float alphaInput = Mathf.Pow(
                normalizedDepth,
                Mathf.Max(0.01f, _alphaDepthPower));
            float alpha = _alphaByDepth != null
                ? Mathf.Clamp01(_alphaByDepth.Evaluate(alphaInput))
                : alphaInput;

            _rectTransform.localScale = new Vector3(scaleX, scaleY, 1f);
            _rectTransform.anchoredPosition = new Vector2(
                _rectTransform.anchoredPosition.x,
                yOffset);
            _canvasGroup.alpha = alpha * _visibilityMultiplier;
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _isInitialized = true;
        }

        private void OnValidate()
        {
            _angleStep = Mathf.Max(0.01f, _angleStep);
            _cylinderRadius = Mathf.Max(0f, _cylinderRadius);
            _edgeHorizontalScale = Mathf.Clamp01(_edgeHorizontalScale);
            _alphaDepthPower = Mathf.Max(0.01f, _alphaDepthPower);

            if (_alphaByDepth == null || _alphaByDepth.length == 0)
            {
                _alphaByDepth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            }
        }
    }
}
