using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

public class TreePlanterScript : MonoBehaviour
{
	System.Random m_random = new System.Random();
	int m_generations = 100; //Number of time steps to simulate
	bool m_naturalAppearance = true; //Whether the trees are placed in rows or randomly
	int[,,] m_cellStatus; //Tree species for each cell in each generation [gen,x,y]
	bool[,] m_permanentDisturbanceMap; //Whether each cell is marked as permanently disturbed
	int[] m_speciesList = new int[6] {-1, 2, 13, 14, 17, 19}; //Unity tree prototypes to include in the community (-1 represents a gap with no tree)
	//Replacement Matrix.  The probability of replacement of a tree of one species 
	//by another species if it is entirely surrounded by the other species.  
	//Example [1,2] is the probability that species 2 
	//will be replaced by species 1, if species 2 is entirely surrounded by 
	//species 1.
    //NOTE: Row zero is always 0's and does not effect the simulation because 
    //gaps (communityMember 0's) do not 'replace' trees.  Gaps occur when an 
    //individual dies due to its environment/age or through disturbance.  
    //Row zero only exists to keep the array indexes meaningful 
    //(row index 1= species 1, row index 2 = species 2, etc).  
    //Column zero IS significant.  It represents the probability that a 
    //species will colonize a gap if the gap is entirely surrounded by that 
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
    bool m_disturbanceOnly = false; //Whether we are loading only a new disturbance map or all data from the webform TODO- Does this need to be global?
    float m_ongoingDisturbanceRate = 0.0f;
	int m_terrainMap = 0; //Which terrain map we are using
    //TODO: These map values are being read from the webform but we aren't using them for anything.  
    //Implementing this will require changes to the Soil Module and its interface.
    int m_salinityMap = 0;
    int m_drainageMap = 0;
    int m_fertilityMap = 0;
    //Number of x and z cells (horizontal plane is xz in Unity3D)
	int m_xCells = 200;
	int m_zCells = 200;
	//Default scale for each tree prototype.
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
	float[] m_prototypeScales; 
	string m_configPath = "http://vmeadowga.aduffy70.org/"; //Url path to community config settings
	int[,,] m_age; //Tracks the age of each plant in each generation.  We need to store age in each generation for when we run a new simulation from a particular starting step.
	int[,] m_totalSpeciesCounts; //Total species counts for each generation.
	Vector3[,] m_cellPositions; //Keeps track of the region coordinates where each plant will be placed so we only have to calculate them once.
	int m_displayedGeneration = 0; //Which generation number is currently visualized
	string m_chosenGeneration = "-";
	List<int> m_loggedGenerations = new List<int>();
	bool m_showLogWindow = false;

	#region Unity3D specific functions

	void Start()
	{
		//Initialize the list of prototype scales
		m_prototypeScales = new float[20] {m_scale0, m_scale1, m_scale2, 
											  m_scale3, m_scale4, m_scale5, 
											  m_scale6, m_scale7, m_scale8, 
											  m_scale9, m_scale10, m_scale11, 
											  m_scale12, m_scale13, m_scale14, 
											  m_scale15, m_scale16, m_scale17, 
											  m_scale18, m_scale19};
		//In production there would never be trees in the scene at startup, but
		//during development it can happen, so delete them just in case.
		DeleteAllTrees();	
	}
		
	void Update()
	{
		//Called once per frame
	}

	void OnApplicationQuit()
	{
		DeleteAllTrees();
	}
	
