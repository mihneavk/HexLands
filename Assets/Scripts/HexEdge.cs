using UnityEngine;

public class HexEdge : MonoBehaviour
{
    public GameObject previewCircle; // Cercul de preview
    public GameObject roadSprite;    // Sprite-ul drumului albastru
    public bool isOccupied = false;

    private void Awake()
    {
        // Când se naște drumul pe hartă, vrem să fie complet invizibil
        if (previewCircle != null) previewCircle.SetActive(false);
        if (roadSprite != null) roadSprite.SetActive(false);

        // Dezactivăm collider-ul inițial ca să nu poți da click 
        // pe un drum unde nu ai încă o casă lângă
        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;
    }

    private void OnMouseDown()
    {
        // Verificăm dacă cercul de preview este activ (adică drumul e disponibil pentru construcție)
        if (previewCircle.activeSelf && !isOccupied)
        {
            BuildRoad();

            // Anunțăm MapGenerator să ascundă restul variantelor
            FindObjectOfType<MapGenerator>().FinishRoadPlacement();
        }
    }

    public void ShowPotentialPath()
    {
        if (!isOccupied)
        {
            // 1. Trezește părintele (cel care e gri în ierarhie)
            this.gameObject.SetActive(true);

            // 2. Pornește vizualul
            if (previewCircle != null) previewCircle.SetActive(true);
            if (roadSprite != null) roadSprite.SetActive(false);

            // 3. Permite click-ul
            if (GetComponent<Collider2D>() != null)
                GetComponent<Collider2D>().enabled = true;
        }
    }

    // În HexEdge.cs

    public void BuildRoad()
    {
        isOccupied = true;
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        if (previewCircle != null) previewCircle.SetActive(false);

        if (roadSprite != null)
        {
            roadSprite.SetActive(true);
            // SCHIMBĂM SPRITE-UL
            roadSprite.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentRoadSprite();
            roadSprite.GetComponent<SpriteRenderer>().color = Color.white;
        }

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        mg.FinishRoadPlacement();
        mg.PrepareNextPlayer();
    }
}