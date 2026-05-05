using UnityEngine;
using TMPro; // Obligatoriu pentru TextMeshPro

public class HexData : MonoBehaviour
{
    public enum ResourceType { Wood, Brick, Sheep, Wheat, Ore, Desert }

    [Header("Date Hexagon")]
    public ResourceType resourceType;
    [Range(2, 12)]
    public int tokenNumber;

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
}