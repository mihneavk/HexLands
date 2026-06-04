#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using TMPro;

public class MonopolyUITests
{
    [Test]
    public void OnConfirmClick_ShouldStealAllChosenResourcesFromOpponent()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        MonopolyUI monopolyUI = go.AddComponent<MonopolyUI>();
        GameManager gm = go.AddComponent<GameManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        // Simulăm dropdown-ul și selectăm indexul 3 (Wheat / Grâu)
        GameObject ddGo = new GameObject();
        monopolyUI.resourceDropdown = ddGo.AddComponent<TMP_Dropdown>();
        monopolyUI.resourceDropdown.value = 3; 

        // Inițializăm portofelele de resurse
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();

        // Jucătorul curent este Blue, oponentul este Orange (care are 4 bobițe de grâu)
        gm.currentPlayer = MapGenerator.Player.Blue;
        resManager.orangePlayer.wheat = 4;
        resManager.bluePlayer.wheat = 0;

        // 2. ACT
        monopolyUI.OnConfirmClick();

        // 3. ASSERT
        Assert.AreEqual(0, resManager.orangePlayer.wheat, "Oponentul ar trebui să rămână cu 0 resurse de tipul ales.");
        Assert.AreEqual(4, resManager.bluePlayer.wheat, "Jucătorul curent ar trebui să primească toate cele 4 resurse furate.");
        Assert.IsFalse(monopolyUI.gameObject.activeSelf, "Fereastra de Monopoly UI ar trebui să se închidă după confirmare.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(ddGo);
    }
}
#endif