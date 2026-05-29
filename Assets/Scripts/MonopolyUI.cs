using UnityEngine;
using TMPro;

public class MonopolyUI : MonoBehaviour
{
    public TMP_Dropdown resourceDropdown;

    public void OnConfirmClick()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        if (gm == null || resManager == null) return;

        MapGenerator.Player currentPlayer = gm.currentPlayer;
        MapGenerator.Player opponent = (currentPlayer == MapGenerator.Player.Blue) ? MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        HexData.ResourceType chosenResource = GetResourceTypeByIndex(resourceDropdown.value);
        int amountToSteal = resManager.GetResourceCount(opponent, chosenResource);

        if (amountToSteal > 0)
        {
            resManager.RemoveResource(opponent, chosenResource, amountToSteal);
            resManager.AddResource(currentPlayer, chosenResource, amountToSteal);

            // Aici este modificarea care oprește eroarea CS0104:
            UnityEngine.Debug.Log($"<color=magenta>MONOPOLY!</color> {currentPlayer} a furat {amountToSteal} x {chosenResource} de la {opponent}!");
        }
        else
        {
            // Și aici:
            UnityEngine.Debug.Log($"<color=magenta>MONOPOLY!</color> Ghinion! {opponent} nu avea nicio resursă de tipul {chosenResource}.");
        }

        gameObject.SetActive(false);
    }

    private HexData.ResourceType GetResourceTypeByIndex(int index)
    {
        switch (index)
        {
            case 0: return HexData.ResourceType.Wood;
            case 1: return HexData.ResourceType.Brick;
            case 2: return HexData.ResourceType.Sheep;
            case 3: return HexData.ResourceType.Wheat;
            case 4: return HexData.ResourceType.Ore;
            default: return HexData.ResourceType.Wood;
        }
    }
}