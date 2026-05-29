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
            EnablePlayerControls(false);
            AIOpponent ai = FindObjectOfType<AIOpponent>();
            if (ai != null) ai.StartAITurn();
        }
        else
        {
            EnablePlayerControls(true);
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

        // Verificăm câștigul doar dacă scorul a crescut (evităm situația în care cineva pierde trofeul și câștigă simultan)
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

    // ==========================================
    // LOGICA NOUĂ PENTRU TRANSFERUL TROFEELOR
    // ==========================================
    [Header("Trofee: Cel Mai Lung Drum")]
    public MapGenerator.Player longestRoadOwner = MapGenerator.Player.None;
    public int longestRoadLength = 4; // Minim 5 drumuri pentru a obține trofeul inițial

    [Header("Trofee: Cea Mai Mare Armată")]
    public MapGenerator.Player largestArmyOwner = MapGenerator.Player.None;
    public int largestArmySize = 2; // Minim 3 cavaleri pentru a obține trofeul inițial

    public void CheckLongestRoad(MapGenerator.Player player)
    {
        try
        {
            MapGenerator mg = FindObjectOfType<MapGenerator>();
            int length = mg != null ? mg.GetLongestRoadForPlayer(player) : 0;

            // Dacă jucătorul curent a stabilit un nou record (trebuie să fie strict mai mare)
            if (length > longestRoadLength)
            {
                longestRoadLength = length;

                // Dacă trofeul abia a fost preluat sau furat
                if (longestRoadOwner != player)
                {
                    // Îi luăm punctele jucătorului care a fost depășit (dacă există)
                    if (longestRoadOwner != MapGenerator.Player.None)
                    {
                        AddVictoryPoint(longestRoadOwner, -2);
                        UnityEngine.Debug.Log($"<color=red>Trofeul 'Drumul' i-a fost furat jucătorului {longestRoadOwner}!</color>");
                    }

                    // Îi dăm trofeul și punctele noului campion
                    longestRoadOwner = player;
                    AddVictoryPoint(player, 2);
                    UnityEngine.Debug.Log($"<color=green>{player} a preluat trofeul 'Cel Mai Lung Drum' ({length})!</color>");
                }
            }
        }
        catch (System.Exception ex) { }
    }

    // Funcție pregătită pentru Cavaler (o poți apela cu numărul total de cavaleri jucați de un jucător)
    public void CheckLargestArmy(MapGenerator.Player player, int knightsPlayed)
    {
        if (knightsPlayed > largestArmySize)
        {
            largestArmySize = knightsPlayed;

            if (largestArmyOwner != player)
            {
                if (largestArmyOwner != MapGenerator.Player.None)
                {
                    AddVictoryPoint(largestArmyOwner, -2);
                    UnityEngine.Debug.Log($"<color=red>Trofeul 'Armata' i-a fost furat jucătorului {largestArmyOwner}!</color>");
                }

                largestArmyOwner = player;
                AddVictoryPoint(player, 2);
                UnityEngine.Debug.Log($"<color=green>{player} a preluat trofeul 'Cea Mai Mare Armată' ({knightsPlayed})!</color>");
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

        // UI-ul va arăta acum și mărimea actuală a drumului/armatei!
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

        if (GUI.Button(new Rect(50, 100, 200, 50), "RESTART JOC"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}