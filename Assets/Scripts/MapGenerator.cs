// Fișier: MapGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MapGenerator : MonoBehaviour
{
    public enum Player { Blue, Orange }
    public Player currentPlayer = Player.Blue;

    [Header("Sprite-uri Jucători")]
    public Sprite blueHouse;
    public Sprite orangeHouse;
    public Sprite blueRoad;
    public Sprite orangeRoad;

    public Sprite GetCurrentHouseSprite()
    {
        return currentPlayer == Player.Blue ? blueHouse : orangeHouse;
    }

    public Sprite GetCurrentRoadSprite()
    {
        return currentPlayer == Player.Blue ? blueRoad : orangeRoad;
    }

    public void PrepareNextPlayer()
    {
        if (currentPlayer == Player.Blue) currentPlayer = Player.Orange;
        else currentPlayer = Player.Blue;

        Debug.Log("Este rândul jucătorului: " + currentPlayer);

        // Reactivăm toate punctele negre (colțurile) care nu sunt ocupate
        UpdateValidCorners();
    }

    [Header("Setup Visual")]
    public GameObject hexPrefab; // Aici tragi prefab-ul făcut la Pasul 1
    public float hexRadius = 1f; // Distanța de la centru la un colț

    [Header("Structura Hărții")]
    public int mapRadius = 2; // Catan standard are rază 2 (centru + 2 inele)

    // O listă pentru a ține evidența obiectelor create (pentru debug/curățare)
    private List<GameObject> generatedHexes = new List<GameObject>();

    private List<HexData.ResourceType> resourcePool;
    private List<int> numberPool;

    private static readonly Vector2Int[] NeighborDirections = {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    private void PreparePools()
    {
        // 1. Pregătim cele 19 resurse
        resourcePool = new List<HexData.ResourceType> {
            HexData.ResourceType.Desert,
            HexData.ResourceType.Brick, HexData.ResourceType.Brick, HexData.ResourceType.Brick,
            HexData.ResourceType.Ore, HexData.ResourceType.Ore, HexData.ResourceType.Ore,
            HexData.ResourceType.Wood, HexData.ResourceType.Wood, HexData.ResourceType.Wood, HexData.ResourceType.Wood,
            HexData.ResourceType.Wheat, HexData.ResourceType.Wheat, HexData.ResourceType.Wheat, HexData.ResourceType.Wheat,
            HexData.ResourceType.Sheep, HexData.ResourceType.Sheep, HexData.ResourceType.Sheep, HexData.ResourceType.Sheep
        };

        // 2. Pregătim cele 18 numere (Deșertul nu primește număr)
        numberPool = new List<int> { 2, 3, 3, 4, 4, 5, 5, 6, 6, 8, 8, 9, 9, 10, 10, 11, 11, 12 };

        // 3. Amestecăm listele (Shuffle)
        Shuffle(resourcePool);
        Shuffle(numberPool);
    }

    // Algoritm simplu de amestecare (Fisher-Yates)
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    [ContextMenu("Generează Harta")]
    public void GenerateMap()
    {
        bool mapIsValid = false;
        int safetyBreak = 0;

        while (!mapIsValid && safetyBreak < 100) // Încercăm până e valid sau atingem limita
        {
            safetyBreak++;
            ClearMap();
            PreparePools();

            // Generăm harta (ca în pasul anterior)
            TryPlaceTiles();

            // Verificăm regula de 6/8
            mapIsValid = ValidateCatanRules();
        }

        if (safetyBreak >= 100) Debug.LogWarning("Nu s-a putut genera o hartă validă după 100 încercări.");
        InitializeHarbors();
    }

    private bool ValidateCatanRules()
    {
        // Luăm toate scripturile HexData din hexagoanele generate
        HexData[] allHexes = generatedHexes.Select(g => g.GetComponent<HexData>()).ToArray();

        foreach (var hex in allHexes)
        {
            // Dacă hexagonul are un număr "roșu" (6 sau 8)
            if (hex.tokenNumber == 6 || hex.tokenNumber == 8)
            {
                // Verificăm vecinii lui
                foreach (var dir in NeighborDirections)
                {
                    // Calculăm coordonatele vecinului presupus
                    int neighborQ = hex.Q + dir.x; // Presupunem că adaugi Q și R în HexData
                    int neighborR = hex.R + dir.y;

                    // Căutăm vecinul în lista de hexagoane generate
                    var neighbor = allHexes.FirstOrDefault(h => h.Q == neighborQ && h.R == neighborR);

                    if (neighbor != null)
                    {
                        // DACĂ vecinul are și el 6 sau 8 -> Harta e invalidă!
                        if (neighbor.tokenNumber == 6 || neighbor.tokenNumber == 8)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true; // Nu s-au găsit conflicte
    }

    // Funcție helper pentru a plasa tile-urile
    private void TryPlaceTiles()
    {
        int resourceIndex = 0;
        int numberIndex = 0;

        for (int q = -mapRadius; q <= mapRadius; q++)
        {
            int rStart = Mathf.Max(-mapRadius, -q - mapRadius);
            int rEnd = Mathf.Min(mapRadius, -q + mapRadius);

            for (int r = rStart; r <= rEnd; r++)
            {
                HexData.ResourceType type = resourcePool[resourceIndex++];
                int number = (type != HexData.ResourceType.Desert) ? numberPool[numberIndex++] : 0;
                CreateHex(q, r, type, number);
            }
        }
    }

    private void CreateHex(int q, int r, HexData.ResourceType type, int number)
    {
        Vector3 worldPosition = HexCoordinates.AxialToWorld(q, r, hexRadius);
        worldPosition += transform.position;

        GameObject hexGo = Instantiate(hexPrefab, worldPosition, Quaternion.identity);
        hexGo.transform.SetParent(this.transform);

        HexData data = hexGo.GetComponent<HexData>();
        if (data != null) data.Initialize(q, r, type, number);

        // Ordinea corectă: întâi colțurile, apoi drumurile
        List<GameObject> corners = CreateCornersForHex(worldPosition);
        CreateEdges(corners);

        generatedHexes.Add(hexGo);
    }

    private void ClearMap()
    {
        // Când jocul rulează, folosim Destroy. Când suntem în editor (ContextMenu), folosim DestroyImmediate.
        bool isPlaying = Application.isPlaying;

        foreach (var hex in generatedHexes)
        {
            if (hex != null) { if (isPlaying) Destroy(hex); else DestroyImmediate(hex); }
        }
        generatedHexes.Clear();

        foreach (var corner in uniqueCorners.Values)
        {
            if (corner != null) { if (isPlaying) Destroy(corner); else DestroyImmediate(corner); }
        }
        uniqueCorners.Clear(); // ACEASTA ESTE LINIA CARE LIPSEA SAU NU SE EXECUTA

        foreach (var edge in uniqueEdges.Values)
        {
            if (edge != null) { if (isPlaying) Destroy(edge); else DestroyImmediate(edge); }
        }
        uniqueEdges.Clear(); // LA FEL ȘI AICI
    }

    public void Start()
    {
        GenerateMap();
    }

    [Header("Settlement Setup")]
    public GameObject cornerPrefab; // Un cerc mic, invizibil sau un punct
    private Dictionary<Vector2, GameObject> uniqueCorners = new Dictionary<Vector2, GameObject>();
    private List<GameObject> CreateCornersForHex(Vector3 hexCenter)
    {
        List<GameObject> hexCorners = new List<GameObject>();

        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i + 30;
            float angle_rad = Mathf.PI / 180 * angle_deg;

            Vector3 cornerPos = new Vector3(
                hexCenter.x + hexRadius * Mathf.Cos(angle_rad),
                hexCenter.y + hexRadius * Mathf.Sin(angle_rad),
                -1f // Setăm Z-ul aici pentru a fi în fața hexagonului
            );

            Vector3 roundedPos = new Vector3(
            Mathf.Round(cornerPos.x * 10f) / 10f,
            Mathf.Round(cornerPos.y * 10f) / 10f,
            -1f);

            GameObject cornerObj;

            if (!uniqueCorners.ContainsKey(roundedPos))
            {
                cornerObj = Instantiate(cornerPrefab, roundedPos, Quaternion.identity);
                cornerObj.transform.SetParent(this.transform);
                uniqueCorners.Add(roundedPos, cornerObj);
            }
            else
            {
                cornerObj = uniqueCorners[roundedPos];
                // IMPORTANT: Dacă refolosim colțul, îi ștergem lista veche de drumuri!
            }

            hexCorners.Add(cornerObj);
        }

        return hexCorners; // Returnăm lista de 6 colțuri
    }

    public void HideAllPotentialCorners()
    {
        foreach (var cornerObj in uniqueCorners.Values)
        {
            // Dezactivăm toate punctele negre de pe hartă
            cornerObj.SetActive(false);
        }
    }

    private Vector2 GetSnappedPos(Vector3 pos)
    {
        // Rotunjim la 2 zecimale pentru a asigura suprapunerea perfectă
        // Returnăm Vector2 pentru a ignora axa Z în căutările din Dictionary
        return new Vector2(
            Mathf.Round(pos.x * 100f) / 100f,
            Mathf.Round(pos.y * 100f) / 100f
        );
    }

    [Header("Road Setup")]
    public GameObject edgePrefab; // Un cerc de altă culoare sau o linie mică
    private Dictionary<Vector2, GameObject> uniqueEdges = new Dictionary<Vector2, GameObject>();
    private void CreateEdges(List<GameObject> cornerObjects)
    {
        for (int i = 0; i < 6; i++)
        {
            int next = (i + 1) % 6;

            // Luăm pozițiile colțurilor între care se va afla drumul
            Vector3 posA = cornerObjects[i].transform.position;
            Vector3 posB = cornerObjects[next].transform.position;

            // 1. Calculăm punctul de mijloc și cheia de rotunjire (Snap)
            Vector3 midPoint = (posA + posB) / 2f;
            // Folosim funcția GetSnappedPos pentru a evita duplicatele la nivel de zecimale
            Vector2 keyPos = GetSnappedPos(midPoint);

            GameObject edgeObj;

            // 2. Gestionarea instanțierii (verificăm dacă drumul a fost creat de un alt hex)
            if (!uniqueEdges.ContainsKey(keyPos))
            {
                // Calculăm rotația: direcția vectorului între cele două puncte
                Vector3 direction = posB - posA;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

                // Aplicăm corecția de -90 de grade pentru sprite-ul tău vertical
                Quaternion rotation = Quaternion.Euler(0, 0, angle - 90f);

                // Instanțiem prefab-ul drumului
                edgeObj = Instantiate(edgePrefab, midPoint, rotation);
                edgeObj.transform.SetParent(this.transform);
                edgeObj.name = $"Edge_{keyPos.x}_{keyPos.y}";

                // Adăugăm în dicționar
                uniqueEdges.Add(keyPos, edgeObj);
            }
            else
            {
                // Dacă drumul există deja, îl recuperăm pe cel din dicționar
                edgeObj = uniqueEdges[keyPos];
            }

            // 3. LOGICA DE CONECTARE (Elementul cheie pentru cele 3 drumuri/colț)
            // Această parte rulează de fiecare dată, pentru fiecare hexagon care atinge acest drum
            HexEdge edgeScript = edgeObj.GetComponent<HexEdge>();
            if (edgeScript != null)
            {
                HexCorner scriptA = cornerObjects[i].GetComponent<HexCorner>();
                HexCorner scriptB = cornerObjects[next].GetComponent<HexCorner>();

                edgeScript.corner1 = scriptA;
                edgeScript.corner2 = scriptB;

                // Adăugăm referința drumului în listele colțurilor, evitând duplicatele
                if (!scriptA.adjacentEdges.Contains(edgeScript))
                {
                    scriptA.adjacentEdges.Add(edgeScript);
                }

                if (!scriptB.adjacentEdges.Contains(edgeScript))
                {
                    scriptB.adjacentEdges.Add(edgeScript);
                }
            }
            else
            {
                Debug.LogError("Prefabul de drum nu are scriptul HexEdge atașat!");
            }

        }
    }
    public void FinishRoadPlacement()
    {
        foreach (var edgeGo in uniqueEdges.Values)
        {
            HexEdge edge = edgeGo.GetComponent<HexEdge>();
            if (edge != null && !edge.isOccupied)
            {
                // Dezactivăm cercul de preview pentru drumurile neocupate
                if (edge.previewCircle != null)
                    edge.previewCircle.SetActive(false);

                // Dezactivăm și collider-ul ca să nu mai poți da click "în gol"
                if (edge.GetComponent<Collider2D>() != null)
                    edge.GetComponent<Collider2D>().enabled = false;
            }
        }

        Debug.Log("Drum plasat. Tura s-a încheiat!");
        // Aici poți adăuga și logica pentru schimbarea jucătorului, dacă ai una
    }

    public void InitializeHarbors()
    {
        Harbor[] allHarbors = FindObjectsOfType<Harbor>();
        foreach (Harbor h in allHarbors)
        {
            h.AssignToCorners();
        }
    }

    public void UpdateValidCorners()
    {
        foreach (var cornerObj in uniqueCorners.Values)
        {
            if (cornerObj == null) continue;

            HexCorner cornerScript = cornerObj.GetComponent<HexCorner>();

            // Folosim logica IsValidForSettlement pe care am scris-o anterior
            // Dacă locul e deja ocupat SAU încalcă regula de distanță față de vecini
            if (!cornerScript.IsValidForSettlement())
            {
                cornerObj.SetActive(false);
            }
            else
            {
                // Dacă locul e valid, îl facem vizibil pentru noul jucător
                cornerObj.SetActive(true);
            }
        }
    }
}