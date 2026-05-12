using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class YearOfPlentyUI : MonoBehaviour
{
    public TMP_Dropdown dropdown1;
    public TMP_Dropdown dropdown2;

    public void OnConfirmClick()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        // 1. Identificăm resursele alese (folosim indexul dropdown-ului)
        AddResourceByIndex(dropdown1.value, gm.currentPlayer, resManager);
        AddResourceByIndex(dropdown2.value, gm.currentPlayer, resManager);

        // 2. Închidem panelul
        gameObject.SetActive(false);

        Debug.Log("Year of Plenty: Resurse adăugate cu succes!");
    }

    private void AddResourceByIndex(int index, MapGenerator.Player player, PlayerResourceManager resManager)
    {
        // Presupunem ordinea: 0: Wood, 1: Brick, 2: Sheep, 3: Wheat, 4: Ore
        switch (index)
        {
            case 0: resManager.AddResource(player, HexData.ResourceType.Wood, 1); break;
            case 1: resManager.AddResource(player, HexData.ResourceType.Brick, 1); break;
            case 2: resManager.AddResource(player, HexData.ResourceType.Sheep, 1); break;
            case 3: resManager.AddResource(player, HexData.ResourceType.Wheat, 1); break;
            case 4: resManager.AddResource(player, HexData.ResourceType.Ore, 1); break;
        }
    }
}