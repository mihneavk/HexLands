using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerInventory : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI woodText;
    public TextMeshProUGUI brickText;
    public TextMeshProUGUI sheepText;
    public TextMeshProUGUI wheatText;
    public TextMeshProUGUI oreText;

    // Un dicționar pentru a stoca rapid numărul de resurse
    // Folosim enum-ul ResourceType deja existent în HexData
    private Dictionary<HexData.ResourceType, int> resources;

    private void Start()
    {
        // Inițializăm inventarul cu 0 pentru toate resursele
        resources = new Dictionary<HexData.ResourceType, int>
        {
            { HexData.ResourceType.Wood, 0 },
            { HexData.ResourceType.Brick, 0 },
            { HexData.ResourceType.Sheep, 0 },
            { HexData.ResourceType.Wheat, 0 },
            { HexData.ResourceType.Ore, 0 }
        };

        UpdateUI();
    }

    // Funcție pentru a adăuga (sau scădea, folosind numere negative) resurse
    public void AddResource(HexData.ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            resources[type] += amount;

            // Nu lăsăm resursele să scadă sub 0
            if (resources[type] < 0) resources[type] = 0;

            UpdateUI();
        }
    }

    // Actualizăm elementele vizuale (textele) din ecran
    public void UpdateUI()
    {
        if (woodText != null) woodText.text = resources[HexData.ResourceType.Wood].ToString();
        if (brickText != null) brickText.text = resources[HexData.ResourceType.Brick].ToString();
        if (sheepText != null) sheepText.text = resources[HexData.ResourceType.Sheep].ToString();
        if (wheatText != null) wheatText.text = resources[HexData.ResourceType.Wheat].ToString();
        if (oreText != null) oreText.text = resources[HexData.ResourceType.Ore].ToString();
    }
}