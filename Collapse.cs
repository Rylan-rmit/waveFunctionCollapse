using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
//public enum ETiles {Empty = -1, DeepWater = 0, ShallowWater = 1, Sand = 2, GrassEmpty = 3, GrassFlower = 4, GrassSmallBush = 5, GrassMediumBush = 6, DeepWater2 = 7, DeepWater3 = 8, DeepWater4 = 9};
public enum ETiles {Empty = -1, DeepWater = 0, ShallowWater = 1, Sand = 2, GrassEmpty = 3, GrassFlower = 4, GrassSmallBush = 5};

public class Collapse : MonoBehaviour
{
    public List<Tile> tiles = new List<Tile>();
    public List<UnityEngine.Object> tilePrefabs = new List<UnityEngine.Object>();
    //private int[,] tileMap;
    //private int[,,] entropyMap;
    //private List<List<List<float>>> entropyMap = new List<List<List<float>>>(); // [Height] [Width] [NeighTileNum] : value {weight}
    //List<List<float>> entropies = new List<List<float>>();
    public int height = 50;
    public int width = 50;

    public Grid grid;
    public bool bEnabled = false;

    private void Start() {
        initializeMaps();
    }
    public GameObject output;
    private int count = 0;

    public void initializeMaps()
    {
        grid = new Grid(height, width);
        bEnabled = true;

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



    private void calculateEntropy(int x, int y)
    {
        List<float> neighbourWeight = new List<float>();
        List<bool> tileFound = new List<bool>();
        for (int i = 0; i < tiles.Count; i ++)
        {
            neighbourWeight.Add(1f);
            tileFound.Add(true);
        }

        if (y + 1 < height)
        {
            if ((int)grid.getCell(x,y+1).tileType != -1)
            {
                tileFound = calculateValidN(tileFound, x, y+1);
                neighbourWeight = calculateTileChance(neighbourWeight, x, y+1);
            }
        }

        if (x + 1 < width)
        {
            if ((int)grid.getCell(x+1,y).tileType != -1)
            {
                tileFound = calculateValidN(tileFound, x+1, y);
                neighbourWeight = calculateTileChance(neighbourWeight, x+1, y);
            }
        }

        if (y - 1 >= 0)
        {
            if ((int)grid.getCell(x,y-1).tileType != -1)
            {
                tileFound = calculateValidN(tileFound, x, y-1);
                neighbourWeight = calculateTileChance(neighbourWeight, x, y-1);
            }
        }

        if (x - 1 >= 0)
        {
            if ((int)grid.getCell(x-1,y).tileType != -1)
            {
                tileFound = calculateValidN(tileFound, x-1, y);
                neighbourWeight = calculateTileChance(neighbourWeight, x-1, y);
            }
        }

        for (int i = 0; i < tileFound.Count; i++)
        {
            if (tileFound[i] == false)
            {
                neighbourWeight[i] = 0;
            }
        }

        float flEntropy = entropy(neighbourWeight);
        grid.getCell(x,y).entropy = flEntropy;
        grid.getCell(x,y).setTileAllowed(tileFound);
        grid.getCell(x,y).setTileChance(neighbourWeight);
    }

    private List<bool> calculateValidN(List<bool> boolList, int x, int y)
    {
        List<bool> tileData = grid.getCell(x, y).getAllowedNeighbours();

        for (int i = 0; i < boolList.Count; i++)
        {
            boolList[i] = (tileData[i] && boolList[i]);
        }

        return boolList;
    }

    private List<float> calculateTileChance(List<float> chanceList, int x, int y)
    {
        List<float> cellData = grid.getCell(x, y).getNeighbourWeights();

        for (int i = 0; i < chanceList.Count; i++)
        {
            chanceList[i] += cellData[i];
        }
        return chanceList;
    }

    public void placeTile(int x, int y)
    {
        List<float> weights = grid.getCell(x,y).getTileChance();
        float tileWeightSum = weights.Sum();
        List<float> weightsAgg = new List<float>();

        for (int i = 0; i < weights.Count; i++)
        {
            if (i>0)
            {
                weightsAgg.Add(weights[i] + weightsAgg[i-1]);
            }
            else
            {
                weightsAgg.Add(weights[i]);
            }
        }

        float tileRNGResult = UnityEngine.Random.Range(0.0f, tileWeightSum);

        ETiles tileResult = ETiles.DeepWater;

        for (int i = weights.Count-1; i >= 0; i--)
        {
            if (i == weights.Count-1)
            {
                if (tileRNGResult <= weightsAgg[i])
                {
                    tileResult = (ETiles)i;
                }
            }
            else
            {
                if (tileRNGResult < weightsAgg[i])
                {
                    tileResult = (ETiles)i;
                } 
            }
        }
        grid.getCell(x,y).tileType = tileResult;
        Vector3 pos = new Vector3(x, y, 0f);
        GameObject tile = (GameObject)Instantiate(tilePrefabs[(int)tileResult], pos, Quaternion.identity);
        tile.transform.parent = output.transform;

        grid.getCell(x,y).setCell(tiles[(int)tileResult]);

        if (y + 1 < height)
        {   
            calculateEntropy(x, y+1);
        }

        if (x + 1 < width)
        {
            calculateEntropy(x+1, y);
        }

        if (y - 1 >= 0)
        {
            calculateEntropy(x, y-1);
        }

        if (x - 1 >= 0)
        {
            calculateEntropy(x-1, y);
        }

    }

    private List<int> findLowestEntropy()
    {
        List<int> tileYX = new List<int>{0,0};

        float lowestEntropy = 99999;

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if ( grid.getCell(j, i).entropy < lowestEntropy && (int)grid.getCell(j, i).tileType == -1)
                {
                    lowestEntropy = grid.getCell(j, i).entropy;
                    tileYX[0] = i;
                    tileYX[1] = j;
                }
            }
        }

