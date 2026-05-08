using UnityEngine;

public class SettlementPlacer : MonoBehaviour
{
    public GameObject settlementPrefab; // Sprite-ul tău cu căsuța

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
                MapGenerator mg = FindObjectOfType<MapGenerator>();
                // Dacă am dat click pe un colț negru
                if (hit.collider.CompareTag("Corner"))
                {
                    HexCorner corner = hit.collider.GetComponent<HexCorner>();
                    if (!corner.isOccupied)
                    {
                        // 1. Instanțiem casa și păstrăm o referință la ea
                        GameObject newSettlement = Instantiate(settlementPrefab, corner.transform.position, Quaternion.identity);

                        // 2. Cerem sprite-ul corect de la MapGenerator
                        newSettlement.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();

                        // 3. Marcăm colțul ca ocupat
                        corner.BuildSettlement(mg.currentPlayer);
                    }
                    if (corner.IsValidForSettlement())
                    {
                        corner.BuildSettlement(mg.currentPlayer);
                    }
                    else
                    {
                        Debug.Log("Prea aproape de altă casă! Regula de distanță nu permite.");
                        // Opțional: Poți pune un sunet de "eroare" aici
                    }
                }
                // Dacă am dat click pe un cerc de drum (Preview)
                else if (hit.collider.CompareTag("Edge"))
                {
                    HexEdge edge = hit.collider.GetComponent<HexEdge>();
                    if (!edge.isOccupied)
                    {
                        edge.BuildRoad();
                        // Opțional: aici poți reactiva colțurile dacă vrei să pui altă casă
                    }
                }
            }

            if (hit.collider != null)
            {
                MapGenerator mg = FindObjectOfType<MapGenerator>();

                // VERIFICARE: Dacă suntem în faza de mutare hoț
                if (mg.isMovingRobber && hit.collider.CompareTag("Hexagon"))
                {
                    HexData hex = hit.collider.GetComponent<HexData>();
                    if (hex != null)
                    {
                        mg.MoveRobberToHex(hex);
                    }
                    return; // Oprim execuția aici ca să nu punem case din greșeală
                }

                // ... restul logicii pentru Corner și Edge ...
            }
        }
    }
}