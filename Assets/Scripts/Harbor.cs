using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harbor : MonoBehaviour
{
    public enum HarborType { None, Generic3to1, Sheep2to1, Wood2to1, Wheat2to1, Ore2to1, Brick2to1 }
    public HarborType type;

    public float detectionRadius = 0.8f; // Raza în care caută colțurile

    public void AssignToCorners()
    {
        // Căutăm toate colțurile din apropierea portului (un port ocupă de obicei 2 colțuri)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRadius);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Corner"))
            {
                HexCorner corner = hit.GetComponent<HexCorner>();
                if (corner != null)
                {
                    corner.GiveHarborBonus(type);
                }
            }
        }
    }
}