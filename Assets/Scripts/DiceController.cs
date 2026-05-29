using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class DiceController : MonoBehaviour
{
    [Header("Referințe Zaruri")]
    public SpriteRenderer dice1SR;
    public SpriteRenderer dice2SR;
    public Sprite[] diceSprites;

    [Header("Stare Zaruri")]
    public bool hasRolled = false;
    public int lastResult = 0;

    [Header("Control Flux")]
    public bool canRoll = false;

    private void OnMouseDown()
    {
        if (!hasRolled && canRoll)
        {
            StartCoroutine(RollAnimation());
        }
    }

    public void RollDiceFromAI()
    {
        if (!hasRolled)
        {
            StartCoroutine(RollAnimation());
        }
    }

    private IEnumerator RollAnimation()
    {
        hasRolled = true;
        int roll1 = 0;
        int roll2 = 0;

        for (int i = 0; i < 10; i++)
        {
            // Am pus UnityEngine.Random aici
            roll1 = UnityEngine.Random.Range(1, 7);
            roll2 = UnityEngine.Random.Range(1, 7);

            dice1SR.sprite = diceSprites[roll1 - 1];
            dice2SR.sprite = diceSprites[roll2 - 1];

            yield return new WaitForSeconds(0.05f);
        }

        MapGenerator mg = FindObjectOfType<MapGenerator>();

        if (mg != null && mg.turnCounter == 2)
        {
            roll1 = 3;
            roll2 = 4;
            dice1SR.sprite = diceSprites[roll1 - 1];
            dice2SR.sprite = diceSprites[roll2 - 1];
        }

        lastResult = roll1 + roll2;
        FindObjectOfType<GameManager>().hasRolled = true;

        // Am pus UnityEngine.Debug aici
        UnityEngine.Debug.Log($"Rezultat: {lastResult}");

        Color fadedColor = new Color(1f, 1f, 1f, 0.7f);
        dice1SR.color = fadedColor;
        dice2SR.color = fadedColor;

        if (lastResult == 7)
        {
            mg.StartRobberPhase();
        }
        else
        {
            mg.DistributeResources(lastResult);
        }
    }

    public void ResetDice()
    {
        hasRolled = false;
        dice1SR.color = Color.white;
        dice2SR.color = Color.white;
    }

    void Update()
    {
        if (!canRoll) return;
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
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
        GetComponent<Button>().interactable = state;
        this.enabled = state;
    }
}