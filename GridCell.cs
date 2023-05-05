using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridCell
{
    public int x;
    public int y;
    public ETiles tileType = ETiles.Empty;
    private List<bool> allowedNeighbours = new List<bool>{true, true, true, true, true, true};
    private List<float> neighbourWeights = new List<float>{1, 1, 1, 1, 1, 1};
    private List<float> tileChance = new List<float>{1, 1, 1, 1, 1, 1};
    private List<bool> tileAllowed = new List<bool>{true, true, true, true, true, true};
    public float entropy = 1;
    public int lastUpdated = -1;
    private int domain = 6;

    public GridCell(int newX, int newY)
    {
        this.x = newX;
        this.y = newY;
    }

    public int getDomain()
    {
        return this.domain;
    }

    public List<float> getNeighbourWeights()
    {
        return neighbourWeights;
    }

    public List<float> getTileChance()
    {
        return tileChance;
    }

    public List<bool> getAllowedNeighbours()
    {
        return allowedNeighbours;
    }

    public List<bool> getTileAllowed()
    {
        return tileAllowed;
    }

    public void setTileChance(List<float> input)
    {  
        if (this.tileType == ETiles.Empty)
        {
            this.tileChance = input;
            calcEntropy();
        }
    }

    public void setTileAllowed(List<bool> input)
    {  
        if (this.tileType == ETiles.Empty)
        {
            this.tileAllowed = input;

            calcEntropy();
            calculateDomain();
        }
    }

    public void setAllowedNeighbours(List<bool> input)
    {
        allowedNeighbours = input;
        calcEntropy();
    }

    public void constrain(int i)
    {
        allowedNeighbours[i] = false;
        tileAllowed[i] = false;

        calcEntropy();
        calculateDomain();
    }

    private void calculateDomain()
    {
        int i = 0;
        foreach (var item in tileAllowed)
        {
            if (item == true)
            {
                i += 1;
            }
        }
        domain = i;
    }

    public void setCell(Tile tile)
    {
        allowedNeighbours = new List<bool>{false, false, false, false, false, false};
        tileAllowed = new List<bool>{false, false, false, false, false, false};
        neighbourWeights = new List<float>{0, 0, 0, 0, 0, 0,};

        for (int i = 0; i < tile.Neighbours.Count; i++)
        {
            allowedNeighbours[(int)tile.Neighbours[i]] = true;
            neighbourWeights[(int)tile.Neighbours[i]] = tile.NeighbourWeights[i];
        }
        this.tileType = tile.E_TileID;
        this.tileAllowed[(int)tileType] = true;
        this.domain = 1;
        //printCellData();
    }
    
    private void calcEntropy()
    {
        List<float> tileChanceCopy = new List<float>();
        float H = 0;
        for (int i = 0; i < tileChance.Count; i++)
        {
            if(tileChance[i] != 0 && tileAllowed[i] == true)
            {
                tileChanceCopy.Add(tileChance[i]);
            }
        }
        int count = tileChance.Count;
        if (count <= 1) return;

        float sum = tileChanceCopy.Sum();
        for (int i = 0; i < tileChanceCopy.Count; i++)
        {
            H -= (tileChanceCopy[i]/sum * Mathf.Log(tileChanceCopy[i]/sum, tileAllowed.Count));
        }
        this.entropy = H;
    }

    public void printCellData()
    {
        Debug.Log("Tile Type:       " + this.tileType);
        Debug.Log("tile Allowed:    " + string.Join(", ", tileAllowed));
        Debug.Log("tile Weights:    " + string.Join(", ", tileChance));
        Debug.Log("tile N Weights:  " + string.Join(", ", neighbourWeights));
        Debug.Log("Domain:          " + this.domain);
        Debug.Log("Entropy:         " + this.entropy);
    }
}
