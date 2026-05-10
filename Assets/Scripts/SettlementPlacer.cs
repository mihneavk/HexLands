using UnityEngine;

public class SettlementPlacer : MonoBehaviour
{
    public GameObject settlementPrefab;

    // Stări pentru a ști ce vrea jucătorul să construiască
    public enum BuildMode { None, PlacingSettlement, PlacingRoad, PlacingCity }
    public BuildMode currentMode = BuildMode.None;

    public void ActivateCityMode()
    {
        if (!CanBuildNow()) return; // Verifică hasRolled
        currentMode = BuildMode.PlacingCity;
        SetVisualsVisibility();
    }

    public void ActivateSettlementMode() {
        GameManager gm = FindObjectOfType<GameManager>();
        if (!gm.hasRolled && gm.currentPhase != GameManager.GamePhase.Setup)
        {
            Debug.LogWarning("Dă cu zarul mai întâi!");
            return;
        }

        currentMode = BuildMode.PlacingSettlement; SetVisualsVisibility(); }
    public void ActivateRoadMode() {
        GameManager gm = FindObjectOfType<GameManager>();
        if (!gm.hasRolled && gm.currentPhase != GameManager.GamePhase.Setup)
        {
            Debug.LogWarning("Dă cu zarul mai întâi!");
            return;
        }

        currentMode = BuildMode.PlacingRoad; SetVisualsVisibility();
        Debug.Log("SUNTEM IN ROADMODE");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GameManager gm = FindObjectOfType<GameManager>();
            PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
            bool isSetup = (gm.currentPhase == GameManager.GamePhase.Setup);

            // Dacă NU suntem în setup și NU am apăsat un buton de construcție, oprim click-ul
            if (!isSetup && currentMode == BuildMode.None) return;

            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider == null) return;

            // --- PLASARE CASĂ ---
            if (hit.collider.CompareTag("Corner") && (isSetup || currentMode == BuildMode.PlacingSettlement))
            {
                HexCorner corner = hit.collider.GetComponent<HexCorner>();
                if (corner.IsValidForSettlement())
                {
                    InstantiateHouse(corner, gm);
                    //ActivateRoadMode();

                    if (!isSetup)
                    {
                        resManager.SpendForSettlement(gm.currentPlayer);
                        currentMode = BuildMode.None; // Ieșim din modul de construire
                        SetVisualsVisibility();
                        FindObjectOfType<BuildUIManager>().RefreshButtons(); // Actualizăm UI
                    }
                }
            }

            // --- PLASARE DRUM ---
            else if (hit.collider.CompareTag("Edge") && (isSetup || currentMode == BuildMode.PlacingRoad))
            {
                HexEdge edge = hit.collider.GetComponent<HexEdge>();
                if (!edge.isOccupied)
                {
                    // BuildRoad() se ocupă acum de tot: owner, vizual și PLATĂ
                    edge.BuildRoad();

                    if (!isSetup)
                    {
                        currentMode = BuildMode.None;
                        SetVisualsVisibility();
                        // RefreshButtons() este apelat deja în BuildRoad
                    }
                }
            }

