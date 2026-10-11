using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZiplaKac.Core
{
    [System.Serializable]
    public class PoolItem
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
    }

    /// <summary>
    /// Oyun içinde engeller, güçlendirmeler ve kombo metinleri için bellek optimize nesne havuzlama sistemi (Object Pooling).
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        public static ObjectPooler Instance { get; private set; }

        [Header("Ön Tanımlı Havuz Ögeleri")]
        public List<PoolItem> poolItems;

        private Dictionary<string, Queue<GameObject>> poolDictionary;
        private Dictionary<string, GameObject> prefabDictionary;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializePools();
        }

        private void InitializePools()
        {
            poolDictionary = new Dictionary<string, Queue<GameObject>>();
            prefabDictionary = new Dictionary<string, GameObject>();

            if (poolItems == null) return;

            foreach (var item in poolItems)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < item.initialSize; i++)
                {
                    GameObject obj = Instantiate(item.prefab, transform);
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(item.tag, objectPool);
                prefabDictionary.Add(item.tag, item.prefab);
            }
        }

        /// <summary>
        /// Yeni bir prefab ile dinamik havuz ekleme (Prosedürel sprite ve runtime prefablar için)
        /// </summary>
        public void RegisterPool(string tag, GameObject prefab, int size = 10)
        {
            if (poolDictionary == null)
            {
                poolDictionary = new Dictionary<string, Queue<GameObject>>();
                prefabDictionary = new Dictionary<string, GameObject>();
            }

            if (poolDictionary.ContainsKey(tag)) return;

            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(tag, objectPool);
            prefabDictionary.Add(tag, prefab);
        }

        /// <summary>
        /// Havuzdan nesne çeker. Eğer havuz boşalırsa dinamik olarak genişletir.
        /// </summary>
        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogWarning($"[ObjectPooler] '{tag}' etiketli havuz bulunamadı!");
                return null;
            }

            GameObject objectToSpawn;

            if (poolDictionary[tag].Count > 0)
            {
                objectToSpawn = poolDictionary[tag].Dequeue();
            }
            else
            {
                // Dinamik havuz genişletme
                objectToSpawn = Instantiate(prefabDictionary[tag], transform);
            }

            objectToSpawn.transform.position = position;
            objectToSpawn.transform.rotation = rotation;
            objectToSpawn.SetActive(true);

            return objectToSpawn;
        }

        /// <summary>
        /// Nesneyi tekrar havuza iade eder.
        /// </summary>
        public void ReturnToPool(string tag, GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (poolDictionary.ContainsKey(tag))
            {
                poolDictionary[tag].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }
    }
}
