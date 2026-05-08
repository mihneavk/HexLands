using UnityEngine;
using System.Collections.Generic;

public class PlayerResourceManager : MonoBehaviour
{
    // Structură pentru a vedea resursele frumos în Inspector
    [System.Serializable]
    public class ResourceWallet
    {
        public int wood, brick, sheep, wheat, ore;
    }

    public ResourceWallet bluePlayer;
    public ResourceWallet orangePlayer;

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

        Debug.Log($"Jucătorul {player} a primit {amount} {type}. Scor nou: W:{wallet.wood} B:{wallet.brick}");
    }
}