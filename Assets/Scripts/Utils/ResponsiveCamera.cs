using UnityEngine;

namespace ZiplaKac.Utils
{
    /// <summary>
    /// Ekran en-boy oranına göre 2D ortografik kameranın görüş alanını (FOV) dinamik ayarlayan bileşen.
    /// Referans (9:16) oranından daha dar/uzun ekranlarda (örn. iPhone 14 Pro Max: 9:19.5 ≈ 0.46)
    /// yatay görüş mesafesini korumak için orthographicSize'ı orantılı büyütür.
    /// Böylece oyuncu yaklaşan engelleri her zaman adil ve aynı mesafeden görür.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ResponsiveCamera : MonoBehaviour
    {
        public const float TARGET_ASPECT = 9f / 16f; // 0.5625f (1080x1920)
        public const float BASE_ORTHO_SIZE = 5.0f;

        private Camera targetCam;
        private float lastAspect = -1f;

        private void Awake()
        {
            targetCam = GetComponent<Camera>();
            AdjustCamera();
        }

        private void Update()
        {
            if (targetCam != null && Mathf.Abs(targetCam.aspect - lastAspect) > 0.005f)
            {
                AdjustCamera();
            }
        }

        public void AdjustCamera()
        {
            if (targetCam == null) targetCam = GetComponent<Camera>();
            if (targetCam == null) return;

            lastAspect = targetCam.aspect;

            if (lastAspect < TARGET_ASPECT)
            {
                // Ekran daha dar: yatay görüş alanını korumak için dikey boyutu genişlet
                targetCam.orthographicSize = BASE_ORTHO_SIZE * (TARGET_ASPECT / lastAspect);
            }
            else
            {
                // Standart veya daha geniş (iPad vb.): temel boyutu koru
                targetCam.orthographicSize = BASE_ORTHO_SIZE;
            }
        }
    }
}
