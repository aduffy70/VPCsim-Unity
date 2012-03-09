using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;

public class TreePlanterScript : MonoBehaviour
{
    System.Random m_random = new System.Random();
    string m_simulationId = "none";
    int m_generations = 201; //Number of time steps to simulate
    int[,,] m_cellStatus; //Tree species for each cell in each generation [gen,x,y]
    bool[,] m_permanentDisturbanceMap; //Whether each cell is marked as permanently disturbed
    int[] m_speciesList = new int[6] {-1, 2, 13, 14, 17, 19}; //Unity tree prototypes to include
                                                              //in the community (-1 represents a
                                                              //gap with no tree)
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
    float[,] m_replacementMatrix = new float[6,6] {{0.0f, 0f, 0f, 0f, 0f, 0f},
                                                   {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
                                                   {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
                                                   {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
                                                   {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f},
                                                   {0.4f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f}};
                                                   //Equivalent to all M's
    //Store the human-readable plant names so we can display them later
    string[] m_prototypeNames = new string[20] {"Alder", "Bamboo", "Grass", "Banyan",
                                       "Bush1", "Bush2", "Bush3", "Bush4",
                                       "Bush5", "Bush5a", "Bush6", "Bush6a",
                                       "Bush7", "Fern", "Maple", "Mimosa",
                                       "Palm", "Pine", "Sycamore", "Willow"};
    float[] m_prototypeScales = new float[20] {1.2f, 1.0f, 3.0f, 80.0f,
                                               5.0f, 7.0f, 6.0f, 7.0f,
                                               5.0f, 2.0f, 5.0f, 2.0f,
                                               7.0f, 3.0f, 0.5f, 0.3f,
                                               75.0f, 1.0f, 1.2f, 1.5f};
    //Maximum age for each prototype
    int[] m_lifespans = new int[20] {25, 25, 25, 25,
                                     25, 25, 25, 25,
                                     25, 25, 25, 25,
                                     25, 25, 25, 25,
                                     25, 25, 25, 25};
    //Optimal values and shape parameters for each prototype
    float[] m_altitudeOptimums = new float[20] {35f, 35f, 35f, 35f,
                                                35f, 35f, 35f, 35f,
                                                35f, 35f, 35f, 35f,
                                                35f, 35f, 35f, 35f,
                                                35f, 35f, 35f, 35f};
    float[] m_altitudeEffects = new float[20] {0.0f, 0.0f, 0.0f, 0.0f,
                                               0.0f, 0.0f, 0.0f, 0.0f,
                                               0.0f, 0.0f, 0.0f, 0.0f,
                                               0.0f, 0.0f, 0.0f, 0.0f,
                                               0.0f, 0.0f, 0.0f, 0.0f};
    float[] m_waterLevelOptimums = new float[20] {0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,};
    float[] m_waterLevelEffects = new float[20] {0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f};
    float[] m_lightLevelOptimums = new float[20] {0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,
                                                  0.5f, 0.5f, 0.5f, 0.5f,};
    float[] m_lightLevelEffects = new float[20] {0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f,
                                                 0f, 0f, 0f, 0f};
    float[] m_temperatureLevelOptimums = new float[20] {0.5f, 0.5f, 0.5f, 0.5f,
                                                   0.5f, 0.5f, 0.5f, 0.5f,
                                                   0.5f, 0.5f, 0.5f, 0.5f,
                                                   0.5f, 0.5f, 0.5f, 0.5f,
                                                   0.5f, 0.5f, 0.5f, 0.5f,};
    float[] m_temperatureLevelEffects = new float[20] {0f, 0f, 0f, 0f,
                                                  0f, 0f, 0f, 0f,
                                                  0f, 0f, 0f, 0f,
                                                  0f, 0f, 0f, 0f,
                                                  0f, 0f, 0f, 0f};
    float m_ongoingDisturbanceRate = 0.0f;
    //int m_terrainMap = 0; //Which terrain map we are using
    //These levels range from 0-1.0.  Default to 0.5 (mid-range or normal)
    float m_waterLevel = 0.5f;
    float m_lightLevel = 0.5f;
    float m_temperatureLevel = 0.5f;
    //Number of x and z cells (horizontal plane is xz in Unity3D)
    int m_xCells = 150;
    int m_zCells = 150;
    int[,,] m_age; //Tracks the age of each plant in each generation.
    int[,] m_totalSpeciesCounts; //Total species counts for each generation.
    Vector3[,] m_cellPositions; //Keeps track of the region coordinates where each plant will
                                //be placed so we only have to calculate them once per simulation.
    int m_displayedGeneration = 0; //Which generation number is currently visualized
    string m_chosenGeneration = "0";
    string m_chosenSimulationId = "";
    string m_logString = "";
    bool m_showLogWindow = false;
    string m_parameterPath = "http://vpcsim.appspot.com";
    string m_errorText = ""; //Debug errors to display on the HUD - DEBUG
    WWW m_www; //Stores xml data downloaded from the web
    bool m_debugMode = false; //Turn the debug window on and off
    Rect m_debugWindow = new Rect(300, 10, 400, 400);
    Rect m_logWindow = new Rect(200, 5, 400, 400);
    string m_currentDataString = "";
    //Convert values from the webform to numbers the simulation can use
    //Ongoing disturbance values - None, Very Low, Low, High, Very High
    float[] m_convertDisturbance = new float[5] {0f, 0.01f, 0.03f, 0.1f, 0.25f};
    //Environmental parameter values - Very Low, Low, Normal, High, Very High
    float[] m_convertWaterLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};
    float[] m_convertLightLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};
    float[] m_convertTemperatureLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};


