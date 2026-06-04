#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SettlementPlacerTests
{
    [Test]
    public void ActivateRoadBuildingCard_ShouldSetTwoRemainingRoads_AndChangeModeToPlacingRoad()
    {
        // 1. ARRANGE (Pregătire)
        GameObject go = new GameObject();
        SettlementPlacer placer = go.AddComponent<SettlementPlacer>();
        
        // Simulăm componentele minime din scenă pentru a evita erorile în caz că 
        // SetVisualsVisibility() încearcă să caute GameManager sau MapGenerator prin FindObjectOfType
        go.AddComponent<GameManager>();
        go.AddComponent<MapGenerator>();

        // Inițial, ne asigurăm că starea este neutră
        placer.currentMode = SettlementPlacer.BuildMode.None;
        placer.roadsRemainingFromCard = 0;

        // 2. ACT (Execuție)
        placer.ActivateRoadBuildingCard();

        // 3. ASSERT (Verificare)
        // Verificăm dacă s-au alocat cele 2 drumuri gratuite din cartea de dezvoltare
        Assert.AreEqual(2, placer.roadsRemainingFromCard, 
            "Eroare: Cartea de Road Building trebuia să ofere exact 2 drumuri rămase!");
        
        // Verificăm dacă jocul a intrat în modul de plasare a drumului
        Assert.AreEqual(SettlementPlacer.BuildMode.PlacingRoad, placer.currentMode, 
            "Eroare: Modul curent de construcție trebuia să fie 'PlacingRoad'!");

        // CLEAN UP (Curățenie în memorie)
        Object.DestroyImmediate(go);
    }
}
#endif