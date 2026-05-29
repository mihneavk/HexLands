using UnityEngine;

public class HexEdge : MonoBehaviour
{
    public GameObject previewCircle;
    public GameObject roadSprite;
    public bool isOccupied = false;
    public MapGenerator.Player owner = MapGenerator.Player.None;
    public HexCorner corner1;
    public HexCorner corner2;

    private void Awake()
    {
        if (previewCircle != null) previewCircle.SetActive(false);
        if (roadSprite != null) roadSprite.SetActive(false);

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;
    }

    public void ShowPotentialPath()
    {
        if (!isOccupied)
        {
            this.gameObject.SetActive(true);
            if (previewCircle != null) previewCircle.SetActive(true);
            if (roadSprite != null) roadSprite.SetActive(false);

            if (GetComponent<Collider2D>() != null)
                GetComponent<Collider2D>().enabled = true;
        }
    }

    public void BuildRoad(bool isFree = false)
    {
        isOccupied = true;
        GameManager gm = FindObjectOfType<GameManager>();
        MapGenerator mg = FindObjectOfType<MapGenerator>();

        if (gm != null) this.owner = gm.currentPlayer;

        if (previewCircle != null) previewCircle.SetActive(false);

        if (roadSprite != null && mg != null)
        {
            roadSprite.SetActive(true);
            roadSprite.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentRoadSprite();
            roadSprite.GetComponent<SpriteRenderer>().color = Color.white;
        }

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        if (gm != null)
        {
            gm.CheckLongestRoad(this.owner);
        }
    }
}