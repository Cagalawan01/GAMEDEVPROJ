using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SakuraPetalsOverlay : MonoBehaviour
{
    [System.Serializable]
    public class Petal
    {
        public RectTransform rectTransform;
        public Image image;
        public Vector2 position;
        public float speed;
        public float swayAmplitude;
        public float swayFrequency;
        public float rotationSpeed;
        public float initialRotation;
        public float size;
        public float seed;
    }

    [Header("Petal Configuration")]
    [SerializeField] private int petalCount = 35;
    [SerializeField] private Color petalColorMin = new Color(1f, 0.78f, 0.88f, 0.65f);
    [SerializeField] private Color petalColorMax = new Color(1f, 0.90f, 0.95f, 0.90f);
    [SerializeField] private float minSpeed = 50f;
    [SerializeField] private float maxSpeed = 120f;
    [SerializeField] private float minSize = 12f;
    [SerializeField] private float maxSize = 24f;

    private List<Petal> petals = new List<Petal>();
    private RectTransform canvasRect;

    private void Start()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            canvasRect = parentCanvas.GetComponent<RectTransform>();
        }

        SpawnPetals();
    }

    private void SpawnPetals()
    {
        Sprite petalSprite = Resources.Load<Sprite>("sakura_petal");

        for (int i = 0; i < petalCount; i++)
        {
            GameObject petalObj = new GameObject($"Petal_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            petalObj.transform.SetParent(transform, false);

            RectTransform rt = petalObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Image img = petalObj.GetComponent<Image>();
            img.raycastTarget = false;
            if (petalSprite != null)
            {
                img.sprite = petalSprite;
            }
            img.color = Color.Lerp(petalColorMin, petalColorMax, Random.value);

            Petal p = new Petal
            {
                rectTransform = rt,
                image = img,
                position = new Vector2(
                    Random.Range(-960f, 960f),
                    Random.Range(-540f, 540f)
                ),
                speed = Random.Range(minSpeed, maxSpeed),
                swayAmplitude = Random.Range(20f, 55f),
                swayFrequency = Random.Range(1.2f, 2.5f),
                rotationSpeed = Random.Range(20f, 75f) * (Random.value > 0.5f ? 1f : -1f),
                initialRotation = Random.Range(0f, 360f),
                size = Random.Range(minSize, maxSize),
                seed = Random.Range(0f, 100f)
            };

            rt.sizeDelta = new Vector2(p.size, p.size * 1.6f);
            rt.anchoredPosition = p.position;
            rt.localRotation = Quaternion.Euler(0, 0, p.initialRotation);

            petals.Add(p);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float time = Time.time;

        for (int i = 0; i < petals.Count; i++)
        {
            Petal p = petals[i];
            if (p.rectTransform == null) continue;

            // Fall downward and drift leftward with wind
            p.position.y -= p.speed * dt;
            float sway = Mathf.Sin((time + p.seed) * p.swayFrequency) * p.swayAmplitude * dt;
            p.position.x -= (p.speed * 0.45f * dt) + sway;

            // Flutter rotation
            p.initialRotation += p.rotationSpeed * dt;

            // Wrap around screen boundaries
            if (p.position.y < -580f)
            {
                p.position.y = 580f + Random.Range(0f, 80f);
                p.position.x = Random.Range(-800f, 1050f);
            }
            if (p.position.x < -1000f)
            {
                p.position.x = 1000f;
            }

            p.rectTransform.anchoredPosition = p.position;
            p.rectTransform.localRotation = Quaternion.Euler(0, 0, p.initialRotation);
        }
    }
}