    #region Unity3D specific functions

    void Start()
    {
        m_errorText += "Start\n"; //Debug
        //There shouldn't be trees in the scene at startup, but just in case...
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
        GUI.Box(new Rect(5, 10, 155, 290), "Simulation");
        //Create an unused button off-screen so we have someplace to move focus out of the TextFields
        GUI.SetNextControlName("focusBuster");
        bool focusBusterButton = GUI.Button(new Rect(-10, -10, 1, 1),
                                            new GUIContent("",
                                            ""));
        bool defaultsButton = GUI.Button(new Rect(10, 35, 60, 20),
                                         new GUIContent("Defaults",
                                         "Load the default ecosystem"));
        bool createButton = GUI.Button(new Rect(75, 35, 80, 20),
                                       new GUIContent("Create new",
                                       "Create a new ecosystem"));
        m_chosenSimulationId = GUI.TextField(new Rect(10, 60, 90, 20),
                                             m_chosenSimulationId, 10);
        bool loadButton = GUI.Button(new Rect(105, 60, 50, 20),
                                     new GUIContent("Load",
                                     "Load new ecosystem parameters"));
        bool firstButton = GUI.Button(new Rect (10, 95, 22, 20),
                                      new GUIContent("[<",
                                      "First time step"));
        bool reverse10Button = GUI.Button(new Rect (34, 95, 25, 20),
                                          new GUIContent("<<",
                                          "Skip backward 10 time steps"));
        bool reverseButton = GUI.Button(new Rect (61, 95, 20, 20),
                                        new GUIContent("<",
                                        "Previous time step"));
        bool forwardButton = GUI.Button(new Rect (83, 95, 20, 20),
                                        new GUIContent(">",
                                        "Next time step"));
        bool forward10Button = GUI.Button(new Rect (105, 95, 25, 20),
                                          new GUIContent(">>",
                                          "Skip forward 10 time steps"));
        bool lastButton = GUI.Button(new Rect (132, 95, 22, 20),
                                     new GUIContent(">]",
                                     "Last time step"));
        GUI.Label(new Rect(10, 120, 35, 20), "Step:");
        m_chosenGeneration = GUI.TextField(new Rect(42, 120, 40, 20),
                                           m_chosenGeneration, 4);
        GUI.Label(new Rect(83, 120, 35, 20), "/ " + (m_generations - 1).ToString());
        bool goButton = GUI.Button(new Rect(120, 120, 35, 20),
                                   new GUIContent("Go",
                                   "View the selected time step"));
        GUI.Label(new Rect(10, 145, 135, 100), m_currentDataString);
        bool logButton = GUI.Button(new Rect(10, 250, 40, 20),
                                    new GUIContent("Log",
                                    "Show log data"));
        bool plotButton = GUI.Button(new Rect(55, 250, 45, 20),
                                     new GUIContent("Plots",
                                     "Show data plots"));
        bool debugButton = GUI.Button(new Rect(10, 275, 60, 20),
                                      new GUIContent("Debug",
                                      "Show/Hide debug messages"));
        if (!System.String.IsNullOrEmpty(GUI.tooltip))
        {
            GUI.Box(new Rect(165 , 35, 220, 20),"");
        }
        GUI.Label(new Rect(170, 35, 210, 20), GUI.tooltip);
        if (m_showLogWindow)
        {
            m_logWindow = GUI.Window(0, m_logWindow, DisplayLogWindow, "Log data");
        }
        if (m_debugMode)
        {
            m_debugWindow = GUI.Window(0, m_debugWindow, DisplayDebugWindow, "Debug");
        }
        if (defaultsButton)
        {
            DeleteAllTrees();
            GenerateRandomCommunity();
            RunSimulation();
            m_logString = GetLogData(false);
            VisualizeGeneration(0);
            m_chosenGeneration = m_displayedGeneration.ToString();
            m_chosenSimulationId = m_simulationId;
            GUI.FocusControl("focusBuster");
        }
        if (createButton)
        {
            Application.ExternalCall("window.open('http://vpcsim.appspot.com','_blank')");
        }
        if (loadButton)
        {
            bool isValidId = false;
            try
            {
                //Make sure the new simulation id can be converted to an integer (it is
                //supposed to be a datecode.)
                int newSimulationId = System.Int32.Parse(m_chosenSimulationId);
                isValidId = true;
                m_errorText += "Valid ID\n";
            }
            catch
            {
                isValidId = false;
                m_errorText += "Not Valid ID\n";
            }
            if (isValidId)
            {
                //Retrieve simulation parameters from the web
                StartCoroutine(GetNewSimulationParameters(m_chosenSimulationId));
            }
            else
            {
                //TODO - Error message: Invalid simulation ID
            }
            GUI.FocusControl("focusBuster");
        }
        if (firstButton)
        {
            //Visualize first simulation step
            if (m_displayedGeneration != 0)
            {
                VisualizeGeneration(0);
                m_chosenGeneration = m_displayedGeneration.ToString();
            }
            GUI.FocusControl("focusBuster");
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
            GUI.FocusControl("focusBuster");
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
            GUI.FocusControl("focusBuster");
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
            GUI.FocusControl("focusBuster");
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
            GUI.FocusControl("focusBuster");
        }
        if (lastButton)
        {
            //Visualize the last simulation step
            if (m_displayedGeneration != m_generations - 1)
            {
                VisualizeGeneration(m_generations - 1);
                m_chosenGeneration = m_displayedGeneration.ToString();
            }
            GUI.FocusControl("focusBuster");
        }
        if (goButton)
        {
            //Visualize the simulation step entered in the text box
            int newGeneration;
            try
            {
                newGeneration = System.Int32.Parse(m_chosenGeneration);
            }
            catch
            {
                newGeneration = m_displayedGeneration;
            }
            if (newGeneration < 0)
            {
                newGeneration = 0;
            }
            if (newGeneration > m_generations - 1)
            {
                newGeneration = m_generations - 1;
            }
            if (newGeneration != m_displayedGeneration)
            {
                VisualizeGeneration(newGeneration);
            }
            m_chosenGeneration = m_displayedGeneration.ToString();
            GUI.FocusControl("focusBuster");
        }
        if (logButton)
        {
            m_showLogWindow = !m_showLogWindow;
            m_chosenGeneration = m_displayedGeneration.ToString();
            GUI.FocusControl("focusBuster");
        }
        if (plotButton)
        {
            if (m_simulationId != "none")
            {
                DisplayPlots();
            }
            GUI.FocusControl("focusBuster");
        }
        if (debugButton)
        {
            m_debugMode = !m_debugMode;
            GUI.FocusControl("focusBuster");
        }
    }

