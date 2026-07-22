using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Glush.Dialogue
{
    /// <summary>
    /// Отображает одинарную или двойную границу между элементами барабана.
    /// Граница не занимает отдельное деление и не влияет на физику.
    /// </summary>
    [RequireComponent(typeof(WheelItemView))]
    public class WheelRuleView : MonoBehaviour
    {
        [Header("Rule Parts")]
        [SerializeField] private Image _firstLine;
        [SerializeField] private Image _secondLine;

        [Header("Rule Appearance")]
        [SerializeField] private float _lineThickness = 1f;
        [SerializeField] private float _doubleLineSpacing = 6f;

        private WheelItemView _projection;
        private bool _isInitialized;

        public void CopyProjectionSettingsFrom(WheelItemView source)
        {
            EnsureInitialized();
            if (!_isInitialized || source == null)
            {
                return;
            }

            _projection.CopyProjectionSettingsFrom(source);
        }

        public void Configure(float boundaryPosition, bool isDouble)
        {
            EnsureInitialized();
            if (!_isInitialized)
            {
                return;
            }

            _projection.SetItemCenterPosition(boundaryPosition);
            _projection.SetVisibility(0f);

            ConfigureLine(_firstLine.rectTransform, isDouble
                ? _doubleLineSpacing * 0.5f
                : 0f);

            _secondLine.gameObject.SetActive(isDouble);
            if (isDouble)
            {
                ConfigureLine(
                    _secondLine.rectTransform,
                    -_doubleLineSpacing * 0.5f);
            }
        }

        public void UpdateProjection(float wheelPosition)
        {
            EnsureInitialized();
            if (_isInitialized)
            {
                _projection.UpdateProjection(wheelPosition);
            }
        }

        public IEnumerator FadeIn(float duration)
        {
            EnsureInitialized();
            if (!_isInitialized)
            {
                yield break;
            }

            if (duration <= 0f)
            {
                _projection.SetVisibility(1f);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _projection.SetVisibility(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            _projection.SetVisibility(1f);
        }

        private void OnEnable()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _projection = GetComponent<WheelItemView>();

            if (_firstLine == null || _secondLine == null)
            {
                Debug.LogError(
                    "WheelRuleView: не назначены обе Image-линии.",
                    this);
                enabled = false;
                return;
            }

            _isInitialized = true;
        }

        private void ConfigureLine(RectTransform line, float yPosition)
        {
            line.anchorMin = new Vector2(0f, 0.5f);
            line.anchorMax = new Vector2(1f, 0.5f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = new Vector2(0f, yPosition);
            line.sizeDelta = new Vector2(0f, _lineThickness);
        }

        private void OnValidate()
        {
            _lineThickness = Mathf.Max(0.5f, _lineThickness);
            _doubleLineSpacing = Mathf.Max(_lineThickness, _doubleLineSpacing);
        }
    }
}
