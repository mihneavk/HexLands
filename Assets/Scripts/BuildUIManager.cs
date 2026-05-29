using UnityEngine;
using UnityEngine.UI;

public class BuildUIManager : MonoBehaviour
{
    [Header("Butoane UI")]
    public Button buildSettlementBtn;
    public Button buildRoadBtn;
    public Button buildCityBtn;
    public Button buyDevCardBtn; // NOU: Butonul pentru Cărți de Dezvoltare

    [Header("Referințe")]
    public PlayerResourceManager resourceManager;
    public GameManager gameManager;

    public void RefreshButtons()
    {
        if (gameManager.currentPhase == GameManager.GamePhase.Setup)
        {
            if (buildSettlementBtn != null) buildSettlementBtn.gameObject.SetActive(false);
            if (buildRoadBtn != null) buildRoadBtn.gameObject.SetActive(false);
            if (buildCityBtn != null) buildCityBtn.gameObject.SetActive(false);
            if (buyDevCardBtn != null) buyDevCardBtn.gameObject.SetActive(false);
            return;
        }

        if (buildSettlementBtn != null) buildSettlementBtn.gameObject.SetActive(true);
        if (buildRoadBtn != null) buildRoadBtn.gameObject.SetActive(true);
        if (buildCityBtn != null) buildCityBtn.gameObject.SetActive(true);
        if (buyDevCardBtn != null) buyDevCardBtn.gameObject.SetActive(true);

        MapGenerator.Player currentPlayer = gameManager.currentPlayer;

        // 1. Verificăm Casa
        if (buildSettlementBtn != null)
        {
            bool canAfford = resourceManager.CanAffordSettlement(currentPlayer);
            buildSettlementBtn.interactable = canAfford;
            buildSettlementBtn.image.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }

        // 2. Verificăm Drumul
        if (buildRoadBtn != null)
        {
            bool canAfford = resourceManager.CanAffordRoad(currentPlayer);
            buildRoadBtn.interactable = canAfford;
            buildRoadBtn.image.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }

        // 3. Verificăm Orașul
        if (buildCityBtn != null)
        {
            bool canAfford = resourceManager.CanAffordCity(currentPlayer);
            buildCityBtn.interactable = canAfford;
            buildCityBtn.image.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }

        // 4. Verificăm Cartea de Dezvoltare
        if (buyDevCardBtn != null)
        {
            bool canAfford = resourceManager.CanAffordDevCard(currentPlayer);
            buyDevCardBtn.interactable = canAfford;
            buyDevCardBtn.image.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private void Update()
    {
        RefreshButtons();
    }
}