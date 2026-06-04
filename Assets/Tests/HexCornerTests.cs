#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class HexCornerTests
{
    [Test]
    public void IsValidForSettlement_ShouldReturnFalse_WhenNeighborIsOccupied()
    {
        // 1. ARRANGE
        GameObject mainGo = new GameObject("MainCorner");
        HexCorner mainCorner = mainGo.AddComponent<HexCorner>();

        GameObject neighborGo = new GameObject("NeighborCorner");
        HexCorner neighborCorner = neighborGo.AddComponent<HexCorner>();

        GameObject edgeGo = new GameObject("ConnectingEdge");
        HexEdge edge = edgeGo.AddComponent<HexEdge>();

        // Conectăm cele două colțuri prin intermediul drumului (muchiei)
        edge.corner1 = mainCorner;
        edge.corner2 = neighborCorner;
        mainCorner.adjacentEdges.Add(edge);
        neighborCorner.adjacentEdges.Add(edge);

        // Simulăm că vecinul are deja o așezare construită (încalcă regula de distanță de 2 piese)
        neighborCorner.isOccupied = true;
        mainCorner.isOccupied = false;

        // 2. ACT
        bool isValid = mainCorner.IsValidForSettlement();

        // 3. ASSERT
        Assert.IsFalse(isValid, "Colțul nu ar trebui să fie valid pentru construcție dacă un vecin direct este ocupat.");

        // CLEAN UP
        Object.DestroyImmediate(mainGo);
        Object.DestroyImmediate(neighborGo);
        Object.DestroyImmediate(edgeGo);
    }

    [Test]
    public void GiveHarborBonus_ShouldSetCorrectTradeRatio_ForGenericAndSpecificHarbors()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        HexCorner corner = go.AddComponent<HexCorner>();

        // 2. ACT & ASSERT (Test pentru portul Generic 3:1)
        corner.GiveHarborBonus(Harbor.HarborType.Generic3to1);
        Assert.AreEqual(3, corner.tradeRatio, "Rata de schimb ar trebui să fie 3 pentru un port generic.");
        Assert.AreEqual(Harbor.HarborType.Generic3to1, corner.currentHarborType, "Tipul de port nu a fost salvat corect.");

        // 3. ACT & ASSERT (Test pentru porturile Specifice 2:1, ex: Sheep)
        corner.GiveHarborBonus(Harbor.HarborType.Sheep2to1);
        Assert.AreEqual(2, corner.tradeRatio, "Rata de schimb ar trebui să fie 2 pentru un port specific de resursă.");
        Assert.AreEqual(Harbor.HarborType.Sheep2to1, corner.currentHarborType, "Tipul de port specific nu a fost salvat corect.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }
}
#endif