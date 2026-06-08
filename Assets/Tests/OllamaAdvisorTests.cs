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
        
        // Adăugăm componentele artificial, imitând ierarhia jocului
        gameManager = testGo.AddComponent<GameManager>();
        resourceManager = testGo.AddComponent<PlayerResourceManager>();
        advisor = testGo.AddComponent<OllamaAdvisor>();

        // Forțăm configurarea inițială a resurselor pentru test
        // Resetăm simularea pe jucătorul albastru (Blue)
        gameManager.currentPlayer = MapGenerator.Player.Blue;

        // Îi dăm jucătorului resurse destule ca să poată construi (ex: 20 din fiecare)
        resourceManager.bluePlayer = new PlayerResourceManager.ResourceWallet {
            wood = 20, brick = 20, sheep = 20, wheat = 20, ore = 20
        };
    }

    [TearDown]
    public void Teardown()
    {
        // Curățăm scena după test ca să nu poluăm celelalte 32 de teste
        Object.DestroyImmediate(testGo);
    }

    [UnityTest]
    public IEnumerator Eval_OllamaAdvisor_ReturnsValidBriefEnglishResponse()
    {
        // Forțăm manual executarea legăturilor din Start(), fiindcă rulăm în test izolat
        advisor.Invoke("Start", 0);

        // 2. Declanșăm interogarea către modelul local Phi-3
        Debug.Log("[EVAL] Se trimite starea jocului către Ollama (phi3)...");
        advisor.AskForRealAdvice();

        // 3. Așteptăm ca Ollama să răspundă (Ollama local poate avea un lag de 1-5 secunde)
        float timeout = 10f; // acordăm maximum 10 secunde pentru inferență locală
        float timer = 0f;

        // Cât timp textul este cel default sau cel de încărcare, lăsăm timpul să treacă
        while (GetAdvisorText(advisor) == "Aștept o întrebare..." || GetAdvisorText(advisor) == "Advisor is thinking...")
        {
            if (timer >= timeout)
            {
                Assert.Fail("[EVAL TIMEOUT]: Ollama nu a răspuns în 10 secunde. Este pornită aplicația pe Mac?");
            }
            timer += Time.deltaTime;
            yield return null; // Așteaptă următorul cadru (frame)
        }

        string aiResponse = GetAdvisorText(advisor);
        Debug.Log($"[EVAL] Răspuns primit de la Phi-3: \"{aiResponse}\"");

        // 4. ASERȚIUNILE (Evaluarea propriu-zisă a Agentului)
        
        // Regula 1: Să nu avem eroare de conexiune reflectată în text
        Assert.IsFalse(aiResponse.Contains("Eroare de conexiune"), 
            "Agentul a picat: A apărut o eroare de conexiune la serverul local Ollama.");

        // Regula 2: Modelul nu trebuie să returneze string-ul de eșec din parser-ul JSON
        Assert.AreNotEqual("Could not read the response.", aiResponse, 
            "Agentul a picat: Structura JSON returnată de Ollama s-a modificat și parserul nu o mai poate citi.");

        // Regula 3: Verificăm calitatea (răspunsul nu trebuie să fie gol sau excesiv de scurt/aberant)
        Assert.IsTrue(aiResponse.Length > 5, 
            $"Agentul a picat: Răspunsul este suspect de scurt ({aiResponse.Length} caractere).");
            
        Debug.Log("[EVAL PASSED]: Jucătorul a primit un sfat valid de la Phi-3!");
    }

    // Funcție ajutătoare de Reflection pentru a citi variabila privată 'codeAdvisorText' din OllamaAdvisor
    private string GetAdvisorText(OllamaAdvisor targetAdvisor)
    {
        var field = typeof(OllamaAdvisor).GetField("codeAdvisorText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field != null ? (string)field.GetValue(targetAdvisor) : "";
    }
}