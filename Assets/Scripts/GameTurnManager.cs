using UnityEngine;

public class GameTurnManager : MonoBehaviour
{
    public enum GamePhase { Setup, Gameplay }
    public GamePhase currentPhase = GamePhase.Setup;

    public MapGenerator.Player currentPlayer = MapGenerator.Player.Blue;

    // Contorizăm plasările din setup
    private int settlementsPlaced = 0;
    private int roadsPlaced = 0;

    // Referințe către alte scripturi
    public DiceController diceRoller;
    public SettlementPlacer settlementPlacer;

    void Start()
    {
        // La început, dezactivăm zarul
        diceRoller.SetInteractable(false);
        Debug.Log("Faza de Setup: Albastru plasează prima casă.");
    }
}
