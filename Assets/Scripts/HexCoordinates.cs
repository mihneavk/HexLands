// Fișier: HexCoordinates.cs
using UnityEngine;
using System;

[Serializable] // Ca să-l putem vedea în Inspectorul Unity
public struct HexCoordinates
{
    // Coordonate Axiale: q (column), r (row)
    [SerializeField]
    private int q, r;

    public int Q => q;
    public int R => r;

    // Coordonata S (pentru sistemul cubic, derivată din q și r: q + r + s = 0)
    public int S => -q - r;

    public HexCoordinates(int q, int r)
    {
        this.q = q;
        this.r = r;
    }

    // Funcție STATICA care convertește coordonatele axiale în poziție World (Vector3)
    // Presupunem orientarea "Pointy-Topped" (Catan clasic)
    public static Vector3 AxialToWorld(int q, int r, float hexRadius)
    {
        // Calculăm poziția pentru 2D (X și Y)
        // Folosim formula pentru "Pointy-Topped" hexes (vârful sus)
        float x = hexRadius * (Mathf.Sqrt(3) * q + Mathf.Sqrt(3) / 2f * r);
        float y = hexRadius * (3f / 2f * r); // Am mutat calculul de pe Z pe Y

        // Z rămâne 0 (sau poți pune o valoare mică pentru sortare dacă ai nevoie)
        return new Vector3(x, y, 0);
    }
}