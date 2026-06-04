#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class DiscardManagerTests
{
    [Test]
    public void HandleRuleOf7_ShouldCalculateCorrectSurplus_ForHumanAndAI()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DiscardManager discardManager = go.AddComponent<DiscardManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();
        MapGenerator mg = go.AddComponent<MapGenerator>();

        // Injectăm dependențele găsite de Start() ca să nu lăsăm referințele null
        System.Reflection.FieldInfo resManagerField = typeof(DiscardManager).GetField("resManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        resManagerField.SetValue(discardManager, resManager);
        
        System.Reflection.FieldInfo mgField = typeof(DiscardManager).GetField("mg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        mgField.SetValue(discardManager, mg);

        // Inițializăm portofelele de resurse
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();

        // Jucătorul Orange (AI) are 8 resurse (peste limita de 7) -> trebuie să arunce 4 (8 / 2)
        resManager.orangePlayer.sheep = 8;

        // Jucătorul Blue (Uman) are 10 resurse (peste limita de 7) -> trebuie să arunce 5 (10 / 2)
        resManager.bluePlayer.wood = 10;

        discardManager.blueCardsToDrop = 0;

        // 2. ACT
        discardManager.HandleRuleOf7();

        // 3. ASSERT
        // AI-ul ar trebui să acționeze instant: 8 - 4 = 4 rămase
        int orangeTotal = resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Sheep);
        Assert.AreEqual(4, orangeTotal, "AI-ul ar fi trebuit să își înjumătățească resursele de oaie.");

        // Jucătorul uman primește doar contorul de penalizare, urmând să le arunce manual din UI
        Assert.AreEqual(5, discardManager.blueCardsToDrop, "Jucătorul uman ar fi trebuit să fie taxat cu exact jumătate din cărți (5).");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DiscardForAI_ShouldPrioritizeSheepOverOtherResources()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DiscardManager discardManager = go.AddComponent<DiscardManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        System.Reflection.FieldInfo resManagerField = typeof(DiscardManager).GetField("resManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        resManagerField.SetValue(discardManager, resManager);

        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();
        
        // Îi dăm AI-ului 2 Oi și 2 Lemne
        resManager.orangePlayer.sheep = 2;
        resManager.orangePlayer.wood = 2;

        // 2. ACT - Îi cerem să arunce 2 cărți folosind metoda privată prin Reflection
        System.Reflection.MethodInfo discardMethod = typeof(DiscardManager).GetMethod("DiscardForAI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        discardMethod.Invoke(discardManager, new object[] { 2 });

        // 3. ASSERT
        // Conform priorității stabilite în cod (Sheep -> Wood), ar trebui să scape de ambele oi și să păstreze lemnele
        int orangeSheep = resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Sheep);
        int orangeWood = resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Wood);

        Assert.AreEqual(0, orangeSheep, "AI-ul ar fi trebuit să arunce cu prioritate oile.");
        Assert.AreEqual(2, orangeWood, "Lemnul nu ar fi trebuit să fie atins deoarece oile au acoperit toată cantitatea cerută.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }
}
#endif