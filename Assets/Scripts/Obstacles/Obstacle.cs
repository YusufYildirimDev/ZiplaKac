using UnityEngine;
using ZiplaKac.Core;

namespace ZiplaKac.Obstacles
{
    /// <summary>
    /// Ekrandaki tekil engel nesnelerinin sağdan sola hareketini,
    /// yavaşlatma (Slow) gücünü ve oyuncu tarafından geçildiğinde kombo tetiklenmesini yöneten sınıf.
    /// </summary>
    public class Obstacle : MonoBehaviour
    {
        public string poolTag = "Cactus";
        public float offscreenX = -12.0f;

        private bool hasBeenPassed = false;
        private Transform playerTransform;

        private void OnEnable()
        {
            hasBeenPassed = false;
            if (playerTransform == null && GameManager.Instance != null)
            {
                Player.PlayerController player = FindObjectOfType<Player.PlayerController>();
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
            {
                return;
            }

            // GameManager'dan Slow etki çarpanı uyarlanmış etkin hızı al
            float speed = GameManager.Instance.GetEffectiveSpeed();
            transform.Translate(Vector3.left * speed * Time.deltaTime);

            // Oyuncu engeli başarıyla geçti mi kontrolü (Kombo hesabı)
            if (!hasBeenPassed && playerTransform != null && transform.position.x < playerTransform.position.x)
            {
                hasBeenPassed = true;
                GameManager.Instance.OnObstacleCleared(transform.position);
            }

            // Ekranın solundan çıkınca nesne havuzuna iade et
            if (transform.position.x < offscreenX)
            {
                RecycleToPool();
            }
        }

        private void RecycleToPool()
        {
            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnToPool(poolTag, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
