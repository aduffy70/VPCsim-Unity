using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TreePlanterScript : MonoBehaviour
{
	int m_generations; //Number of time steps to simulate
	bool m_naturalAppearance; //Whether the plants are placed in rows or randomly
	int[,,] m_cellStatus; //Plant type for each cell in each generation [gen,x,y]
	bool[,] m_permanentDisturbanceMap; //Whether eac cell is marked as permanently disturbed
	int[] m_communityMembers = new int[6] {0, 11, 1, 5, 16, 17}; //Default plants to include in the community
	//Replacement Matrix.  The probability of replacement of one species by a surrounding species.  Example [1,2] is the probability that species 2 will be replaced by species 1, if species 2 is entirely surrounded by species 1.
        //NOTE: Row zero is always 0's and does not effect the simulation because gaps (species 0's) do not 'replace' plants.  Gaps occur when an individual dies due to its environment/age or through disturbance.  Row zero only exists to keep the array indexes meaningful (row index 1= species 1, row index 2 = species 2, etc).  Column zero IS significant.  It represents the probability that a species with colonize a gap if the gap is entirely surrounded by that species.  When we calculate replacement values, we make the colonization values a multiple of the other values since colonization of a gap should be more likely than replacement of an existing plant.
	float[,] m_replacementMatrix = new float[6,6] {
    	{0.0f, 0f, 0f, 0f, 0f, 0f},
        {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
        {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
        {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
        {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
        {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f}};  //Equivalent to all M's
    int[] m_lifespans = new int[6] {0, 25, 25, 25, 25, 25}; //Maximum age for each species
    //Optimal values and shape parameters for each species
    float[] m_altitudeOptimums = new float[6] {0f, 35f, 35f, 35f, 35f, 35f};
    float[] m_altitudeEffects = new float[6] {0f, 0f, 0f, 0f, 0f, 0f};
    float[] m_salinityOptimums = new float[6] {0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f};
    float[] m_salinityEffects = new float[6] {0f, 0f, 0f, 0f, 0f, 0f};
    float[] m_drainageOptimums = new float[6] {0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f};
    float[] m_drainageEffects = new float[6] {0f, 0f, 0f, 0f, 0f, 0f};
    float[] m_fertilityOptimums = new float[6] {0f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f};
    float[] m_fertilityEffects = new float[6] {0f, 0f, 0f, 0f, 0f, 0f};
    bool m_disturbanceOnly = false;
    float m_ongoingDisturbanceRate = 0.0f;
	int m_terrainMap = 0;
    //TODO: These map values are being read from the webform but we aren't using them for anything.  Implementing this will require changes to the Soil Module and its interface.
    int m_salinityMap = 0;
    int m_drainageMap = 0;
    int m_fertilityMap = 0;
	int m_xCells = 200;
	int m_yCells = 200;
	public float m_scale0 = 0.5f;
	public float m_scale1 = 0.5f;
	public float m_scale2 = 1.0f;
	public float m_scale3 = 1.0f;
	public float m_scale4 = 1.0f;
	public float m_scale5 = 1.0f;
	public float m_scale6 = 1.0f;
	public float m_scale7 = 1.0f;
	public float m_scale8 = 1.0f;
	public float m_scale9 = 1.0f;
	public float m_scale10 = 1.0f;
	public float m_scale11 = 1.0f;
	public float m_scale12 = 1.0f;
	public float m_scale13 = 1.0f;
	public float m_scale14 = 0.5f;
	public float m_scale15 = 0.5f;
	public float m_scale16 = 1.0f;
	public float m_scale17 = 0.5f;
	public float m_scale18 = 0.5f;
	public float m_scale19 = 0.5f;
	float[] m_plantDefaultScales; 	

	// Use this for initialization
	void Start()
	{
		m_plantDefaultScales = new float[20] {m_scale0, m_scale1, m_scale2, m_scale3, m_scale4, m_scale5, m_scale6, m_scale7, m_scale8, m_scale9, m_scale10, m_scale11, m_scale12, m_scale13, m_scale14, m_scale15, m_scale16, m_scale17, m_scale18, m_scale19};

		ClearAllTrees();	
		GenerateRandomCommunity();
	}
		
	// Update is called once per frame
	void Update()
	{
	}

	//Before shutting down
	void OnApplicationQuit()
	{
		ClearAllTrees();
	}
	
	//Generate the GUI controls and HUD
	void OnGUI()
	{
		GUI.Box(new Rect(10, 10, 100, 90), "Inworld Controls");
		bool randomizeButton = GUI.Button(new Rect(20, 40, 80, 20), new GUIContent("Randomize", "Generate a new random community"));
		bool anotherButton = GUI.Button(new Rect(20, 70, 80, 20), new GUIContent("Null", "This button doesn't work"));
		GUI.Label(new Rect(115, 35, 200, 100), GUI.tooltip);
		if (randomizeButton)
		{
			ChangeManyTrees();
		}
	}
	
	void PlantTree(Vector3 location, int index)
	{
		//if (Terrain.activeTerrain.terrainData.GetSteepness(location.x, location.z) < 45f)
		//{
			TreeInstance tree = new TreeInstance();
			tree.position = location;
			tree.prototypeIndex = index;
			//float scale = 1.0f;
			//float scale = Random.Range(0.5f, 1.5f);
			float scale = m_plantDefaultScales[index];
			tree.widthScale = scale;
			tree.heightScale = scale;
			tree.color = Color.white;
			tree.lightmapColor = Color.white;
			Terrain.activeTerrain.AddTreeInstance(tree);
		//}
	}
	
		
	void ChangeManyTrees()
	{
		//Test function to change all trees in the scene
		TreeInstance[] oldTrees = Terrain.activeTerrain.terrainData.treeInstances;
		Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
		for (int i=0; i< oldTrees.Length; i++)
		{
			if (oldTrees[i].prototypeIndex == 19)
			{
				oldTrees[i].prototypeIndex = 0;
			}
			else
			{
				oldTrees[i].prototypeIndex++;
			}
			Terrain.activeTerrain.AddTreeInstance(oldTrees[i]);
		}
		Terrain.activeTerrain.Flush();	
	}
		
	void GenerateRandomCommunity()
	{
		int tempCount = 0;
		for (float x=0.0f; x<1.0f;x=x+0.004f)
		{
			for (float z=0.0f; z<1.0f; z=z+0.01f)
			{
				//Randomize about the location a bit
				//Vector3 location = new Vector3(x + Random.Range(-0.01f, 0.01f), 
				//							   0.0f, z + Random.Range(-0.01f, 0.01f));
				Vector3 location = new Vector3(x, 0.0f, z);
				//int index = Random.Range(0, 20);
				int index = tempCount % 20;
				PlantTree(location, index);
				tempCount++;
			}
		}
		Terrain.activeTerrain.Flush();
	}
	
	void ClearAllTrees()
	{
		Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
		Terrain.activeTerrain.Flush();
	}
}
