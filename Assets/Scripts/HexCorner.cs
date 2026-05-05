using System.Collections.Generic;
using UnityEngine;

public class HexCorner : MonoBehaviour
{
    public bool isOccupied = false;
    public GameObject currentSettlement; // Referință către sprite-ul căsuței

    public List<HexEdge> adjacentEdges = new List<HexEdge>();

    // În HexCorner.cs

    public void BuildSettlement()
    {
        if (isOccupied) return;

        isOccupied = true;
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        SpriteRenderer houseSR = GetComponent<SpriteRenderer>();

        if (houseSR != null)
        {
            // SCHIMBĂM SPRITE-UL, nu culoarea
            houseSR.sprite = mg.GetCurrentHouseSprite();
            houseSR.enabled = true;

            // Resetăm culoarea la alb (în caz că a rămas vreo tentă de la teste)
            houseSR.color = Color.white;
        }

        mg.HideAllPotentialCorners();

        foreach (HexEdge edge in adjacentEdges)
        {
            if (edge != null) edge.ShowPotentialPath();
        }
    }

    public Harbor.HarborType currentHarborType = Harbor.HarborType.None; 
    public int tradeRatio = 4; 

    public void GiveHarborBonus(Harbor.HarborType type)
    {
        currentHarborType = type;

        if (type == Harbor.HarborType.Generic3to1)
            tradeRatio = 3;
        else
            tradeRatio = 2; // Porturile specifice sunt 2:1

        Debug.Log($"Colțul {gameObject.name} a primit bonus de port: {type}");
    }
}