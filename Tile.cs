using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public UnityEngine.Object tileObject = null;
    public ETiles E_TileID;
    public List<ETiles> Neighbours = new List<ETiles>();
    public List<float> NeighbourWeights = new List<float>();
    public GameObject controller;
    // Start is called before the first frame update

   
}
