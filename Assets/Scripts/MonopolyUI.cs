using UnityEngine;
using TMPro;

public class MonopolyUI : MonoBehaviour
{
    public TMP_Dropdown resourceDropdown;

    // Adăugat o proprietate pentru ca testele unitare să poată injecta direct resursa dorită fără UI
    public HexData.ResourceType testChosenResource = HexData.ResourceType.Wood;

    public void OnConfirmClick()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        MapGenerator.Player currentPlayer = (gm != null) ? gm.currentPlayer : MapGenerator.Player.Blue;
        MapGenerator.Player opponent = (currentPlayer == MapGenerator.Player.Blue) ? MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        // REPARAT PENTRU TESTE UNITARE: Dacă avem UI-ul asignat, citim din el. Dacă nu, folosim resursa de test
        HexData.ResourceType chosenResource = testChosenResource;
        if (resourceDropdown != null)
        {
            chosenResource = GetResourceTypeByIndex(resourceDropdown.value);
        }

        int amountToSteal = (resManager != null) ? resManager.GetResourceCount(opponent, chosenResource) : 0;

        // REPARAT PENTRU TESTE: Dacă suntem în test unitar și resManager e simulat/creat manual de test, forțăm cantitatea dacă e cazul
        if (resManager == null)
        {
            // Dacă testul nu a instanțiat un manager complet în scenă, ne oprim în siguranță
            gameObject.SetActive(false);
            return;
        }

        if (amountToSteal > 0)
        {
            resManager.RemoveResource(opponent, chosenResource, amountToSteal);
            resManager.AddResource(currentPlayer, chosenResource, amountToSteal);

            UnityEngine.Debug.Log($"<color=magenta>MONOPOLY!</color> {currentPlayer} a furat {amountToSteal} x {chosenResource} de la {opponent}!");
        }
        else
        {
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