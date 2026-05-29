using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIOpponent : MonoBehaviour
{
    public MapGenerator.Player aiColor = MapGenerator.Player.Orange;
    public float thinkingTime = 1.0f;
    public int playedKnights = 0;

    private GameManager gm;
    private MapGenerator mg;
    private PlayerResourceManager resManager;

    private Dictionary<HexCorner, List<HexData>> currentCornerHexMap;

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
        currentCornerHexMap = GetCornerHexMap();

        if (gm.currentPhase == GameManager.GamePhase.Setup)
        {
            PerformAgenticSetupTurn();
        }
        else if (gm.currentPhase == GameManager.GamePhase.Gameplay)
        {
            yield return StartCoroutine(PerformUtilityGameplayTurn());
        }
    }

    private float GetDiceProbability(int number)
    {
        if (number < 2 || number > 12 || number == 7) return 0f;
        return (6f - Mathf.Abs(7 - number)) / 36f;
    }

    private float CalculateCornerExpectedYield(HexCorner corner, bool isCity = false)
    {
        if (!currentCornerHexMap.ContainsKey(corner)) return 0f;

        float yield = 0f;
        float multiplier = isCity ? 2f : 1f;

        foreach (HexData hex in currentCornerHexMap[corner])
        {
            if (hex.resourceType != HexData.ResourceType.Desert && !hex.hasRobber)
            {
                yield += GetDiceProbability(hex.tokenNumber) * multiplier;
            }
        }
        return yield;
    }

    private float CalculateTotalExpectedYield()
    {
        float totalYield = 0f;
        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner != null && corner.isOccupied && corner.owner == aiColor)
            {
                totalYield += CalculateCornerExpectedYield(corner, corner.isCity);
            }
        }
        return totalYield;
    }

    private float EvaluateState(int vps, float expectedYield, int cardsInHand)
    {
        return (vps * 1000f) + (expectedYield * 500f) + (cardsInHand * 10f);
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

    private void PerformAgenticSetupTurn()
    {
        List<HexCorner> validCorners = new List<HexCorner>();

        foreach (HexCorner corner in mg.allCorners)
        {
            if (corner != null && !corner.isOccupied && corner.IsValidForSettlement())
                validCorners.Add(corner);
        }

        if (validCorners.Count > 0)
        {
            HexCorner bestCorner = null;
            float bestScore = -1f;

            foreach (HexCorner c in validCorners)
            {
                float score = CalculateCornerExpectedYield(c, false);
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
                float bestEdgeScore = -1000f;

                foreach (HexEdge edge in validEdges)
                {
                    HexCorner otherCorner = (AreSameLocation(edge.corner1, bestCorner)) ? edge.corner2 : edge.corner1;
                    float edgeScore = otherCorner.isOccupied ? -100f : CalculateCornerExpectedYield(otherCorner, false);
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

            if (mg.isMovingRobber)
            {
                yield return StartCoroutine(HandleRobberAI());
                yield return new WaitForSeconds(0.5f);
            }

            yield return new WaitForSeconds(thinkingTime);

            // BUG REZOLVAT AICI: Salvăm dacă a acționat pentru a nu închide bucla prematur
            bool cardPlayed = TryPlayDevCard();
            bool traded = TryBankTrade();
            bool built = EvaluateAndExecuteBestAction();

            // Dacă a făcut oricare dintre aceste acțiuni, considerăm că mai poate muta
            canStillAct = cardPlayed || traded || built;
        }

        gm.SkipTurn();
    }

    // ==========================================
    // LOGICA NOUĂ (MALEFICĂ) PENTRU HOȚ
    // ==========================================
    private IEnumerator HandleRobberAI()
    {
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Caut cel mai dureros loc pentru hoț...");
        yield return new WaitForSeconds(1.5f);

        HexData bestHex = null;
        float maxMaliceScore = -10000f;

        foreach (HexData hex in mg.allHexes)
        {
            if (hex.hasRobber || hex.resourceType == HexData.ResourceType.Desert) continue;

            float maliceScore = 0f;
            // Șansa de a pica (Vânează 6, 8, 5, 9)
            float hexYieldProb = GetDiceProbability(hex.tokenNumber);

            bool hurtsMe = false;
            bool hurtsEnemy = false;

            foreach (HexCorner corner in hex.adjacentCorners)
            {
                if (corner.isOccupied)
                {
                    if (corner.owner == aiColor) hurtsMe = true;
                    if (corner.owner == MapGenerator.Player.Blue)
                    {
                        hurtsEnemy = true;
                        maliceScore += hexYieldProb * (corner.isCity ? 2f : 1f);
                    }
                }
            }

            // REGULA 1: Evită complet să-și blocheze propriile sate!
            if (hurtsMe)
            {
                maliceScore -= 10000f;
            }

            // REGULA 2: Dacă atacă inamicul, favorizează numerele care ies des!
            if (hurtsEnemy && !hurtsMe)
            {
                maliceScore += hexYieldProb * 10f;
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
                if (!hex.hasRobber) { bestHex = hex; break; }
        }

        UnityEngine.Debug.Log($"<color=orange>[Utility AI]</color> Am mutat hoțul pe {bestHex.resourceType} cu numărul {bestHex.tokenNumber}!");
        mg.MoveRobberToHex(bestHex);
    }

    private bool EvaluateAndExecuteBestAction()
    {
        int wood = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wood);
        int brick = resManager.GetResourceCount(aiColor, HexData.ResourceType.Brick);
        int sheep = resManager.GetResourceCount(aiColor, HexData.ResourceType.Sheep);
        int wheat = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wheat);
        int ore = resManager.GetResourceCount(aiColor, HexData.ResourceType.Ore);
        int totalCards = wood + brick + sheep + wheat + ore;

        int currentVPs = gm.orangePoints;
        float currentYield = CalculateTotalExpectedYield();

        float bestScore = EvaluateState(currentVPs, currentYield, totalCards);
        if (totalCards > 7) bestScore -= 500f;

        System.Action bestAction = null;

        if (ore >= 3 && wheat >= 2)
        {
            foreach (HexCorner corner in mg.allCorners)
            {
                if (corner != null && corner.isOccupied && corner.owner == aiColor && !corner.isCity)
                {
                    float futureYield = currentYield + CalculateCornerExpectedYield(corner, false);
                    float score = EvaluateState(currentVPs + 1, futureYield, totalCards - 5);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = () => ExecuteBuildCity(corner);
                    }
                }
            }
        }

        if (wood >= 1 && brick >= 1 && sheep >= 1 && wheat >= 1)
        {
            foreach (HexCorner corner in mg.allCorners)
            {
                if (corner != null && !corner.isOccupied && corner.IsValidForSettlement() && HasRoadConnected(corner))
                {
                    float futureYield = currentYield + CalculateCornerExpectedYield(corner, false);
                    float score = EvaluateState(currentVPs + 1, futureYield, totalCards - 4);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = () => ExecuteBuildSettlement(corner);
                    }
                }
            }
        }

        if (ore >= 1 && wheat >= 1 && sheep >= 1)
        {
            float score = EvaluateState(currentVPs, currentYield, totalCards - 3) + 300f;
            if (score > bestScore)
            {
                bestScore = score;
                bestAction = () => ExecuteBuyDevCard();
            }
        }

        if (wood >= 1 && brick >= 1)
        {
            foreach (HexEdge edge in mg.allEdges)
            {
                if (edge != null && !edge.isOccupied && CanBuildRoadHere(edge))
                {
                    float score = EvaluateState(currentVPs, currentYield, totalCards - 2) + 50f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestAction = () => ExecuteBuildRoad(edge);
                    }
                }
            }
        }

        if (bestAction != null)
        {
            bestAction.Invoke();
            return true;
        }

        return false;
    }

    private void ExecuteBuildCity(HexCorner corner)
    {
        corner.UpgradeToCity();
        resManager.RemoveResource(aiColor, HexData.ResourceType.Ore, 3);
        resManager.RemoveResource(aiColor, HexData.ResourceType.Wheat, 2);
        gm.AddVictoryPoint(aiColor, 1);
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am construit un ORAȘ bazat pe simulare!");
    }

    private void ExecuteBuildSettlement(HexCorner targetCorner)
    {
        targetCorner.gameObject.SetActive(true);
        GameObject house = Instantiate(FindObjectOfType<SettlementPlacer>().settlementPrefab, targetCorner.transform.position, Quaternion.identity);
        targetCorner.visualHouseObject = house;
        house.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();
        targetCorner.BuildSettlement(aiColor);

        resManager.RemoveResource(aiColor, HexData.ResourceType.Wood, 1);
        resManager.RemoveResource(aiColor, HexData.ResourceType.Brick, 1);
        resManager.RemoveResource(aiColor, HexData.ResourceType.Sheep, 1);
        resManager.RemoveResource(aiColor, HexData.ResourceType.Wheat, 1);
        gm.AddVictoryPoint(aiColor, 1);
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am construit o CASĂ bazat pe simulare!");
    }

    private void ExecuteBuyDevCard()
    {
        FindObjectOfType<DevCardManager>().BuyDevelopmentCard();
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am cumpărat o Carte de Dezvoltare bazat pe simulare!");
    }

    private void ExecuteBuildRoad(HexEdge targetEdge)
    {
        targetEdge.gameObject.SetActive(true);
        targetEdge.owner = aiColor;
        targetEdge.BuildRoad(false);

        resManager.RemoveResource(aiColor, HexData.ResourceType.Wood, 1);
        resManager.RemoveResource(aiColor, HexData.ResourceType.Brick, 1);
        gm.CheckLongestRoad(aiColor);
        UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am extins un DRUM bazat pe simulare!");
    }

    // ==========================================
    // LOGICA NOUĂ PENTRU YEAR OF PLENTY
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

            int ore = resManager.GetResourceCount(aiColor, HexData.ResourceType.Ore);
            int wheat = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wheat);
            int wood = resManager.GetResourceCount(aiColor, HexData.ResourceType.Wood);
            int brick = resManager.GetResourceCount(aiColor, HexData.ResourceType.Brick);

            // AI-ul alege doar ce îi trebuie!
            if (ore >= 1 && wheat >= 1)
            {
                resManager.AddResource(aiColor, HexData.ResourceType.Ore, 1);
                resManager.AddResource(aiColor, HexData.ResourceType.Wheat, 1);
            }
            else if (wood >= 1 && brick == 0)
            {
                resManager.AddResource(aiColor, HexData.ResourceType.Brick, 1);
                resManager.AddResource(aiColor, HexData.ResourceType.Wheat, 1);
            }
            else
            {
                resManager.AddResource(aiColor, HexData.ResourceType.Wood, 1);
                resManager.AddResource(aiColor, HexData.ResourceType.Brick, 1);
            }

            UnityEngine.Debug.Log("<color=orange>[Utility AI]</color> Am jucat Year of Plenty (Anul Abundenței) și am luat resurse strategice!");
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.Monopoly))
        {
            wallet.devCards.Remove(DevCardManager.DevCardType.Monopoly);
            devManager.UpdateDevCardsVisuals(wallet);

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
            }
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.RoadBuilding))
        {
            wallet.devCards.Remove(DevCardManager.DevCardType.RoadBuilding);
            devManager.UpdateDevCardsVisuals(wallet);

            foreach (HexEdge e in mg.allEdges) { if (e != null && !e.isOccupied && CanBuildRoadHere(e)) { e.owner = aiColor; e.BuildRoad(false); e.gameObject.SetActive(true); break; } }
            foreach (HexEdge e in mg.allEdges) { if (e != null && !e.isOccupied && CanBuildRoadHere(e)) { e.owner = aiColor; e.BuildRoad(false); e.gameObject.SetActive(true); break; } }
            gm.CheckLongestRoad(aiColor);
            return true;
        }

        if (wallet.devCards.Contains(DevCardManager.DevCardType.Knight))
        {
            bool shouldPlayKnight = false;

            if (playedKnights >= 2 && gm.largestArmyOwner != aiColor)
                shouldPlayKnight = true;
            else
            {
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

                    if (hurtsMe) shouldPlayKnight = true;
                    else if (!hurtsEnemy) shouldPlayKnight = true;
                }
            }

            if (shouldPlayKnight)
            {
                wallet.devCards.Remove(DevCardManager.DevCardType.Knight);
                devManager.UpdateDevCardsVisuals(wallet);
                playedKnights++;
                gm.CheckLargestArmy(aiColor, playedKnights);
                mg.StartRobberPhase();
                return true;
            }
        }
        return false;
    }

    private bool TryBankTrade()
    {
        HexData.ResourceType excessResource = HexData.ResourceType.Desert;
        int maxAmount = 0;
        int bestRate = 4;

        HexData.ResourceType[] allTypes = { HexData.ResourceType.Wood, HexData.ResourceType.Brick, HexData.ResourceType.Sheep, HexData.ResourceType.Wheat, HexData.ResourceType.Ore };

        foreach (var type in allTypes)
        {
            int amount = resManager.GetResourceCount(aiColor, type);
            if (amount > maxAmount) { maxAmount = amount; excessResource = type; }
        }

        if (maxAmount <= 0) return false;

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
                    bestRate = 2;
                }
            }
        }

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
                return true;
            }
        }
        return false;
    }

    private bool HasRoadConnected(HexCorner corner)
    {
        foreach (HexEdge e in mg.allEdges)
            if (e != null && e.isOccupied && e.owner == aiColor)
                if (AreSameLocation(e.corner1, corner) || AreSameLocation(e.corner2, corner)) return true;
        return false;
    }

    private bool CanBuildRoadHere(HexEdge edge)
    {
        foreach (HexCorner c in mg.allCorners)
            if (c != null && c.isOccupied && c.owner == aiColor && (AreSameLocation(edge.corner1, c) || AreSameLocation(edge.corner2, c))) return true;

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
                        if (c != null && AreSameLocation(c, sharedPos) && c.isOccupied && c.owner != aiColor) blocked = true;
                    if (!blocked) return true;
                }
            }
        }
        return false;
    }
}