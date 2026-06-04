#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class DevCardItemTests
{
    [Test]
    public void OnMouseDown_ShouldTriggerUseCard_InDevCardManager()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DevCardManager devManager = go.AddComponent<DevCardManager>();
        GameManager gm = go.AddComponent<GameManager>();
        MapGenerator mapGen = go.AddComponent<MapGenerator>(); // Adăugat MapGenerator pentru a preveni erori la mutarea hoțului
        
        // Configurăm un partener de test minimal pentru a preveni erori în lanț
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();
        resManager.bluePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.orangePlayer = new PlayerResourceManager.ResourceWallet();
        resManager.bluePlayer.devCardContainer = new GameObject("Container").transform;

        // Pregătim elementul fizic de carte de dezvoltare
        GameObject cardGo = new GameObject("TestCard");
        DevCardItem cardItem = cardGo.AddComponent<DevCardItem>();
        cardItem.cardType = DevCardManager.DevCardType.Knight;
        cardItem.owner = MapGenerator.Player.Blue;

        // Adăugăm cartea în inventarul jucătorului pentru ca metoda UseCard să o poată șterge
        resManager.bluePlayer.devCards.Add(DevCardManager.DevCardType.Knight);

        // Setăm GameManager-ul pe tura jucătorului corect
        gm.currentPlayer = MapGenerator.Player.Blue;

        // 2. ACT
        // Simulăm declanșarea evenimentului fizic de click de la Unity (OnMouseDown) via Reflection
        System.Reflection.MethodInfo mouseDownMethod = typeof(DevCardItem).GetMethod("OnMouseDown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        // Protejăm invocarea: chiar dacă logica vizuală ulterioară (PlayKnight/Mută hoțul) caută UI și dă Null,
        // pe noi ne interesează dacă evenimentul s-a declanșat și a procesat eliminarea cărții.
        try
        {
            mouseDownMethod.Invoke(cardItem, null);
        }
        catch (System.Reflection.TargetInvocationException ex)
        {
            // Prindem eroarea de UI de la hoț ca să lăsăm testul să meargă la ASSERT
            Debug.Log("OnMouseDown a declanșat cu succes logica, ignorăm erorile vizuale de rețele/UI: " + ex.InnerException.Message);
        }

        // 3. ASSERT
        // Indiferent dacă UI-ul hoțului a crăpat, verificăm dacă UseCard a fost apelat și a scăzut cartea din inventar
        Assert.AreEqual(0, resManager.bluePlayer.devCards.Count, "Cartea ar fi trebuit să fie eliminată din inventar în urma executării click-ului.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(cardGo);
        Object.DestroyImmediate(resManager.bluePlayer.devCardContainer.gameObject);
    }
}
#endif