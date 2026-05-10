using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BankTradeManager : MonoBehaviour
{
    [Header("Referințe UI")]
    public TMP_Dropdown giveDropdown;
    public TMP_InputField giveAmountInput;
    public TMP_Dropdown receiveDropdown;
    public TMP_InputField receiveAmountInput;
    public TextMeshProUGUI rateInfoText; // Opțional: Afișează "Rata curentă: 2:1"

    public void AttemptTrade()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        // 1. Preluăm datele din UI
        if (string.IsNullOrEmpty(giveAmountInput.text) || string.IsNullOrEmpty(receiveAmountInput.text)) return;

        int giveAmount = int.Parse(giveAmountInput.text);
        int receiveAmount = int.Parse(receiveAmountInput.text);

        // Convertim dropdown index la ResourceType (Asigură-te că ordinea în dropdown coincide cu Enum-ul tău)
        // Presupunem: 0=Wood, 1=Brick, 2=Sheep, 3=Wheat, 4=Ore
        HexData.ResourceType giveType = (HexData.ResourceType)(giveDropdown.value);
        HexData.ResourceType receiveType = (HexData.ResourceType)(receiveDropdown.value);

        // 2. CALCULĂM RATA DINAMICĂ
        int bestRate = GetBestRateForPlayer(gm.currentPlayer, giveType);

        // 3. VERIFICĂRI
        if (giveAmount != receiveAmount * bestRate)
        {
            Debug.LogError($"Rată invalidă! Pentru {giveType} ai rata de {bestRate}:1. Ar trebui să dai {receiveAmount * bestRate} resurse.");
            return;
        }

        if (resManager.GetResourceCount(gm.currentPlayer, giveType) < giveAmount)
        {
            Debug.LogError($"Nu ai suficiente resurse! {giveType}");
            return;
        }

        // 4. EXECUTĂM SCHIMBUL
        resManager.RemoveResource(gm.currentPlayer, giveType, giveAmount);
        resManager.AddResource(gm.currentPlayer, receiveType, receiveAmount);

        Debug.Log($"Trade reușit la rata {bestRate}:1!");

        // Curățăm UI-ul
        giveAmountInput.text = "";
        receiveAmountInput.text = "";
    }

    // Funcția care caută cel mai bun port al jucătorului
    private int GetBestRateForPlayer(MapGenerator.Player player, HexData.ResourceType resourceToGive)
    {
        int currentBest = 4; // Rata standard a băncii
        HexCorner[] allCorners = FindObjectsOfType<HexCorner>();

        foreach (HexCorner corner in allCorners)
        {
            // Verificăm doar colțurile jucătorului care au un port
            if (corner.isOccupied && corner.owner == player && corner.currentHarborType != Harbor.HarborType.None)
            {
                // 1. Verificăm dacă este port generic 3:1
                if (corner.currentHarborType == Harbor.HarborType.Generic3to1)
                {
                    if (currentBest > 3) currentBest = 3;
                }
                // 2. Verificăm dacă este port specific 2:1 pentru resursa dată
                else if (IsSpecificHarborForResource(corner.currentHarborType, resourceToGive))
                {
                    return 2; // 2:1 este cea mai bună rată posibilă, putem returna direct
                }
            }
        }

        return currentBest;
    }

    // Helper pentru a corela HarborType cu ResourceType
    private bool IsSpecificHarborForResource(Harbor.HarborType harbor, HexData.ResourceType resource)
    {
        if (harbor == Harbor.HarborType.Wood2to1 && resource == HexData.ResourceType.Wood) return true;
        if (harbor == Harbor.HarborType.Brick2to1 && resource == HexData.ResourceType.Brick) return true;
        if (harbor == Harbor.HarborType.Sheep2to1 && resource == HexData.ResourceType.Sheep) return true;
        if (harbor == Harbor.HarborType.Wheat2to1 && resource == HexData.ResourceType.Wheat) return true;
        if (harbor == Harbor.HarborType.Ore2to1 && resource == HexData.ResourceType.Ore) return true;

        return false;
    }

    // Adaugă asta în BankTradeManager și leagă-l de evenimentul "On Value Changed" al Dropdown-ului
    public void UpdateRateDisplay()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        HexData.ResourceType giveType = (HexData.ResourceType)(giveDropdown.value + 1);
        int rate = GetBestRateForPlayer(gm.currentPlayer, giveType);

        if (rateInfoText != null)
            rateInfoText.text = $"Rata ta actuală pentru această resursă: {rate}:1";
    }
}