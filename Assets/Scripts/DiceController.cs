using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DiceController : MonoBehaviour
{
    [Header("Referințe Zaruri")]
    public SpriteRenderer dice1SR;
    public SpriteRenderer dice2SR;
    public Sprite[] diceSprites; // Array de 6 sprite-uri (0 = cifra 1, 5 = cifra 6)

    [Header("Stare Zaruri")]
    public bool hasRolled = false;
    public int lastResult = 0;

    [Header("Control Flux")]
    public bool canRoll = false; // Începe cu false pentru Setup

    // Metoda apelată când dai click pe zaruri (necesită Collider pe obiectul cu scriptul)
    private void OnMouseDown()
    {
        if (!hasRolled && canRoll)
        {
            StartCoroutine(RollAnimation());
        }
    }

    private IEnumerator RollAnimation()
    {
        hasRolled = true;
        int roll1 = 0;
        int roll2 = 0;

        // Animația de rotire
        for (int i = 0; i < 10; i++)
        {
            roll1 = Random.Range(1, 7);
            roll2 = Random.Range(1, 7);

            dice1SR.sprite = diceSprites[roll1 - 1];
            dice2SR.sprite = diceSprites[roll2 - 1];

            yield return new WaitForSeconds(0.05f);
        }

        MapGenerator mg = FindObjectOfType<MapGenerator>();

        // Verificăm dacă suntem la a doua tură (sau oricare alta vrei să o testezi)
        if (mg != null && mg.turnCounter == 2)
        {
            // Forțăm rezultatul să fie 7 (ex: 3 și 4)
            roll1 = 3;
            roll2 = 4;

            // Actualizăm vizualul final pentru a reflecta rezultatul trucat
            dice1SR.sprite = diceSprites[roll1 - 1];
            dice2SR.sprite = diceSprites[roll2 - 1];
        }

        lastResult = roll1 + roll2;
        FindObjectOfType<GameManager>().hasRolled = true;
        Debug.Log($"Rezultat: {lastResult}");

        // --- AICI ADĂUGĂM TRANSPARENȚA ---
        // Creăm o culoare albă dar cu Alpha de 0.5f (50% transparență)
        Color fadedColor = new Color(1f, 1f, 1f, 0.7f);
        dice1SR.color = fadedColor;
        dice2SR.color = fadedColor;

        lastResult = roll1 + roll2;
        Debug.Log($"Rezultat: {lastResult}");
        if (lastResult == 7)
        {
            mg.StartRobberPhase(); // Activăm faza de mutare
        }
        else
        {
            mg.DistributeResources(lastResult);
        }
    }

    public void ResetDice()
    {
        hasRolled = false;

        // --- RESETĂM LA OPACITATE MAXIMĂ ---
        // Color.white este același lucru cu new Color(1, 1, 1, 1)
        dice1SR.color = Color.white;
        dice2SR.color = Color.white;
    }

    void Update()
    {
        if (!canRoll) return;
        // Verificăm dacă s-a apăsat click stânga
        if (Input.GetMouseButtonDown(0))
        {
            // Tragem o rază din cameră spre poziția mouse-ului
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
                // Verificăm dacă obiectul lovit este unul dintre cele două zaruri
                if (hit.collider.gameObject == dice1SR.gameObject || hit.collider.gameObject == dice2SR.gameObject)
                {
                    if (!hasRolled && canRoll)
                    {
                        StartCoroutine(RollAnimation());
                    }
                }
            }
        }
    }
    public void SetInteractable(bool state)
    {
        // Dacă ai un buton de UI
        GetComponent<Button>().interactable = state;
        // Sau dacă dai click pe un obiect 3D de zar
        this.enabled = state;
    }
}   