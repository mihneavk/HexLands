#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BankTradeManagerTests
{
    [Test]
    public void GetBestRateForPlayer_ShouldReturnDefaultRate4_WhenPlayerHasNoHarbors()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        BankTradeManager tradeManager = go.AddComponent<BankTradeManager>();
        
        // Cream un colt ocupat de jucator, dar fara port attached
        GameObject cornerGo = new GameObject("Corner");
        HexCorner corner = cornerGo.AddComponent<HexCorner>();
        corner.isOccupied = true;
        corner.owner = MapGenerator.Player.Blue;
        corner.currentHarborType = Harbor.HarborType.None;

        // 2. ACT
        System.Reflection.MethodInfo rateMethod = typeof(BankTradeManager).GetMethod("GetBestRateForPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int rate = (int)rateMethod.Invoke(tradeManager, new object[] { MapGenerator.Player.Blue, HexData.ResourceType.Wood });

        // 3. ASSERT
        Assert.AreEqual(4, rate, "Fără porturi, rata standard de schimb cu banca trebuie să fie obligatoriu 4:1.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(cornerGo);
    }

    [Test]
    public void GetBestRateForPlayer_ShouldReturnRate2_WhenPlayerHasSpecificHarbor()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        BankTradeManager tradeManager = go.AddComponent<BankTradeManager>();
        
        // Creăm un colț cu port specializat de Lemn (Wood 2:1)
        GameObject cornerGo = new GameObject("CornerWithHarbor");
        HexCorner corner = cornerGo.AddComponent<HexCorner>();
        corner.isOccupied = true;
        corner.owner = MapGenerator.Player.Blue;
        corner.currentHarborType = Harbor.HarborType.Wood2to1;

        // 2. ACT
        System.Reflection.MethodInfo rateMethod = typeof(BankTradeManager).GetMethod("GetBestRateForPlayer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        int rateForWood = (int)rateMethod.Invoke(tradeManager, new object[] { MapGenerator.Player.Blue, HexData.ResourceType.Wood });
        int rateForWheat = (int)rateMethod.Invoke(tradeManager, new object[] { MapGenerator.Player.Blue, HexData.ResourceType.Wheat });

        // 3. ASSERT
        Assert.AreEqual(2, rateForWood, "Jucătorul deține port de lemn, deci rata pentru Lemn trebuie să fie 2:1.");
        Assert.AreEqual(4, rateForWheat, "Portul de lemn nu ar trebui să influențeze rata pentru Grău (trebuie să rămână 4:1).");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(cornerGo);
    }

    [Test]
    public void AttemptTrade_ShouldExecuteSuccessfully_WithCorrectResourceDeduction()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        BankTradeManager tradeManager = go.AddComponent<BankTradeManager>();
        GameManager gm = go.AddComponent<GameManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        // Inițializăm portofelul și ne asigurăm că dicționarele interne sunt populate dacă clasa le folosește
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        
        // Apelăm Awake/Start manual prin Reflection dacă e nevoie să se configureze structura internă
        System.Reflection.MethodInfo awakeMethod = typeof(PlayerResourceManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (awakeMethod != null) awakeMethod.Invoke(resManager, null);

        gm.currentPlayer = MapGenerator.Player.Blue;

        // Simulăm componentele de UI necesare introducerii datelor
        GameObject d1 = new GameObject(); tradeManager.giveDropdown = d1.AddComponent<TMP_Dropdown>();
        GameObject d2 = new GameObject(); tradeManager.receiveDropdown = d2.AddComponent<TMP_Dropdown>();
        GameObject i1 = new GameObject(); tradeManager.giveAmountInput = i1.AddComponent<TMP_InputField>();
        GameObject i2 = new GameObject(); tradeManager.receiveAmountInput = i2.AddComponent<TMP_InputField>();

        // Setăm valorile: Schimbăm Lemn (Index 0) pe Cărămiză (Index 1)
        tradeManager.giveDropdown.value = 0; // Wood
        tradeManager.receiveDropdown.value = 1; // Brick
        
        // Rata default fiind 4:1, dăm 4 Lemne ca să primim 1 Cărămidă
        tradeManager.giveAmountInput.text = "4";
        tradeManager.receiveAmountInput.text = "1";

        // IMPORTANT: Folosim metodele oficiale de adăugare ca să fim siguri că resManager le înregistrează corect
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Wood, 4);
        // În caz de siguranță, setăm și variabila directă pentru wallet
        resManager.bluePlayer.wood = 4;
        resManager.bluePlayer.brick = 0;

        // 2. ACT
        tradeManager.AttemptTrade();

        // 3. ASSERT
        int finalWood = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Wood);
        int finalBrick = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Brick);

        Assert.AreEqual(0, finalWood, "Cele 4 lemne trebuiau extrase din inventar.");
        Assert.AreEqual(1, finalBrick, "Jucătorul trebuia să primească exact 1 cărămidă în urma schimbului.");
        Assert.AreEqual("", tradeManager.giveAmountInput.text, "UI-ul trebuia să fie curățat (resetat la string gol) după tranzacție.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(d1);
        Object.DestroyImmediate(d2);
        Object.DestroyImmediate(i1);
        Object.DestroyImmediate(i2);
    }
}
#endif