using NUnit.Framework;
using UnityEngine;

public class GameLogicTests
{
    [Test]
    public void TestareFazaInitialaJoc()
    {
        // 1. Creăm un obiect de test în memorie
        GameObject go = new GameObject();
        GameTurnManager manager = go.AddComponent<GameTurnManager>();

        // Atașăm componentele de care are nevoie Start() ca să nu crape
        go.AddComponent<DiceController>();
        go.AddComponent<SettlementPlacer>();
        manager.diceRoller = go.GetComponent<DiceController>();
        manager.settlementPlacer = go.GetComponent<SettlementPlacer>();

        // 2. Verificăm dacă faza inițială este Setup (așa cum scrie în codul tău)
        Assert.AreEqual(GameTurnManager.GamePhase.Setup, manager.currentPhase, "Eroare: Jocul ar trebui să înceapă în faza de Setup!");

        // 3. Curățăm memoria
        Object.DestroyImmediate(go);
    }
}