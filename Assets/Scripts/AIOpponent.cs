using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIOpponent : MonoBehaviour
{
    public MapGenerator.Player aiColor = MapGenerator.Player.Orange;
    public float thinkingTime = 1.0f;
    public int playedKnights = 0; // Ținem evidența armatei

    private GameManager gm;
    private MapGenerator mg;
    private PlayerResourceManager resManager;

    void Start()
    {
        gm = FindObjectOfType<GameManager>();
        mg = FindObjectOfType<MapGenerator>();
        resManager = FindObjectOfType<PlayerResourceManager>();
    }

    public void StartAITurn()
    {
        StartCoroutine(AILogicRoutine());
    }

    private IEnumerator AILogicRoutine()
    {
        yield return new WaitForSeconds(thinkingTime);

        if (gm.currentPhase == GameManager.GamePhase.Setup)
        {
            PerformAgenticSetupTurn();
        }
        else if (gm.currentPhase == GameManager.GamePhase.Gameplay)
        {
            yield return StartCoroutine(PerformUtilityGameplayTurn());
        }
    }

    private bool AreSameLocation(UnityEngine.Component obj1, UnityEngine.Component obj2)
    {
        if (obj1 == null || obj2 == null) return false;
        return Vector2.Distance(obj1.transform.position, obj2.transform.position) < 0.5f;
    }

    private Dictionary<HexCorner, List<HexData>> GetCornerHexMap()
    {
        HexData[] allHexes = FindObjectsOfType<HexData>();
        Dictionary<HexCorner, List<HexData>> map = new Dictionary<HexCorner, List<HexData>>();

        foreach (HexData hex in allHexes)
        {
            foreach (HexCorner corner in hex.adjacentCorners)
            {
                if (corner == null) continue;
                if (!map.ContainsKey(corner)) map[corner] = new List<HexData>();
                map[corner].Add(hex);
            }
        }
        return map;
    }

    private int GetHexYieldScore(int tokenNumber)
    {
        if (tokenNumber == 0 || tokenNumber == 7) return 0;
        return 6 - Mathf.Abs(7 - tokenNumber);
    }

    private int EvaluateCorner(HexCorner corner, Dictionary<HexCorner, List<HexData>> cornerHexMap)
    {
        if (!cornerHexMap.ContainsKey(corner)) return 0;
        int totalYield = 0;
        HashSet<HexData.ResourceType> diversity = new HashSet<HexData.ResourceType>();

        foreach (HexData hex in cornerHexMap[corner])
        {
            totalYield += GetHexYieldScore(hex.tokenNumber);
            if (hex.resourceType != HexData.ResourceType.Desert) diversity.Add(hex.resourceType);
        }
        return totalYield + (diversity.Count * 2);
    }

    private void PerformAgenticSetupTurn()
    {
        var cornerHexMap = GetCornerHexMap();
        List<HexCorner> validCorners = new List<HexCorner>();

        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner != null && !corner.isOccupied && corner.IsValidForSettlement())
                validCorners.Add(corner);
        }

        if (validCorners.Count > 0)
        {
            HexCorner bestCorner = null;
            int bestScore = -1;

            foreach (HexCorner c in validCorners)
            {
                int score = EvaluateCorner(c, cornerHexMap);
                if (score > bestScore) { bestScore = score; bestCorner = c; }
            }

            bestCorner.gameObject.SetActive(true);
            GameObject house = Instantiate(FindObjectOfType<SettlementPlacer>().settlementPrefab, bestCorner.transform.position, Quaternion.identity);
            bestCorner.visualHouseObject = house;
            house.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();
            bestCorner.BuildSettlement(aiColor);
            gm.OnSettlementPlaced(bestCorner);

            List<HexEdge> validEdges = new List<HexEdge>();
            foreach (HexEdge edge in mg.allEdges)
            {
                if (edge != null && !edge.isOccupied && (AreSameLocation(edge.corner1, bestCorner) || AreSameLocation(edge.corner2, bestCorner)))
                    validEdges.Add(edge);
            }

            if (validEdges.Count > 0)
            {
                HexEdge bestEdge = null;
                int bestEdgeScore = -1000;

                foreach (HexEdge edge in validEdges)
                {
                    HexCorner otherCorner = (AreSameLocation(edge.corner1, bestCorner)) ? edge.corner2 : edge.corner1;
                    int edgeScore = otherCorner.isOccupied ? -100 : EvaluateCorner(otherCorner, cornerHexMap);
                    if (edgeScore > bestEdgeScore) { bestEdgeScore = edgeScore; bestEdge = edge; }
                }

                if (bestEdge == null) bestEdge = validEdges[0];

                bestEdge.gameObject.SetActive(true);
                bestEdge.owner = aiColor;
                bestEdge.BuildRoad(true);
                gm.OnRoadFinished();
            }
        }
    }

    private IEnumerator PerformUtilityGameplayTurn()
    {
        if (!gm.hasRolled)
        {
            DiceController dice = FindObjectOfType<DiceController>();
            if (dice != null) dice.RollDiceFromAI();
            gm.hasRolled = true;
            yield return new WaitForSeconds(3.0f);
        }

        bool canStillAct = true;
        int safetyLimit = 0;

        while (canStillAct && safetyLimit < 15)
        {
            safetyLimit++;

            // Hoțul este evaluat constant (în caz că dă cu zarul 7, sau joacă un cavaler!)
            if (mg.isMovingRobber)
            {
                yield return StartCoroutine(HandleRobberAI());
                yield return new WaitForSeconds(0.5f); // Pauză după mutare
            }

            yield return new WaitForSeconds(thinkingTime);
            canStillAct = EvaluateAndExecuteBestAction();
        }

        gm.SkipTurn();
    }

    private IEnumerator HandleRobberAI()
    {
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Caut cel mai malefic loc pentru hoț...");
        yield return new WaitForSeconds(1.5f);

        HexData bestHex = null;
        int maxMaliceScore = -1000;

        foreach (HexData hex in mg.allHexes)
        {
            if (hex.hasRobber || hex.resourceType == HexData.ResourceType.Desert) continue;

            int maliceScore = 0;
            int hexYield = GetHexYieldScore(hex.tokenNumber);

            foreach (HexCorner corner in hex.adjacentCorners)
            {
                if (corner.isOccupied)
                {
                    int pointValue = corner.isCity ? 2 : 1;

                    if (corner.owner == MapGenerator.Player.Blue)
                    {
                        maliceScore += hexYield * pointValue;
                    }
                    else if (corner.owner == aiColor)
                    {
                        maliceScore -= (hexYield * pointValue * 5); // Evită masiv propriile locuri
                    }
                }
            }

            if (maliceScore > maxMaliceScore)
            {
                maxMaliceScore = maliceScore;
                bestHex = hex;
            }
        }

        if (bestHex == null)
        {
            foreach (HexData hex in mg.allHexes)
            {
                if (!hex.hasRobber) { bestHex = hex; break; }
            }
        }

        UnityEngine.Debug.Log($"<color=orange>[Utility AI]</color> Am mutat hoțul pe {bestHex.resourceType} cu numărul {bestHex.tokenNumber}!");
        mg.MoveRobberToHex(bestHex);
    }

    private bool EvaluateAndExecuteBestAction()
    {
        // 1. Cărțile de dezvoltare au prioritate maximă
        if (TryPlayDevCard()) return true;

        int wood = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wood);
        int brick = resManager.GetResourceCount(aiColor, HexData.ResourceType.Brick);
        int sheep = resManager.GetResourceCount(aiColor, HexData.ResourceType.Sheep);
        int wheat = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wheat);
        int ore = resManager.GetResourceCount(aiColor, HexData.ResourceType.Ore);

        int cityScore = (ore >= 3 && wheat >= 2) ? 100 : 0;
        int settlementScore = (wood >= 1 && brick >= 1 && sheep >= 1 && wheat >= 1) ? 80 : 0;
        int devCardScore = (ore >= 1 && wheat >= 1 && sheep >= 1) ? 60 : 0;
        int roadScore = (wood >= 1 && brick >= 1) ? 40 : 0;

        // 2. Acțiuni standard
        if (cityScore > 0 && TryBuildCity()) return true;
        if (settlementScore > 0 && TryBuildSettlement()) return true;
        if (devCardScore > 0 && TryBuyDevCard()) return true;
        if (roadScore > 0 && TryBuildRoad(false)) return true;

        // 3. Dacă e complet blocat, încearcă să facă Trade cu banca!
        if (TryBankTrade()) return true;

        return false;
    }

    // ==========================================
    // LOGICA INTELIGENTĂ A CĂRȚILOR DE DEZVOLTARE
    // ==========================================
    private bool TryPlayDevCard()
    {
        var wallet = resManager.orangePlayer;
        if (wallet.devCards.Count == 0) return false;

        DevCardManager devManager = FindObjectOfType<DevCardManager>();

        if (wallet.devCards.Contains(DevCardManager.DevCardType.YearOfPlenty))
        {
            wallet.devCards.Remove(DevCardManager.DevCardType.YearOfPlenty);
            devManager.UpdateDevCardsVisuals(wallet);
            // Efect AI: Își acordă Lemn și Argilă gratuit ca să se poată extinde rapid
            resManager.AddResource(aiColor, HexData.ResourceType.Wood, 1);
            resManager.AddResource(aiColor, HexData.ResourceType.Brick, 1);
            UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am jucat Year of Plenty (Anul Abundenței)!");
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.Monopoly))
        {
            wallet.devCards.Remove(DevCardManager.DevCardType.Monopoly);
            devManager.UpdateDevCardsVisuals(wallet);

            // Efect AI: Furt malefic. Scanează ce ai tu (Blue) cel mai mult și fură tot!
            HexData.ResourceType bestToSteal = HexData.ResourceType.Ore;
            int maxEnemyHas = 0;
            HexData.ResourceType[] allTypes = { HexData.ResourceType.Wood, HexData.ResourceType.Brick, HexData.ResourceType.Sheep, HexData.ResourceType.Wheat, HexData.ResourceType.Ore };

            foreach (var t in allTypes)
            {
                int amt = resManager.GetResourceCount(MapGenerator.Player.Blue, t);
                if (amt > maxEnemyHas) { maxEnemyHas = amt; bestToSteal = t; }
            }
            if (maxEnemyHas > 0)
            {
                resManager.RemoveResource(MapGenerator.Player.Blue, bestToSteal, maxEnemyHas);
                resManager.AddResource(aiColor, bestToSteal, maxEnemyHas);
                UnityEngine.Debug.Log($"<color=orange>[Utility AI]</color> Am jucat Monopoly și ți-am furat tot {bestToSteal}-ul ({maxEnemyHas} buc)!");
            }
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.RoadBuilding))
        {
            wallet.devCards.Remove(DevCardManager.DevCardType.RoadBuilding);
            devManager.UpdateDevCardsVisuals(wallet);
            UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am jucat Construire Drumuri!");
            TryBuildRoad(true);
            TryBuildRoad(true);
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.Knight))
        {
            bool shouldPlayKnight = false;

            // EXCEPȚIE ARMATA: Dacă îl jucăm pe acesta și preluăm conducerea
            if (playedKnights >= 2 && !gm.orangeHasLargestArmyBonus)
            {
                shouldPlayKnight = true;
            }
            else
            {
                // CONDITII TACTICE
                HexData currentRobberHex = null;
                foreach (HexData hex in mg.allHexes) { if (hex.hasRobber) { currentRobberHex = hex; break; } }

                if (currentRobberHex != null)
                {
                    bool hurtsMe = false;
                    bool hurtsEnemy = false;

                    foreach (HexCorner corner in currentRobberHex.adjacentCorners)
                    {
                        if (corner.isOccupied)
                        {
                            if (corner.owner == aiColor) hurtsMe = true;
                            if (corner.owner == MapGenerator.Player.Blue) hurtsEnemy = true;
                        }
                    }

                    if (hurtsMe) shouldPlayKnight = true; // Mă sugrumă pe mine, scap de el!
                    else if (!hurtsEnemy) shouldPlayKnight = true; // Nu sugrumă pe nimeni relevant, mută-l ca să facă rău!
                    // Dacă (hurtsEnemy && !hurtsMe) -> Îl lăsăm acolo. Ne convine suferința inamicului.
                }
            }

            if (shouldPlayKnight)
            {
                wallet.devCards.Remove(DevCardManager.DevCardType.Knight);
                devManager.UpdateDevCardsVisuals(wallet);
                playedKnights++;
                UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am jucat un Cavaler tactic!");
                mg.StartRobberPhase();
                // Returnează true, iar bucla principală va muta pionul instant!
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // LOGICA SCHIMBURILOR COMERCIALE
    // ==========================================
    private bool TryBankTrade()
    {
        HexData.ResourceType excessResource = HexData.ResourceType.Desert;
        int maxAmount = 0;
        int bestRate = 4; // Baza băncii

        HexData.ResourceType[] allTypes = { HexData.ResourceType.Wood, HexData.ResourceType.Brick, HexData.ResourceType.Sheep, HexData.ResourceType.Wheat, HexData.ResourceType.Ore };

        // 1. Caută de ce resursă avem prea mult
        foreach (var type in allTypes)
        {
            int amount = resManager.GetResourceCount(aiColor, type);
            if (amount > maxAmount) { maxAmount = amount; excessResource = type; }
        }

        if (maxAmount <= 0) return false;

        // 2. Caută cele mai bune porturi pe care le deține AI-ul
        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner.isOccupied && corner.owner == aiColor && corner.currentHarborType != Harbor.HarborType.None)
            {
                if (corner.currentHarborType == Harbor.HarborType.Generic3to1 && bestRate > 3) bestRate = 3;

                if ((corner.currentHarborType == Harbor.HarborType.Wood2to1 && excessResource == HexData.ResourceType.Wood) ||
                    (corner.currentHarborType == Harbor.HarborType.Brick2to1 && excessResource == HexData.ResourceType.Brick) ||
                    (corner.currentHarborType == Harbor.HarborType.Sheep2to1 && excessResource == HexData.ResourceType.Sheep) ||
                    (corner.currentHarborType == Harbor.HarborType.Wheat2to1 && excessResource == HexData.ResourceType.Wheat) ||
                    (corner.currentHarborType == Harbor.HarborType.Ore2to1 && excessResource == HexData.ResourceType.Ore))
                {
                    bestRate = 2; // Mai bun de atât nu se poate
                }
            }
        }

        // 3. Execută schimbul dacă e permis
        if (maxAmount >= bestRate)
        {
            HexData.ResourceType neededResource = HexData.ResourceType.Desert;
            int minAmount = 99;
            foreach (var type in allTypes)
            {
                if (type == excessResource) continue;
                int amount = resManager.GetResourceCount(aiColor, type);
                if (amount < minAmount) { minAmount = amount; neededResource = type; }
            }

            if (neededResource != HexData.ResourceType.Desert)
            {
                resManager.RemoveResource(aiColor, excessResource, bestRate);
                resManager.AddResource(aiColor, neededResource, 1);
                UnityEngine.Debug.Log($"<color=orange>[Utility AI]</color> TRADE: Am dat {bestRate} {excessResource} pentru 1 {neededResource}.");
                return true;
            }
        }
        return false;
    }

    private bool TryBuildCity()
    {
        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner != null && corner.isOccupied && corner.owner == aiColor && !corner.isCity)
            {
                corner.UpgradeToCity();
                resManager.RemoveResource(aiColor, HexData.ResourceType.Ore, 3);
                resManager.RemoveResource(aiColor, HexData.ResourceType.Wheat, 2);
                gm.AddVictoryPoint(aiColor, 1);
                return true;
            }
        }
        return false;
    }

    private bool TryBuildSettlement()
    {
        var cornerHexMap = GetCornerHexMap();
        HexCorner bestTarget = null;
        int bestScore = -1;

        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner != null && !corner.isOccupied && corner.IsValidForSettlement() && HasRoadConnected(corner))
            {
                int score = EvaluateCorner(corner, cornerHexMap);
                if (score > bestScore) { bestScore = score; bestTarget = corner; }
            }
        }

        if (bestTarget != null)
        {
            bestTarget.gameObject.SetActive(true);
            GameObject house = Instantiate(FindObjectOfType<SettlementPlacer>().settlementPrefab, bestTarget.transform.position, Quaternion.identity);
            bestTarget.visualHouseObject = house;
            house.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();
            bestTarget.BuildSettlement(aiColor);

            resManager.RemoveResource(aiColor, HexData.ResourceType.Wood, 1);
            resManager.RemoveResource(aiColor, HexData.ResourceType.Brick, 1);
            resManager.RemoveResource(aiColor, HexData.ResourceType.Sheep, 1);
            resManager.RemoveResource(aiColor, HexData.ResourceType.Wheat, 1);
            gm.AddVictoryPoint(aiColor, 1);
            return true;
        }
        return false;
    }

    private bool TryBuyDevCard()
    {
        DevCardManager devManager = FindObjectOfType<DevCardManager>();
        if (devManager != null)
        {
            devManager.BuyDevelopmentCard();
            return true;
        }
        return false;
    }

    // Am adăugat isFree ca să știm dacă e de la o carte sau din buzunar!
    private bool TryBuildRoad(bool isFree)
    {
        foreach (HexEdge edge in mg.allEdges)
        {
            if (edge != null && !edge.isOccupied && CanBuildRoadHere(edge))
            {
                edge.gameObject.SetActive(true);
                edge.owner = aiColor;
                edge.BuildRoad(false); // MapGen se ocupă de vizual

                if (!isFree)
                {
                    resManager.RemoveResource(aiColor, HexData.ResourceType.Wood, 1);
                    resManager.RemoveResource(aiColor, HexData.ResourceType.Brick, 1);
                }
                FindObjectOfType<GameManager>().CheckLongestRoad(aiColor);
                return true;
            }
        }
        return false;
    }

    private bool HasRoadConnected(HexCorner corner)
    {
        foreach (HexEdge e in mg.allEdges)
        {
            if (e != null && e.isOccupied && e.owner == aiColor)
            {
                if (AreSameLocation(e.corner1, corner) || AreSameLocation(e.corner2, corner)) return true;
            }
        }
        return false;
    }

    private bool CanBuildRoadHere(HexEdge edge)
    {
        foreach (HexCorner c in mg.allCorners)
        {
            if (c != null && c.isOccupied && c.owner == aiColor && (AreSameLocation(edge.corner1, c) || AreSameLocation(edge.corner2, c))) return true;
        }
        foreach (HexEdge otherEdge in mg.allEdges)
        {
            if (otherEdge != null && otherEdge.isOccupied && otherEdge.owner == aiColor)
            {
                UnityEngine.Component sharedPos = null;
                if (AreSameLocation(otherEdge.corner1, edge.corner1) || AreSameLocation(otherEdge.corner2, edge.corner1)) sharedPos = edge.corner1;
                else if (AreSameLocation(otherEdge.corner1, edge.corner2) || AreSameLocation(otherEdge.corner2, edge.corner2)) sharedPos = edge.corner2;

                if (sharedPos != null)
                {
                    bool blocked = false;
                    foreach (HexCorner c in mg.allCorners)
                    {
                        if (c != null && AreSameLocation(c, sharedPos) && c.isOccupied && c.owner != aiColor) blocked = true;
                    }
                    if (!blocked) return true;
                }
            }
        }
        return false;
    }
}