using UnityEngine;
using TMPro; // Obligatoriu pentru TextMeshPro

public class PlayerResourceManager : MonoBehaviour
{
    [System.Serializable]
    public class ResourceWallet
    {
        public int wood, brick, sheep, wheat, ore;
        public GameObject uiDisplayObject; // Referință către textul de pe ecran
    }

    public ResourceWallet bluePlayer;
    public ResourceWallet orangePlayer;
    public BuildUIManager buildUIManager;

    public void AddResource(MapGenerator.Player player, HexData.ResourceType type, int amount)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;

        switch (type)
        {
            case HexData.ResourceType.Wood: wallet.wood += amount; break;
            case HexData.ResourceType.Brick: wallet.brick += amount; break;
            case HexData.ResourceType.Sheep: wallet.sheep += amount; break;
            case HexData.ResourceType.Wheat: wallet.wheat += amount; break;
            case HexData.ResourceType.Ore: wallet.ore += amount; break;
        }

        UpdateUI(wallet); // Actualizăm textul de pe ecran
        buildUIManager.RefreshButtons();
        Debug.Log($"Jucătorul {player} a primit {amount} {type}.");
    }

    private void UpdateUI(ResourceWallet wallet)
    {
        // 1. Verificăm dacă am tras un obiect în căsuță
        if (wallet.uiDisplayObject != null)
        {
            // 2. Încercăm să accesăm componenta de TextMeshPro de pe acel GameObject
            TextMeshPro textComponent = wallet.uiDisplayObject.GetComponent<TextMeshPro>();

            // 3. Dacă am găsit componenta, îi modificăm textul
            if (textComponent != null)
            {
                textComponent.text = $"Lemn: {wallet.wood} | Argilă: {wallet.brick}\n" +
                                     $"Lână: {wallet.sheep} | Grâu: {wallet.wheat} | Minereu: {wallet.ore}";
            }
            else
            {
                Debug.LogError($"Obiectul {wallet.uiDisplayObject.name} nu are o componentă TextMeshProUGUI pe el!");
            }
        }
    }

    // În PlayerResourceManager.cs

    public bool CanAffordSettlement(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        // Cost: 1 Lemn, 1 Argilă, 1 Lână, 1 Grâu
        return wallet.wood >= 1 && wallet.brick >= 1 && wallet.sheep >= 1 && wallet.wheat >= 1;
    }

    public void SpendForSettlement(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        wallet.wood--; wallet.brick--; wallet.sheep--; wallet.wheat--;
        UpdateUI(wallet); // Actualizăm textul de pe ecran
    }

    public bool CanAffordRoad(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        // Cost: 1 Lemn, 1 Argilă
        return wallet.wood >= 1 && wallet.brick >= 1;
    }

    public void SpendForRoad(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        wallet.wood--; wallet.brick--;
        UpdateUI(wallet);
    }
}