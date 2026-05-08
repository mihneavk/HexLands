using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections; // Obligatoriu pentru TextMeshPro

public class HexData : MonoBehaviour
{
    public enum ResourceType { Wood, Brick, Sheep, Wheat, Ore, Desert }
    public List<HexCorner> adjacentCorners = new List<HexCorner>();

    [Header("Date Hexagon")]
    public ResourceType resourceType;
    [Range(2, 12)]
    public int tokenNumber;
    // În HexData.cs
    [Header("Stare Specială")]
    public bool hasRobber = false;

    // Funcție pentru a seta starea (utilă pentru mai târziu)
    public void SetRobberStatus(bool status)
    {
        hasRobber = status;
    }

    // ... restul codului rămâne la fel ...

    [Header("Referințe Vizuale")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro numberText; // Șterge "UGUI"

    // ... restul codului ...

    [System.Serializable]
    public struct ResourceVisual
    {
        public ResourceType type;
        public Sprite sprite;
    }
    public ResourceVisual[] visualLibrary;

    public int Q { get; private set; }
    public int R { get; private set; }

    public void Initialize(int q, int r, ResourceType type, int number)
    {
        this.Q = q;
        this.R = r;
        this.resourceType = type;
        this.tokenNumber = number;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 1. Actualizare Sprite (logica veche)
        foreach (var visual in visualLibrary)
        {
            if (visual.type == resourceType)
            {
                spriteRenderer.sprite = visual.sprite;
                break;
            }
        }

        // 2. Actualizare Text (logica nouă)
        if (numberText != null)
        {
            if (resourceType == ResourceType.Desert)
            {
                numberText.text = ""; // Deșertul nu are număr
            }
            else
            {
                numberText.text = tokenNumber.ToString();

                // Opțional: Pune culoarea roșie pentru 6 și 8 (ca în Catanul original)
                if (tokenNumber == 6 || tokenNumber == 8)
                    numberText.color = Color.red;
                else
                    numberText.color = Color.black;
            }
        }
    }

    private Coroutine pulseCoroutine;
    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void StartPulsing()
    {
        if (pulseCoroutine == null)
        {
            pulseCoroutine = StartCoroutine(PulseRoutine());
        }
    }

    public void StopPulsing()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
            transform.localScale = originalScale; // Resetăm la mărimea normală
        }
    }

    private IEnumerator PulseRoutine()
    {
        float pulseSpeed = 1f;       // Cât de repede pulsează
        float pulseMagnitude = 0.01f; // Cât de mult se măreşte (0.05 = 5%)

        while (true)
        {
            // Folosim funcția Sinus pentru o mișcare fluidă, dus-întors
            float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
            transform.localScale = originalScale + new Vector3(scaleOffset, scaleOffset, 0);

            yield return null; // Așteptăm următorul cadru
        }
    }
}