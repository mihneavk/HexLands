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
                // Dacă am dat click pe un colț negru
                if (hit.collider.CompareTag("Corner"))
                {
                    HexCorner corner = hit.collider.GetComponent<HexCorner>();
                    if (!corner.isOccupied)
                    {
                        // 1. Instanțiem casa și păstrăm o referință la ea
                        GameObject newSettlement = Instantiate(settlementPrefab, corner.transform.position, Quaternion.identity);

                        // 2. Cerem sprite-ul corect de la MapGenerator
                        MapGenerator mg = FindObjectOfType<MapGenerator>();
                        newSettlement.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentHouseSprite();

                        // 3. Marcăm colțul ca ocupat
                        corner.BuildSettlement();
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
        }
    }
}