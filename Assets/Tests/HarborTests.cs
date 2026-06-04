#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class HarborTests
{
    [Test]
    public void AssignToCorners_ShouldFindAndApplyBonusToValidAdjacentCorners()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        Harbor harbor = go.AddComponent<Harbor>();
        harbor.type = Harbor.HarborType.Wheat2to1;
        harbor.detectionRadius = 1.0f;

        // Creăm un colț simulat în raza de acțiune a portului
        GameObject cornerGo = new GameObject("TestCorner");
        cornerGo.tag = "Corner";
        HexCorner corner = cornerGo.AddComponent<HexCorner>();
        CircleCollider2D collider = cornerGo.AddComponent<CircleCollider2D>();

        // Poziționăm colțul aproape de port și celălalt obiect la originea comună a scenei
        go.transform.position = Vector3.zero;
        cornerGo.transform.position = new Vector3(0.2f, 0.2f, 0f);

        // Resetăm valorile din colț pentru a verifica modificarea lor ulterioară
        corner.currentHarborType = Harbor.HarborType.None;
        corner.tradeRatio = 4;

        // 2. ACT
        harbor.AssignToCorners();

        // 3. ASSERT
        Assert.AreEqual(Harbor.HarborType.Wheat2to1, corner.currentHarborType, 
            "Portul ar trebui să identifice colțul din apropiere și să îi seteze tipul corect.");
        Assert.AreEqual(2, corner.tradeRatio, 
            "Rata de schimb a colțului ar trebui să fie actualizată la 2 conform bonusului de port specific.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(cornerGo);
    }
}
#endif