using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class WFC : MonoBehaviour
{
    public List<Tile> tiles = new List<Tile>();
    public List<UnityEngine.Object> tilePrefabs = new List<UnityEngine.Object>();
    public int height = 50;
    public int width = 50;

    public Grid grid;
    public bool bEnabled = false; 
    public GameObject output;
    private bool firstPlace = true;

    public int printX = 0;
    public int printY = 0;
    public bool drawDomain;
    public bool drawEntropy;
    public bool bComplete;
    private float startTime;
    private float finishTime;
    private bool printed;

    public void initializeMaps()
    {
        //UnityEngine.Random.InitState(1);
        bComplete = false;
        firstPlace = true;
        grid = new Grid(height, width);
        bEnabled = true;
        printed = false;

        if (output == null){
			Transform ot = transform.Find("outputTiles");
			if (ot != null){output = ot.gameObject;}}
        if (output == null){
			output = new GameObject("outputTiles");
			output.transform.parent = transform;
			output.transform.position = this.gameObject.transform.position;
			output.transform.rotation = this.gameObject.transform.rotation;}
         foreach (Transform child in output.transform) {
             GameObject.DestroyImmediate(child.gameObject);
         }
    }

    public void iterate()
    {
        List<int> coords;
        if (firstPlace)
        {
            int coordX = UnityEngine.Random.Range(0, width);
            print(coordX);
            int coordY = UnityEngine.Random.Range(0, height);
            print(coordY);
            coords = new List<int> {coordX, coordY};
            firstPlace = false;
        }
        else
        {
            coords = get_min_entropy_coords();
        }
        collapse_at(coords[0], coords[1]);
        propergate(coords[0], coords[1]);
    }

    private void collapse_at(int x, int y)
    {
        List<float> cellChances = getCellChance(x, y);

        float tileRNGResult = UnityEngine.Random.Range(0, cellChances.Max());

        ETiles tileResult = ETiles.DeepWater;

        for (int i = cellChances.Count-1; i >= 0; i--)
        {
            if (i == cellChances.Count-1)
            {
                if (tileRNGResult <= cellChances[i])
                {
                    tileResult = (ETiles)i;
                }
            }
            else
            {
                if (tileRNGResult < cellChances[i])
                {
                    tileResult = (ETiles)i;
                } 
            }
        }

        grid.getCell(x,y).setCell(tiles[(int)tileResult]);

        Vector3 pos = new Vector3(x, y, 0f);
        GameObject tile = (GameObject)Instantiate(tiles[(int)tileResult].tileObject, pos, Quaternion.identity);
        tile.transform.parent = output.transform;

        foreach (var other_coords in valid_dirs(new List<int>{x, y}))
        {
            grid.getCell(other_coords[0], other_coords[1]).setTileChance(getCellChance(other_coords[0], other_coords[1]));
        }
    }

    private List<float> getCellChance(int x, int y)
    {
        List<int> cur_coords = new List<int> {x, y};
        List<float> chances = new List<float>();

        bool first = true;

        foreach (var other_coords in valid_dirs(cur_coords))
        {
            if (first)
            {
                chances = getNeighbourWeightsByType(grid.getCell(other_coords[0], other_coords[1]).tileType);
            }
            else
            {
                List<float> other_weights = getNeighbourWeightsByType(grid.getCell(other_coords[0], other_coords[1]).tileType);
                for (int i = 0; i < chances.Count; i++)
                {
                    chances[i] += other_weights[i];
                }
            }
        }

        List<bool> allowedTiles = grid.getCell(x, y).getTileAllowed();
        for (int i = 0; i < chances.Count; i++)
        {
            if (allowedTiles[i] == false)
            {
                chances[i] = 0;
            }
        }

        for (int i = 0; i < chances.Count; i++)
        {
            if (i>0)
            {
                chances[i] += chances[i-1];
            }
        }

        return chances;
    }

    private List<float> getNeighbourWeightsByType(ETiles tileType)
    {
        if (tileType == ETiles.Empty)
        {
            return (new List<float>{1, 1, 1, 1, 1, 1});
        }
        List<float> neighbourWeights = new List<float>{0, 0, 0, 0, 0, 0,};
        for (int i = 0; i < tiles[(int)tileType].Neighbours.Count; i++)
        {
            neighbourWeights[(int)tiles[(int)tileType].Neighbours[i]] = tiles[(int)tileType].NeighbourWeights[i];
        }

        return neighbourWeights;
    }

    private void propergate(int x, int y)
    {
        Queue queue = new Queue();
        queue.Enqueue(new List<int>{x, y});
        int iterations = 200;
        int maxQueueSize = 0;
        while (queue.Count > 0 && iterations > 0)
        {
            iterations -= 1;
            List<int> cur_coords = (List<int>)queue.Dequeue();

            foreach (var other_coords in valid_dirs(cur_coords))
            {
                List<bool> other_possible_tiles = get_possibilities(other_coords[0], other_coords[1]);

                List<bool> possible_neighbours = get_possible_neighbours(cur_coords[0], cur_coords[1]);

                if (other_possible_tiles.Count == 0)
                {
                    continue;
                }

                for (int i = 0; i < other_possible_tiles.Count; i++)
                {
                    bool other_tile = other_possible_tiles[i];
                    if (possible_neighbours[i] == false && other_tile == true)
                    {
                        constrain(other_coords[0], other_coords[1], i);
                        if ( !(coordInQueue(queue, other_coords)))
                        {
                            queue.Enqueue(other_coords);
                            if (queue.Count > maxQueueSize)
                            {
                                maxQueueSize = queue.Count;
                            }
                        }
                    }
                }
            }
        }
    }

    private void constrain(int x, int y, int i)
    {
        grid.getCell(x,y).constrain(i);
    }

    private bool coordInQueue(Queue queue, List<int> coords)
    {
        Queue checkQueue = (Queue)queue.Clone();
        while (checkQueue.Count > 0)
        {
            List<int> check = (List<int>)checkQueue.Dequeue();
            if (check[0] == coords[0] && check[1] == coords[1])
            {
                return true;
            }
        }
        return false;
    }

    private List<bool> get_possible_neighbours(int x, int y)
    {

        List<bool> possible_tiles = get_possibilities(x, y);
        List<bool> allowedNeighbours = new List<bool>{false, false, false, false, false, false};

        for (int i = 0; i < possible_tiles.Count; i++)
        {
            if (possible_tiles[i])
            {
                List<ETiles> tilePreferences = tiles[i].Neighbours;
                for (int j = 0; j < tilePreferences.Count; j++)
                {
                    allowedNeighbours[(int)tilePreferences[j]] = true;
                }
            }
        }
        return allowedNeighbours;
    }

    private List<bool> get_possibilities(int x, int y)
    {
        List<bool> possibilities = grid.getCell(x, y).getTileAllowed();
        return possibilities;
    }

    private List<int> get_min_entropy_coords()
    {
        bool complete = true;
        List<List<int>> coords_list = new List<List<int>>();
        List<int> coords = new List<int>{0,0};

        float lowestEntropy = float.MaxValue;

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if ( grid.getCell(i, j).entropy < lowestEntropy && (int)grid.getCell(i, j).tileType == -1)
                {
                    lowestEntropy = grid.getCell(i, j).entropy;
                    coords[0] = i;
                    coords[1] = j;
                    coords_list.Clear();
                    complete = false;
                    //coords_list.Add(new List<int>{coords[0], coords[1]});
                }
                else if ( grid.getCell(i, j).entropy == lowestEntropy && (int)grid.getCell(i, j).tileType == -1 )
                {
                    coords[0] = i;
                    coords[1] = j;
                    complete = false;
                    //coords_list.Add(new List<int>{coords[0], coords[1]});
                }
            }
        }
        //int randomIndex = UnityEngine.Random.Range(0, coords_list.Count);
        bComplete = complete;
        //return coords_list[randomIndex];
        return coords;
    }

    private float entropy(List<float> weights)
    {
        float H = 0;
        List<float> weightsCopy = new List<float>();
        for (int i = 0; i < weights.Count; i++)
        {
            if(weights[i] != 0)
            {
                weightsCopy.Add(weights[i]);
            }
        }
        int count = weightsCopy.Count;
        if (count <= 1) return 0;
        float sum = weightsCopy.Sum();
        for (int i = 0; i < count; i++)
        {
            H -= (weightsCopy[i]/sum * Mathf.Log(weightsCopy[i]/sum, count));
        }
        return H;
    }

    private List<List<int>> valid_dirs(List<int> coords)
    {
        List<List<int>> dirs = new List<List<int>>();
        int x = coords[0];
        int y = coords[1];

        if (y + 1 < height)
        {   
            dirs.Add(new List<int>{x, y+1});
        }

        if (x + 1 < width)
        {
            dirs.Add(new List<int>{x + 1, y});
        }

        if (y - 1 >= 0)
        {
            dirs.Add(new List<int>{x, y-1});
        }

        if (x - 1 >= 0)
        {
            dirs.Add(new List<int>{x - 1, y});
        }

        return dirs;
    }

    
    public void printCellData()
    {
        grid.getCell(this.printX, this.printY).printCellData();
    }

    static void drawString(string text, Vector3 worldPos, Color? colour = null) {
		UnityEditor.Handles.BeginGUI();
		if (colour.HasValue) GUI.color = colour.Value;
		var view = UnityEditor.SceneView.currentDrawingSceneView;
		Vector3 screenPos = view.camera.WorldToScreenPoint(worldPos);
		Vector2 size = GUI.skin.label.CalcSize(new GUIContent(text));
		GUI.Label(new Rect(screenPos.x - (size.x / 2), -screenPos.y + view.position.height-35, size.x, size.y), text);
		UnityEditor.Handles.EndGUI();
	}

    private void Start() {
        initializeMaps();

        startTime = Time.time;
    }
    private void Update() {
        if (!bComplete)
        {
            /*
            i < 5 = 21 sec

            */
            for (int i = 0; i < (width*height+1)/100; i++)
            {
                iterate();
            }
        }
        else if (!printed)
        {
            finishTime = Time.time;
            print(finishTime - startTime);
            printed = true;
        }

    }

    private void OnDrawGizmos() {
        for (int i = 0; i < width; i++)
        {
            drawString(i.ToString(), new Vector3(i, -1, 0));

            for (int j = 0; j < height; j++)
            {
                if (bEnabled)
                {
                    if (drawEntropy)
                    {
                        drawString(grid.getCell(i, j).entropy.ToString("0.000"), new Vector3(i, j, 0));
                    }

                    if (drawDomain)
                    {
                        drawString(grid.getCell(i,j).getDomain().ToString(), new Vector3(i, j, 0));
                    }
                    
                }
            }
        }

        for (int j = 0; j < height; j++)
        {
            drawString(j.ToString(), new Vector3(-1, j, 0));
        }

        Gizmos.DrawLine(new Vector3(-2 , -0.5f, 0), new Vector3(width , -0.5f, 0));
        Gizmos.DrawLine(new Vector3(-0.5f , -2, 0), new Vector3(-0.5f , height, 0));
    }
}

#if UNITY_EDITOR
[CustomEditor (typeof(WFC))]
public class CollapseEditor : Editor {
	public override void OnInspectorGUI () {
		WFC generator = (WFC)target;
        if(GUILayout.Button("Init")){
            generator.initializeMaps();
        }
        if(GUILayout.Button("Iterate")){
            for (int i = 0; i < 1; i++)
            {
                generator.iterate();          
            }
        }

        if(GUILayout.Button("Print Cell")){
            for (int i = 0; i < 1; i++)
            {
                generator.printCellData();        
            }
        }
        if(GUILayout.Button("STOP")){
            generator.bEnabled = false;
        }
		DrawDefaultInspector ();
	}
}
#endif
