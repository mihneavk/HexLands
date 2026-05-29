using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Punctaj")]
    public int bluePoints = 0;
    public int orangePoints = 0;
    public int pointsToWin = 10;

    public TMPro.TextMeshProUGUI bluePointsText;
    public TMPro.TextMeshProUGUI orangePointsText;

    // SCUTUL PENTRU BUTOANE (Opțional, vezi pașii de mai jos)
    public GameObject aiBlockerShield;

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

        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers) sp.ActivateSettlementMode();
    }

    public HexCorner lastPlacedSettlement;

    public void OnSettlementPlaced(HexCorner corner)
    {
        AddVictoryPoint(corner.owner, 1);
        if (currentPhase == GamePhase.Setup)
        {
            if (setupStep == 2 || setupStep == 3) DistributeInitialResources(corner);
            lastPlacedSettlement = corner;

            SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
            foreach (SettlementPlacer sp in placers) sp.ActivateRoadMode();
        }
    }

    public void OnRoadFinished()
    {
        if (currentPhase == GamePhase.Setup) AdvanceSetup();
    }

    private void DistributeInitialResources(HexCorner corner)
    {
        HexData[] allHexes = FindObjectsOfType<HexData>();
        PlayerResourceManager resourceManager = FindObjectOfType<PlayerResourceManager>();

        foreach (HexData hex in allHexes)
        {
            if (hex.adjacentCorners.Contains(corner) && hex.resourceType != HexData.ResourceType.Desert)
            {
                if (resourceManager != null) resourceManager.AddResource(currentPlayer, hex.resourceType, 1);
            }
        }
    }

    private void AdvanceSetup()
    {
        setupStep++;
        if (setupStep < setupOrder.Length)
        {
            currentPlayer = setupOrder[setupStep];
            if (currentPlayer == MapGenerator.Player.Orange)
            {
                EnablePlayerControls(false); // Oprim mouse-ul omului
                AIOpponent ai = FindObjectOfType<AIOpponent>();
                if (ai != null) ai.StartAITurn();
            }
            else
            {
                EnablePlayerControls(true); // Repornim mouse-ul omului
                SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
                foreach (SettlementPlacer sp in placers) sp.ActivateSettlementMode();
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
        foreach (SettlementPlacer sp in placers) sp.SetVisualsVisibility();

        if (buildUIManager != null) buildUIManager.RefreshButtons();
        currentPhase = GamePhase.Gameplay;
        currentPlayer = MapGenerator.Player.Blue;

        EnablePlayerControls(true);

        if (diceController != null)
        {
            diceController.canRoll = true;
            diceController.ResetDice();
        }

        if (currentPlayer == MapGenerator.Player.Orange)
        {
            EnablePlayerControls(false);
            AIOpponent ai = FindObjectOfType<AIOpponent>();
            if (ai != null) ai.StartAITurn();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backslash)) GiveDebugResources();
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            if (buildUIManager != null) buildUIManager.RefreshButtons();
        }
    }

    public void SkipTurn()
    {
        if (currentPhase == GamePhase.Setup) return;

        if (currentPlayer == MapGenerator.Player.Blue)
        {
            if (!hasRolled) return;
            MapGenerator mapGen = FindObjectOfType<MapGenerator>();
            if (mapGen != null && mapGen.isMovingRobber) return;
        }

        currentPlayer = (currentPlayer == MapGenerator.Player.Blue) ? MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        MapGenerator mg = FindObjectOfType<MapGenerator>();
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
            diceController.canRoll = (currentPlayer == MapGenerator.Player.Blue);
            diceController.ResetDice();
        }

        if (buildUIManager != null) buildUIManager.RefreshButtons();

        if (currentPlayer == MapGenerator.Player.Orange)
        {
            EnablePlayerControls(false); // Oprim mouse-ul tău
            AIOpponent ai = FindObjectOfType<AIOpponent>();
            if (ai != null) ai.StartAITurn();
        }
        else
        {
            EnablePlayerControls(true); // Repornim mouse-ul tău
        }
    }

    // Funcția supremă care închide tot click-ul tău
    private void EnablePlayerControls(bool enable)
    {
        // 1. Oprește click-urile pe hartă
        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers)
        {
            sp.enabled = enable;
        }

        // 2. Pornește scutul care blochează butoanele de UI (Zar, Dezvoltare, Trade)
        if (aiBlockerShield != null)
        {
            aiBlockerShield.SetActive(!enable);
        }
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
    public bool blueHasLargestArmyBonus = false;
    public bool orangeHasLargestArmyBonus = false;

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
                }
                else if (player == MapGenerator.Player.Orange && !orangeHasLongestRoadBonus)
                {
                    orangeHasLongestRoadBonus = true;
                    AddVictoryPoint(player, 2);
                }
            }
        }
        catch (System.Exception ex) { }
    }

    // ==========================================
    // UI DIN COD PENTRU TROFEE (Fără a fi nevoie de Canvas)
    // ==========================================
    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;

        // Am centrat și textul din interiorul cutiei ca să arate mai bine
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        string roadText = "🏆 Cel mai lung drum: Nimeni";
        if (blueHasLongestRoadBonus) roadText = "🏆 Cel mai lung drum: Albastru";
        else if (orangeHasLongestRoadBonus) roadText = "🏆 Cel mai lung drum: Portocaliu";

        string armyText = "⚔️ Cea mai mare armată: Nimeni";
        if (blueHasLargestArmyBonus) armyText = "⚔️ Cea mai mare armată: Albastru";
        else if (orangeHasLargestArmyBonus) armyText = "⚔️ Cea mai mare armată: Portocaliu";

        // Mutăm cutia pe mijloc: X = (Screen.width / 2) - 160, Y = 10 (rămâne sus de tot)
        GUI.Box(new Rect((Screen.width / 2) - 160, 10, 320, 60), roadText + "\n" + armyText, style);
    }
}