    void DisplayPlots()
    {
        string logData = GetLogData(true);
        Application.ExternalCall("OpenCountsPlotPage", logData);
    }

    void DisplayLogWindow(int windowID)
    {
        if (GUI.Button(new Rect(330,370,50,20), "Close"))
        {
            m_showLogWindow = !m_showLogWindow;
        }
        if (m_simulationId != "none")
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), m_logString);
        }
        else
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), "You haven't logged any data yet");
        }
        GUI.DragWindow();
    }

    void DisplayDebugWindow(int windowID)
    {
        GUI.TextArea(new Rect(5, 20, 390, 350), m_errorText);
        GUI.DragWindow();
    }

    string GetCurrentData()
    {
        //Generates a string to display the current species counts formatted for the HUD
        StringBuilder currentDataBuilder = new StringBuilder();
        string currentData;
        currentDataBuilder.Append("Gaps: ");
        currentDataBuilder.Append(m_totalSpeciesCounts[m_displayedGeneration, 0].ToString() + "\n");
        for (int i=1; i<6; i++)
        {
            currentDataBuilder.Append(m_prototypeNames[m_speciesList[i]] + ": ");
            currentDataBuilder.Append(m_totalSpeciesCounts[m_displayedGeneration, i].ToString() + "\n");
        }
        currentData = currentDataBuilder.ToString();
        return currentData;
    }

    string GetLogData(bool isForPlotting)
    {
        //Generates a string of log data suitable for either displaying to humans or
        //for sending out for plotting
        StringBuilder logDataBuilder = new StringBuilder();
        string logData;
        if (isForPlotting)
        {
            //Generate string for sending out for plotting
            logDataBuilder.Append("\"time step,Gaps,");
            for (int i=1; i<6; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != 5)
                {
                    logDataBuilder.Append(",");
                }
            }
            logDataBuilder.Append("\\\n\" + ");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append("\"");
                logDataBuilder.Append(generation.ToString() + ',');
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 0].ToString() + ",");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 1].ToString() + ",");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 2].ToString() + ",");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 3].ToString() + ",");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 4].ToString() + ",");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 5].ToString() + "\\\n\"");
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append(" + ");
                }
            }
        }
        else
        {
            //Generate string for displaying to humans
            logDataBuilder.Append("Time_step, Gaps, ");
            for (int i=1; i<6; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != 5)
                {
                    logDataBuilder.Append(", ");
                }
            }
            logDataBuilder.Append("\n");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append(generation.ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 0].ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 1].ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 2].ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 3].ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 4].ToString() + ", ");
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 5].ToString());
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append("\n");
                }
            }
        }
        logData = logDataBuilder.ToString();
        print(logData);
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
                Vector3 newTreeLocation = m_cellPositions[x, z];
                AddTree(newTreeLocation, newTreeSpecies, m_age[generation, x, z]);
            }
        }
        Terrain.activeTerrain.Flush();
        m_displayedGeneration = generation;
        m_currentDataString = GetCurrentData();
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
            float scale = m_prototypeScales[treePrototype] * ((age / (float)m_lifespans[treePrototype]) + 0.5f);
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

    #endregion

    #region Simulation functions

    IEnumerator GetNewSimulationParameters(string fileName)
    {
        //Get simulation parameters from the VPCsim webapp
        string url = System.IO.Path.Combine(m_parameterPath,"data?id=" + fileName);
        m_www = new WWW(url);
        yield return m_www;
        if (m_www.isDone)
        {
            m_errorText += "WWW read isDone\n";
            bool unpackSuccess = UnpackParameters();
            if (unpackSuccess)
            {
                m_errorText += "Unpacked success\n";
                DeleteAllTrees();
                m_errorText += "DeleteTrees success\n";
                RunSimulation();
                m_errorText += "RunSimulation success\n";
                m_logString = GetLogData(false);
                m_errorText += "GetLogData success\n";
                VisualizeGeneration(0);
                m_errorText += "VisualizeGeneration success\n";
                m_chosenGeneration = m_displayedGeneration.ToString();
                m_chosenSimulationId = m_simulationId;
            }
            else
            {
                m_errorText += "Unpack fail\n";
            }
        }
        else
        {
            m_errorText += "WWW not isDone\n";
        }
    }

    bool UnpackParameters()
    {
        m_errorText += "Unpacking\n";
        //Unpack the xml data into appropriate variables
        Dictionary<string, string> newParameters = new Dictionary<string, string>();
        XmlTextReader reader;
        try
        {
            reader = new XmlTextReader(new System.IO.StringReader(m_www.text));
            m_errorText += "opened stream\n";
            reader.WhitespaceHandling = WhitespaceHandling.Significant;
        }
        catch
        {
            return false;
        }
        while (reader.Read())
        {
            if (reader.Name == "property")
            {
                string parameterName = reader.GetAttribute("name");
                string parameterValue = reader.ReadString();
                newParameters.Add(parameterName, parameterValue);
            }
        }
        //Store all parameters
        m_simulationId = newParameters["id"];
        //m_terrainMap = System.Int32.Parse(newParameters["terrain"]);
        int waterLevelCode = System.Int32.Parse(newParameters["water_level"]);
        m_waterLevel = m_convertWaterLevel[waterLevelCode];
        int lightLevelCode = System.Int32.Parse(newParameters["light_level"]);
        m_lightLevel = m_convertLightLevel[lightLevelCode];
        int temperatureLevelCode = System.Int32.Parse(newParameters["temperature_level"]);
        m_temperatureLevel = m_convertTemperatureLevel[temperatureLevelCode];
        string[] speciesList = newParameters["plant_types"].Split(',');
        for (int i = 1; i<6; i++) //Start at index 1 so index 0 stays "None" to represent gaps
        {
            m_speciesList[i] = System.Int32.Parse(speciesList[i - 1]);
        }
        int ongoingDisturbanceCode = System.Int32.Parse(newParameters["disturbance_level"]);
        m_ongoingDisturbanceRate = m_convertDisturbance[ongoingDisturbanceCode];
        char[]startingPlants = newParameters["starting_matrix"].ToCharArray();
        m_cellStatus = new int[m_generations, m_xCells, m_zCells];
        m_permanentDisturbanceMap = new bool[m_xCells, m_zCells];
        m_age = new int[m_generations, m_xCells, m_zCells];
        m_totalSpeciesCounts = new int[m_generations, 6];
        m_cellPositions = new Vector3[m_xCells, m_zCells];
        for (int z=0; z<m_zCells; z++)
        {
            for (int x=0; x<m_xCells; x++)
            {
                //Randomize about the cell position a bit
                //Coordinates for trees on the terrain range from 0-1.0
                //To evenly space trees on each axis we need to divide 1.0 by
                //the number of x or z cells
                Vector3 position = new Vector3(x * (1.0f / m_xCells) + Random.Range(-0.01f, 0.01f),
                                               0.0f,
                                               z * (1.0f / m_zCells) + Random.Range(-0.01f, 0.01f));
                //Store the coordinates of each position so we don't have to recalculate them
                m_cellPositions[x, z] = position;
                int newSpecies;
                //The world has a 150x150 matrix of plants but the form to control it
                //is only 50x50 so we need to make a conversion
                int startingMatrixCell = ((z/3) * (m_xCells/3)) + (x/3);
                if (startingPlants[startingMatrixCell] == 'R')
                {
                    //Randomly select the plant type
                    newSpecies = Random.Range(0,6);
                    m_cellStatus[0, x, z] = newSpecies;
                    m_age[0, x, z] = Random.Range(0, m_lifespans[m_speciesList[newSpecies]] / 3);
                    m_totalSpeciesCounts[0, newSpecies]++;
                }
                else if (startingPlants[startingMatrixCell] == 'N')
                {
                    //Permanent gap
                    m_cellStatus[0, x, z] = 0;
                    m_permanentDisturbanceMap[x, z] = true;
                }
                else
                {
                    //A numbered plant type (or a gap for 0)
                    newSpecies = System.Int32.Parse(startingPlants[startingMatrixCell].ToString());
                    m_cellStatus[0, x, z] = newSpecies;
                    m_age[0, x, z] = Random.Range(0, m_lifespans[m_speciesList[newSpecies]] / 3);
                    m_totalSpeciesCounts[0, newSpecies]++;
                }
            }
        }
        return true;
    }


    void GenerateRandomCommunity()
    {
        //Generate starting matrix with random species and determine the
        //region x,y,z coordinates where each tree will be placed
        //Unity tree prototypes to include in the default community (-1 represents a gap with no tree)
        m_speciesList = new int[6] {-1, 2, 13, 14, 17, 19};
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
                //Randomize about the cell position a bit
                //Coordinates for trees on the terrain range from 0-1.0
                //To evenly space trees on each axis we need to divide 1.0 by
                //the number of x or z cells
                Vector3 position = new Vector3(x * (1.0f / m_xCells) + Random.Range(-0.01f, 0.01f),
                                               0.0f,
                                               z * (1.0f / m_zCells) + Random.Range(-0.01f, 0.01f));
                //Store the coordinates of each position so we don't have to recalculate them
                m_cellPositions[x, z] = position;
                //Choose a species at random
                int newSpecies = Random.Range(0,6);
                m_cellStatus[0, x, z] = newSpecies;
                //Assign a random age to each plant so there isn't a massive
                //dieoff early in the simulation, but skew the distribution
                //of ages downward or we will start with a dieoff because a
                //random selection of ages has many more old ages than expected.
                if (newSpecies != 0)
                {
                  m_age[0, x, z] = Random.Range(0, m_lifespans[m_speciesList[newSpecies]] / 3);
                }
                else
                {
                  m_age[0, x, z] = 0;
                }
                m_totalSpeciesCounts[0, newSpecies]++;
            }
        }
        m_simulationId = "default";
    }

    void RunSimulation()
    {
        //Generate the simulation data
        for (int generation=0; generation<m_generations - 1; generation++)
        {
            //Setup some variables we will need later
            int nextGeneration = generation + 1;
            int rowabove;
            int rowbelow;
            int colleft;
            int colright;
            //Calculate a disturbance map for this next generation
            bool[,] disturbance = CalculateDisturbance();
            //Step through the cells of the matrix and determine which plant should go in each
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
                            //This will be a gap because it was decided by the ongoing disturbance rate
                            m_cellStatus[nextGeneration, x, z] = 0;
                            m_totalSpeciesCounts[nextGeneration, 0]++;
                            m_age[nextGeneration, x, z] = 0; //We don't track the age of gaps
                        }
                        else
                        {
                            //Get species counts of neighbors
                            int[] neighborSpeciesCounts = GetNeighborSpeciesCounts(x, z, rowabove,
                                                                                   rowbelow, colright,
                                                                                   colleft, generation);
                            //Determine plant survival based on age and environment
                            bool plantSurvives = CalculateSurvival(currentSpecies,
                                                                   m_age[generation, x, z],
                                                                   m_cellPositions[x, z]);
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
            int prototypeIndex = m_speciesList[species];
            //Generate a float from 0-1.0 representing the probability of
            //survival based on plant age, altitude, water level, light level, and temperature level
            float ageHealth = CalculateAgeHealth(age, m_lifespans[prototypeIndex]);
            float altitudeHealth = CalculateAltitudeHealth(coordinates.y,
                                   m_altitudeOptimums[prototypeIndex],
                                   m_altitudeEffects[prototypeIndex]);
            float waterHealth = CalculateHealth(m_waterLevel,
                                                m_waterLevelOptimums[prototypeIndex],
                                                m_waterLevelEffects[prototypeIndex]);
            float lightHealth = CalculateHealth(m_lightLevel,
                                                m_lightLevelOptimums[prototypeIndex],
                                                m_lightLevelEffects[prototypeIndex]);
            float temperatureHealth = CalculateHealth(m_temperatureLevel,
                                                      m_temperatureLevelOptimums[prototypeIndex],
                                                      m_temperatureLevelEffects[prototypeIndex]);
            //Overall survival probability is the product of these separate survival probabilities
            float survivalProbability = (ageHealth * altitudeHealth * waterHealth *
                                         lightHealth * temperatureHealth);
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

    float CalculateHealth(float actual, float optimal, float shape)
    {
        //Returns a value from 0-1.0 representing the health of an individual
        //with an 'actual' value for some environmental parameter given the
        //optimal value and shape. This function works for any parameter where
        //the actual values will range from 0-1.0 (or can be converted to that
        //range.  With an optimal of 1.0 and a shape of 1,  values range from
        //1.0 at 1.0 to 0.0 at 0.0.  Lower values for shape flatten the
        //'fitness curve'. With shape <= 0, health will always equal 1.
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

    #endregion
}

