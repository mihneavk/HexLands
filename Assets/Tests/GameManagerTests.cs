#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class GameManagerTests
{
    [Test]
    public void AddVictoryPoint_ShouldIncrementPoints_AndTriggerWinCondition()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        GameManager gm = go.AddComponent<GameManager>();

        // Setăm valorile de bază pentru punctaj și condiția de victorie
        gm.bluePoints = 0;
        gm.pointsToWin = 10;
        gm.currentPhase = GameManager.GamePhase.Gameplay;

        // 2. ACT - Adăugăm 5 puncte (nu ar trebui să câștige încă)
        gm.AddVictoryPoint(MapGenerator.Player.Blue, 5);

        // 3. ASSERT 1
        Assert.AreEqual(5, gm.bluePoints, "Punctele jucătorului Albastru ar fi trebuit să crească la 5.");
        Assert.AreEqual(GameManager.GamePhase.Gameplay, gm.currentPhase, "Jocul ar trebui să fie încă în faza de Gameplay.");

        // 4. ACT - Adăugăm încă 5 puncte ca să atingem pragul de pointsToWin (10)
        gm.AddVictoryPoint(MapGenerator.Player.Blue, 5);

        // 5. ASSERT 2
        Assert.AreEqual(10, gm.bluePoints, "Punctele jucătorului Albastru ar fi trebuit să ajungă la 10.");
        Assert.AreEqual(GameManager.GamePhase.Setup, gm.currentPhase, "Faza jocului ar fi trebuit să fie resetată la Setup în urma declanșării câștigului.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }

    [Test]
    public void CheckLargestArmy_ShouldTransferTrofee_AndAdjustVictoryPoints()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        GameManager gm = go.AddComponent<GameManager>();

        // Inițializăm scorurile și pragul minim pentru armată (care în script este setat inițial la 2)
        gm.bluePoints = 0;
        gm.orangePoints = 0;
        gm.largestArmySize = 2;
        gm.largestArmyOwner = MapGenerator.Player.None;

        // 2. ACT - Jucătorul Blue joacă 3 Cavaleri (depășește pragul de 2)
        gm.CheckLargestArmy(MapGenerator.Player.Blue, 3);

        // 3. ASSERT 1
        Assert.AreEqual(MapGenerator.Player.Blue, gm.largestArmyOwner, "Trofeul ar trebui să fie deținut de Albastru.");
        Assert.AreEqual(3, gm.largestArmySize, "Dimensiunea celei mai mari armate ar trebui să fie actualizată la 3.");
        Assert.AreEqual(2, gm.bluePoints, "Jucătorul Albastru ar trebui să primească 2 puncte de victorie pentru trofeu.");

        // 4. ACT - Jucătorul Orange joacă 4 Cavaleri (îi fură trofeul lui Blue)
        gm.CheckLargestArmy(MapGenerator.Player.Orange, 4);

        // 5. ASSERT 2
        Assert.AreEqual(MapGenerator.Player.Orange, gm.largestArmyOwner, "Trofeul ar fi trebuit să fie preluat de Portocaliu.");
        Assert.AreEqual(4, gm.largestArmySize, "Dimensiunea celei mai mari armate ar trebui să devină 4.");
        Assert.AreEqual(0, gm.bluePoints, "Albastru ar fi trebuit să piardă cele 2 puncte de victorie.");
        Assert.AreEqual(2, gm.orangePoints, "Portocaliu ar fi trebuit să primească cele 2 puncte de victorie.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }
}
#endif