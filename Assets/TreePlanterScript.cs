using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

public class TreePlanterScript : MonoBehaviour
{
	int m_generations = 5000; //Number of time steps to simulate
	bool m_naturalAppearance = true; //Whether the trees are placed in rows or randomly
	int[,,] m_cellStatus; //Tree type for each cell in each generation [gen,x,y]
	bool[,] m_permanentDisturbanceMap; //Whether each cell is marked as permanently disturbed
	int[] m_communityMembers = new int[6] {0, 11, 1, 5, 16, 17}; //Default tree types to include in the community
	//Replacement Matrix.  The probability of replacement of one species by a 
	//surrounding species.  Example [1,2] is the probability that species 2 
	//will be replaced by species 1, if species 2 is entirely surrounded by 
	//species 1.
    //NOTE: Row zero is always 0's and does not effect the simulation because 
    //gaps (species 0's) do not 'replace' trees.  Gaps occur when an 
    //individual dies due to its environment/age or through disturbance.  
    //Row zero only exists to keep the array indexes meaningful 
    //(row index 1= species 1, row index 2 = species 2, etc).  
    //Column zero IS significant.  It represents the probability that a 
    //species with colonize a gap if the gap is entirely surrounded by that 
    //species.  When we calculate replacement values, we make the 
    //colonization values a multiple of the other values since colonization 
    //of a gap should be more likely than replacement of an existing tree.
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
    //TODO: These map values are being read from the webform but we aren't using them for anything.  
    //Implementing this will require changes to the Soil Module and its interface.
    int m_salinityMap = 0;
    int m_drainageMap = 0;
    int m_fertilityMap = 0;
    //Number of x and z cells (horizontal plane is xz in Unity3D)
	int m_xCells = 200;
	int m_zCells = 200;
	//Default scale for each tree type
	float m_scale0 = 1.2f; //Alder
	float m_scale1 = 1.0f; //Bamboo
	float m_scale2 = 3.0f; //Grass leaves
	float m_scale3 = 80.0f; //Banyan
	float m_scale4 = 5.0f; //Bush1
	float m_scale5 = 7.0f; //Bush2
	float m_scale6 = 6.0f; //Bush3
	float m_scale7 = 7.0f; //Bush4
	float m_scale8 = 5.0f; //Bush5
	float m_scale9 = 2.0f; //Bush5 Low Poly
	float m_scale10 = 5.0f; //Bush6
	float m_scale11 = 2.0f; //Bush6 Low Poly
	float m_scale12 = 7.0f; //Bush7
	float m_scale13 = 3.0f; //Fern
	float m_scale14 = 0.5f; //Japanese Maple
	float m_scale15 = 0.3f; //Mimosa
	float m_scale16 = 75.0f; //Palm (group)
	float m_scale17 = 1.0f; //Cots Pinetype
	float m_scale18 = 1.2f; //Sycamore
	float m_scale19 = 1.5f; //Willow
	float[] m_treeScales; 
	string m_configPath = "http://vmeadowga.aduffy70.org/"; //Url path to community config settings	

	#region Unity3D specific functions

	// Use this for initialization
	void Start()
	{
		m_treeScales = new float[20] {m_scale0, m_scale1, m_scale2, 
											  m_scale3, m_scale4, m_scale5, 
											  m_scale6, m_scale7, m_scale8, 
											  m_scale9, m_scale10, m_scale11, 
											  m_scale12, m_scale13, m_scale14, 
											  m_scale15, m_scale16, m_scale17, 
											  m_scale18, m_scale19};
		ClearAllTrees();	
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
		bool randomizeButton = GUI.Button(new Rect(20, 40, 80, 20), 
										  new GUIContent("Randomize", 
										  "Generate a new random community"));
		bool testButton = GUI.Button(new Rect(20, 70, 80, 20), 
										new GUIContent("Test", 
										"Not useful yet"));
		GUI.Label(new Rect(115, 35, 200, 100), GUI.tooltip);
		if (randomizeButton)
		{
			ClearAllTrees();
			GenerateRandomCommunity();
		}
		if (testButton)
		{
			
		}
	}
	
	#endregion
	
	#region Visualization functions
	
