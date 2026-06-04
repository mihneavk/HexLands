#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using TMPro;

public class YearOfPlentyUITests
{
    [Test]
    public void OnConfirmClick_ShouldAddCorrectResources_AndClosePanel()
    {
        // 1. ARRANGE (Pregătire)
        // Creăm un obiect temporar în memorie pentru a găzdui scriptul de testat
        GameObject go = new GameObject();
        YearOfPlentyUI yopUI = go.AddComponent<YearOfPlentyUI>();

        // Pentru că scriptul tău folosește FindObjectOfType, trebuie să injectăm 
        // aceste componente pe același obiect (sau în scenă) ca să fie găsite la rulare
        GameManager gm = go.AddComponent<GameManager>();
        PlayerResourceManager resManager = go.AddComponent<PlayerResourceManager>();

        // Simulăm dropdown-urile din Unity UI (TextMeshPro)
        GameObject dd1Go = new GameObject();
        GameObject dd2Go = new GameObject();
        yopUI.dropdown1 = dd1Go.AddComponent<TMP_Dropdown>();
        yopUI.dropdown2 = dd2Go.AddComponent<TMP_Dropdown>();

        // Setăm alegerile simulate ale jucătorului:
        // Index 0 = Wood (Lemn)
        // Index 4 = Ore (Minereu)
        yopUI.dropdown1.value = 0;
        yopUI.dropdown2.value = 4;

        // Setăm jucătorul curent pe Albastru
        gm.currentPlayer = MapGenerator.Player.Blue;

        // 2. ACT (Execuție)
        // Apelăm metoda pe care o testăm, exact cum ar face-o butonul de Confirm din joc
        yopUI.OnConfirmClick();

        // 3. ASSERT (Verificare)
        // Verificăm dacă în inventarul resManager s-au adăugat resursele corecte
        // Notă: Înlocuiește metodele de mai jos cu cele reale din proiectul vostru dacă diferă numele (ex: GetResource)
        int woodCount = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Wood);
        int oreCount = resManager.GetResourceCount(MapGenerator.Player.Blue, HexData.ResourceType.Ore);

        Assert.AreEqual(1, woodCount, "Eroare: Dropdown index 0 ar fi trebuit să adauge exact 1 Wood!");
        Assert.AreEqual(1, oreCount, "Eroare: Dropdown index 4 ar fi trebuit să adauge exact 1 Ore!");

        // Verificăm dacă panoul de UI s-a închis automat după ce s-au dat resursele
        Assert.IsFalse(yopUI.gameObject.activeSelf, "Eroare: Panelul YearOfPlentyUI ar fi trebuit să se dezactiveze după confirmare!");

        // CLEAN UP (Curățenie în memorie)
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(dd1Go);
        Object.DestroyImmediate(dd2Go);
    }
}
#endif