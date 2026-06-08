#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class BuildUIManagerTests
{
    [Test]
    public void RefreshButtons_ShouldHideAllButtons_DuringSetupPhase()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        BuildUIManager uiManager = go.AddComponent<BuildUIManager>();
        GameManager gm = go.AddComponent<GameManager>();
        uiManager.gameManager = gm;

        // Creăm butoanele fizice pentru test
        GameObject b1 = new GameObject("Btn1"); uiManager.buildSettlementBtn = b1.AddComponent<Button>();
        GameObject b2 = new GameObject("Btn2"); uiManager.buildRoadBtn = b2.AddComponent<Button>();
        GameObject b3 = new GameObject("Btn3"); uiManager.buildCityBtn = b3.AddComponent<Button>();
        GameObject b4 = new GameObject("Btn4"); uiManager.buyDevCardBtn = b4.AddComponent<Button>();

        // Setăm faza de setup
        gm.currentPhase = GameManager.GamePhase.Setup;

        // 2. ACT
        uiManager.RefreshButtons();

        // 3. ASSERT
        Assert.IsFalse(uiManager.buildSettlementBtn.gameObject.activeSelf, "Butonul de așezare ar trebui să fie ascuns în Setup.");
        Assert.IsFalse(uiManager.buildRoadBtn.gameObject.activeSelf, "Butonul de drum ar trebui să fie ascuns în Setup.");
        Assert.IsFalse(uiManager.buildCityBtn.gameObject.activeSelf, "Butonul de oraș ar trebui să fie ascuns în Setup.");
        Assert.IsFalse(uiManager.buyDevCardBtn.gameObject.activeSelf, "Butonul de cărți de dezvoltare ar trebui să fie ascuns în Setup.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(b1);
        Object.DestroyImmediate(b2);
        Object.DestroyImmediate(b3);
        Object.DestroyImmediate(b4);
    }

    public void RefreshButtons_ShouldEnableButtons_WhenPlayerCanAffordThem()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        BuildUIManager uiManager = go.AddComponent<BuildUIManager>();
        GameManager gm = go.AddComponent<GameManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();
        
        uiManager.gameManager = gm;
        uiManager.resourceManager = resManager;

        // CORECTURĂ CRITICĂ: Instanțiem TOATE butoanele pe care clasa le folosește în metoda RefreshButtons
        // Astfel prevenim NullReferenceException pe oricare dintre ele (de exemplu la linia 39)
        GameObject b1 = new GameObject("Btn1"); uiManager.buildSettlementBtn = b1.AddComponent<Button>();
        uiManager.buildSettlementBtn.gameObject.AddComponent<Image>();

        GameObject b2 = new GameObject("Btn2"); uiManager.buildRoadBtn = b2.AddComponent<Button>();
        uiManager.buildRoadBtn.gameObject.AddComponent<Image>();

        GameObject b3 = new GameObject("Btn3"); uiManager.buildCityBtn = b3.AddComponent<Button>();
        uiManager.buildCityBtn.gameObject.AddComponent<Image>();

        GameObject b4 = new GameObject("Btn4"); uiManager.buyDevCardBtn = b4.AddComponent<Button>();
        uiManager.buyDevCardBtn.gameObject.AddComponent<Image>();

        gm.currentPhase = GameManager.GamePhase.Gameplay;
        gm.currentPlayer = MapGenerator.Player.Blue;

        // Inițializăm portofelul jucătorului Blue
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        
        // În caz că clasa are nevoie de inițializarea dicționarelor interne în EditMode
        System.Reflection.MethodInfo awakeMethod = typeof(PlayerResourceManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (awakeMethod != null) awakeMethod.Invoke(resManager, null);

        // Îi dăm resurse suficiente pentru așezare (1 Lemn, 1 Argilă, 1 Grâu, 1 Lână)
        resManager.bluePlayer.wood = 1;
        resManager.bluePlayer.brick = 1;
        resManager.bluePlayer.wheat = 1;
        resManager.bluePlayer.sheep = 1;
        
        // Sincronizăm și prin metoda AddResource în caz că managerul tău ține starea într-un dicționar paralel
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Wood, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Brick, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Wheat, 1);
        resManager.AddResource(MapGenerator.Player.Blue, HexData.ResourceType.Sheep, 1);

        // 2. ACT
        uiManager.RefreshButtons();

        // 3. ASSERT
        Assert.IsTrue(uiManager.buildSettlementBtn.gameObject.activeSelf, "Butonul ar trebui să fie vizibil în Gameplay.");
        Assert.IsTrue(uiManager.buildSettlementBtn.interactable, "Butonul ar trebui să fie interactiv deoarece jucătorul își permite așezarea.");
        Assert.AreEqual(Color.white, uiManager.buildSettlementBtn.image.color, "Culoarea imaginii butonului ar trebui să fie alb complet.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(b1);
        Object.DestroyImmediate(b2);
        Object.DestroyImmediate(b3);
        Object.DestroyImmediate(b4);
    }
}
#endif