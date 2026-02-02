using TMPro;
using UnityEngine;

namespace PixelSurvival
{
    public class DamageText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro _text;

        private float Duration = 0.6f;
        private float RiseSpeed = 1.5f;
        private float _time;
        
        private bool _playing;
        
        private void Awake()
        {
            _text = GetComponent<TextMeshPro>();
            if (!_text) _text = gameObject.AddComponent<TextMeshPro>();
            _text.alignment = TextAlignmentOptions.Center;
            _text.sortingOrder = 1000;
        }

        public void Play(int amount, Vector3 worldPos, Color color)
        {
            transform.position = worldPos;

            _text.text = amount.ToString();
            _text.color = color;

            _time = 0f;
            _playing = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_playing) return;

            _time += Time.deltaTime;
            float progress = Mathf.Clamp01(_time / Duration);

            transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

            var color = _text.color;
            color.a = 1f - progress;
            _text.color = color;

            if (_time >= Duration)
            {
                _playing = false;
                DamageTextPool.Instance.Despawn(this);
            }
        }
    }
}