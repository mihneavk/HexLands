using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;

public class OllamaAdvisor : MonoBehaviour
{
    [Header("Ollama Settings")]
    public string ollamaUrl = "http://127.0.0.1:11434/api/generate";
    public string modelName = "phi3";

    [Header("Game References")]
    public GameManager gameManager;
    public PlayerResourceManager resourceManager;

    [Header("UI References")]
    public TextMeshProUGUI advisorTextDisplay;
    public GameObject popupPanel;

    [System.Serializable]
    private class OllamaRequest
    {
        public string model;
        public string prompt;
        public bool stream;
    }

    private void Start()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<PlayerResourceManager>();
    }

    public void AskForRealAdvice()
    {
        if (gameManager == null || resourceManager == null) return;

        if (popupPanel != null) popupPanel.SetActive(true);

        if (advisorTextDisplay != null) advisorTextDisplay.text = "Advisor is thinking...";

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

        // AICI AM MODIFICAT PROMPT-UL PENTRU A-L FACE SĂ SCRIE PUȚIN:
        string realPrompt = $"I am playing Catan as the {currentPlayer} player. " +
                            $"My resources are: {wood} Wood, {brick} Brick, " +
                            $"{sheep} Sheep, {wheat} Wheat, and {ore} Ore. " +
                            $"Available spots:\n{boardContext}\n" +
                            "What is the best move? Answer strictly in 1 or 2 short sentences in English. Do not write long explanations. Be extremely brief.";

        StartCoroutine(SendRequestToOllama(realPrompt));
    }

    public void ClosePopup()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
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
            if (advisorTextDisplay != null) advisorTextDisplay.text = "Eroare! Vezi consola.";
        }
        else
        {
            string responseText = request.downloadHandler.text;
            string advice = ExtractResponseFromJson(responseText);

            if (advisorTextDisplay != null) advisorTextDisplay.text = advice;
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
}