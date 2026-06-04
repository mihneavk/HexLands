#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class HexCoordinatesTests
{
    [Test]
    public void ConstructorAndProperties_ShouldCalculateCorrectSCoordinate()
    {
        // 1. ARRANGE & ACT
        // Creăm o structură de coordonate axiale (q = 2, r = -1)
        HexCoordinates coords = new HexCoordinates(2, -1);

        // 3. ASSERT
        Assert.AreEqual(2, coords.Q, "Coordonata Q nu a fost setată corect.");
        Assert.AreEqual(-1, coords.R, "Coordonata R nu a fost setată corect.");
        
        // În sistemul cubic din Catan, q + r + s = 0, deci s = -q - r -> s = -2 - (-1) = -1
        Assert.AreEqual(-1, coords.S, "Coordonata cubică S derivată (q + r + s = 0) este incorectă.");
    }

    [Test]
    public void AxialToWorld_ShouldReturnOrigin_WhenCoordinatesAreZero()
    {
        // 1. ARRANGE
        float radius = 1.0f;

        // 2. ACT
        Vector3 worldPos = HexCoordinates.AxialToWorld(0, 0, radius);

        // 3. ASSERT
        Assert.AreEqual(Vector3.zero, worldPos, "Centrul hărții (0,0) ar trebui să fie exact la originea lumii vectoriale (0,0,0).");
    }

    [Test]
    public void AxialToWorld_ShouldCalculateCorrectYPosition_BasedOnRowCoordinate()
    {
        // 1. ARRANGE
        int q = 0;
        int r = 2;
        float radius = 1.0f;

        // Formula din script pentru Y este: radius * (3f / 2f * r) -> 1.0 * (1.5 * 2) = 3.0
        float expectedY = 3.0f;

        // 2. ACT
        Vector3 worldPos = HexCoordinates.AxialToWorld(q, r, radius);

        // 3. ASSERT
        Assert.AreEqual(expectedY, worldPos.y, 0.001f, "Calculul matematic pe axa verticală Y pentru orientarea Pointy-Topped este incorect.");
        Assert.AreEqual(0f, worldPos.z, "Axa Z ar trebui să rămână implicit 0 în conversia 2D a coordonatelor.");
    }
}
#endif