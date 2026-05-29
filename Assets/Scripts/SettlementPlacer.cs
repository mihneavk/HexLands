using UnityEngine;
using System;

public class SettlementPlacer : MonoBehaviour
{
    public GameObject settlementPrefab;

    public enum BuildMode { None, PlacingSettlement, PlacingRoad, PlacingCity }
    public BuildMode currentMode = BuildMode.None;

    [Header("Road Building Logic")]
    public int roadsRemainingFromCard = 0;

    private bool AreSameLocation(UnityEngine.Component obj1, UnityEngine.Component obj2)
    {
        if (obj1 == null || obj2 == null) return false;
        return Vector2.Distance(obj1.transform.position, obj2.transform.position) < 0.5f;
    }

    private bool HasRoadConnected(HexCorner corner, MapGenerator.Player player)
    {
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        if (mg == null || mg.allEdges == null) return false;

        foreach (HexEdge e in mg.allEdges)
        {
            if (e != null && e.isOccupied && e.owner == player)
            {
                if (AreSameLocation(e.corner1, corner) || AreSameLocation(e.corner2, corner))
                    return true;
            }
        }
        return false;
    }

    private bool CanBuildRoadHere(HexEdge edge, MapGenerator.Player player, bool isSetupPhase, HexCorner lastSettlement)
    {
        if (edge == null || edge.isOccupied) return false;
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        if (mg == null) return false;

        if (isSetupPhase)
        {
            if (lastSettlement != null && lastSettlement.owner == player)
            {
                if (AreSameLocation(edge.corner1, lastSettlement) || AreSameLocation(edge.corner2, lastSettlement))
                    return true;
            }
            return false;
        }

        foreach (HexCorner c in mg.allCorners)
        {
            if (c != null && c.isOccupied && c.owner == player)
            {
                if (AreSameLocation(edge.corner1, c) || AreSameLocation(edge.corner2, c))
                    return true;
            }
        }

        foreach (HexEdge otherEdge in mg.allEdges)
        {
            if (otherEdge != null && otherEdge.isOccupied && otherEdge.owner == player)
            {
                UnityEngine.Component sharedPos = null;
                if (AreSameLocation(otherEdge.corner1, edge.corner1) || AreSameLocation(otherEdge.corner2, edge.corner1))
                    sharedPos = edge.corner1;
                else if (AreSameLocation(otherEdge.corner1, edge.corner2) || AreSameLocation(otherEdge.corner2, edge.corner2))
                    sharedPos = edge.corner2;

                if (sharedPos != null)
                {
                    bool blocked = false;
                    foreach (HexCorner c in mg.allCorners)
                    {
                        if (c != null && AreSameLocation(c, sharedPos))
                        {
                            if (c.isOccupied && c.owner != player)
                            {
                                blocked = true;
                                break;
                            }
                        }
                    }
                    if (!blocked) return true;
                }
            }
        }
        return false;
    }

    public void ActivateRoadBuildingCard() { roadsRemainingFromCard = 2; currentMode = BuildMode.PlacingRoad; SetVisualsVisibility(); }
    public void ActivateCityMode() { if (!CanBuildNow()) return; currentMode = BuildMode.PlacingCity; SetVisualsVisibility(); }
    public void ActivateSettlementMode() { currentMode = BuildMode.PlacingSettlement; SetVisualsVisibility(); }
    public void ActivateRoadMode() { currentMode = BuildMode.PlacingRoad; SetVisualsVisibility(); }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // RADARUL: Aflăm de ce te blochezi
            if (currentMode == BuildMode.None)
            {
                UnityEngine.Debug.Log("❌ CLICK IGNORAT: Jocul este în modul 'None' (Pauză). Așteaptă o comandă de la joc sau apasă un buton de construcție!");
                return;
            }

            if (Camera.main == null)
            {
                UnityEngine.Debug.LogError("🚨 EROARE: Nu găsesc Camera! Asigură-te că obiectul camerei din ierarhie are tag-ul 'MainCamera'.");
                return;
            }

            GameManager gm = FindObjectOfType<GameManager>();
            PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();
            if (gm == null) return;

            bool isSetup = (gm.currentPhase == GameManager.GamePhase.Setup);

            RaycastHit2D[] hits = Physics2D.RaycastAll(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hits.Length == 0)
            {
                UnityEngine.Debug.LogWarning("⚠️ Ai dat click pe un loc gol sau îți blochează ceva interfața UI!");
                return;
            }

            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;

