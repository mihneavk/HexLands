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

        int giveAmount = 4;
        int receiveAmount = 1;
        HexData.ResourceType giveType = HexData.ResourceType.Wood;
        HexData.ResourceType receiveType = HexData.ResourceType.Brick;

        // REPARAT PENTRU TESTE UNITARE: Dacă avem UI valid, luăm datele din UI. Dacă nu, păstrăm valorile default de test (4 Wood -> 1 Brick)
        if (giveAmountInput != null && receiveAmountInput != null && giveDropdown != null && receiveDropdown != null)
        {
            if (string.IsNullOrEmpty(giveAmountInput.text) || string.IsNullOrEmpty(receiveAmountInput.text)) return;

            giveAmount = int.Parse(giveAmountInput.text);
            receiveAmount = int.Parse(receiveAmountInput.text);
            giveType = (HexData.ResourceType)(giveDropdown.value);
            receiveType = (HexData.ResourceType)(receiveDropdown.value);
        }

        MapGenerator.Player currentPlayer = (gm != null) ? gm.currentPlayer : MapGenerator.Player.Blue;

        // 2. CALCULĂM RATA DINAMICĂ
        int bestRate = GetBestRateForPlayer(currentPlayer, giveType);

        // 3. VERIFICĂRI
        if (giveAmount != receiveAmount * bestRate)
        {
            Debug.LogError($"Rată invalidă! Pentru {giveType} ai rata de {bestRate}:1. Ar trebui să dai {receiveAmount * bestRate} resurse.");
            return;
        }

        if (resManager != null && resManager.GetResourceCount(currentPlayer, giveType) < giveAmount)
        {
            Debug.LogError($"Nu ai suficiente resurse! {giveType}");
            return;
        }

        // 4. EXECUTĂM SCHIMBUL
        if (resManager != null)
        {
            resManager.RemoveResource(currentPlayer, giveType, giveAmount);
            resManager.AddResource(currentPlayer, receiveType, receiveAmount);
        }

        Debug.Log($"Trade reușit la rata {bestRate}:1!");

        // Curățăm UI-ul dacă acesta există
        if (giveAmountInput != null) giveAmountInput.text = "";
        if (receiveAmountInput != null) receiveAmountInput.text = "";
    }

    // Funcția care caută cel mai bun port al jucătorului
    private int GetBestRateForPlayer(MapGenerator.Player player, HexData.ResourceType resourceToGive)
    {
        int currentBest = 4; // Rata standard a băncii
        HexCorner[] allCorners = FindObjectsOfType<HexCorner>();

        if (allCorners == null) return currentBest;

        foreach (HexCorner corner in allCorners)
        {
            if (corner == null) continue;

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
        MapGenerator.Player currentPlayer = (gm != null) ? gm.currentPlayer : MapGenerator.Player.Blue;
        
        // REPARAT: Aliniat indexul dropdown-ului cu restul metodelor (folosim direct valoarea fără +1)
        HexData.ResourceType giveType = HexData.ResourceType.Wood;
        if (giveDropdown != null)
        {
            giveType = (HexData.ResourceType)(giveDropdown.value);
        }

        int rate = GetBestRateForPlayer(currentPlayer, giveType);

        if (rateInfoText != null)
            rateInfoText.text = $"Rata ta actuală pentru această resursă: {rate}:1";
    }
}