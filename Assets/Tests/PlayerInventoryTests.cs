#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using TMPro;

public class PlayerInventoryTests
{
    [Test]
    public void PlayerInventory_AddResource_ShouldModifyCount_AndNotDropBelowZero()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        PlayerInventory inventory = go.AddComponent<PlayerInventory>();

        // CORECTURĂ: Creăm câte un GameObject separat pentru fiecare componentă de text ca să evităm regula Unity "only one Graphic component"
        GameObject t1 = new GameObject("WoodText"); inventory.woodText = t1.AddComponent<TextMeshProUGUI>();
        GameObject t2 = new GameObject("BrickText"); inventory.brickText = t2.AddComponent<TextMeshProUGUI>();
        GameObject t3 = new GameObject("SheepText"); inventory.sheepText = t3.AddComponent<TextMeshProUGUI>();
        GameObject t4 = new GameObject("WheatText"); inventory.wheatText = t4.AddComponent<TextMeshProUGUI>();
        GameObject t5 = new GameObject("OreText"); inventory.oreText = t5.AddComponent<TextMeshProUGUI>();

        // Apelăm manual o metodă de inițializare reflectată din Start pentru a popula dicționarul privat
        System.Reflection.MethodInfo startMethod = typeof(PlayerInventory).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (startMethod != null) startMethod.Invoke(inventory, null);

        // 2. ACT & ASSERT 1 (Adăugare resurse)
        inventory.AddResource(HexData.ResourceType.Wood, 5);
        Assert.AreEqual(5, inventory.GetResourceCount(HexData.ResourceType.Wood), "Resursa Wood ar trebui să fie 5 după adăugare.");

        // 3. ACT & ASSERT 2 (Scădere sub zero)
        inventory.AddResource(HexData.ResourceType.Wood, -10);
        Assert.AreEqual(0, inventory.GetResourceCount(HexData.ResourceType.Wood), "Resursa Wood nu ar trebui să scadă sub 0.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(t1);
        Object.DestroyImmediate(t2);
        Object.DestroyImmediate(t3);
        Object.DestroyImmediate(t4);
        Object.DestroyImmediate(t5);
    }
}
#endif