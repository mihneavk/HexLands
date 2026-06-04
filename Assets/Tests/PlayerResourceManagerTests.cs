#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class PlayerResourceManagerTests
{
    [Test]
    public void PlayerResourceManager_AddResource_And_CanAffordRoad_LogicCheck()
    {
        // 1. ARRANGE (Pregătire)
        GameObject go = new GameObject();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        // Inițializăm portofelele (wallets) pentru a evita erorile de tip NullReferenceException
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();

        // Ne asigurăm că pornește de la 0 resurse
        resManager.bluePlayer.wood = 0;
        resManager.bluePlayer.brick = 0;

        // 2. ACT & ASSERT 1 (Verificare stării inițiale - nu își permite drum)
        bool canAffordBefore = resManager.CanAffordRoad(MapGenerator.Player.Blue);
        Assert.IsFalse(canAffordBefore, "Eroare: Jucătorul n-ar trebui să își permită un drum cu 0 resurse!");

        // 3. ACT 2 (Adăugăm exact resursele pentru un drum: 1 Wood și 1 Brick)
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Wood, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Brick, 1);

        // 4. ASSERT 2 (Verificăm dacă valorile s-au salvat corect în wallet)
        int woodCount = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Wood);
        int brickCount = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Brick);

        Assert.AreEqual(1, woodCount, "Eroare: Cantitatea de Wood din wallet ar trebui să fie 1!");
        Assert.AreEqual(1, brickCount, "Eroare: Cantitatea de Brick din wallet ar trebui să fie 1!");

        // 5. ASSERT 3 (Acum ar trebui să returneze true pentru cumpărarea drumului)
        bool canAffordAfter = resManager.CanAffordRoad(MapGenerator.Player.Blue);
        Assert.IsTrue(canAffordAfter, "Eroare: Jucătorul are 1 Wood și 1 Brick, deci ar trebui să își permită drumul!");

        // CLEAN UP (Curățenie în memorie)
        Object.DestroyImmediate(go);
    }
}
#endif