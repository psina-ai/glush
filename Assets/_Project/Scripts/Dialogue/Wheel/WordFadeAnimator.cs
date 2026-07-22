using System.Collections;
using TMPro;
using UnityEngine;

namespace Glush.Dialogue
{
    /// <summary>
    /// Проявляет уже полностью свёрстанный TMP-текст по одному слову.
    /// Геометрия строки не меняется во время анимации.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class WordFadeAnimator : MonoBehaviour
    {
        private TMP_Text _text;
        private bool _isRevealing;
        private bool _completeRequested;

        public bool IsRevealing => _isRevealing;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void PrepareHidden()
        {
            EnsureText();
            _text.ForceMeshUpdate();

            SetAllVisibleCharactersAlpha(_text.textInfo, 0);
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        public void CompleteImmediately()
        {
            if (_isRevealing)
            {
                _completeRequested = true;
            }
        }

        public IEnumerator Reveal(float wordFadeDuration, float delayBetweenWords)
        {
            PrepareHidden();
            TMP_TextInfo textInfo = _text.textInfo;
            _isRevealing = true;
            _completeRequested = false;

            if (textInfo.wordCount == 0)
            {
                CompleteAll(textInfo);
                yield break;
            }

            for (int wordIndex = 0; wordIndex < textInfo.wordCount; wordIndex++)
            {
                yield return FadeWord(
                    textInfo,
                    wordIndex,
                    Mathf.Max(0f, wordFadeDuration));

                if (_completeRequested)
                {
                    break;
                }

                if (delayBetweenWords > 0f && wordIndex < textInfo.wordCount - 1)
                {
                    float elapsed = 0f;
                    while (elapsed < delayBetweenWords && !_completeRequested)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }

            CompleteAll(textInfo);
        }

        private IEnumerator FadeWord(
            TMP_TextInfo textInfo,
            int wordIndex,
            float duration)
        {
            if (duration <= 0f)
            {
                SetWordAlpha(textInfo, wordIndex, 255);
                _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration && !_completeRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                byte alpha = (byte)Mathf.RoundToInt(
                    Mathf.Clamp01(elapsed / duration) * 255f);

                SetWordAlpha(textInfo, wordIndex, alpha);
                _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                yield return null;
            }

            SetWordAlpha(textInfo, wordIndex, 255);
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void CompleteAll(TMP_TextInfo textInfo)
        {
            SetAllVisibleCharactersAlpha(textInfo, 255);
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            _completeRequested = false;
            _isRevealing = false;
        }

        private void OnDisable()
        {
            _completeRequested = false;
            _isRevealing = false;
        }

        private static void SetAllVisibleCharactersAlpha(
            TMP_TextInfo textInfo,
            byte alpha)
        {
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                SetCharacterAlpha(textInfo, character, alpha);
            }
        }

        private static void SetWordAlpha(
            TMP_TextInfo textInfo,
            int wordIndex,
            byte alpha)
        {
            int firstCharacter = wordIndex == 0
                ? 0
                : textInfo.wordInfo[wordIndex].firstCharacterIndex;
            int lastCharacterExclusive = wordIndex < textInfo.wordCount - 1
                ? textInfo.wordInfo[wordIndex + 1].firstCharacterIndex
                : textInfo.characterCount;

            // Вместе со словом проявляем следующую за ним пунктуацию.
            for (int i = firstCharacter; i < lastCharacterExclusive; i++)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];
                if (!character.isVisible)
                {
                    continue;
                }

                SetCharacterAlpha(textInfo, character, alpha);
            }
        }

        private static void SetCharacterAlpha(
            TMP_TextInfo textInfo,
            TMP_CharacterInfo character,
            byte alpha)
        {
            Color32[] colors = textInfo.meshInfo[character.materialReferenceIndex].colors32;
            int vertexIndex = character.vertexIndex;

            colors[vertexIndex].a = alpha;
            colors[vertexIndex + 1].a = alpha;
            colors[vertexIndex + 2].a = alpha;
            colors[vertexIndex + 3].a = alpha;
        }

        private void EnsureText()
        {
            if (_text == null)
            {
                _text = GetComponent<TMP_Text>();
            }
        }
    }
}
