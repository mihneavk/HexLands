using UnityEngine;
using TMPro;
using System.Collections.Generic; // Obligatoriu pentru TextMeshPro

public class PlayerResourceManager : MonoBehaviour
{
    [System.Serializable]
    public class ResourceWallet
    {
        public int wood, brick, sheep, wheat, ore;
        public List<DevCardManager.DevCardType> devCards = new List<DevCardManager.DevCardType>(); // Listă cu cărțile deținute
        public GameObject uiDisplayObject; // Referință către textul de pe ecran
        public Transform devCardContainer;
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

    public void StealResource(MapGenerator.Player victim, MapGenerator.Player attacker)
    {
        // 1. Facem o listă cu toate resursele pe care victima le are efectiv (cantitate > 0)
        List<HexData.ResourceType> availableResources = new System.Collections.Generic.List<HexData.ResourceType>();

        // Presupunând că ai un dicționar sau o structură de date pentru resurse:
        // Exemplu ipotetic:
        foreach (HexData.ResourceType type in System.Enum.GetValues(typeof(HexData.ResourceType)))
        {
            if (type == HexData.ResourceType.Desert) continue;

            // Verificăm dacă victima are cel puțin o bucată din acea resursă
            if (GetResourceCount(victim, type) > 0)
            {
                availableResources.Add(type);
            }
        }

        if (availableResources.Count > 0)
        {
            // 2. Alegem una la întâmplare
            HexData.ResourceType stolenType = availableResources[Random.Range(0, availableResources.Count)];

            // 3. Executăm transferul
            RemoveResource(victim, stolenType, 1);
            AddResource(attacker, stolenType, 1);

            Debug.Log($"Jucătorul {attacker} a furat {stolenType} de la {victim}!");
        }
        else
        {
            Debug.Log($"{victim} nu are nicio resursă de furat. Ghinion!");
        }
    }

    public int GetResourceCount(MapGenerator.Player player, HexData.ResourceType type)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;

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
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;

        switch (type)
        {
            case HexData.ResourceType.Wood: wallet.wood -= amount; break;
            case HexData.ResourceType.Brick: wallet.brick -= amount; break;
            case HexData.ResourceType.Sheep: wallet.sheep -= amount; break;
            case HexData.ResourceType.Wheat: wallet.wheat -= amount; break;
            case HexData.ResourceType.Ore: wallet.ore -= amount; break;
        }

        // După ce am eliminat resursa, actualizăm UI-ul și butoanele
        UpdateUI(wallet);
        if (buildUIManager != null) buildUIManager.RefreshButtons();
    }

    public bool CanAffordCity(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        return wallet.ore >= 3 && wallet.wheat >= 2;
    }

    public void SpendForCity(MapGenerator.Player player)
    {
        ResourceWallet wallet = (player == MapGenerator.Player.Blue) ? bluePlayer : orangePlayer;
        wallet.ore -= 3; wallet.wheat -= 2;
        UpdateUI(wallet);
    }
}