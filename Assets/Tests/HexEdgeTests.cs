#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class HexEdgeTests
{
    [Test]
    public void Awake_ShouldDeactivateVisuals_AndDisableCollider()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        HexEdge edge = go.AddComponent<HexEdge>();
        EdgeCollider2D collider = go.AddComponent<EdgeCollider2D>();

        GameObject preview = new GameObject("Preview");
        GameObject road = new GameObject("Road");
        edge.previewCircle = preview;
        edge.roadSprite = road;

        // Forțăm starea inițială activă pentru a testa dacă Awake le oprește
        preview.SetActive(true);
        road.SetActive(true);
        collider.enabled = true;

        // 2. ACT
        // Apelăm manual metoda Awake deoarece în teste EditMode ciclul de viață nu pornește automat
        System.Reflection.MethodInfo awakeMethod = typeof(HexEdge).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod.Invoke(edge, null);

        // 3. ASSERT
        Assert.IsFalse(preview.activeSelf, "PreviewCircle ar trebui să fie dezactivat în Awake.");
        Assert.IsFalse(road.activeSelf, "RoadSprite ar trebui să fie dezactivat în Awake.");
        Assert.IsFalse(collider.enabled, "Collider-ul de pe drum ar trebui să fie dezactivat în Awake.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(road);
    }

    [Test]
    public void BuildRoad_ShouldOccupyEdge_SetOwner_AndEnableRoadSprite()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        HexEdge edge = go.AddComponent<HexEdge>();
        go.AddComponent<EdgeCollider2D>(); // Collider-ul necesar

        GameObject preview = new GameObject("Preview");
        GameObject road = new GameObject("Road");
        road.AddComponent<SpriteRenderer>(); // Necesar pentru GetCurrentRoadSprite() din cod
        edge.previewCircle = preview;
        edge.roadSprite = road;

        // Simulăm managerii de joc din scenă
        GameManager gm = go.AddComponent<GameManager>();
        MapGenerator mg = go.AddComponent<MapGenerator>();
        gm.currentPlayer = MapGenerator.Player.Orange;

        edge.isOccupied = false;
        edge.owner = MapGenerator.Player.None;

        // 2. ACT
        edge.BuildRoad(true);

        // 3. ASSERT
        Assert.IsTrue(edge.isOccupied, "Drumul ar trebui să fie marcat ca ocupat după construcție.");
        Assert.AreEqual(MapGenerator.Player.Orange, edge.owner, "Proprietarul drumului ar trebui să devină jucătorul curent.");
        Assert.IsFalse(preview.activeSelf, "Cercul de preview ar trebui să se ascundă după ce drumul e construit.");
        Assert.IsTrue(road.activeSelf, "Sprite-ul de drum ar trebui să devină vizibil.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(preview);
        Object.DestroyImmediate(road);
    }
}
#endif