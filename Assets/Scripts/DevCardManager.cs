using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static SettlementPlacer;

public class DevCardManager : MonoBehaviour
{
    [Header("Sprites Cărți")]
    public Sprite knightSprite;
    public Sprite roadBuildingSprite;
    public Sprite yearOfPlentySprite;
    public Sprite monopolySprite;
    public Sprite victoryPointSprite;

    public enum DevCardType { Knight, RoadBuilding, YearOfPlenty, Monopoly, VictoryPoint }

    private List<DevCardType> deck = new List<DevCardType>();

    void Start()
    {
        InitializeDeck();
    }

    private void InitializeDeck()
    {
        for (int i = 0; i < 14; i++) deck.Add(DevCardType.Knight);
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.RoadBuilding);
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.YearOfPlenty);
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.Monopoly);
        for (int i = 0; i < 5; i++) deck.Add(DevCardType.VictoryPoint);

        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        // REPARAT AICI: i-am zis exact să folosească UnityEngine.Random
        deck = deck.OrderBy(x => UnityEngine.Random.value).ToList();
    }

    public void BuyDevelopmentCard()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        if (!gm.hasRolled && gm.currentPhase != GameManager.GamePhase.Setup) return;

        if (resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Wheat) >= 1 &&
            resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Sheep) >= 1 &&
            resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Ore) >= 1)
        {
            if (deck.Count > 0)
            {
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Wheat, 1);
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Sheep, 1);
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Ore, 1);

                DevCardType drawnCard = deck[0];
                deck.RemoveAt(0);

                ProcessDrawnCard(drawnCard, gm.currentPlayer);
            }
            else
            {
                UnityEngine.Debug.Log("Pachetul de cărți este gol!");
            }
        }
        else
        {
            UnityEngine.Debug.Log("Nu ai resurse pentru o carte de dezvoltare!");
        }
    }

    public Sprite GetSpriteForCard(DevCardType type)
    {
        switch (type)
        {
            case DevCardType.Knight: return knightSprite;
            case DevCardType.RoadBuilding: return roadBuildingSprite;
            case DevCardType.YearOfPlenty: return yearOfPlentySprite;
            case DevCardType.Monopoly: return monopolySprite;
            case DevCardType.VictoryPoint: return victoryPointSprite;
            default: return null;
        }
    }

    public GameObject devCardPrefab;
    public float cardSpacing = 0.075f;
    public float startOffsetX = -2f;

    private void ProcessDrawnCard(DevCardType card, MapGenerator.Player player)
    {
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
        var wallet = (player == MapGenerator.Player.Blue) ? resManager.bluePlayer : resManager.orangePlayer;

        wallet.devCards.Add(card);
        UpdateDevCardsVisuals(wallet);

        if (card == DevCardType.VictoryPoint)
            FindObjectOfType<GameManager>().AddVictoryPoint(player, 1);
    }

    public void UpdateDevCardsVisuals(PlayerResourceManager.ResourceWallet wallet)
    {
        foreach (Transform child in wallet.devCardContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < wallet.devCards.Count; i++)
        {
            GameObject newCard = Instantiate(devCardPrefab, wallet.devCardContainer);

            DevCardItem itemScript = newCard.AddComponent<DevCardItem>();
            itemScript.cardType = wallet.devCards[i];
            itemScript.owner = (wallet == FindObjectOfType<PlayerResourceManager>().bluePlayer) ? MapGenerator.Player.Blue : MapGenerator.Player.Orange;

            float xPos = startOffsetX + (i * cardSpacing);
            float zPos = i * -0.1f;
            newCard.transform.localPosition = new Vector3(xPos, 0, zPos);
            newCard.transform.localRotation = Quaternion.identity;
            newCard.transform.localScale = new Vector3(0.035f, 0.15f, 1f);

            SpriteRenderer sr = newCard.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = GetSpriteForCard(wallet.devCards[i]);
                sr.sortingOrder = 15;
            }
        }
    }

    public void UseCard(DevCardType type, MapGenerator.Player player)
    {
        GameManager gm = FindObjectOfType<GameManager>();

        if (gm.currentPlayer != player)
        {
            UnityEngine.Debug.Log("Nu este rândul tău!");
            return;
        }

        switch (type)
        {
            case DevCardType.Knight:
                PlayKnight();
                break;
            case DevCardType.RoadBuilding:
                PlayRoadBuilding();
                break;
            case DevCardType.YearOfPlenty:
                PlayYearOfPlenty();
                break;
            case DevCardType.Monopoly:
                PlayMonopoly();
                break;
            case DevCardType.VictoryPoint:
                UnityEngine.Debug.Log("Punctele de victorie sunt folosite automat.");
                return;
        }

        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
        var wallet = (player == MapGenerator.Player.Blue) ? resManager.bluePlayer : resManager.orangePlayer;

        wallet.devCards.Remove(type);
        UpdateDevCardsVisuals(wallet);
    }

    [Header("Year of Plenty UI")]
    public GameObject yearOfPlentyPanel;

    [Header("Monopoly UI")]
    public GameObject monopolyPanel;

    private void PlayYearOfPlenty()
    {
        if (yearOfPlentyPanel != null) yearOfPlentyPanel.SetActive(true);
        else UnityEngine.Debug.LogError("Panelul Year of Plenty nu este asignat în DevCardManager!");
    }

    private void PlayMonopoly()
    {
        if (monopolyPanel != null) monopolyPanel.SetActive(true);
        else UnityEngine.Debug.LogError("Panelul Monopoly nu este asignat în DevCardManager!");
    }

    private void PlayKnight()
    {
        UnityEngine.Debug.Log("Ai jucat un Cavaler! Mută hoțul.");
        FindObjectOfType<MapGenerator>().StartRobberPhase();
    }

    private void PlayRoadBuilding()
    {
        SettlementPlacer placer = FindObjectOfType<SettlementPlacer>();
        placer.roadsRemainingFromCard = 2;
        placer.currentMode = BuildMode.PlacingRoad;
        placer.SetVisualsVisibility();
    }
}