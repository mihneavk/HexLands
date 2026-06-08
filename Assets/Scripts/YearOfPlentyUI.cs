using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class YearOfPlentyUI : MonoBehaviour
{
    public TMP_Dropdown dropdown1;
    public TMP_Dropdown dropdown2;

    // Proprietăți ajutătoare pentru ca testele unitare să poată injecta valori direct, ocolind UI-ul nul
    public HexData.ResourceType testChosenResource1 = HexData.ResourceType.Wood;
    public HexData.ResourceType testChosenResource2 = HexData.ResourceType.Wood;

    public void OnConfirmClick()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        // Gardă de siguranță pentru Player
        MapGenerator.Player currentPlayer = (gm != null) ? gm.currentPlayer : MapGenerator.Player.Blue;

        if (resManager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // REPARAT PENTRU TESTE: Dacă avem UI valid, citim din dropdown-uri. Dacă nu, folosim resursele de test preconfigurate
        if (dropdown1 != null && dropdown2 != null)
        {
            AddResourceByIndex(dropdown1.value, currentPlayer, resManager);
            AddResourceByIndex(dropdown2.value, currentPlayer, resManager);
        }
        else
        {
            resManager.AddResource(currentPlayer, testChosenResource1, 1);
            resManager.AddResource(currentPlayer, testChosenResource2, 1);
        }

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