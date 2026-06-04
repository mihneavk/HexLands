#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

public class DiceControllerTests
{
    [Test]
    public void ResetDice_ShouldClearHasRolledFlag_AndRestoreFullAlpha()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DiceController controller = go.AddComponent<DiceController>();

        // Creăm obiecte pentru sprite-urile zarurilor
        GameObject d1Go = new GameObject();
        GameObject d2Go = new GameObject();
        controller.dice1SR = d1Go.AddComponent<SpriteRenderer>();
        controller.dice2SR = d2Go.AddComponent<SpriteRenderer>();

        // Forțăm o stare modificată (zaruri deja aruncate și estompate vizual)
        controller.hasRolled = true;
        controller.dice1SR.color = new Color(1f, 1f, 1f, 0.5f);
        controller.dice2SR.color = new Color(1f, 1f, 1f, 0.5f);

        // 2. ACT
        controller.ResetDice();

        // 3. ASSERT
        Assert.IsFalse(controller.hasRolled, "Flag-ul hasRolled ar trebui să fie resetat la false.");
        Assert.AreEqual(Color.white, controller.dice1SR.color, "Zarul 1 ar trebui să revină la opacitate maximă (Color.white).");
        Assert.AreEqual(Color.white, controller.dice2SR.color, "Zarul 2 ar trebui să revină la opacitate maximă (Color.white).");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(d1Go);
        Object.DestroyImmediate(d2Go);
    }

    // CORECTURĂ CRITICĂ: Am schimbat din [Test] în [UnityTest] deoarece testul returnează un IEnumerator (Corutină)
    [UnityTest]
    public IEnumerator RollAnimation_ShouldCalculateTotalResult_AndBlockFurtherRolls()
    {
        // 1. ARRANGE
        GameObject go = new GameObject();
        DiceController controller = go.AddComponent<DiceController>();
        GameManager gm = go.AddComponent<GameManager>();
        MapGenerator mg = go.AddComponent<MapGenerator>();

        GameObject d1Go = new GameObject();
        GameObject d2Go = new GameObject();
        controller.dice1SR = d1Go.AddComponent<SpriteRenderer>();
        controller.dice2SR = d2Go.AddComponent<SpriteRenderer>();

        // În caz că DiceController caută o sursă audio la aruncare ca să nu dea NullReferenceException
        if (go.GetComponent<AudioSource>() == null)
        {
            go.AddComponent<AudioSource>();
        }

        // Generăm un set de sprite-uri fictive pentru a evita erorile de index din animație
        controller.diceSprites = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            controller.diceSprites[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
        }

        controller.hasRolled = false;
        controller.canRoll = true;

        // 2. ACT
        // Pornim corutina manual folosind containerul de test Unity
        yield return controller.StartCoroutine("RollAnimation");

        // 3. ASSERT
        Assert.IsTrue(controller.hasRolled, "Controlerul ar trebui să blocheze aruncările repetate în timpul sau după animație.");
        Assert.IsTrue(gm.hasRolled, "GameManager-ul ar trebui să fie notificat că zarurile au fost aruncate.");
        Assert.GreaterOrEqual(controller.lastResult, 2, "Rezultatul minim obținut cu două zaruri nu poate fi mai mic de 2.");
        Assert.LessOrEqual(controller.lastResult, 12, "Rezultatul maxim obținut cu două zaruri nu poate depăși valoarea 12.");

        // CLEAN UP
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(d1Go);
        Object.DestroyImmediate(d2Go);
    }
}
#endif