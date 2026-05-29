using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Punctaj")]
    public int bluePoints = 0;
    public int orangePoints = 0;
    public int pointsToWin = 10;

    public TMPro.TextMeshProUGUI bluePointsText;
    public TMPro.TextMeshProUGUI orangePointsText;

    public enum GamePhase { Setup, Gameplay }
    public GamePhase currentPhase = GamePhase.Setup;
    public bool hasRolled = false;

    public MapGenerator.Player currentPlayer;

    private MapGenerator.Player[] setupOrder = {
        MapGenerator.Player.Blue,
        MapGenerator.Player.Orange,
        MapGenerator.Player.Orange,
        MapGenerator.Player.Blue
    };

    private int setupStep = 0;

    public DiceController diceController;
    public BuildUIManager buildUIManager;

    void Start()
    {
        // BLINDAJ: Indiferent ce lipsește, jocul NU va mai crăpa la început!
        try
        {
            UpdatePointsUI();
            hasRolled = true;
            currentPlayer = setupOrder[0];

            if (diceController != null)
            {
                diceController.canRoll = false;
            }
            else
            {
                UnityEngine.Debug.LogWarning("⚠️ ATENȚIE: Obiectul 'Dice Controller' nu este legat în GameManager!");
            }

            UnityEngine.Debug.Log($"Faza Setup: {currentPlayer} plasează prima casă și drum.");

            SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
            foreach (SettlementPlacer sp in placers)
            {
                sp.ActivateSettlementMode();
            }
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"🚨 EROARE FATALĂ în GameManager la pornire: {ex.Message}");
        }
    }

    public HexCorner lastPlacedSettlement;

    public void OnSettlementPlaced(HexCorner corner)
    {
        AddVictoryPoint(corner.owner, 1);
        if (currentPhase == GamePhase.Setup)
        {
            if (setupStep == 2 || setupStep == 3)
            {
                DistributeInitialResources(corner);
            }
            lastPlacedSettlement = corner;

            SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
            foreach (SettlementPlacer sp in placers)
            {
                sp.ActivateRoadMode();
            }
        }
    }

    public void OnRoadFinished()
    {
        if (currentPhase == GamePhase.Setup)
        {
            AdvanceSetup();
        }
    }

    private void DistributeInitialResources(HexCorner corner)
    {
        HexData[] allHexes = FindObjectsOfType<HexData>();
        PlayerResourceManager resourceManager = FindObjectOfType<PlayerResourceManager>();

        foreach (HexData hex in allHexes)
        {
            if (hex.adjacentCorners.Contains(corner))
            {
                if (hex.resourceType != HexData.ResourceType.Desert)
                {
                    resourceManager.AddResource(currentPlayer, hex.resourceType, 1);
                    UnityEngine.Debug.Log($"Resursă de start: {currentPlayer} a primit {hex.resourceType} de la hex-ul {hex.gameObject.name}");
                }
            }
        }
    }

    private void AdvanceSetup()
    {
        setupStep++;

        if (setupStep < setupOrder.Length)
        {
            currentPlayer = setupOrder[setupStep];
            UnityEngine.Debug.Log($"Setup: Acum e rândul lui {currentPlayer}.");

            SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
            foreach (SettlementPlacer sp in placers)
            {
                sp.ActivateSettlementMode();
            }
        }
        else
        {
            StartGameplay();
        }
    }

    private void StartGameplay()
    {
        hasRolled = false;

        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers)
        {
            sp.SetVisualsVisibility();
        }

        if (buildUIManager != null) buildUIManager.RefreshButtons();
        currentPhase = GamePhase.Gameplay;
        currentPlayer = MapGenerator.Player.Blue;

        if (diceController != null)
        {
            diceController.canRoll = true;
            diceController.ResetDice();
        }

        UnityEngine.Debug.Log("Setup terminat! Începe jocul normal. Albastru, dă cu zarul.");
    }

    private void Update() { }

    public void SkipTurn()
    {
        if (currentPhase == GamePhase.Setup) return;

        if (!hasRolled)
        {
            UnityEngine.Debug.LogWarning("Trebuie să dai cu zarul înainte de a termina tura!");
            return;
        }

        MapGenerator mg = FindObjectOfType<MapGenerator>();

        if (mg != null && mg.isMovingRobber)
        {
            UnityEngine.Debug.LogWarning("Legea deșertului: Nu poți încheia tura până nu muți hoțul!");
            return;
        }

        currentPlayer = (currentPlayer == MapGenerator.Player.Blue) ? MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        if (mg != null) mg.PrepareNextPlayer();

        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers)
        {
            sp.currentMode = SettlementPlacer.BuildMode.None;
            sp.SetVisualsVisibility();
        }

        hasRolled = false;

        if (diceController != null)
        {
            diceController.canRoll = true;
            diceController.ResetDice();
        }

        if (buildUIManager != null) buildUIManager.RefreshButtons();

        UnityEngine.Debug.Log($"Tura s-a încheiat. Acum este rândul lui: {currentPlayer}");
    }

    public void AddVictoryPoint(MapGenerator.Player player, int amount = 1)
    {
        if (player == MapGenerator.Player.Blue) bluePoints += amount;
        else if (player == MapGenerator.Player.Orange) orangePoints += amount;

        UpdatePointsUI();
        CheckForWin(player);
    }

    private void UpdatePointsUI()
    {
        if (bluePointsText != null) bluePointsText.text = $"Puncte: {bluePoints}";
        if (orangePointsText != null) orangePointsText.text = $"Puncte: {orangePoints}";
    }

    private void CheckForWin(MapGenerator.Player player)
    {
        int currentPoints = (player == MapGenerator.Player.Blue) ? bluePoints : orangePoints;

        if (currentPoints >= pointsToWin)
        {
            UnityEngine.Debug.Log($"<color=green>JUCĂTORUL {player} A CÂȘTIGAT JOCUL!</color>");
            currentPhase = GamePhase.Setup;
            if (diceController != null) diceController.canRoll = false;
        }
    }

    [Header("Bonusuri")]
    public bool blueHasLongestRoadBonus = false;
    public bool orangeHasLongestRoadBonus = false;

    public void CheckLongestRoad(MapGenerator.Player player)
    {
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        int length = mg != null ? mg.GetLongestRoadForPlayer(player) : 0;

        if (length >= 5)
        {
            if (player == MapGenerator.Player.Blue && !blueHasLongestRoadBonus)
            {
                blueHasLongestRoadBonus = true;
                AddVictoryPoint(player, 2);
                UnityEngine.Debug.Log("<color=blue>Albastru a primit bonusul pentru Cel Mai Lung Drum!</color>");
            }
            else if (player == MapGenerator.Player.Orange && !orangeHasLongestRoadBonus)
            {
                orangeHasLongestRoadBonus = true;
                AddVictoryPoint(player, 2);
                UnityEngine.Debug.Log("<color=orange>Portocaliu a primit bonusul pentru Cel Mai Lung Drum!</color>");
            }
        }
    }
}