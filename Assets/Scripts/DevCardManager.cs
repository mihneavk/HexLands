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
        // 14 Cavaleri
        for (int i = 0; i < 14; i++) deck.Add(DevCardType.Knight);
        // 2 din fiecare progres
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.RoadBuilding);
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.YearOfPlenty);
        for (int i = 0; i < 2; i++) deck.Add(DevCardType.Monopoly);
        // 5 Puncte de Victorie
        for (int i = 0; i < 5; i++) deck.Add(DevCardType.VictoryPoint);

        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        deck = deck.OrderBy(x => Random.value).ToList();
    }

    public void BuyDevelopmentCard()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        // 1. Verificăm dacă suntem în gameplay și dacă s-a dat cu zarul
        // Folosim metoda ta de siguranță CanBuildNow (dacă ai pus-o în SettlementPlacer)
        // Sau facem o verificare rapidă aici:
        if (!gm.hasRolled && gm.currentPhase != GameManager.GamePhase.Setup) return;

        // 2. Verificăm resursele (1 Wheat, 1 Sheep, 1 Ore)
        if (resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Wheat) >= 1 &&
            resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Sheep) >= 1 &&
            resManager.GetResourceCount(gm.currentPlayer, HexData.ResourceType.Ore) >= 1)
        {
            if (deck.Count > 0)
            {
                // Consumăm resursele
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Wheat, 1);
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Sheep, 1);
                resManager.RemoveResource(gm.currentPlayer, HexData.ResourceType.Ore, 1);

                // Tragem cartea
                DevCardType drawnCard = deck[0];
                deck.RemoveAt(0);

                ProcessDrawnCard(drawnCard, gm.currentPlayer);
            }
            else
            {
                Debug.Log("Pachetul de cărți este gol!");
            }
        }
        else
        {
            Debug.Log("Nu ai resurse pentru o carte de dezvoltare!");
        }
    }

    private void ShowCardInUI(DevCardType card)
    {
        // Aici vei activa un popup care să arate sprite-ul cărții trase
        // Ex: cardDisplayImage.sprite = GetSpriteForCard(card);
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
        // 1. Curățăm cărțile vechi
        foreach (Transform child in wallet.devCardContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. Generăm cărțile noi
        for (int i = 0; i < wallet.devCards.Count; i++)
        {
            // Instanțiem fără a seta poziția de lume (World Space) încă
            GameObject newCard = Instantiate(devCardPrefab, wallet.devCardContainer);

            DevCardItem itemScript = newCard.AddComponent<DevCardItem>(); // Sau GetComponent dacă e deja pe prefab
            itemScript.cardType = wallet.devCards[i];
            itemScript.owner = (wallet == FindObjectOfType<PlayerResourceManager>().bluePlayer) ? MapGenerator.Player.Blue : MapGenerator.Player.Orange;

            // 3. SETĂM LOCAL POSITION
            // startOffsetX (-2f) + i * spacing (0.5f)
            float xPos = startOffsetX + (i * cardSpacing);
            float zPos = i * -0.1f;
            newCard.transform.localPosition = new Vector3(xPos, 0, zPos);

            // 4. Resetăm rotația și scala locală
            newCard.transform.localRotation = Quaternion.identity;

            // Folosește valori egale pentru X și Y ca să nu fie strivită imaginea!
            newCard.transform.localScale = new Vector3(0.035f, 0.15f, 1f);

            // 5. Setăm Sprite-ul
            SpriteRenderer sr = newCard.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = GetSpriteForCard(wallet.devCards[i]);
                // Opțional: forțăm un Sorting Order mare să fie deasupra hărții
                sr.sortingOrder = 15;
            }
        }
    }

    public void UseCard(DevCardType type, MapGenerator.Player player)
    {
        GameManager gm = FindObjectOfType<GameManager>();

        // 1. Verificăm dacă este rândul jucătorului care a dat click
        if (gm.currentPlayer != player)
        {
            Debug.Log("Nu este rândul tău!");
            return;
        }

        // 2. Executăm efectul cărții
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
            case DevCardType.VictoryPoint:
                Debug.Log("Punctele de victorie sunt folosite automat.");
                return; // Nu ștergem punctele de victorie, ele rămân vizibile
                        // Aici vei adăuga restul (Monopoly, etc.)
        }


        // 3. Ștergem cartea din lista logică a jucătorului
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
        var wallet = (player == MapGenerator.Player.Blue) ? resManager.bluePlayer : resManager.orangePlayer;

        wallet.devCards.Remove(type);

        // 4. Actualizăm vizualul (va șterge tot și va redesena cărțile rămase)
        UpdateDevCardsVisuals(wallet);
    }

    [Header("Year of Plenty UI")]
    public GameObject yearOfPlentyPanel;

    private void PlayYearOfPlenty()
    {
        if (yearOfPlentyPanel != null)
        {
            yearOfPlentyPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Panelul Year of Plenty nu este asignat în DevCardManager!");
        }
    }

    private void PlayKnight()
    {
        Debug.Log("Ai jucat un Cavaler! Mută hoțul.");

        // Apelăm logica de mutare a hoțului din MapGenerator
        // Presupunem că ai metoda StartRobberPhase definită anterior
        FindObjectOfType<MapGenerator>().StartRobberPhase();
    }

    private void PlayRoadBuilding()
    {
        SettlementPlacer placer = FindObjectOfType<SettlementPlacer>();
        placer.roadsRemainingFromCard = 2; // Setează 2 drumuri
        placer.currentMode = BuildMode.PlacingRoad;
        placer.SetVisualsVisibility();
    }
}   