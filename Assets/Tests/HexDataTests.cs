#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections;

public class HexDataTests
{
    [Test]
    public void Initialize_ShouldSetCoordinatesAndType_AndUpdateVisuals()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        HexData hex = go.AddComponent<HexData>();

        // Setăm un SpriteRenderer simulat pentru a nu arunca NullReferenceException în UpdateVisuals
        GameObject visualGo = new GameObject();
        SpriteRenderer sr = visualGo.AddComponent<SpriteRenderer>();
        
        // Injectăm componenta privată prin Reflection
        System.Reflection.FieldInfo srField = typeof(HexData).GetField("spriteRenderer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        srField.SetValue(hex, sr);

        // Creăm o librărie vizuală minimă pentru test
        hex.visualLibrary = new HexData.ResourceVisual[1];
        hex.visualLibrary[0] = new HexData.ResourceVisual 
        { 
            type = HexData.ResourceType.Wood, 
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero) 
        };

        // 2. ACT
        hex.Initialize(2, -1, HexData.ResourceType.Wood, 8);

        // 3. ASSERT
        Assert.AreEqual(2, hex.Q, "Coordonata axială Q nu a fost inițializată corect.");
        Assert.AreEqual(-1, hex.R, "Coordonata axială R nu a fost inițializată corect.");
        Assert.AreEqual(HexData.ResourceType.Wood, hex.resourceType, "Tipul de resursă nu a fost setat corect.");
        Assert.AreEqual(8, hex.tokenNumber, "Numărul jetonului de producție nu a fost setat corect.");
        Assert.AreEqual(hex.visualLibrary[0].sprite, sr.sprite, "Sprite-ul nu a fost actualizat corect în funcție de resursă.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(visualGo);
    }

    [Test]
    public void SetRobberStatus_ShouldModifyHasRobberFlag()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        HexData hex = go.AddComponent<HexData>();
        hex.hasRobber = false;

        // 2. ACT
        hex.SetRobberStatus(true);

        // 3. ASSERT
        Assert.IsTrue(hex.hasRobber, "Statusul hoțului (hasRobber) ar trebui să fie true după apelarea metodei.");

        // CLEAN UP
        Object.DestroyImmediate(go);
    }
}
#endif