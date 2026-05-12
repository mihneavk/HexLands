using UnityEngine;

public class DevCardItem : MonoBehaviour
{
    public DevCardManager.DevCardType cardType;
    public MapGenerator.Player owner;

    private void OnMouseDown()
    {
        // Trimitem comanda către manager
        FindObjectOfType<DevCardManager>().UseCard(cardType, owner);
    }
}