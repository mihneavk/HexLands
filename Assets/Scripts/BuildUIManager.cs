using UnityEngine;
using UnityEngine.UI; // Folosim UI clasic pentru butoane/imagini

public class BuildUIManager : MonoBehaviour
{
    [Header("Butoane UI")]
    public Button buildSettlementBtn;
    public Button buildRoadBtn;

    [Header("Referințe")]
    public PlayerResourceManager resourceManager;
    public GameManager gameManager;

    // Funcție apelată ori de câte ori se schimbă ceva (tura sau resursele)
    public void RefreshButtons()
    {
        // Dacă suntem în faza de Setup, butoanele ar putea fi ascunse sau dezactivate 
        // (deoarece jucătorul pune casele gratuit, fără butoane)
        if (gameManager.currentPhase == GameManager.GamePhase.Setup)
        {
            buildSettlementBtn.gameObject.SetActive(false);
            buildRoadBtn.gameObject.SetActive(false);
            return;
        }

        // Activăm butoanele și le facem vizibile
        buildSettlementBtn.gameObject.SetActive(true);
        buildRoadBtn.gameObject.SetActive(true);

        MapGenerator.Player currentPlayer = gameManager.currentPlayer;

        // 1. Verificăm Casa
        if (resourceManager.CanAffordSettlement(currentPlayer))
        {
            buildSettlementBtn.interactable = true;
            buildSettlementBtn.image.color = Color.white; // Opac
        }
        else
        {
            buildSettlementBtn.interactable = false;
            buildSettlementBtn.image.color = new Color(1f, 1f, 1f, 0.85f); // Transparent
        }

        // 2. Verificăm Drumul
        if (resourceManager.CanAffordRoad(currentPlayer))
        {
            buildRoadBtn.interactable = true;
            buildRoadBtn.image.color = Color.white; // Opac
        }
        else
        {
            buildRoadBtn.interactable = false;
            buildRoadBtn.image.color = new Color(1f, 1f, 1f, 0.85f); // Transparent
        }
    }

    private void Update()
    {
        RefreshButtons();
    }
}