	void AddTree(Vector3 location, int index)
	{
		//if (Terrain.activeTerrain.terrainData.GetSteepness(location.x, location.z) < 45f)
		//{
		TreeInstance tree = new TreeInstance();
		tree.position = location;
		tree.prototypeIndex = index;
		//Vary the height and width of individual trees a bit
		float scaleHeight = m_treeScales[index] * Random.Range(0.75f, 1.5f);
		float scaleWidth = m_treeScales[index] * Random.Range(0.75f, 1.5f);
		tree.widthScale = scaleWidth;
		tree.heightScale = scaleHeight;
		tree.color = Color.white;
		tree.lightmapColor = Color.white;
		Terrain.activeTerrain.AddTreeInstance(tree);
		//}
	}
	
	void GenerateRandomCommunity()
	{
		//Coordinates for trees on the terrain range from 0-1.0
		//To evenly space trees on each axis we need to divide 1.0 by 
		//the number of x or z cells
		float xSpacing = 1.0f / m_xCells;
		float zSpacing = 1.0f / m_zCells;
		for (float x=0.0f; x<1.0f; x=x+xSpacing)
		{
			for (float z=0.0f; z<1.0f; z=z+zSpacing)
			{
				Vector3 location;
				if (m_naturalAppearance)
				{
					//Randomize about the location a bit
					location = new Vector3(x + Random.Range(-0.01f, 0.01f), 
										   0.0f, 
										   z + Random.Range(-0.01f, 0.01f));
				}
				else
				{
					location = new Vector3(x, 0.0f, z);
				}
				//Pick a random tree
				int index = Random.Range(0, 20);
				AddTree(location, index);
			}
		}
		Terrain.activeTerrain.Flush();
	}
	
	void ClearAllTrees()
	{
		Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
		Terrain.activeTerrain.Flush();
	}
	
	void OnCycleTimer(object source, ElapsedEventArgs e)
	{
		//Display the next generation forward or backward
	}
	
	void CalculateSummaryStatistics(int generation, int lastVisualizedGeneration, bool needToLog)
	{
		//Generate summary statistics for the currently viewed generation
	}
	
	void ClearLogs()
	{
		//Delete all logged data
	}
	
	void LogSummaryStatistics(string logString)
	{
		//Write summary statistics to the log... however we are going to handle this now...
	}
	
	void DisplaySummaryStatistics(string hudString)
	{
		//Display summary statistics on the HUD
	}
	
	void StartCycling(bool isReverse)
	{
		//Start stepping forward or backward through the generations
	}
	
	void StopCycling()
	{
		//Stop stepping through the generations
	}
	
	void VisualizeGeneration(int nextGeneration)
	{
		//Update the trees in the community to show the next generation
	}
	
	#endregion

	#region Simulation functions
	
	int[] GetNeighborTreeTypeCounts(int x, int y, int rowAbove, int rowBelow, int colRight, int colLeft, int generation)
	{
		//Get the counts of how many of each tree type are neighboring cell x,y
		return new int[0];
	}
	
	float GetReplacementProbabilities(int currentSpecies, int[] neighborSpeciesCounts, int generation)
	{
		//Calculate the probability that the current plant will be replaced by each species
		return 0f;
	} 
	
	
	#endregion
}



//Code I may not need but don't want to lose:
//if (Terrain.activeTerrain.terrainData.GetSteepness(location.x, location.z) < 45f)

//	void ChangeManyTrees()
//	{
//		//Test function to change all trees in the scene
//		TreeInstance[] oldTrees = Terrain.activeTerrain.terrainData.treeInstances;
//		Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
//		for (int i=0; i< oldTrees.Length; i++)
//		{
//			if (oldTrees[i].prototypeIndex == 19)
//			{
//				oldTrees[i].prototypeIndex = 0;				
//			}
//			else
//			{
//				oldTrees[i].prototypeIndex++;
//			}
//			float scaleHeight = m_treeScales[oldTrees[i].prototypeIndex] * Random.Range(0.75f, 1.5f);
//			float scaleWidth = m_treeScales[oldTrees[i].prototypeIndex] * Random.Range(0.75f, 1.5f);
//			oldTrees[i].widthScale = scaleWidth;
//			oldTrees[i].heightScale = scaleHeight;
//			Terrain.activeTerrain.AddTreeInstance(oldTrees[i]);
//		}
//		Terrain.activeTerrain.Flush();	
//	}

