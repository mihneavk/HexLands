// Fișier: MapGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static HexData;
using static UnityEditor.PlayerSettings;

public class MapGenerator : MonoBehaviour
{
    public enum Player { None, Blue, Orange }

    [Header("Sprite-uri Jucători")]
    public Sprite blueHouse;
    public Sprite orangeHouse;
    public Sprite blueRoad;
    public Sprite orangeRoad;

    public HexData[] allHexes = null;
    public HexCorner[] allCorners = null;
    public HexEdge[] allEdges = null;

    public Sprite GetCurrentHouseSprite()
    {
        // Căutăm jucătorul activ în singurul loc unde contează: GameManager
        GameManager gm = FindObjectOfType<GameManager>();
        return gm.currentPlayer == Player.Blue ? blueHouse : orangeHouse;
    }

    public Sprite GetCurrentRoadSprite()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        return gm.currentPlayer == Player.Blue ? blueRoad : orangeRoad;
    }

    [Header("Debug / Testing")]
    public int turnCounter = 1;

    public void PrepareNextPlayer()
    {

        DiceController dc = FindObjectOfType<DiceController>();
        if (dc != null) dc.ResetDice();

        turnCounter++;

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

        allHexes = FindObjectsOfType<HexData>();
        allCorners = FindObjectsOfType<HexCorner>();

        // 4. APELUL CORECT:
        foreach (HexData hex in allHexes)
        {
            // Trimitem atât obiectul (hex.gameObject) cât și poziția lui (hex.transform.position)
            PopulateHexCorners(hex.gameObject, hex.transform.position);
        }

        allEdges = FindObjectsOfType<HexEdge>();
        InitializeHarbors();
        SetupInitialRobber();
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

    public void SetupInitialRobber()
    {
        // 1. Luăm toate obiectele care au scriptul HexTile (sau cum se numește scriptul tău de pe hexagon)
        HexData[] allTiles = FindObjectsOfType<HexData>();

        foreach (HexData tile in allTiles)
        {
            // 2. Verificăm care dintre ele este Desert
            // Presupunând că ai o variabilă resourceType în scriptul HexTile
            if (tile.resourceType == ResourceType.Desert)
            {
                // 3. Plasăm hoțul pe poziția acelui hexagon
                Debug.Log("Am gasit desertul");
                PlaceRobberOnDesert(tile.transform.position);
                break; // Am găsit desertul, ne putem opri
            }
        }
    }

    private void CreateHex(int q, int r, HexData.ResourceType type, int number)
    {
        // 1. Calculăm poziția normală (X, Y)
        Vector3 worldPosition = HexCoordinates.AxialToWorld(q, r, hexRadius);
        worldPosition += transform.position;

        // 2. MODIFICARE: Forțăm Z-ul la 1 înainte de instanțiere
        // Asta îl trimite "în spate", deci colțurile de la Z=0 vor fi "peste" el
        worldPosition.z = 1f;

        // 3. Instanțiem cu noul worldPosition care are Z=1
        GameObject hexGo = Instantiate(hexPrefab, worldPosition, Quaternion.identity);
        hexGo.transform.SetParent(this.transform);

        HexData data = hexGo.GetComponent<HexData>();
        if (data != null) data.Initialize(q, r, type, number);

        // 4. Atenție: Când creăm colțurile, le trimitem poziția originală (X, Y) 
        // sau ne asigurăm că în CreateCornersForHex ele primesc Z = 0 sau Z = -1
        List<GameObject> corners = CreateCornersForHex(new Vector3(worldPosition.x, worldPosition.y, 0f));
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
            float angle_deg = 60 * i + 30f;
            float angle_rad = Mathf.PI / 180 * angle_deg;

            Vector3 cornerPos = new Vector3(
                hexCenter.x + hexRadius * Mathf.Cos(angle_rad),
                hexCenter.y + hexRadius * Mathf.Sin(angle_rad),
                -1f // Setăm Z-ul aici pentru a fi în fața hexagonului
            );

            Vector3 roundedPos = new Vector3(
            Mathf.Round(cornerPos.x * 100f) / 100f,
            Mathf.Round(cornerPos.y * 100f) / 100f,
            0f
        );

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
                //cornerObj.SetActive(true);
            }
        }
    }

    [Header("Referință Hoț")]
    public GameObject robberPrefab;
    private GameObject spawnedRobber; // Păstrăm referința pentru a-l putea muta ulterior

    public void PlaceRobberOnDesert(Vector3 desertPosition)
    {
        // Ajustăm înălțimea. 0.3f este o valoare de test, o poți mări dacă vrei mai sus.
        float yOffset = 0.02f;
        Vector3 adjustedPos = new Vector3(desertPosition.x, desertPosition.y + yOffset, -2f);

        if (spawnedRobber == null)
        {
            spawnedRobber = Instantiate(robberPrefab, adjustedPos, Quaternion.identity);
        }
        else
        {
            spawnedRobber.transform.position = adjustedPos;
        }
    }

    public bool isMovingRobber = false;
    private HexData currentRobberHex;

    // Chemăm asta când zarul dă 7
    public void StartRobberPhase()
    {
        isMovingRobber = true;
        Debug.Log("Avem 7. Pune unde vrei");
        // Punem toate hexagoanele să pulseze
        HexData[] allHexes = FindObjectsOfType<HexData>();
        foreach (HexData hex in allHexes)
        {
            // Opțional: nu pulsa deșertul sau hexagonul unde este deja hoțul
            if (!hex.hasRobber)
            {
                hex.StartPulsing();
            }
        }
    }

    public void MoveRobberToHex(HexData targetHex)
    {
        // Regula: Să nu fie același hexagon
        if (targetHex == currentRobberHex)
        {
            Debug.Log("Trebuie să muți hoțul pe ALT hexagon!");
            return;
        }

        // 1. Curățăm vechiul hexagon
        if (currentRobberHex != null) currentRobberHex.hasRobber = false;

        // 2. Setăm noul hexagon
        targetHex.hasRobber = true;
        currentRobberHex = targetHex;

        // 3. Mutăm vizual pionul (folosind offset-ul de data trecută)
        PlaceRobberOnDesert(targetHex.transform.position);
        HexData[] allHexes = FindObjectsOfType<HexData>();
        foreach (HexData hex in allHexes)
        {
            hex.StopPulsing();
        }
        // 4. Ieșim din faza de mutare
        isMovingRobber = false;
        Debug.Log("Hoțul a fost mutat!");
        HandleRobberStealing(targetHex);
    }

    private void HandleRobberStealing(HexData hex)
    {
        GameManager gm = FindObjectOfType<GameManager>();
        PlayerResourceManager resManager = FindObjectOfType<PlayerResourceManager>();

        Player attacker = gm.currentPlayer;
        Player victim = (attacker == Player.Blue) ? Player.Orange : Player.Blue;

        bool victimFound = false;

        // Verificăm dacă victima are vreo casă pe colțurile acestui hexagon
        foreach (HexCorner corner in hex.adjacentCorners)
        {
            if (corner.isOccupied && corner.owner == victim)
            {
                victimFound = true;
                break;
            }
        }

        if (victimFound)
        {
            // Apelăm o metodă în ResourceManager care să execute furtul
            resManager.StealResource(victim, attacker);
        }
        else
        {
            Debug.Log("Nu e nimeni de la culoarea opusă pe acest hexagon. Nimic de furat.");
        }
    }

    public void DistributeResources(int diceRoll)
    {
        // Căutăm toate hexagoanele
        HexData[] allHexes = FindObjectsOfType<HexData>();

        foreach (HexData hex in allHexes)
        {
            // 1. Verificăm dacă numărul de pe hex coincide cu zarul și NU are hoțul pe el
            if (hex.tokenNumber == diceRoll && !hex.hasRobber)
            {
                // 2. Verificăm toate cele 6 colțuri ale hexagonului
                foreach (HexCorner corner in hex.adjacentCorners)
                {
                    if (corner.isOccupied)
                    {
                        // 3. Dăm resursa proprietarului colțului
                        int amount = corner.isCity ? 2 : 1;
                        FindObjectOfType<PlayerResourceManager>().AddResource(corner.owner, hex.resourceType, 1);
                    }
                }
            }
        }
    }

    public void PopulateHexCorners(GameObject hexObj, Vector3 hexPos)
    {
        HexData hexData = hexObj.GetComponent<HexData>();
        if (hexData == null) return;

        // Curățăm lista în caz că regenerăm harta
        hexData.adjacentCorners.Clear();

        // Calculăm cele 6 poziții ale colțurilor pentru acest hexagon
        // Folosește aceeași logică/matematică pe care ai folosit-o la crearea colțurilor
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 60 * i + 30; // Poate fi 60 * i + 30 depinde de orientarea hex-ului tău
            float angle_rad = Mathf.Deg2Rad * angle_deg;

            // Dimensiunea hexagonului (size-ul pe care îl folosești deja)
            float size = 1.0f;

            Vector3 cornerPos = new Vector3(
                hexPos.x + size * Mathf.Cos(angle_rad),
                hexPos.y + size * Mathf.Sin(angle_rad),
                0f
            );

            // Rotunjim poziția pentru a evita erorile de precizie float la căutarea în dicționar
            Vector3 roundedPos = new Vector3(
                Mathf.Round(cornerPos.x * 100f) / 100f,
                Mathf.Round(cornerPos.y * 100f) / 100f,
                0f
            );

            // Căutăm colțul în dicționarul tău de colțuri unice
            if (uniqueCorners.ContainsKey(roundedPos))
            {
                HexCorner cornerScript = uniqueCorners[roundedPos].GetComponent<HexCorner>();
                Debug.Log($"Hexagonul de la {hexPos} a găsit colțul la {roundedPos}");

                // Adăugăm referința în lista hexagonului
                if (!hexData.adjacentCorners.Contains(cornerScript))
                {
                    hexData.adjacentCorners.Add(cornerScript);
                }
            }
            else
            {
                Debug.LogWarning($"Hexagonul de la {hexPos} NU a găsit colț la poziția calculată: {roundedPos}");
            }
        }
    }
    public int GetLongestRoadForPlayer(Player player)
    {
        int maxLength = 0;
        List<HexEdge> playerEdges = new List<HexEdge>();

        // 1. Colectăm toate drumurile care aparțin jucătorului
        foreach (var edge in allEdges)
        {
            if (edge.isOccupied && edge.owner == player)
                playerEdges.Add(edge);
        }

        // 2. Pentru fiecare drum, încercăm să găsim cel mai lung drum care pornește de acolo
        foreach (var startEdge in playerEdges)
        {
            maxLength = Mathf.Max(maxLength, ExploreRoad(startEdge, player, new List<HexEdge>()));
        }

        return maxLength;
    }

    private int ExploreRoad(HexEdge currentEdge, Player player, List<HexEdge> visited)
    {
        visited.Add(currentEdge);
        int maxSubPath = 0;

        // Verificăm ambele capete ale drumului (Corner 1 și Corner 2)
        HexCorner[] corners = { currentEdge.corner1, currentEdge.corner2 };

        foreach (var corner in corners)
        {
            // REGULĂ CATAN: Nu poți trece cu drumul printr-o casă inamică!
            if (corner.isOccupied && corner.owner != player) continue;

            foreach (var nextEdge in corner.adjacentEdges)
            {
                if (nextEdge != null && nextEdge.isOccupied && nextEdge.owner == player && !visited.Contains(nextEdge))
                {
                    maxSubPath = Mathf.Max(maxSubPath, ExploreRoad(nextEdge, player, new List<HexEdge>(visited)));
                }
            }
        }

        return 1 + maxSubPath;
    }

    [Header("Sprite-uri Orașe")]
    public Sprite blueCity;
    public Sprite orangeCity;

    public Sprite GetCurrentCitySprite()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        return gm.currentPlayer == Player.Blue ? blueCity : orangeCity;
    }
}