        return tileYX;
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
        if ( !(H < 10 && H > 0) )
        {
            string result = "Weights: ";
            foreach (var item in weightsCopy)
            {
                result += item.ToString() + ", ";
            }
            print("ENTROPY");
            print("ENTROPY");
            print(result);
            print("Count: " + count);
            print("Sum: " + sum);
            print("H: " + H);
        }
        return H;
    }

    public void placeAtLowestE()
    {
        List<int> lowestEntropy = findLowestEntropy();
        placeTile(lowestEntropy[1], lowestEntropy[0]);
    }

    private void OnDrawGizmos() {
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                if (bEnabled)
                {
                    int a = 0;
                        foreach (bool item in grid.getCell(i,j).getTileAllowed())
                        {
                            if (item == true)
                            {
                                a = a + 1;
                            }
                        }


                    //drawString(grid.getCell(i, j).entropy.ToString("0.000"), new Vector3(i, j, 0));
                    drawString(a.ToString(), new Vector3(i, j, 0));
                }
            }
        }
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

    IEnumerator place()
    {
        for (int i = 0; i < 200; i++)
        {
            placeAtLowestE();
            yield return new WaitForSeconds(1f);
        }
    }
}

/* #if UNITY_EDITOR
[CustomEditor (typeof(Collapse))]
public class CollapseEditor : Editor {
	public override void OnInspectorGUI () {
		Collapse generator = (Collapse)target;
        if(GUILayout.Button("Init")){
            generator.initializeMaps();
            generator.placeTile(5,5);
            //generator.reCalcAll(); 
        }
        if(GUILayout.Button("placeTile")){
            for (int i = 0; i < 1; i++)
            {
                generator.placeAtLowestE();
                //generator.reCalcAll();             
            }

        }
        if(GUILayout.Button("STOP")){
            generator.bEnabled = false;
        }
		DrawDefaultInspector ();
	}
}
#endif

*/


