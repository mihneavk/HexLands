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

    public GameObject aiBlockerShield;

    public enum GamePhase { Setup, Gameplay }
    public GamePhase currentPhase = GamePhase.Setup;
    public bool hasRolled = false;
    private bool gameEnded = false;

    public MapGenerator.Player currentPlayer;

    // AICI SUNT VARIABILELE DINAMICE
    public MapGenerator.Player humanPlayer;
    public MapGenerator.Player aiPlayer;

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
        // CITIM CE A ALES JUCĂTORUL ÎN MENIU!
        string colorPref = PlayerPrefs.GetString("HumanColor", "Blue");
        if (colorPref == "Orange")
        {
            humanPlayer = MapGenerator.Player.Orange;
            aiPlayer = MapGenerator.Player.Blue;
        }
        else
        {
            humanPlayer = MapGenerator.Player.Blue;
            aiPlayer = MapGenerator.Player.Orange;
        }

        UpdatePointsUI();
        hasRolled = true;
        currentPlayer = setupOrder[0];
        if (diceController != null) diceController.canRoll = false;

        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers) sp.ActivateSettlementMode();

        // Dacă jucătorul 1 (Albastru) este AI-ul, îi dăm startul imediat!
        if (currentPlayer == aiPlayer)
        {
            EnablePlayerControls(false);
            FindObjectOfType<AIOpponent>().StartAITurn();
        }
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
            if (currentPlayer == aiPlayer)
            {
                EnablePlayerControls(false);
                AIOpponent ai = FindObjectOfType<AIOpponent>();
                if (ai != null) ai.StartAITurn();
            }
            else
            {
                EnablePlayerControls(true);
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
        currentPlayer = MapGenerator.Player.Blue; // Jocul mereu începe cu Blue, indiferent de cine îl controlează

        if (diceController != null)
        {
            diceController.ResetDice();
        }

        if (currentPlayer == aiPlayer)
        {
            EnablePlayerControls(false);
            if (diceController != null) diceController.canRoll = false;
            AIOpponent ai = FindObjectOfType<AIOpponent>();
            if (ai != null) ai.StartAITurn();
        }
        else
        {
            EnablePlayerControls(true);
            if (diceController != null) diceController.canRoll = true;
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

        if (currentPlayer == humanPlayer)
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

        if (buildUIManager != null) buildUIManager.RefreshButtons();

        if (currentPlayer == aiPlayer)
        {
            EnablePlayerControls(false);
            if (diceController != null) { diceController.canRoll = false; diceController.ResetDice(); }
            AIOpponent ai = FindObjectOfType<AIOpponent>();
            if (ai != null) ai.StartAITurn();
        }
        else
        {
            EnablePlayerControls(true);
            if (diceController != null) { diceController.canRoll = true; diceController.ResetDice(); }
        }
    }

    private void EnablePlayerControls(bool enable)
    {
        SettlementPlacer[] placers = FindObjectsOfType<SettlementPlacer>();
        foreach (SettlementPlacer sp in placers) sp.enabled = enable;
        if (aiBlockerShield != null) aiBlockerShield.SetActive(!enable);
    }

    public void AddVictoryPoint(MapGenerator.Player player, int amount = 1)
    {
        if (player == MapGenerator.Player.Blue) bluePoints += amount;
        else if (player == MapGenerator.Player.Orange) orangePoints += amount;

        UpdatePointsUI();
        if (amount > 0) CheckForWin(player);
    }

    private void UpdatePointsUI()
    {
        if (bluePointsText != null) bluePointsText.text = $"Puncte: {bluePoints}";
        if (orangePointsText != null) orangePointsText.text = $"Puncte: {orangePoints}";
    }

    private void CheckForWin(MapGenerator.Player player)
    {
        int currentPoints = (player == MapGenerator.Player.Blue) ? bluePoints : orangePoints;
        if (currentPoints >= pointsToWin && !gameEnded)
        {
            gameEnded = true;
            UnityEngine.Debug.Log($"<color=green>JUCĂTORUL {player} A CÂȘTIGAT JOCUL!</color>");
            currentPhase = GamePhase.Setup;
            if (diceController != null) diceController.canRoll = false;
            EnablePlayerControls(false);
        }
    }

    [Header("Trofee: Cel Mai Lung Drum")]
    public MapGenerator.Player longestRoadOwner = MapGenerator.Player.None;
    public int longestRoadLength = 4;

    [Header("Trofee: Cea Mai Mare Armată")]
    public MapGenerator.Player largestArmyOwner = MapGenerator.Player.None;
    public int largestArmySize = 2;

    public void CheckLongestRoad(MapGenerator.Player player)
    {
        try
        {
            MapGenerator mg = FindObjectOfType<MapGenerator>();
            int length = mg != null ? mg.GetLongestRoadForPlayer(player) : 0;

            if (length > longestRoadLength)
            {
                longestRoadLength = length;

                if (longestRoadOwner != player)
                {
                    if (longestRoadOwner != MapGenerator.Player.None) AddVictoryPoint(longestRoadOwner, -2);
                    longestRoadOwner = player;
                    AddVictoryPoint(player, 2);
                }
            }
        }
        catch (System.Exception ex) { }
    }

    public void CheckLargestArmy(MapGenerator.Player player, int knightsPlayed)
    {
        if (knightsPlayed > largestArmySize)
        {
            largestArmySize = knightsPlayed;

            if (largestArmyOwner != player)
            {
                if (largestArmyOwner != MapGenerator.Player.None) AddVictoryPoint(largestArmyOwner, -2);
                largestArmyOwner = player;
                AddVictoryPoint(player, 2);
            }
        }
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 16;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.textColor = Color.white;

        string roadText = longestRoadOwner != MapGenerator.Player.None ? $"🏆 Cel mai lung drum: {longestRoadOwner} ({longestRoadLength})" : "🏆 Cel mai lung drum: Nimeni";
        string armyText = largestArmyOwner != MapGenerator.Player.None ? $"⚔️ Cea mai mare armată: {largestArmyOwner} ({largestArmySize})" : "⚔️ Cea mai mare armată: Nimeni";

        GUI.Box(new Rect((Screen.width / 2) - 160, 10, 320, 60), roadText + "\n" + armyText, style);

        if (gameEnded)
        {
            GUIStyle winStyle = new GUIStyle(GUI.skin.window);
            winStyle.fontSize = 24;
            winStyle.alignment = TextAnchor.MiddleCenter;

            Rect winRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 100, 300, 200);
            GUI.Window(2, winRect, DrawWinWindow, "JOC TERMINAT", winStyle);
        }
    }

    private void DrawWinWindow(int windowID)
    {
        string winner = (bluePoints >= pointsToWin) ? "ALBASTRU A CÂȘTIGAT!" : "PORTOCALIU A CÂȘTIGAT!";
        GUI.Label(new Rect(10, 40, 280, 50), winner);

        if (GUI.Button(new Rect(50, 100, 200, 50), "MENIU PRINCIPAL"))
        {
            SceneManager.LoadScene(0); // Acum te întoarce la meniul principal!
        }
    }
}