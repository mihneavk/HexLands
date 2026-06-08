#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class DevCardManagerTests
{
    [Test]
    public void InitializeDeck_ShouldCreateExactly25Cards_WithCorrectDistribution()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DevCardManager manager = go.AddComponent<DevCardManager>();

        // 2. ACT
        // Metoda InitializeDeck rulează automat în Start(), dar o putem apela prin Reflection
        // pentru a izola testul fără să așteptăm frame-ul de Start
        System.Reflection.MethodInfo initMethod = typeof(DevCardManager).GetMethod("InitializeDeck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        initMethod.Invoke(manager, null);

        // Extragem pachetul privat folosind Reflection
        System.Reflection.FieldInfo deckField = typeof(DevCardManager).GetField("deck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        List<DevCardManager.DevCardType> deck = (List<DevCardManager.DevCardType>)deckField.GetValue(manager);

        // 3. ASSERT
        // În Catan sunt: 14 Cavaleri + 2 Drumuri + 2 Abundență + 2 Monopol + 5 Puncte = 25 cărți
        Assert.AreEqual(25, deck.Count, "Pachetul total de cărți inițializat ar trebui să conțină exact 25 de cărți.");

        int knightCount = deck.FindAll(c => c == DevCardManager.DevCardType.Knight).Count;
        int vpCount = deck.FindAll(c => c == DevCardManager.DevCardType.VictoryPoint).Count;

        Assert.AreEqual(14, knightCount, "Numărul de cărți Cavaler ar trebui să fie exact 14.");
        Assert.AreEqual(5, vpCount, "Numărul de cărți Punct de Victorie ar trebui să fie exact 5.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }

    public void BuyDevelopmentCard_ShouldDeductResources_AndAddCardToWallet_WhenResourcesAreValid()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DevCardManager devManager = go.AddComponent<DevCardManager>();
        GameManager gm = go.AddComponent<GameManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        // Inițializăm wallet-urile pentru a evita NullReference în script-ul tău
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.bluePlayer.devCardContainer = new GameObject("Container").transform;

        // CORECTURĂ CRITICĂ: Injectăm Prefab-uri simulate prin Reflection pentru a evita eroarea "The Object you want to instantiate is null"
        GameObject mockPrefab = new GameObject("MockCardPrefab");
        
        // În funcție de cum le-ai numit în clasa ta (ex: knightPrefab, cardPrefab sau o listă),
        // căutăm câmpurile de tip GameObject din DevCardManager și le dăm o valoare validă.
        System.Reflection.FieldInfo[] fields = typeof(DevCardManager).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(GameObject))
            {
                field.SetValue(devManager, mockPrefab);
            }
        }

        // Configurăm starea GameManager-ului
        gm.currentPhase = GameManager.GamePhase.Gameplay;
        gm.hasRolled = true;
        gm.currentPlayer = MapGenerator.Player.Blue;

        // Îi dăm jucătorului fix resursele necesare (1 Grâu, 1 Lână, 1 Minereu)
        resManager.bluePlayer.wheat = 1;
        resManager.bluePlayer.sheep = 1;
        resManager.bluePlayer.ore = 1;
        
        // Sincronizăm și prin metoda AddResource în caz că managerul tău verifică prin GetResourceCount()
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Wheat, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Sheep, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Ore, 1);

        // Adăugăm manual o carte în pachet ca să poată fi extrasă
        System.Reflection.FieldInfo deckField = typeof(DevCardManager).GetField("deck", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        List<DevCardManager.DevCardType> simulatedDeck = new List<DevCardManager.DevCardType> { DevCardManager.DevCardType.Knight };
        deckField.SetValue(devManager, simulatedDeck);

        // 2. ACT
        devManager.BuyDevelopmentCard();

        // 3. ASSERT
        // Resursele trebuie să fi fost consumate (ajung la 0)
        Assert.AreEqual(0, resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Wheat), "Grâul nu a fost dedus corect.");
        Assert.AreEqual(0, resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Sheep), "Lâna nu a fost dedus corect.");
        Assert.AreEqual(0, resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Ore), "Minereul nu a fost dedus corect.");

        // Cartea trebuie să fi ajuns în portofelul (wallet-ul) jucătorului Blue
        Assert.AreEqual(1, resManager.bluePlayer.devCards.Count, "Jucătorul ar fi trebuit să primească o carte în listă.");
        Assert.AreEqual(DevCardManager.DevCardType.Knight, resManager.bluePlayer.devCards[0], "Cartea extrasă nu corespunde cu cea din pachet.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(mockPrefab);
        Object.DestroyImmediate(resManager.bluePlayer.devCardContainer.gameObject);
    }
}
#endif