	void OnGUI()
	{
		//Generate the GUI controls and HUD
		GUI.Box(new Rect(5, 10, 155, 170), "Simulation");
		bool randomizeButton = GUI.Button(new Rect(10, 35, 80, 20), 
										  new GUIContent("Randomize", 
										  "Generate a new random community"));
		bool firstButton = GUI.Button(new Rect (10, 60, 22, 20),
										new GUIContent("[<",
										"First simulation step"));										  
		bool reverse10Button = GUI.Button(new Rect (34, 60, 25, 20),
										new GUIContent("<<",
										"Skip forward 10 simulation steps"));
		bool reverseButton = GUI.Button(new Rect (61, 60, 20, 20),
										new GUIContent("<",
										"Previous simulation step"));
		bool forwardButton = GUI.Button(new Rect (83, 60, 20, 20),
										new GUIContent(">",
										"Next simulation step"));
		bool forward10Button = GUI.Button(new Rect (105, 60, 25, 20),
										new GUIContent(">>",
										"Skip backward simulation steps"));
		bool lastButton = GUI.Button(new Rect (132, 60, 22, 20),
										new GUIContent(">]",
										"Last simulation step"));								
		m_chosenGeneration = GUI.TextField(new Rect(10, 85, 40, 20),
									m_chosenGeneration, 4);
		bool goButton = GUI.Button(new Rect(55, 85, 35, 20),
											 new GUIContent("Go",
											 "View the selected simulation step")); 
		bool logButton = GUI.Button(new Rect(10, 110, 50, 20), 
										new GUIContent("Log", 
										"Store data for the current simulation step"));
		bool clearButton = GUI.Button(new Rect(70, 110, 50, 20), 
										new GUIContent("Clear", 
										"Clear all logged data"));
		bool showButton = GUI.Button(new Rect(10, 135, 50, 20), 
										new GUIContent("Show", 
										"Show logged data"));
		GUI.Label(new Rect(165, 35, 200, 100), GUI.tooltip);
		if (m_showLogWindow)
		{
			Rect logWindow = GUI.Window(0, new Rect(150, 10, 400, 400), DisplayLogWindow, "Log data");
		}	
		if (randomizeButton)
		{
			DeleteAllTrees();
			GenerateRandomCommunity();
			RunSimulation();
			VisualizeGeneration(0);
			m_chosenGeneration = m_displayedGeneration.ToString();
		}
		if (firstButton)
		{
			//Visualize first simulation step
			if (m_displayedGeneration != 0)
			{
				VisualizeGeneration(0);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}
		if (reverse10Button)
		{
			//Move the visualization back 10 simulation steps
			int newGeneration = m_displayedGeneration - 10;
			if (newGeneration < 0)
			{
				newGeneration = 0;
			}
			if (newGeneration != m_displayedGeneration)
			{
				VisualizeGeneration(newGeneration);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}
		if (reverseButton)
		{
			//Visualize the previous simulation step
			int newGeneration = m_displayedGeneration - 1;
			if (newGeneration >= 0)
			{
				VisualizeGeneration(newGeneration);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}
		if (forwardButton)
		{
			//Visualize the next simulation step
			int newGeneration = m_displayedGeneration + 1;
			if (newGeneration <= m_generations - 1)
			{
				VisualizeGeneration(newGeneration);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}
		if (forward10Button)
		{
			//Move the visualization forward 10 steps
			int newGeneration = m_displayedGeneration + 10;
			if (newGeneration > m_generations - 1)
			{
				newGeneration = m_generations - 1;
			}
			if (newGeneration != m_displayedGeneration)
			{
				VisualizeGeneration(newGeneration);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}
		if (lastButton)
		{
			//Visualize the last simulation step
			if (m_displayedGeneration != m_generations - 1)
			{
				VisualizeGeneration(m_generations - 1);
				m_chosenGeneration = m_displayedGeneration.ToString();
			}
		}		
		if (goButton)
		{
			//Visualize the simulation step entered in the text box
			try
			{
				int chosenGeneration = System.Int32.Parse(m_chosenGeneration);
				if (chosenGeneration >= 0 && chosenGeneration <= m_generations - 1)
				{
					VisualizeGeneration(chosenGeneration);
				}
				else
				{
					//TODO - DIsplay some error message (invalid step)
				}
			}
			catch
			{
				//TODO - Display some error message (not a valid integer)
			}
		}
		if (logButton)
		{
			LogSummaryStatistics(m_displayedGeneration);
		}
		if (clearButton)
		{
			ClearLog();
		}
		if (showButton)
		{
			m_showLogWindow = !m_showLogWindow;
			ShowLog();
		}
	}
	
	void DisplayLogWindow(int windowID)
	{
		if (GUI.Button(new Rect(330,370,50,20), "Close"))
		{
			m_showLogWindow = !m_showLogWindow;
		}
		string logData = GetLogData();
		if (m_loggedGenerations.Count > 0)
		{
			GUI.TextArea(new Rect(5, 20, 390, 350), logData);
		}
		else
		{
			GUI.TextArea(new Rect(5, 20, 390, 350), "You haven't logged any data yet");
		}
		GUI.DragWindow(new Rect(0, 0, 10000, 10000));
	}
	
	string GetLogData()
	{
		//Generates a string of data for all logged generations
		string logData = "Gaps, Species1, Species2, Species3, Species4, Species5\n";
		foreach(int generation in m_loggedGenerations)
		{
			logData += (generation.ToString() + "," +
						m_totalSpeciesCounts[generation, 0].ToString() + ", " +
						m_totalSpeciesCounts[generation, 1].ToString() + ", " +
						m_totalSpeciesCounts[generation, 2].ToString() + ", " +
				    	m_totalSpeciesCounts[generation, 3].ToString() + ", " +
						m_totalSpeciesCounts[generation, 4].ToString() + ", " +
						m_totalSpeciesCounts[generation, 5].ToString() + "\n");
		}
		return logData;
	}
	#endregion
		
	#region Visualization functions
	
	void VisualizeGeneration(int generation)
    {
        //Update the visualization with plants from a particular generation.
        //Remove old trees from the terrain
        Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
        //Add new trees to the terrain
        for (int z=0; z<m_zCells; z++)
        {
            for (int x=0; x<m_xCells; x++)
            {
                int newTreeSpecies = m_cellStatus[generation, x, z];
                int newTreePrototype = m_speciesList[newTreeSpecies];
                Vector3 newTreeLocation = m_cellPositions[x, z];
                //AddTree(newTreeLocation, newTreePrototype);
                AddTree(newTreeLocation, newTreeSpecies, m_age[generation, x, z]);
            }
        }
        Terrain.activeTerrain.Flush();
        CalculateSummaryStatistics(generation, m_displayedGeneration, true);
        m_displayedGeneration = generation;
        print("Generation: " + m_displayedGeneration); //TODO Move this to the HUD?
    }
	
	void AddTree(Vector3 position, int treeSpecies, int age)
	{
		//Add a tree to the terrain sized according to its age
		if (treeSpecies != 0) // -1 would be a gap (no tree)
		{
			int treePrototype = m_speciesList[treeSpecies];
			TreeInstance tree = new TreeInstance();
			tree.position = position;
			tree.prototypeIndex = treePrototype;
			//Vary the height and width of individual trees according to age
			float scale = m_prototypeScales[treePrototype] * (
						  (age / (float)m_lifespans[treeSpecies]) + 0.5f);
			tree.widthScale = scale;
			tree.heightScale = scale;
			tree.color = Color.white;
			tree.lightmapColor = Color.white;
			Terrain.activeTerrain.AddTreeInstance(tree);
		}
	}
	
	void DeleteAllTrees()
	{
		//Remove all trees from the terrain
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
	
	void ClearLog()
	{
		//Delete all logged data
		m_loggedGenerations = new List<int>();
	}
	
	void LogSummaryStatistics(int generation)
	{
		//Stores a list of the generations that have been logged so we can display
		//Statistics for those generations when requested
		m_loggedGenerations.Add(generation);
	}
	
	void ShowLog()
	{
		//Display logged summary statistics
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
	
	#endregion

	#region Simulation functions

	void GenerateRandomCommunity()
	{
		//Generate starting matrix with random species and determine the 
		//region x,y,z coordinates where each tree will be placed
		m_cellStatus = new int[m_generations, m_xCells, m_zCells];
        m_age = new int[m_generations, m_xCells, m_zCells];
        m_totalSpeciesCounts = new int[m_generations, 6];
        m_cellPositions = new Vector3[m_xCells, m_zCells];
        //Make a default disturbance map with no permanent disturbances
        m_permanentDisturbanceMap = new bool[m_xCells, m_zCells];
        for (int z=0; z<m_zCells; z++)
        {
            for (int x=0; x<m_xCells; x++)
            {
            	Vector3 position;
                if (m_naturalAppearance)
              	{
                	//Randomize about the cell position a bit
                	//Coordinates for trees on the terrain range from 0-1.0
					//To evenly space trees on each axis we need to divide 1.0 by 
					//the number of x or z cells
					position = new Vector3(x * (1.0f / m_xCells) + Random.Range(-0.01f, 0.01f), 
										   0.0f, 
										   z * (1.0f / m_zCells) + Random.Range(-0.01f, 0.01f));
				}
				else
				{
					position = new Vector3(x * (1.0f / m_xCells), 0.0f, z * (1.0f / m_zCells));
				}
				//Store the coordinates of each position so we don't have to recalculate them
				m_cellPositions[x, z] = position;
				//Choose a species at random
				int newSpecies = Random.Range(0,6);
                m_cellStatus[0, x, z] = newSpecies;
                //Assign a random age to each plant so there isn't a massive 
                //dieoff early in the simulation, but skew the distribution 
                //of ages downward or we will start with a dieoff because a 
                //random selection of ages has many more old ages than expected.
                //TODO - there is probably a distribution we could draw from to
                //do this better.
                m_age[0, x, z] = Random.Range(0, m_lifespans[newSpecies] / 3);             
                m_totalSpeciesCounts[0, newSpecies]++;
            }
        }
	}
	
	void RunSimulation()
    {
    	//Generate the simulation data
        for (int generation=0; generation<m_generations - 1; generation++)
        {
            if (generation % 100 == 0)
            {
                //Provide status updates every 100 generations
                print("Generation# " + generation); //TODO- this needs to be on the hud
            }
            int nextGeneration = generation + 1;
            int rowabove;
            int rowbelow;
            int colleft;
            int colright;
            bool[,] disturbance = CalculateDisturbance();
            for (int z=0; z<m_zCells; z++)
            {
                rowabove = z + 1;
                rowbelow = z - 1;
                for (int x=0; x<m_xCells; x++)
                {
                    colright = x + 1;
                    colleft = x - 1;
                    int currentSpecies = m_cellStatus[generation, x, z];
                    if (currentSpecies != -1) //Don't ever try to update a permanent gap
                    {
                        if (disturbance[x, z])
                        {
                            m_cellStatus[nextGeneration, x, z] = 0;
                            m_totalSpeciesCounts[nextGeneration, 0]++;
                            m_age[nextGeneration, x, z] = 0;
                        }
                        else
                        {
                            //Get species counts of neighbors
                            int[] neighborSpeciesCounts = GetNeighborSpeciesCounts(x, z, rowabove,
                            							  rowbelow, colright, colleft, generation);
                            //Determine plant survival based on age and environment
                            bool plantSurvives = CalculateSurvival(currentSpecies, 
                            					 m_age[generation, x, z], m_cellPositions[x, z]);
                            if (plantSurvives)
                            {
                                //Calculate replacement probabilities based on current plant
                                float[] replacementProbability = GetReplacementProbabilities(
                                								 currentSpecies, 
                                								 neighborSpeciesCounts, 
                                								 generation);
                                //Determine the next generation plant based on those probabilities
                                int newSpecies = SelectNextGenerationSpecies(replacementProbability,
                                				 currentSpecies);
                                if (newSpecies == -1)
                                {
                                    //The old plant is still there
                                    m_age[nextGeneration, x, z] = m_age[generation, x, z] + 1;
                                    m_cellStatus[nextGeneration, x, z] = currentSpecies;
                                    m_totalSpeciesCounts[nextGeneration, currentSpecies]++;
                                }
                                else
                                {
                                    //The old plant has been replaced (though possibly by another 
                                    //of the same species...)
                                    m_age[nextGeneration, x, z] = 0;
                                    m_cellStatus[nextGeneration, x, z] = newSpecies;
                                    m_totalSpeciesCounts[nextGeneration, newSpecies]++;
                                }
                            }
                            else
                            {
                                //Calculate replacement probabilities based on a gap
                                float[] replacementProbability = GetReplacementProbabilities(0, 
                                								 neighborSpeciesCounts, 
                                								 generation);
                                m_age[nextGeneration, x, z] = 0;
                                //Determine the next generation plant based on those probabilities
                                int newSpecies = SelectNextGenerationSpecies(replacementProbability, 0);
                                if (newSpecies == -1)
                                {
                                    //No new plant was selected.  It will still be a gap.
                                    m_cellStatus[nextGeneration, x, z] = 0;
                                    m_totalSpeciesCounts[nextGeneration, 0]++;
                                }
                                else
                                {
                                    //Store the new plant status and update the total species counts
                                    m_cellStatus[nextGeneration, x, z] = newSpecies;
                                    m_totalSpeciesCounts[nextGeneration, newSpecies]++;
                                }
                            }
                        }
                    }
                    else
                    {
                        //Permanent gaps stay gaps
                        m_cellStatus[nextGeneration, x, z] = -1;
                    }
                }
            }
        }
    }
	
	int[] GetNeighborSpeciesCounts(int x, int z, int rowabove, int rowbelow, int colright, 
								   int colleft, int generation)
    {
        //Get counts of neighborspecies
        //Edge cells will have fewer neighbors.  That is ok.  We only care about 
        //the count of neighbors of each species so a neighbor that is a gap or 
        //off the edge of the matrix doesn't matter.
        int[] neighborSpeciesCounts = new int[6] {0, 0, 0, 0, 0, 0};
        int neighborType;
        if (colleft >= 0)
        {
            neighborType = m_cellStatus[generation, colleft, z];
            if (neighborType != -1) //Don't count permanent gaps
            {
                neighborSpeciesCounts[neighborType]++;
            }
            if (rowbelow >= 0)
            {
                neighborType = m_cellStatus[generation, colleft, rowbelow];
                if (neighborType != -1)
                {
                    neighborSpeciesCounts[neighborType]++;
                }
            }
            if (rowabove < m_zCells)
            {
                neighborType = m_cellStatus[generation, colleft, rowabove];
                if (neighborType != -1)
                {
                    neighborSpeciesCounts[neighborType]++;
                }
            }
        }
        if (colright < m_xCells)
        {
            neighborType = m_cellStatus[generation, colright, z];
            if (neighborType != -1)
            {
                neighborSpeciesCounts[neighborType]++;
            }
            if (rowbelow >= 0)
            {
                neighborType = m_cellStatus[generation, colright, rowbelow];
                if (neighborType != -1)
                {
                    neighborSpeciesCounts[neighborType]++;
                }
            }
            if (rowabove < m_zCells)
            {
                neighborType = m_cellStatus[generation, colright, rowabove];
                if (neighborType != -1)
                {
                    neighborSpeciesCounts[neighborType]++;
                }
            }
        }
        if (rowbelow >= 0)
        {
            neighborType = m_cellStatus[generation, x, rowbelow];
            if (neighborType != -1)
            {
                neighborSpeciesCounts[neighborType]++;
            }
        }
        if (rowabove < m_zCells)
        {
            neighborType = m_cellStatus[generation, x, rowabove];
            if (neighborType != -1)
            {
                neighborSpeciesCounts[neighborType]++;
            }
        }
        return neighborSpeciesCounts;
	}	
	
	bool[,] CalculateDisturbance()
    {
        //Returns a matrix of true and false values representing 'disturbed' 
        //areas where no plants will be allowed to grow, and 'undisturbed' 
        //locations where plants will grow normally.  Combines the permanent 
        //disturbance sites with sites generated randomly based on the 
        //ongoing disturbance rate.
        bool[,] disturbanceMatrix = new bool[m_xCells, m_zCells]; //Defaults to all false values
        for (int z=0; z<m_zCells; z++)
        {
            for (int x=0; x<m_xCells; x++)
            {
                if (m_permanentDisturbanceMap[x, z] == true)
                {
                    disturbanceMatrix[x, z] = true;
                }
                else if (m_random.NextDouble() <= m_ongoingDisturbanceRate)
                {
                    disturbanceMatrix[x, z] = true;
                }
            }
        }
        return disturbanceMatrix;
    }
    
    bool CalculateSurvival(int species, int age, Vector3 coordinates)
    {
        //Return true if the plant survives or false if it does not
        if (species == 0) //If there is no plant it can't possibly survive...
        {
            return false;
        }
        else
        {
            //Generate a float from 0-1.0 representing the probability of 
            //survival based on plant age, and altitude
            float ageHealth = CalculateAgeHealth(age, m_lifespans[species]);
            float altitudeHealth = CalculateAltitudeHealth(coordinates.y, 
            					   m_altitudeOptimums[species], m_altitudeEffects[species]);
            //Get the soil values for the plant's coordinates and calculate a 
            //probability of survival based on those values
            Vector3 soilType = GetSoilType(coordinates);
            float salinityHealth = CalculateSoilHealth(soilType.x, m_salinityOptimums[species],
            					   m_salinityEffects[species]);
            float drainageHealth = CalculateSoilHealth(soilType.y, m_drainageOptimums[species],
            					   m_drainageEffects[species]);
            float fertilityHealth = CalculateSoilHealth(soilType.z, m_fertilityOptimums[species], 
            						m_fertilityEffects[species]);
            //Overall survival probability is the product of these separate survival probabilities
            float survivalProbability = (ageHealth * altitudeHealth * salinityHealth * 
            							 drainageHealth * fertilityHealth);
            //Select a random float from 0-1.0.  Plant survives if 
            //random number <= probability of survival
            float randomFloat = (float)m_random.NextDouble();
            if (randomFloat <= survivalProbability)
            {
                //Plant survives
                return true;
            }
            else
            {
                //Plant does not survive
                return false;
            }
        }
    }
	
	float CalculateAgeHealth(int actual, int maximumAge)
    {
        //Returns a value from 0-1.0 representing the health of an individual 
        //with an 'actual' value for some environmental parameter given the 
        //optimal value and shape. This function works for age or others 
        //parameters with a maximum rather than optimal value. Health is 
        //highest (1.0) when age = 0 and decreases linearly to 0.0 when age = maximumAge.
        float health = ((maximumAge - actual) / (float)maximumAge);
         //Don't allow return values >1 or <0
        if (health > 1.0f)
        {
            health = 1.0f;
        }
        if (health < 0f)
        {
            health = 0f;
        }
        return health;
    }
	
	float CalculateAltitudeHealth(float actual, float optimal, float shape)
    {
        //Returns a value from 0-1.0 representing the health of an individual 
        //with an 'actual' value for some environmental parameter given the 
        //optimal value and shape. This function works for altitude.  With an 
        //optimal of 50 and a shape of 1, values range (linearly) from 1.0 at 
        //50m to 0.0 at 0m.  Lower values for shape flatten the 'fitness curve'. 
        //With shape <= 0, health will always equal 1.0.
        float health = 1.0f - (System.Math.Abs(((optimal - actual) / 50f)) * shape);
        //Don't allow return values >1 or <0
        if (health > 1.0f)
        {
            health = 1.0f;
        }
        if (health < 0f)
        {
            health = 0f;
        }
        return health;
    }
	
	float[] GetReplacementProbabilities(int currentSpecies, int[] neighborSpeciesCounts, int generation)
    {
        //Calculate the probability that the current plant will be replaced by each species.
        //The first value is always 0 because gaps cannot replace a plant through competition. 
        //Gaps arise only when a plant dies and no replacement is selected.
        float[] replacementProbabilities = new float[6];
        for (int species=1; species<6; species++)
        {
        	//90% local, 9.95% distant, 0.05% out-of-area
            replacementProbabilities[species] = ((m_replacementMatrix[species, currentSpecies] *
            									 ((float)neighborSpeciesCounts[species] / 8.0f)) * 
            									 0.9f) + 
            									 ((m_replacementMatrix[species, currentSpecies] *
            									 ((float)m_totalSpeciesCounts[generation, species] /
            									  (m_xCells * m_zCells))) * 0.0995f) + 0.0005f;
        }
        return replacementProbabilities;
    }
	
	int SelectNextGenerationSpecies(float[] replacementProbability, int currentSpecies)
    {
        //Randomly determine the new species based on the replacement probablilities.  
        //We aren't concerned with the probability of replacement by no plant, 
        //since we are looking at competition between species here.
        float randomReplacement = (float)m_random.NextDouble();
        if (randomReplacement <= replacementProbability[1])
        {
            return 1;
        }
        else if (randomReplacement <= replacementProbability[2] + replacementProbability[1])
        {
            return 2;
        }
        else if (randomReplacement <= replacementProbability[3] + replacementProbability[2] +
        		 replacementProbability[1])
        {
            return 3;
        }
        else if (randomReplacement <= replacementProbability[4] + replacementProbability[3] + 
        		 replacementProbability[2] + replacementProbability[1])
        {
            return 4;
        }
        else if (randomReplacement <= replacementProbability[5] + replacementProbability[4] + 
        		 replacementProbability[3] + replacementProbability[2] + replacementProbability[1])
        {
            return 5;
        }
        else
        {
            //Indicate that the current plant was not replaced (we use -1 for 
            //this because returning the current species integer would indicate 
            //that the current individual was replaced by a new member of the 
            //same species.
            return -1;
        }
    }
	
	public Vector3 GetSoilType(Vector3 location)
    {
        //Return the soiltype vector (salinity, drainage, fertility) for a location.	
        Vector3 soil = new Vector3(0f,0f,0f);
        return soil;
    }

	float CalculateSoilHealth(float actual, float optimal, float shape)
    {
        //Returns a value from 0-1.0 representing the health of an individual 
        //with an 'actual' value for some environmental parameter given the 
        //optimal value and shape. This function works for things like soil 
        //values where the actual values will range from 0-1.0.  It doesn't 
        //have to be soil values. With an optimal of 1.0 and a shape of 1, 
        //values range (linearly) from 1.0 at 1.0 to 0.0 at 0.0.  Lower values 
        //for shape flatten the 'fitness curve'. With shape <= 0, health will 
        //always equal 1.
        float health = 1.0f - (System.Math.Abs(optimal - actual) * shape);
        //Don't allow return values >1 or <0
        if (health > 1.0f)
        {
            health = 1.0f;
        }
        if (health < 0f)
        {
            health = 0f;
        }
        return health;
    }
		
	#endregion
}



//Code I may not need but don't want to lose:
//if (Terrain.activeTerrain.terrainData.GetSteepness(location.x, location.z) < 45f)
