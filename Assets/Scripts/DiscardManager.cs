using UnityEngine;
using System.Collections.Generic;

public class DiscardManager : MonoBehaviour
{
    public int blueCardsToDrop = 0;
    private PlayerResourceManager resManager;
    private MapGenerator mg;

    void Start()
    {
        resManager = FindObjectOfType<PlayerResourceManager>();
        mg = FindObjectOfType<MapGenerator>();
    }

    public void HandleRuleOf7()
    {
        int blueTotal = GetTotalCards(MapGenerator.Player.Blue);
        int orangeTotal = GetTotalCards(MapGenerator.Player.Orange);

        // 1. AI-ul își aruncă surplusul
        if (orangeTotal > 7)
        {
            int orangeToDrop = orangeTotal / 2;
            DiscardForAI(orangeToDrop);
        }

        // 2. Verificăm jucătorul uman
        if (blueTotal > 7)
        {
            blueCardsToDrop = blueTotal / 2;
            UnityEngine.Debug.Log($"<color=red>[Regula lui 7]</color> Jucătorul uman trebuie să arunce {blueCardsToDrop} cărți.");
            // Fereastra GUI va prelua controlul automat acum
        }
        else
        {
            // Dacă omul nu are de aruncat nimic, trecem direct la mutarea hoțului
            mg.StartRobberPhase();
        }
    }

    private int GetTotalCards(MapGenerator.Player player)
    {
        return resManager.GetResourceCount(player, HexData.ResourceType.Wood) +
               resManager.GetResourceCount(player, HexData.ResourceType.Brick) +
               resManager.GetResourceCount(player, HexData.ResourceType.Sheep) +
               resManager.GetResourceCount(player, HexData.ResourceType.Wheat) +
               resManager.GetResourceCount(player, HexData.ResourceType.Ore);
    }

    private void DiscardForAI(int amount)
    {
        UnityEngine.Debug.Log($"<color=orange>[Utility AI]</color> Aoleu, am {GetTotalCards(MapGenerator.Player.Orange)} cărți! Arunc {amount} ca să scap de hoț...");

        for (int i = 0; i < amount; i++)
        {
            // Prioritizează aruncarea: Lână -> Lemn -> Argilă -> Grâu -> Minereu
            if (resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Sheep) > 0) resManager.RemoveResource(MapGenerator.Player.Orange, HexData.ResourceType.Sheep, 1);
            else if (resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Wood) > 0) resManager.RemoveResource(MapGenerator.Player.Orange, HexData.ResourceType.Wood, 1);
            else if (resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Brick) > 0) resManager.RemoveResource(MapGenerator.Player.Orange, HexData.ResourceType.Brick, 1);
            else if (resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Wheat) > 0) resManager.RemoveResource(MapGenerator.Player.Orange, HexData.ResourceType.Wheat, 1);
            else if (resManager.GetResourceCount(MapGenerator.Player.Orange, HexData.ResourceType.Ore) > 0) resManager.RemoveResource(MapGenerator.Player.Orange, HexData.ResourceType.Ore, 1);
        }
    }

    // ==========================================
    // UI PENTRU JUCĂTORUL UMAN (Apare doar dacă trebuie să arunci)
    // ==========================================
    private void OnGUI()
    {
        if (blueCardsToDrop > 0)
        {
            GUIStyle style = new GUIStyle(GUI.skin.window);
            style.fontSize = 16;
            style.fontStyle = FontStyle.Bold;

            Rect windowRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 320);
            GUI.ModalWindow(1, windowRect, DiscardWindow, "A picat 7! Tâlharii atacă!", style);
        }
    }

    private void DiscardWindow(int windowID)
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 18;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Color.red;

        GUI.Label(new Rect(10, 30, 380, 30), $"Mai ai de aruncat: {blueCardsToDrop} cărți", labelStyle);

        DrawResourceButton(HexData.ResourceType.Wood, 70, "Lemn");
        DrawResourceButton(HexData.ResourceType.Brick, 115, "Argilă");
        DrawResourceButton(HexData.ResourceType.Sheep, 160, "Lână");
        DrawResourceButton(HexData.ResourceType.Wheat, 205, "Grâu");
        DrawResourceButton(HexData.ResourceType.Ore, 250, "Minereu");
    }

    private void DrawResourceButton(HexData.ResourceType type, int yPos, string roName)
    {
        int currentAmount = resManager.GetResourceCount(MapGenerator.Player.Blue, type);

        // Butonul e activat doar dacă jucătorul are acea resursă
        GUI.enabled = (currentAmount > 0 && blueCardsToDrop > 0);

        if (GUI.Button(new Rect(50, yPos, 300, 40), $"Aruncă 1 {roName} (Ai: {currentAmount})"))
        {
            resManager.RemoveResource(MapGenerator.Player.Blue, type, 1);
            blueCardsToDrop--;

            // Dacă am terminat de aruncat, închidem meniul și activăm Hoțul!
            if (blueCardsToDrop == 0)
            {
                mg.StartRobberPhase();
            }
        }

        GUI.enabled = true; // Resetăm starea pentru următoarele elemente UI
    }
}