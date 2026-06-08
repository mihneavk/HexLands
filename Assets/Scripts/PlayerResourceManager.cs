using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerResourceManager : MonoBehaviour
{
    [System.Serializable]
    public class ResourceWallet
    {
        public int wood, brick, sheep, wheat, ore;

        public List<DevCardManager.DevCardType> devCards = new List<DevCardManager.DevCardType>();

        public GameObject uiDisplayObject;
        public Transform devCardContainer;
    }

    public ResourceWallet bluePlayer;
    public ResourceWallet orangePlayer;
    public BuildUIManager buildUIManager;

    // Helper intern pentru a lua wallet-ul în siguranță, prevenind crash-ul în teste
    private ResourceWallet GetWallet(MapGenerator.Player player)
    {
        if (player == MapGenerator.Player.Blue)
        {
            if (bluePlayer == null) bluePlayer = new ResourceWallet();
            return bluePlayer;
        }
        else
        {
            if (orangePlayer == null) orangePlayer = new ResourceWallet();
            return orangePlayer;
        }
    }

    public void AddResource(MapGenerator.Player player, HexData.ResourceType type, int amount)
    {
        ResourceWallet wallet = GetWallet(player);

        switch (type)
        {
            case HexData.ResourceType.Wood: wallet.wood += amount; break;
            case HexData.ResourceType.Brick: wallet.brick += amount; break;
            case HexData.ResourceType.Sheep: wallet.sheep += amount; break;
            case HexData.ResourceType.Wheat: wallet.wheat += amount; break;
            case HexData.ResourceType.Ore: wallet.ore += amount; break;
        }

        UpdateUI(wallet);
        if (buildUIManager != null) buildUIManager.RefreshButtons();
        UnityEngine.Debug.Log($"Jucătorul {player} a primit {amount} {type}.");
    }

    private void UpdateUI(ResourceWallet wallet)
    {
        if (wallet == null || wallet.uiDisplayObject == null) return; // Gardă de siguranță pentru teste

        TextMeshPro textComponent = wallet.uiDisplayObject.GetComponent<TextMeshPro>();

        if (textComponent != null)
        {
            textComponent.text = $"Lemn: {wallet.wood} | Argilă: {wallet.brick}\n" +
                                 $"Lână: {wallet.sheep} | Grâu: {wallet.wheat} | Minereu: {wallet.ore}";
        }
        else
        {
            UnityEngine.Debug.LogError($"Obiectul {wallet.uiDisplayObject.name} nu are o componentă TextMeshProUGUI pe el!");
        }
    }

    public bool CanAffordSettlement(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        return wallet.wood >= 1 && wallet.brick >= 1 && wallet.sheep >= 1 && wallet.wheat >= 1;
    }

    public void SpendForSettlement(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        wallet.wood--; wallet.brick--; wallet.sheep--; wallet.wheat--;
        UpdateUI(wallet);
    }

    public bool CanAffordRoad(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        return wallet.wood >= 1 && wallet.brick >= 1;
    }

    public void SpendForRoad(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        wallet.wood--; wallet.brick--;
        UpdateUI(wallet);
    }

    public void StealResource(MapGenerator.Player victim, MapGenerator.Player attacker)
    {
        List<HexData.ResourceType> availableResources = new List<HexData.ResourceType>();

        foreach (HexData.ResourceType type in System.Enum.GetValues(typeof(HexData.ResourceType)))
        {
            if (type == HexData.ResourceType.Desert) continue;

            if (GetResourceCount(victim, type) > 0)
            {
                availableResources.Add(type);
            }
        }

        if (availableResources.Count > 0)
        {
            HexData.ResourceType stolenType = availableResources[UnityEngine.Random.Range(0, availableResources.Count)];

            RemoveResource(victim, stolenType, 1);
            AddResource(attacker, stolenType, 1);

            UnityEngine.Debug.Log($"Jucătorul {attacker} a furat {stolenType} de la {victim}!");
        }
        else
        {
            UnityEngine.Debug.Log($"{victim} nu are nicio resursă de furat. Ghinion!");
        }
    }

    public int GetResourceCount(MapGenerator.Player player, HexData.ResourceType type)
    {
        ResourceWallet wallet = GetWallet(player);
        if (wallet == null) return 0;

        switch (type)
        {
            case HexData.ResourceType.Wood: return wallet.wood;
            case HexData.ResourceType.Brick: return wallet.brick;
            case HexData.ResourceType.Sheep: return wallet.sheep;
            case HexData.ResourceType.Wheat: return wallet.wheat;
            case HexData.ResourceType.Ore: return wallet.ore;
            default: return 0;
        }
    }

    public void RemoveResource(MapGenerator.Player player, HexData.ResourceType type, int amount)
    {
        ResourceWallet wallet = GetWallet(player);

        switch (type)
        {
            case HexData.ResourceType.Wood: wallet.wood -= amount; break;
            case HexData.ResourceType.Brick: wallet.brick -= amount; break;
            case HexData.ResourceType.Sheep: wallet.sheep -= amount; break;
            case HexData.ResourceType.Wheat: wallet.wheat -= amount; break;
            case HexData.ResourceType.Ore: wallet.ore -= amount; break;
        }

        UpdateUI(wallet);
        if (buildUIManager != null) buildUIManager.RefreshButtons();
    }

    public bool CanAffordCity(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        return wallet.ore >= 3 && wallet.wheat >= 2;
    }

    public void SpendForCity(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        wallet.ore -= 3; wallet.wheat -= 2;
        UpdateUI(wallet);
    }

    public bool CanAffordDevCard(MapGenerator.Player player)
    {
        ResourceWallet wallet = GetWallet(player);
        return wallet.wheat >= 1 && wallet.sheep >= 1 && wallet.ore >= 1;
    }
}