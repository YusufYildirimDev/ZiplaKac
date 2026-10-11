using UnityEngine;
using UnityEngine.UI;

namespace ZiplaKac.UI
{
    /// <summary>
    /// Her 5 engelde bir ekranda belirip yukarı doğru süzülerek solan "Kombo xN! +10" floating metin bileşeni.
    /// </summary>
    public class FloatingText : MonoBehaviour
    {
        public Text textComponent;
        public float floatSpeed = 2.0f;
        public float fadeDuration = 1.0f;
        public string poolTag = "FloatingText";

        private float timer = 0f;
        private Color initialColor;

        private void OnEnable()
        {
            timer = 0f;
            if (textComponent == null) textComponent = GetComponentInChildren<Text>();
            if (textComponent != null) initialColor = textComponent.color;
        }

        public void SetText(string message, Color color)
        {
            if (textComponent == null) textComponent = GetComponentInChildren<Text>();
            if (textComponent != null)
            {
                textComponent.text = message;
                textComponent.color = color;
                initialColor = color;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // Yukarı süzülme
            transform.Translate(Vector3.up * floatSpeed * Time.deltaTime, Space.World);

            // Solma (Fade out)
            if (textComponent != null)
            {
                float alpha = Mathf.Lerp(1.0f, 0f, timer / fadeDuration);
                textComponent.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);
            }

            if (timer >= fadeDuration)
            {
                RecycleToPool();
            }
        }

        private void RecycleToPool()
        {
            if (Core.ObjectPooler.Instance != null)
            {
                Core.ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