                if (hit.collider.CompareTag("Corner") && currentMode == BuildMode.PlacingSettlement)
                {
                    HexCorner corner = hit.collider.GetComponent<HexCorner>();
                    if (corner != null && !corner.isOccupied && corner.IsValidForSettlement() && (isSetup || HasRoadConnected(corner, gm.currentPlayer)))
                    {
                        if (settlementPrefab == null)
                        {
                            UnityEngine.Debug.LogError("🚨 EROARE CRITICĂ: Nu ai pus modelul casei în căsuța 'Settlement Prefab' a scriptului SettlementPlacer!");
                            return;
                        }

                        InstantiateHouse(corner, gm);
                        if (!isSetup) { if (resManager != null) resManager.SpendForSettlement(gm.currentPlayer); currentMode = BuildMode.None; SetVisualsVisibility(); }
                        return;
                    }
                }
                else if (hit.collider.CompareTag("Edge") && currentMode == BuildMode.PlacingRoad)
                {
                    HexEdge edge = hit.collider.GetComponent<HexEdge>();
                    if (edge != null && !edge.isOccupied && CanBuildRoadHere(edge, gm.currentPlayer, isSetup, gm.lastPlacedSettlement))
                    {
                        edge.BuildRoad(roadsRemainingFromCard > 0);
                        if (roadsRemainingFromCard > 0) roadsRemainingFromCard--;

                        if (roadsRemainingFromCard == 0 || isSetup) { currentMode = BuildMode.None; SetVisualsVisibility(); gm.OnRoadFinished(); }
                        else { if (resManager != null) resManager.SpendForRoad(gm.currentPlayer); currentMode = BuildMode.None; SetVisualsVisibility(); }

                        if (FindObjectOfType<BuildUIManager>() != null) FindObjectOfType<BuildUIManager>().RefreshButtons();
                        return;
                    }
                }
                else if (hit.collider.CompareTag("Corner") && currentMode == BuildMode.PlacingCity)
                {
                    HexCorner corner = hit.collider.GetComponent<HexCorner>();
                    if (corner != null && corner.isOccupied && corner.owner == gm.currentPlayer && !corner.isCity)
                    {
                        corner.UpgradeToCity();
                        if (resManager != null) resManager.SpendForCity(gm.currentPlayer);
                        gm.AddVictoryPoint(gm.currentPlayer, 1);
                        currentMode = BuildMode.None; SetVisualsVisibility();
                        if (FindObjectOfType<BuildUIManager>() != null) FindObjectOfType<BuildUIManager>().RefreshButtons();
                        return;
                    }
                }
            }
        }
    }

    public void SetVisualsVisibility()
    {
        try
        {
            GameManager gm = FindObjectOfType<GameManager>();
            MapGenerator mg = FindObjectOfType<MapGenerator>();
            if (gm == null || mg == null) return;

            MapGenerator.Player currentPlayer = gm.currentPlayer;

            if (mg.allCorners != null)
            {
                foreach (HexCorner c in mg.allCorners)
                    if (c != null && c.gameObject != null) c.gameObject.SetActive(c.isOccupied);
            }

            if (mg.allEdges != null)
            {
                foreach (HexEdge e in mg.allEdges)
                    if (e != null && e.gameObject != null && !e.isOccupied) e.gameObject.SetActive(false);
            }

            if (currentMode == BuildMode.PlacingSettlement)
            {
                if (mg.allCorners != null)
                {
                    foreach (HexCorner c in mg.allCorners)
                    {
                        if (c != null && c.gameObject != null && !c.isOccupied && c.IsValidForSettlement())
                        {
                            if (gm.currentPhase == GameManager.GamePhase.Setup || HasRoadConnected(c, currentPlayer))
                                c.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else if (currentMode == BuildMode.PlacingRoad)
            {
                if (mg.allEdges != null)
                {
                    foreach (HexEdge e in mg.allEdges)
                    {
                        if (e != null && e.gameObject != null && !e.isOccupied)
                        {
                            if (CanBuildRoadHere(e, currentPlayer, (gm.currentPhase == GameManager.GamePhase.Setup), gm.lastPlacedSettlement))
                                e.ShowPotentialPath();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"Eroare vizuală izolată: {ex.Message}");
        }
    }

    private void InstantiateHouse(HexCorner corner, GameManager gm)
    {
        if (corner == null || gm == null || settlementPrefab == null) return;
        GameObject newSettlement = Instantiate(settlementPrefab, corner.transform.position, Quaternion.identity);
        corner.visualHouseObject = newSettlement;
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        if (mg != null) newSettlement.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();
        corner.BuildSettlement(gm.currentPlayer);
        gm.OnSettlementPlaced(corner);
    }

    private bool CanBuildNow()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        if (gm != null && gm.currentPhase == GameManager.GamePhase.Setup) return true;
        if (gm != null && !gm.hasRolled) return false;
        if (mg != null && mg.isMovingRobber) return false;
        return true;
    }
}