using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

public class HexEdge : MonoBehaviour
{
    public GameObject previewCircle; // Cercul de preview
    public GameObject roadSprite;    // Sprite-ul drumului albastru
    public bool isOccupied = false;
    public MapGenerator.Player owner = MapGenerator.Player.None;
    public HexCorner corner1;
    public HexCorner corner2;

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
        if (previewCircle.activeSelf && !isOccupied)
        {
            SettlementPlacer placer = FindObjectOfType<SettlementPlacer>();
            GameManager gm = FindObjectOfType<GameManager>();

            if (placer != null && placer.roadsRemainingFromCard > 0)
            {
                Debug.LogWarning("HexEdge: Construiesc drum GRATIS din OnMouseDown");
                BuildRoad(true);
                placer.roadsRemainingFromCard--;

                if (placer.roadsRemainingFromCard == 0)
                {
                    // S-au terminat drumurile de la carte
                    placer.SetVisualsVisibility();
                    gm.OnRoadFinished();

                    // ABIA ACUM închidem tot procesul vizual de pe hartă
                    FindObjectOfType<MapGenerator>().FinishRoadPlacement();
                }
                else
                {
                    Debug.Log("HexEdge: Mai ai un drum de pus. Refresh la vizualuri.");

                    // Rămânem în build mode
                    placer.currentMode = SettlementPlacer.BuildMode.PlacingRoad;

                    // RECALCULĂM punctele unde se pot pune drumuri. 
                    // Important: acum se va activa și preview-ul de lângă drumul tocmai pus!
                    placer.SetVisualsVisibility();

                    // NU chemăm FinishRoadPlacement aici, pentru că vrem să vedem cercurile în continuare
                }
            }
            else
            {
                // CAZ NORMAL (sau Setup)
                Debug.LogWarning("HexEdge: Construiesc drum NORMAL");
                BuildRoad(false);

                if (placer != null)
                {
                    // Nu resetăm currentMode (pentru flow-ul tău de Setup)
                    placer.SetVisualsVisibility();
                }

                // Aici probabil vrei să închizi vizualul dacă nu mai ești în Setup
                if (gm.currentPhase != GameManager.GamePhase.Setup)
                {
                    FindObjectOfType<MapGenerator>().FinishRoadPlacement();
                }
            }
        }
    }

    public void SetPreviewActive(bool state)
    {
        if (!isOccupied && previewCircle != null)
        {
            previewCircle.SetActive(state);
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

    public bool IsValidForPlayer(MapGenerator.Player player)
    {
        // Regula 1: E deja ocupat? Nu e valid.
        if (isOccupied) return false;

        // Regula 2: Atinge o casă de-a jucătorului?
        if ((corner1 != null && corner1.isOccupied && corner1.owner == player) ||
            (corner2 != null && corner2.isOccupied && corner2.owner == player))
        {
            return true;
        }

        // Regula 3: Atinge un alt drum de-al jucătorului?
        // (Pentru asta ai nevoie de o metodă care verifică muchiile adiacente. 
        // O variantă simplă este să cauți în toate muchiile dacă ating aceleași colțuri și aparțin jucătorului).

        MapGenerator mg = FindObjectOfType<MapGenerator>();
        foreach (HexEdge alteDrum in mg.allEdges)
        {
            if (alteDrum.isOccupied && alteDrum.owner == player)
            {
                // Dacă celălalt drum are UNUL din colțurile comune cu acest drum
                if (alteDrum.corner1 == this.corner1 || alteDrum.corner1 == this.corner2 ||
                    alteDrum.corner2 == this.corner1 || alteDrum.corner2 == this.corner2)
                {
                    // ATENȚIE: În mod normal trebuie să verifici dacă nu există o casă inamică 
                    // pe colțul comun care să taie drumul!
                    return true;
                }
            }
        }

        return false; // Nu atingem nimic valid
    }

    // În HexEdge.cs

    public void BuildRoad(bool isFree = false)
    {
        Debug.LogWarning("AM DAT BUILD LA ROAD");
        if(isFree)
        {
            Debug.LogWarning("DA, ESTE MOCA");
        }
        else
        {
            Debug.LogWarning("NU A MERS CUM TREBUIE");
        }
        isOccupied = true;
        MapGenerator mg = FindObjectOfType<MapGenerator>();
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        // 1. Setăm owner-ul (va fi cel corect, pentru că rândul nu s-a schimbat încă)
        this.owner = gm.currentPlayer;

        if (previewCircle != null) previewCircle.SetActive(false);

        if (roadSprite != null)
        {
            roadSprite.SetActive(true);
            // 2. Luăm sprite-ul corect pentru jucătorul care tocmai a pus casa
            roadSprite.GetComponent<SpriteRenderer>().sprite = mg.GetCurrentRoadSprite();
            roadSprite.GetComponent<SpriteRenderer>().color = Color.white;
        }

        if (GetComponent<Collider2D>() != null)
            GetComponent<Collider2D>().enabled = false;

        if (gm.currentPhase != GameManager.GamePhase.Setup && !isFree)
        {
            resManager.SpendForRoad(gm.currentPlayer);
            FindObjectOfType<BuildUIManager>().RefreshButtons();
        }

        gm.CheckLongestRoad(this.owner);

        // Atenție: Dacă e Road Building, s-ar putea să nu vrei să apelezi OnRoadFinished imediat
        // deoarece jucătorul mai are un drum de pus în aceeași "construcție".
        if (!isFree)
        {
            gm.OnRoadFinished();
        }

        // 4. (Opțional) Șterge mg.PrepareNextPlayer() de aici dacă GameManager se ocupă de rânduri
    }
}