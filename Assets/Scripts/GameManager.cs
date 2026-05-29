using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement; // <-- NOU: Adăugat pentru a putea reîncărca scena

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
        UpdatePointsUI();
        hasRolled = true;
        currentPlayer = setupOrder[0];
        if (diceController != null) diceController.canRoll = false;
        UnityEngine.Debug.Log($"Faza Setup: {currentPlayer} plasează prima casă și drum.");

        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers)
        {
            sp.ActivateSettlementMode();
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
                    if (resourceManager != null) resourceManager.AddResource(currentPlayer, hex.resourceType, 1);
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

    private void Update()
    {
        // CHEAT CODE PENTRU DEBUG: Apasă tasta "\" (Backslash) pentru resurse
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            GiveDebugResources();
        }

        // CHEAT CODE PENTRU RESET: Apasă tasta "R" pentru a reîncepe jocul de la zero
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.Debug.Log("<color=red>[DEBUG] JOCUL A FOST RESETAT!</color>");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void GiveDebugResources()
    {
        PlayerResourceManager resourceManager = FindObjectOfType<PlayerResourceManager>();
        if (resourceManager != null)
        {
            resourceManager.AddResource(currentPlayer, HexData.ResourceType.Wood, 8);
            resourceManager.AddResource(currentPlayer, HexData.ResourceType.Brick, 8);
            resourceManager.AddResource(currentPlayer, HexData.ResourceType.Sheep, 8);
            resourceManager.AddResource(currentPlayer, HexData.ResourceType.Wheat, 8);
            resourceManager.AddResource(currentPlayer, HexData.ResourceType.Ore, 8);

            UnityEngine.Debug.Log($"<color=magenta>[DEBUG CHEAT]</color> S-au adăugat câte 8 resurse din toate tipurile pentru jucătorul {currentPlayer}!");

            if (buildUIManager != null) buildUIManager.RefreshButtons();
        }
    }

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
        try
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
        catch (System.Exception ex)
        {
            // Am blocat eroarea ca să nu îți mai blocheze jocul!
            UnityEngine.Debug.LogWarning($"Eroare ignorată la calculul drumului: {ex.Message}");
        }
    }
}