            else if (hit.collider.CompareTag("Corner") && currentMode == BuildMode.PlacingCity)
            {
                HexCorner corner = hit.collider.GetComponent<HexCorner>();

                // REGULĂ: Trebuie să fie casa TA și să NU fie deja oraș
                if (corner.isOccupied && corner.owner == gm.currentPlayer && !corner.isCity)
                {
                    corner.UpgradeToCity();
                    FindObjectOfType<PlayerResourceManager>().SpendForCity(gm.currentPlayer);
                    gm.AddVictoryPoint(gm.currentPlayer, 1); // Orașul valorează 2 puncte (casa avea deja 1)

                    currentMode = BuildMode.None;
                    SetVisualsVisibility();
                    FindObjectOfType<BuildUIManager>().RefreshButtons();
                }
            }
        }
    }

    public void SetVisualsVisibility()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        MapGenerator.Player currentPlayer = gm.currentPlayer;

        foreach (HexCorner corner in mg.allCorners)
        {
            // --- REPARAREA EROREI ---
            if (corner == null) continue;
            // ------------------------

            if (currentMode == BuildMode.PlacingSettlement && !corner.isOccupied)
            {
                bool isSetup = gm.currentPhase == GameManager.GamePhase.Setup;
                bool hasConnection = corner.HasAdjacentRoadOfPlayer(currentPlayer);

                // --- LOGICA CORECTATĂ ---
                // Un punct trebuie să apară DOAR DACĂ:
                // 1. Este valid conform regulii de distanță (IsValidForSettlement)
                // ȘI
                // 2. (Suntem în Setup SAU avem un drum conectat)

                bool canPlaceHere = corner.IsValidForSettlement(); // Verifică vecinii
                bool hasRequiredConnection = isSetup || hasConnection; // Verifică faza/drumul

                corner.gameObject.SetActive(canPlaceHere && hasRequiredConnection);
            }
            else
            {
                // Dacă colțul este ocupat, trebuie să rămână ACTIV pentru a vedea casa!
                if (corner.isOccupied)
                    corner.gameObject.SetActive(true);
                else
                    corner.gameObject.SetActive(false);
            }
        }

        foreach (HexEdge edge in mg.allEdges)
        {
            if (edge == null) continue;

            if (edge.corner1 == null || edge.corner2 == null)
            {
                Debug.LogWarning($"Drumul {edge.gameObject.name} nu are colțurile setate!");
                continue;
            }

            if (currentMode == BuildMode.PlacingRoad && !edge.isOccupied)
            {
                bool canBuild = false;
                bool isSetup = (gm.currentPhase == GameManager.GamePhase.Setup);


                if (isSetup)
                {
                    // --- REGULA STRICTĂ PENTRU SETUP ---
                    // Drumul trebuie să atingă FIX ultima casă plasată
                    if (edge.corner1 == gm.lastPlacedSettlement || edge.corner2 == gm.lastPlacedSettlement)
                    {
                        canBuild = true;
                    }
                }
                else
                {
                    // --- REGULA PENTRU JOCUL NORMAL ---
                    // Conectare la orice casă PROPRIE
                    if ((edge.corner1.isOccupied && edge.corner1.owner == currentPlayer) ||
                        (edge.corner2.isOccupied && edge.corner2.owner == currentPlayer))
                    {
                        canBuild = true;
                    }

                    // Verificăm dacă drumul atinge un ALT DRUM al jucătorului curent
                    if (!canBuild && !isSetup)
                    {
                        foreach (HexEdge otherEdge in mg.allEdges)
                        {
                            // CRITIC: Verificăm otherEdge.isOccupied
                            if (otherEdge != null && otherEdge.isOccupied && otherEdge.owner == currentPlayer)
                            {
                                if (otherEdge.corner1 == edge.corner1 || otherEdge.corner1 == edge.corner2 ||
                                    otherEdge.corner2 == edge.corner1 || otherEdge.corner2 == edge.corner2)
                                {
                                    canBuild = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Activăm/Dezactivăm în funcție de canBuild
                if (canBuild) edge.ShowPotentialPath();
                else edge.gameObject.SetActive(false);
            }
            else
            {
                // Dacă drumul e deja ocupat, trebuie să rămână vizibil (SetActive true), 
                // dar fără cercul de preview.
                if (edge.isOccupied)
                {
                    edge.gameObject.SetActive(true);
                    if (edge.previewCircle != null) edge.previewCircle.SetActive(false);
                }
                else
                {
                    edge.gameObject.SetActive(false);
                }
            }
        }

        // Repetă verificarea de null și pentru mg.allEdges dacă ai o listă similară!
    }

    private void InstantiateHouse(HexCorner corner, GameManager gm)
    {
        GameObject newSettlement = Instantiate(settlementPrefab, corner.transform.position, Quaternion.identity);
        corner.visualHouseObject = newSettlement;
        newSettlement.GetComponent<SpriteRenderer>().sprite = FindObjectOfType<MapGenerator>().GetCurrentHouseSprite();
        corner.BuildSettlement(gm.currentPlayer);
        gm.OnSettlementPlaced(corner);
    }

    private bool CanBuildNow()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        // 1. În faza de Setup, lăsăm jucătorul să construiască mereu (fără zar)
        if (gm.currentPhase == GameManager.GamePhase.Setup)
            return true;

        // 2. În faza de Gameplay, verificăm condițiile:
        // Trebuie să fi dat cu zarul ȘI să nu fie în mijlocul mutării hoțului
        if (!gm.hasRolled)
        {
            Debug.Log("Trebuie să dai cu zarul mai întâi!");
            return false;
        }

        if (mg.isMovingRobber)
        {
            Debug.Log("Mută hoțul înainte de a construi!");
            return false;
        }

        return true; // Dacă a trecut de toate, poate construi!
    }


}