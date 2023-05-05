using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid
{
    List<List<GridCell>> grid = new List<List<GridCell>>();
    public Grid(int height, int width)
    {
        
        for (int i = 0; i < height; i++)
        {
            List<GridCell> wide = new List<GridCell>();
            for (int j = 0; j < width; j++)
            {
                GridCell gridCell = new GridCell(j, i); 
                wide.Add(gridCell);
            }
            grid.Add(wide);
        }
    }

    public GridCell getCell(int x, int y)
    {
        return grid[y][x];
    }
}
