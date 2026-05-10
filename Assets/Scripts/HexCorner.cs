using System.Collections.Generic;
using UnityEngine;

public class HexCorner : MonoBehaviour
{
    public bool isOccupied = false;
    public GameObject currentSettlement; // Referință către sprite-ul căsuței

    public List<HexEdge> adjacentEdges = new List<HexEdge>();
    public MapGenerator.Player owner = MapGenerator.Player.None; // Salvăm cine a pus casa

    public GameObject visualHouseObject;

    // În HexCorner.cs

    public void BuildSettlement(MapGenerator.Player player)
    {
        if (isOccupied) return;

        isOccupied = true;
        owner = player;
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

    public bool IsValidForSettlement()
    {
        // 1. Dacă acest colț e deja ocupat, evident că nu se poate
        if (isOccupied) return false;

        // 2. Verificăm toți vecinii direcți prin intermediul drumurilor
        foreach (HexEdge edge in adjacentEdges)
        {
            if (edge != null)
            {
                // Găsim "celălalt" colț de la capătul drumului
                HexCorner neighbor = (edge.corner1 == this) ? edge.corner2 : edge.corner1;

                if (neighbor != null && neighbor.isOccupied)
                {
                    // Am găsit un vecin care are deja casă! Regula de distanță încălcată.
                    return false;
                }
            }
        }

        return true; // Niciun vecin ocupat, e liber la construit!
    }

    public bool HasAdjacentRoadOfPlayer(MapGenerator.Player player)
    {
        // Găsim managerul hărții pentru a accesa lista de muchii
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        if (mg == null) return false;

        // Trecem prin toate drumurile de pe hartă
        foreach (HexEdge edge in mg.allEdges)
        {
            // Verificăm dacă drumul este construit și aparține jucătorului curent
            if (edge.isOccupied && edge.owner == player)
            {
                // Verificăm dacă unul dintre capetele drumului atinge ACEST colț
                if (edge.corner1 == this || edge.corner2 == this)
                {
                    return true; // Am găsit un drum conectat!
                }
            }
        }

        return false; // Nu am găsit niciun drum conectat
    }

    public bool isCity = false;

    public void UpgradeToCity()
    {
        isCity = true;
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        if (visualHouseObject != null)
        {
            SpriteRenderer sr = visualHouseObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Debug.LogWarning("Cum trebuia");
                sr.sprite = mg.GetCurrentCitySprite();
            }
        }
        else
        {
            Debug.LogWarning("Am facut fallback");
            // Fallback: dacă nu avem obiect separat, încercăm pe cel actual
            GetComponent<SpriteRenderer>().sprite = mg.GetCurrentCitySprite();
        }
        GetComponent<SpriteRenderer>().enabled = false;
        Debug.Log($"Settlement-ul de la {gameObject.name} a devenit ORAȘ!");
    }
}