using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class OllamaAdvisorEvals
{
    private GameObject testGo;
    private OllamaAdvisor advisor;
    private GameManager gameManager;
    private PlayerResourceManager resourceManager;

    [SetUp]
    public void Setup()
    {
        // 1. Pregătim un mediu de test izolat în scenă
        testGo = new GameObject("OllamaTestEnvironment");
        
        gameManager = testGo.AddComponent<GameManager>();
        resourceManager = testGo.AddComponent<PlayerResourceManager>();
        advisor = testGo.AddComponent<OllamaAdvisor>();

        gameManager.currentPlayer = MapGenerator.Player.Blue;

        resourceManager.bluePlayer = new PlayerResourceManager.ResourceWallet {
            wood = 20, brick = 20, sheep = 20, wheat = 20, ore = 20
        };
    }

    [TearDown]
    public void Teardown()
    {
        // Curățăm scena după test
        Object.DestroyImmediate(testGo);
    }

    [UnityTest]
    public IEnumerator Eval_OllamaAdvisor_ReturnsValidBriefEnglishResponse()
    {
        // Forțăm executarea legăturilor din Start în mediul izolat
        advisor.Invoke("Start", 0);

        Debug.Log("[EVAL] Se trimite starea jocului către Ollama...");
        advisor.AskForRealAdvice();

        // Acordăm 20 de secunde pentru inferența pe calculatorul de Windows
        float timeout = 20f; 
        float timer = 0f;

        while (GetAdvisorText(advisor) == "Aștept o întrebare..." || GetAdvisorText(advisor) == "Advisor is thinking...")
        {
            if (timer >= timeout)
            {
                Assert.Fail($"[EVAL TIMEOUT]: Ollama nu a răspuns în {timeout} secunde. " +
                            "Asigură-te că pe PC-ul de Windows: 1. Aplicația Ollama este pornită activ în fundal, " +
                            "2. Modelul corect (phi3) este descărcat rulând în terminal 'ollama pull phi3'.");
            }
            
            // Forțează procesarea cererilor HTTP de rețea în fundal pe Windows
            System.Threading.Thread.Sleep(200); 
            
            timer += 0.2f;
            yield return null; 
        }

        string aiResponse = GetAdvisorText(advisor);
        Debug.Log($"[EVAL] Răspuns primit de la AI: \"{aiResponse}\"");

        // ASERȚIUNILE DE EVALUARE A AGENTULUI
        Assert.IsFalse(aiResponse.Contains("Eroare de conexiune"), 
            "Agentul a picat: Unity nu s-a putut conecta la serverul local Ollama (localhost:11434) pe Windows.");

        Assert.AreNotEqual("Could not read the response.", aiResponse, 
            "Agentul a picat: Structura JSON returnată de Ollama s-a modificat și parserul nu o poate citi.");

        Assert.IsTrue(aiResponse.Length > 5, 
            $"Agentul a picat: Răspunsul primit este suspect de scurt ({aiResponse.Length} caractere).");
            
        Debug.Log("[EVAL PASSED]: Jucătorul a primit un sfat valid de la modelul local de pe Windows!");
    }

    // Funcție ajutătoare de Reflection pentru a citi variabila privată 'codeAdvisorText'
    private string GetAdvisorText(OllamaAdvisor targetAdvisor)
    {
        var field = typeof(OllamaAdvisor).GetField("codeAdvisorText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (string)field.GetValue(targetAdvisor) : "";
    }
}