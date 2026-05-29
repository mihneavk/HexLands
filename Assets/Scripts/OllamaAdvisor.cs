using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class OllamaAdvisor : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    public string modelName = "phi3";

    // Nu le mai punem public, le găsim automat din cod!
    private GameManager gameManager;
    private PlayerResourceManager resourceManager;

    // --- Variabile pentru UI-ul din Cod ---
    private bool showCodePopup = false;
    private string codeAdvisorText = "Aștept o întrebare...";
    // ---------------------------------------

    [System.Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    private void Start()
    {
        // Scriptul își caută singur colegii, ZERO efort în Inspector
        gameManager = FindObjectOfType<GameManager>();
        resourceManager = FindObjectOfType<PlayerResourceManager>();

        if (gameManager == null) UnityEngine.Debug.LogError("OllamaAdvisor: Nu găsesc GameManager în scenă!");
        if (resourceManager == null) UnityEngine.Debug.LogError("OllamaAdvisor: Nu găsesc PlayerResourceManager în scenă!");
    }

    public void AskForRealAdvice()
    {
        if (gameManager == null || resourceManager == null) return;

        showCodePopup = true;
        codeAdvisorText = "Advisor is thinking...";

        MapGenerator.Player currentPlayer = gameManager.currentPlayer;
        PlayerResourceManager.ResourceWallet wallet = (currentPlayer == MapGenerator.Player.Blue)
            ? resourceManager.bluePlayer
            : resourceManager.orangePlayer;

        int wood = wallet.wood;
        int brick = wallet.brick;
        int sheep = wallet.sheep;
        int wheat = wallet.wheat;
        int ore = wallet.ore;

        string boardContext = GetBoardState();

        string realPrompt = $"I am playing Catan as the {currentPlayer} player. " +
                            $"My resources are: {wood} Wood, {brick} Brick, " +
                            $"{sheep} Sheep, {wheat} Wheat, and {ore} Ore. " +
                            $"Available spots:\n{boardContext}\n" +
                            "What is the best move? Answer strictly in 1 or 2 short sentences in English. Do not write long explanations. Be extremely brief.";

        StartCoroutine(SendRequestToOllama(realPrompt));
    }

    private string GetBoardState()
    {
        HexData[] allHexes = FindObjectsOfType<HexData>();
        Dictionary<HexCorner, List<HexData>> cornerConnections = new Dictionary<HexCorner, List<HexData>>();

        foreach (HexData hex in allHexes)
        {
            foreach (HexCorner corner in hex.adjacentCorners)
            {
                if (corner == null) continue;

                if (!cornerConnections.ContainsKey(corner))
                    cornerConnections[corner] = new List<HexData>();

                cornerConnections[corner].Add(hex);
            }
        }

        StringBuilder sb = new StringBuilder();
        int spotCount = 1;

        foreach (var kvp in cornerConnections)
        {
            HexCorner corner = kvp.Key;
            List<HexData> adjacentHexes = kvp.Value;

            if (corner.IsValidForSettlement())
            {
                sb.Append($"- Spot {spotCount}: touches ");
                foreach (HexData hex in adjacentHexes)
                {
                    if (hex.resourceType != HexData.ResourceType.Desert)
                    {
                        sb.Append($"[{hex.resourceType} {hex.tokenNumber}] ");
                    }
                }
                sb.Append("\n");
                spotCount++;

                if (spotCount > 6) break;
            }
        }

        if (spotCount == 1) return "No valid building spots available.";
        return sb.ToString();
    }

    private IEnumerator SendRequestToOllama(string prompt)
    {
        OllamaRequest reqData = new OllamaRequest { model = modelName, prompt = prompt, stream = false };
        string jsonPayload = JsonUtility.ToJson(reqData);

        UnityWebRequest request = new UnityWebRequest(ollamaUrl, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
        {
            string errorMsg = $"Ollama Error: {request.error}\nServer replied: {request.downloadHandler.text}";
            UnityEngine.Debug.LogError(errorMsg);
            codeAdvisorText = "Eroare de conexiune la Ollama! Asigură-te că aplicația rulează în fundal.";
        }
        else
        {
            string responseText = request.downloadHandler.text;
            string advice = ExtractResponseFromJson(responseText);
            codeAdvisorText = advice;
        }
    }

    private string ExtractResponseFromJson(string json)
    {
        string searchString = "\"response\":\"";
        int startIndex = json.IndexOf(searchString);

        if (startIndex != -1)
        {
            startIndex += searchString.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            string extracted = json.Substring(startIndex, endIndex - startIndex);
            return extracted.Replace("\\n", "\n").Replace("\\\"", "\"").Replace("\\r", "");
        }
        return "Could not read the response.";
    }

    // ==========================================
    // UI 100% DIN COD - NU DEPINDEM DE IERARHIE
    // ==========================================
    private void OnGUI()
    {
        // 1. Butonul mereu vizibil pe ecran
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle = FontStyle.Bold;

        // AM SCHIMBAT Y din 100 în 300 PENTRU A-L COBORA MAI JOS
        // (X = dreapta ecranului - 220, Y = 400 de sus în jos, Lățime = 200, Înălțime = 50)
        if (GUI.Button(new Rect(Screen.width - 220, 400, 200, 50), "Ask AI Advisor", buttonStyle))
        {
            AskForRealAdvice();
        }

        // 2. Fereastra de Popup (apare doar după ce ai dat click)
        if (showCodePopup)
        {
            GUI.Window(0, new Rect(Screen.width / 2 - 250, Screen.height / 2 - 150, 500, 300), DrawCodePopup, "Ollama Phi-3 Advisor");
        }
    }

    private void DrawCodePopup(int windowID)
    {
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 16;
        textStyle.wordWrap = true;

        GUI.Label(new Rect(20, 40, 460, 200), codeAdvisorText, textStyle);

        GUIStyle closeButtonStyle = new GUIStyle(GUI.skin.button);
        closeButtonStyle.fontSize = 14;

        if (GUI.Button(new Rect(200, 250, 100, 40), "Închide", closeButtonStyle))
        {
            showCodePopup = false;
        }
    }
}