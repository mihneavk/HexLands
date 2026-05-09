using UnityEngine;
using System.Collections.Generic;
using static UnityEngine.EventSystems.EventTrigger;

public class GameManager : MonoBehaviour
{
    public enum GamePhase { Setup, Gameplay }
    public GamePhase currentPhase = GamePhase.Setup;
    public bool hasRolled = false;

    // Folosim Player din MapGenerator pentru consistență
    public MapGenerator.Player currentPlayer;

    // Ordinea specifică: Albastru, Portocaliu, Portocaliu, Albastru
    private MapGenerator.Player[] setupOrder = {
        MapGenerator.Player.Blue,
        MapGenerator.Player.Orange,
        MapGenerator.Player.Orange,
        MapGenerator.Player.Blue
    };

    private int setupStep = 0; // De la 0 la 3

    public DiceController diceController;
    public BuildUIManager buildUIManager;
    private bool waitingForRoad = false;

    void Start()
    {
        // Începem cu primul din listă (Albastru)
        hasRolled = true;
        currentPlayer = setupOrder[0];
        diceController.canRoll = false;
        Debug.Log($"Faza Setup: {currentPlayer} plasează prima casă și drum.");
    }

    public HexCorner lastPlacedSettlement;

    public void OnSettlementPlaced(HexCorner corner)
    {
        if (currentPhase == GamePhase.Setup)
        {
            if (setupStep == 2 || setupStep == 3) // Ordinea 0,1,2,3 -> pasul 2 și 3 sunt resursele
            {
                DistributeInitialResources(corner);
            }
            lastPlacedSettlement = corner;
            waitingForRoad = true;

            // Trecem în modul drum, dar NU schimbăm încă jucătorul!
            SettlementPlacer sp = FindObjectOfType<SettlementPlacer>();
            sp.ActivateRoadMode();
        }
    }

    public void OnRoadFinished()
    {
        if (currentPhase == GamePhase.Setup)
        {
            Debug.LogWarning("Intram in Advance setup");
            waitingForRoad = false;
            AdvanceSetup(); // Abia acum schimbăm jucătorul
        }
        else
        {
            Debug.LogWarning("Sau nu");
        }
    }

    public void NextTurn()
    {
        // Schimbăm jucătorul
        currentPlayer = (currentPlayer == MapGenerator.Player.Blue) ?
                         MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        // Anunțăm MapGenerator să curețe vizualul (punctele negre)
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        mg.PrepareNextPlayer();

        // Actualizăm butoanele UI pentru noul jucător
        buildUIManager.RefreshButtons();

        Debug.Log($"Este rândul lui: {currentPlayer}");
    }

    private void DistributeInitialResources(HexCorner corner)
    {
        // Căutăm toate hexagoanele din hartă care ating acest colț
        HexData[] allHexes = FindObjectsOfType<HexData>();
        PlayerResourceManager resourceManager = FindObjectOfType<PlayerResourceManager>();

        foreach (HexData hex in allHexes)
        {
            // Dacă hexagonul are acest colț în lista lui de colțuri adiacente
            if (hex.adjacentCorners.Contains(corner))
            {
                // Nu dăm resurse pentru Deșert
                if (hex.resourceType != HexData.ResourceType.Desert)
                {
                    // Adăugăm 1 resursă jucătorului curent
                    resourceManager.AddResource(currentPlayer, hex.resourceType, 1);
                    Debug.Log($"Resursă de start: {currentPlayer} a primit {hex.resourceType} de la hex-ul {hex.gameObject.name}");
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
            Debug.Log($"Setup: Acum e rândul lui {currentPlayer}.");

            // IMPORTANT: Îi spunem scriptului de plasare să activeze iar casele pentru noul jucător
            SettlementPlacer sp = FindObjectOfType<SettlementPlacer>();
            sp.ActivateSettlementMode();
        }
        else
        {
            StartGameplay();
        }
    }

    private void StartGameplay()
    {
        hasRolled = false;
        SettlementPlacer settlementPlacer = FindAnyObjectByType<SettlementPlacer>();
        settlementPlacer.SetVisualsVisibility();
        buildUIManager.RefreshButtons();
        currentPhase = GamePhase.Gameplay;
        currentPlayer = MapGenerator.Player.Blue; // Albastru începe mereu jocul propriu-zis

        diceController.canRoll = true;
        diceController.ResetDice(); // Ne asigurăm că sunt opace și gata de joc

        Debug.Log("Setup terminat! Începe jocul normal. Albastru, dă cu zarul.");
    }

    private void Update()
    {

    }

    // În GameManager.cs

    public void SkipTurn()
    {
        // 0. Nu lăsăm skip în setup (regulament Catan)
        if (currentPhase == GamePhase.Setup) return;

        if (!hasRolled)
        {
            Debug.LogWarning("Trebuie să dai cu zarul înainte de a termina tura!");
            return;
        }

        // 1. Schimbăm jucătorul și resetăm stările interne
        currentPlayer = (currentPlayer == MapGenerator.Player.Blue) ?
                         MapGenerator.Player.Orange : MapGenerator.Player.Blue;

        MapGenerator mg = FindObjectOfType<MapGenerator>();
        mg.PrepareNextPlayer(); // Aceasta resetează zarul și apelează UpdateValidCorners modificat mai sus

        // 2. Resetăm modul de construcție
        SettlementPlacer sp = FindObjectOfType<SettlementPlacer>();
        sp.currentMode = SettlementPlacer.BuildMode.None;

        // 3. CURĂȚĂM HARTA: Aceasta va închide toate bulinele pentru că modul e "None"
        sp.SetVisualsVisibility();

        // 4. Resetăm zarurile pentru noul jucător
        hasRolled = false;
        diceController.canRoll = true;
        diceController.ResetDice();
        buildUIManager.RefreshButtons(); 
        //buildUIManager.RefreshButtons();

        Debug.Log($"Tura s-a încheiat. Acum este rândul lui: {currentPlayer}");
    }

}