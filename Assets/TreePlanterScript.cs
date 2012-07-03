using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text;

public class TreePlanterScript : MonoBehaviour
{
    //A version string to display on the GUI so we can spot browsers running outdated cached versions
    string m_versionID = "v1.0";
    System.Random m_random = new System.Random();
    string m_simulationId = "none";
    //Number of time steps to simulate
    int m_generations = 201;
    //Tree species for each cell in each generation [gen,x,z]
    int[,,] m_cellStatus;
    //Whether each cell is marked as permanently disturbed
    bool[,] m_permanentDisturbanceMap;
    //Number of species included in the community (actually number of species +1 for gaps)
    int m_species;
    //Unity tree prototypes to include in the community (-1 represents a gap with no tree)
    int[] m_speciesList;
    //Replacement Matrix.  The probability of replacement of a tree of one prototype
    //by another prototype if it is entirely surrounded by the other species.
    //Example [1,2] is the probability that species 2 will be replaced by species 1, if
    //species 2 is entirely surrounded by species 1.  Row one is all 0.0 because gaps don't replace plants.
    //Row  and column indices are 1+ the prototype number (because row 1 and column 1 are for gaps.)
    float[,] m_masterReplacementMatrix = new float[15,15]{
        //      Alder  Aspen  Start  Junip  Servi  Sageb  Sumac  Wildr  Fern   Maple  Elder  Pine   Cotto  Willo
        {0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f},
        {0.16f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Alder
        {0.16f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Aspen
        {0.95f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f}, //Starthistle
        {0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Juniper
        {0.45f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Serviceberry
        {0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Sagebrush
        {0.45f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Sumac
        {0.90f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Wildrose
        {0.90f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f}, //Fern
        {0.16f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f, 0.01f}, //Maple
        {0.45f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f, 0.01f}, //Elderberry
        {0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f, 0.01f}, //Pine
        {0.16f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f, 0.01f}, //Cottonwood
        {0.45f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.05f}  //Willow
        };
    //This smaller replacement matrix is for convenience.  We only have to dig through
    //the master replacement matrix once (to generate this smaller matrix) when we
    //import simulation settings rather than everytime we need a replacement value.
    //The first row will always be zero's because a gap never replaces a plant (that
    //only happens when the plant dies due to age or environment). But by having that
    //row of zero's the index numbers can correspond to the species numbers.
    float[,] m_replacementMatrix;
    //Store the human-readable plant names so we can display them later
    string[] m_prototypeNames = new string[14] {"Alder",
                                                "Aspen",
                                                "Starthistle",
                                                "Juniper",
                                                "Serviceberry",
                                                "Sagebrush",
                                                "Sumac",
                                                "Wildrose",
                                                "Fern",
                                                "Maple",
                                                "Elderberry",
                                                "Pine",
                                                "Cottonwood",
                                                "Willow"};
    //Maximum age for each prototype
    int[] m_lifespans = new int[14] {   25, //Alder
                                        25, //Aspen
                                        3,  //Starthistle
                                        100,//Juniper
                                        10, //Serviceberry
                                        100,//Sagebrush
                                        10, //Sumac
                                        3,  //Wildrose
                                        3,  //Fern
                                        25, //Maple
                                        10, //Elderberry
                                        100, //Pine
                                        25,  //Cottonwood
                                        10}; //Willow
    //The biomass of a newly established (age=0) individual of each prototype
    float[] m_baseBiomass = new float[14] { 10f,  //Alder
                                            10f,  //Aspen
                                            1f,  //Starthistle
                                            10f,  //Juniper
                                            5f,  //Serviceberry
                                            5f,  //Sagebrush
                                            1f,  //Sumac
                                            5f,  //Wildrose
                                            1f,  //Fern
                                            10f,  //Maple
                                            1f,  //Elderberry
                                            10f,  //Pine
                                            10f,  //Cottonwood
                                            10f}; //Willow
    //The amount of biomass increase per year at maximum health for each prototype
    float[] m_biomassIncrease = new float[14] { 5f,  //Alder
                                                5f,  //Aspen
                                                1f,  //Starthistle
                                                1f,  //Juniper
                                                5f,  //Serviceberry
                                                1f,  //Sagebrush
                                                5f,  //Sumac
                                                5f,  //Wildrose
                                                1f,  //Fern
                                                5f,  //Maple
                                                5f,  //Elderberry
                                                10f,  //Pine
                                                10f,  //Cottonwood
                                                10f}; //Willow
    //Optimal values and shape parameters for each prototype
    float[] m_elevationOptimums = new float[14] {1f,  //Alder
                                                95f,  //Aspen
                                                95f,  //Starthistle
                                                95f,  //Juniper
                                                150f,  //Serviceberry
                                                95f,  //Sagebrush
                                                95f,  //Sumac
                                                95f,  //Wildrose
                                                95f,  //Fern
                                                95f,  //Maple
                                                150f,  //Elderberry
                                                95f,  //Pine
                                                1f,  //Cottonwood
                                                95f}; //Willow
    float[] m_elevationEffects = new float[14] { 15f,  //Alder
                                                15f,  //Aspen
                                                1.5f,  //Starthistle
                                                15f,  //Juniper
                                                3f,  //Serviceberry
                                                15f,  //Sagebrush
                                                15f,  //Sumac
                                                15f,  //Wildrose
                                                15f,  //Fern
                                                15f,  //Maple
                                                3f,  //Elderberry
                                                15f,  //Pine
                                                15f,  //Cottonwood
                                                15f}; //Willow
    float[] m_waterLevelOptimums = new float[14] {  0.75f,  //Alder
                                                    0.5f,  //Aspen
                                                    0.5f,  //Starthistle
                                                    0.25f,  //Juniper
                                                    0.5f,  //Serviceberry
                                                    0.25f,  //Sagebrush
                                                    0.5f,  //Sumac
                                                    0.5f,  //Wildrose
                                                    0.75f,  //Fern
                                                    0.5f,  //Maple
                                                    0.5f,  //Elderberry
                                                    0.25f,  //Pine
                                                    0.5f,  //Cottonwood
                                                    0.75f,}; //Willow
    float[] m_waterLevelEffects = new float[14] {   2f,  //Alder
                                                    2f,  //Aspen
                                                    1f,  //Starthistle
                                                    2f,  //Juniper
                                                    2f,  //Serviceberry
                                                    2f,  //Sagebrush
                                                    2f,  //Sumac
                                                    2f,  //Wildrose
                                                    2f,  //Fern
                                                    2f,  //Maple
                                                    2f,  //Elderberry
                                                    2f,  //Pine
                                                    2f,  //Cottonwood
                                                    2f}; //Willow
    float[] m_lightLevelOptimums = new float[14] {  0.5f,  //Alder
                                                    0.5f,  //Aspen
                                                    0.5f,  //Starthistle
                                                    0.5f,  //Juniper
                                                    0.25f,  //Serviceberry
                                                    0.75f,  //Sagebrush
                                                    0.75f,  //Sumac
                                                    0.25f,  //Wildrose
                                                    0.55f,  //Fern
                                                    0.5f,  //Maple
                                                    0.5f,  //Elderberry
                                                    0.5f,  //Pine
                                                    0.5f,  //Cottonwood
                                                    0.5f}; //Willow
    float[] m_lightLevelEffects = new float[14] {   1.5f,  //Alder
                                                    1.5f,  //Aspen
                                                    0.75f,  //Starthistle
                                                    1.5f,  //Juniper
                                                    1.5f,  //Serviceberry
                                                    1.5f,  //Sagebrush
                                                    1.5f,  //Sumac
                                                    1.5f,  //Wildrose
                                                    1.5f,  //Fern
                                                    1.5f,  //Maple
                                                    1.5f,  //Elderberry
                                                    1.5f,  //Pine
                                                    1.5f,  //Cottonwood
                                                    1.5f}; //Willow
    float[] m_temperatureLevelOptimums = new float[14] {0.25f,  //Alder
                                                        0.5f,  //Aspen
                                                        0.5f,  //Starthistle
                                                        0.5f,  //Juniper
                                                        0.5f,  //Serviceberry
                                                        0.5f,  //Sagebrush
                                                        0.25f,  //Sumac
                                                        0.5f,  //Wildrose
                                                        0.5f,  //Fern
                                                        0.5f,  //Maple
                                                        0.5f,  //Elderberry
                                                        0.5f,  //Pine
                                                        0.5f,  //Cottonwood
                                                        0.5f}; //Willow
    float[] m_temperatureLevelEffects = new float[14] { 3f,  //Alder
                                                        3f,  //Aspen
                                                        1.5f,  //Starthistle
                                                        3f,  //Juniper
                                                        3f,  //Serviceberry
                                                        3f,  //Sagebrush
                                                        3f,  //Sumac
                                                        3f,  //Wildrose
                                                        3f,  //Fern
                                                        3f,  //Maple
                                                        3f,  //Elderberry
                                                        3f,  //Pine
                                                        3f,  //Cottonwood
                                                        3f}; //Willow
    //Adjust the default sizes of the plants
    float[] m_prototypeScales = new float[14] { 1.2f,  //Alder
                                                1.5f,  //Aspen
                                                3.0f,  //Starthistle
                                                80.0f,  //Juniper
                                                5.0f,  //Serviceberry
                                                10.0f,  //Sagebrush
                                                6.0f,  //Sumac
                                                9.0f,  //Wildrose
                                                5.0f,  //Fern
                                                0.5f,  //Maple
                                                0.3f,  //Elderberry
                                                1.5f,  //Pine
                                                1.2f,  //Cottonwood
                                                1.0f}; //Willow
    float m_ongoingDisturbanceRate;
    //These levels range from 0-1.0.  Default to 0.5 (mid-range or normal)
    float m_waterLevel;
    float m_lightLevel;
    float m_temperatureLevel;
    //Number of x and z cells (horizontal plane is xz in Unity3D)
    int m_xCells = 100;
    int m_zCells = 100;
    //Tracks the age of each plant in each generation
    int[,,] m_age;
    //Tracks the biomass of each plant in each generation
    float[,,] m_biomass;
    //Total species counts for each generation.
    int[,] m_totalSpeciesCounts;
    //Species average ages for each generation
    float[,] m_averageSpeciesAges;
    //Species total biomass for each generation
    float[,] m_totalSpeciesBiomass;
    //Keeps track of the region coordinates where each plant will be placed.
    Vector3[,] m_cellPositions;
    //Which generation number is currently visualized
    int m_displayedGeneration = 0;
    //The generation number the user selects from the GUI
    string m_chosenGeneration = "0";
    string m_chosenSimulationId = "";
    string m_countLogString = "";
    string m_ageLogString = "";
    string m_biomassLogString = "";
    //Whether to display the window with species count log data
    bool m_showCountLogWindow = false;
    //Whether to display the window with age log data
    bool m_showAgeLogWindow = false;
    //Whether to display the window with biomass log data
    bool m_showBiomassLogWindow = false;
    Rect m_countLogWindow = new Rect(200, 5, 400, 400);
    Rect m_ageLogWindow = new Rect(230, 30, 400, 400);
    Rect m_biomassLogWindow = new Rect(260, 55, 400, 400);
    //Base URL of parameter webapp
    string m_parameterPath = "http://vpcsim.appspot.com:80/";
    string m_debugString = ""; //Debug errors to display on the HUD
    //Stores xml data downloaded from the web
    WWW m_www;
    bool m_showDebugWindow = false; //Whether to display the window with debug messages
    Rect m_debugWindow = new Rect(300, 10, 400, 400);
    string m_currentDataString = "Not Available.\nLoad a simulation.";
    //Whether to display the window with error messages
    bool m_showErrorWindow = false;
    Rect m_errorWindow = new Rect(360, 250, 250, 150);
    string m_errorString = "";
    //Convert values from the webapp to numbers the simulation can use
    //Ongoing disturbance values - None, Very Low, Low, High, Very High
    float[] m_convertDisturbance = new float[5] {0f, 0.01f, 0.03f, 0.1f, 0.25f};
    //Environmental parameter values - Very Low, Low, Normal, High, Very High
    float[] m_convertWaterLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};
    float[] m_convertLightLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};
    float[] m_convertTemperatureLevel = new float[5] {0f, 0.25f, 0.5f, 0.75f, 1.0f};
    //Some features of the environment don't change during the simulation.  Let's store
    //the resulting health values so we don't recalculate them unnecessarily.
    float[] m_fixedHealth;


    #region Unity3D specific functions

    void Start()
    {
        m_debugString += "Started\n"; //Debug
        //There shouldn't be trees in the scene at startup, but just in case...
        DeleteAllTrees();
    }

    void Update()
    {
        //Called once per frame
    }

    void OnApplicationQuit()
    {
        //I don't think it matters but just in case...
        DeleteAllTrees();
    }

    void OnGUI()
    {
        //Generate the GUI controls and HUD
        //Display the release version in the bottom right corner
        GUI.Label(new Rect(770, 580, 35, 20), m_versionID);
        //Create an unused button off-screen so we can move focus out of the TextFields
        GUI.SetNextControlName("focusBuster");
        bool focusBusterButton = GUI.Button(new Rect(-10, -10, 1, 1),
                                            new GUIContent("",
                                            ""));
        //Buttons to run a simulation
        GUI.Box(new Rect(5, 5, 165, 100), "Simulation settings");
        bool defaultsButton = GUI.Button(new Rect(10, 30, 70, 20),
                                         new GUIContent("Defaults",
                                         "Load the default ecosystem"));
        bool createButton = GUI.Button(new Rect(85, 30, 80, 20),
                                       new GUIContent("Create new",
                                       "Create a new ecosystem"));
        bool showParametersButton = GUI.Button(new Rect(10, 80, 155, 20),
                                               new GUIContent("Show current settings",
                                               "Show ecosystem parameters"));
        m_chosenSimulationId = GUI.TextField(new Rect(10, 55, 112, 20),
                                             m_chosenSimulationId, 14);
        bool loadButton = GUI.Button(new Rect(124, 55, 40, 20),
                                     new GUIContent("Load",
                                     "Load new ecosystem parameters"));
        //Buttons to move between simulation steps
        GUI.Box(new Rect(5, 110, 165, 75), "Time view");
        bool firstButton = GUI.Button(new Rect (10, 135, 23, 20),
                                      new GUIContent("[<",
                                      "First year"));
        bool reverse10Button = GUI.Button(new Rect (36, 135, 26, 20),
                                          new GUIContent("<<",
                                          "Skip backward 10 years"));
        bool reverseButton = GUI.Button(new Rect (65, 135, 21, 20),
                                        new GUIContent("<",
                                        "Previous year"));
        bool forwardButton = GUI.Button(new Rect (89, 135, 21, 20),
                                        new GUIContent(">",
                                        "Next year"));
        bool forward10Button = GUI.Button(new Rect (113, 135, 26, 20),
                                          new GUIContent(">>",
                                          "Skip forward 10 years"));
        bool lastButton = GUI.Button(new Rect (142, 135, 23, 20),
                                     new GUIContent(">]",
                                     "Last year"));
        GUI.Label(new Rect(10, 160, 35, 20), "Year:");
        m_chosenGeneration = GUI.TextField(new Rect(42, 160, 40, 20),
                                           m_chosenGeneration, 4);
        GUI.Label(new Rect(83, 160, 35, 20), "/ " + (m_generations - 1).ToString());
        bool goButton = GUI.Button(new Rect(125, 160, 40, 20),
                                   new GUIContent("Go",
                                   "View the selected year"));
        //Buttons to display log data or plots
        GUI.Box(new Rect(5, 190, 165, 215), "Data");
        GUI.Label(new Rect(10, 215, 135, 100), m_currentDataString);
        GUI.Label(new Rect(10, 320, 35, 20), "Plots:");
        bool countPlotButton = GUI.Button(new Rect(10, 340, 46, 20),
                                     new GUIContent("Count",
                                     "Show species count plots"));
        bool agePlotButton = GUI.Button(new Rect(58, 340, 38, 20),
                                     new GUIContent("Age",
                                     "Show average age plots"));
        bool biomassPlotButton = GUI.Button(new Rect(98, 340, 66, 20),
                                     new GUIContent("Biomass",
                                     "Show biomass plots"));
        GUI.Label(new Rect(10, 360, 35, 20), "Logs:");
        bool countLogButton = GUI.Button(new Rect(10, 380, 46, 20),
                                    new GUIContent("Count",
                                    "Show species count log data"));
        bool ageLogButton = GUI.Button(new Rect(58, 380, 38, 20),
                                     new GUIContent("Age",
                                     "Show average age log data"));
        bool biomassLogButton = GUI.Button(new Rect(98, 380, 66, 20),
                                     new GUIContent("Biomass",
                                     "Show biomass log data"));
        //Button to display debug messages - TODO: Remove?  This is not for students.
        bool debugButton = GUI.Button(new Rect(10, 580, 60, 20),
                                      new GUIContent("Debug",
                                      "Show/Hide debug messages"));
        if (!System.String.IsNullOrEmpty(GUI.tooltip))
        {
            GUI.Box(new Rect(175 , 30, 220, 20),"");
        }
        GUI.Label(new Rect(180, 30, 210, 20), GUI.tooltip);
        if (m_showCountLogWindow)
        {
            //Setup species count log data window
            m_countLogWindow = GUI.Window(1, m_countLogWindow, DisplayCountLogWindow, "Species Counts");
        }
        if (m_showAgeLogWindow)
        {
            //Setup age log data window
            m_ageLogWindow = GUI.Window(2, m_ageLogWindow, DisplayAgeLogWindow, "Average Ages");
        }
        if (m_showBiomassLogWindow)
        {
            //Setup biomass log data window
            m_biomassLogWindow = GUI.Window(3, m_biomassLogWindow, DisplayBiomassLogWindow, "Species Biomass");
        }
        if (countLogButton)
        {
            //Show or hide the count log data window
            m_showCountLogWindow = !m_showCountLogWindow;
            m_chosenGeneration = m_displayedGeneration.ToString();
            GUI.FocusControl("focusBuster");
        }
        if (ageLogButton)
        {
            //Show or hide the age log data window
            m_showAgeLogWindow = !m_showAgeLogWindow;
            m_chosenGeneration = m_displayedGeneration.ToString();
            GUI.FocusControl("focusBuster");
        }
        if (biomassLogButton)
        {
            //Show or hide the biomass log data window
            m_showBiomassLogWindow = !m_showBiomassLogWindow;
            m_chosenGeneration = m_displayedGeneration.ToString();
            GUI.FocusControl("focusBuster");
        }
        if (m_showDebugWindow)
        {
            //Setup the debug message window
            m_debugWindow = GUI.Window(5, m_debugWindow, DisplayDebugWindow, "Debug");
        }
        if (m_showErrorWindow)
        {
            //Setup the error message window
            m_errorWindow = GUI.Window(4, m_errorWindow, DisplayErrorWindow, "***  Error!  ***");
            GUI.BringWindowToFront(4);
        }
        if (debugButton)
        {
            //Show or hide the debug message window
            m_showDebugWindow = !m_showDebugWindow;
            GUI.FocusControl("focusBuster");
        }
        if (defaultsButton)
        {
            //Run a simulation using the default settings
            DeleteAllTrees();
            GenerateRandomCommunity();
            RunSimulation();
            m_countLogString = GetCountLogData(false);
            m_ageLogString = GetAgeLogData(false);
            m_biomassLogString = GetBiomassLogData(false);
            VisualizeGeneration(0);
            m_chosenGeneration = m_displayedGeneration.ToString();
            m_chosenSimulationId = m_simulationId;
            GUI.FocusControl("focusBuster");
        }
        if (createButton)
        {
            //Open the parameters webapp in a new browser window or tab
            Application.ExternalCall("window.open('http://vpcsim.appspot.com','_blank')");
        }
        if (showParametersButton)
        {
            //Open the parameters webapp to show the current parameters in a new browser window or tab
            Application.ExternalCall("window.open('http://vpcsim.appspot.com/show?id=" +
                                     m_chosenSimulationId + "','_blank')");
        }
        if (loadButton)
        {
            //Run a simulation using the parameters for the simulation id entered in the textbox
            if (m_chosenSimulationId == "default")
            {
                //Run a simulation using the default settings
                DeleteAllTrees();
                GenerateRandomCommunity();
                RunSimulation();
                m_countLogString = GetCountLogData(false);
                m_ageLogString = GetAgeLogData(false);
                m_biomassLogString = GetBiomassLogData(false);
                VisualizeGeneration(0);
                m_chosenGeneration = m_displayedGeneration.ToString();
                m_chosenSimulationId = m_simulationId;
                GUI.FocusControl("focusBuster");
            }
            else
            {
                bool isValidId = false;
                try
                {
                    //Make sure the new simulation id can be converted to a long integer (it is
                    //supposed to be a datecode.)
                    long newSimulationId = System.Int64.Parse(m_chosenSimulationId);
                    isValidId = true;
                }
                catch(System.Exception e)
                {
                    isValidId = false;
                    m_errorString = "Invalid simulation id.";
                    m_debugString += e.ToString() + "\n";
                    m_showErrorWindow = true;
                }
                if (isValidId)
                {
                    //Retrieve simulation parameters from the web
                    StartCoroutine(GetNewSimulationParameters(m_chosenSimulationId));
                }
                GUI.FocusControl("focusBuster");
            }
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
        if (countPlotButton)
        {
            //Display plots of the species count data
            if (m_simulationId != "none")
            {
                DisplayCountPlot();
            }
            else
            {
                m_errorString = "Cannot view.\nNo simulation is loaded.";
                m_showErrorWindow = true;
            }
            GUI.FocusControl("focusBuster");
        }
        if (agePlotButton)
        {
            //Display plots of the average age data
            if (m_simulationId != "none")
            {
                DisplayAgePlot();
            }
            else
            {
                m_errorString = "Cannot view.\nNo simulation is loaded.";
                m_showErrorWindow = true;
            }
            GUI.FocusControl("focusBuster");
        }
        if (biomassPlotButton)
        {
            //Display plots of species biomass
            if (m_simulationId != "none")
            {
                DisplayBiomassPlot();
            }
            else
            {
                m_errorString = "Cannot view.\nNo simulation is loaded.";
                m_showErrorWindow = true;
            }
            GUI.FocusControl("focusBuster");
        }
    }

    void DisplayCountPlot()
    {
        //Send species count logdata to the surrounding web page where it will be redirected to the webapp
        //which will generate plots in a new browser window or tab.
        string logData = GetCountLogData(true);
        Application.ExternalCall("OpenCountsPlotPage", m_simulationId, logData);
    }

    void DisplayAgePlot()
    {
        //Send average age logdata to the surrounding web page where it will be redirected to the webapp
        //which will generate plots in a new browser window or tab.
        string logData = GetAgeLogData(true);
        Application.ExternalCall("OpenAgePlotPage", m_simulationId, logData);
    }

     void DisplayBiomassPlot()
    {
        //Send species biomass logdata to the surrounding web page where it will be redirected to the webapp
        //which will generate plots in a new browser window or tab.
        string logData = GetBiomassLogData(true);
        Application.ExternalCall("OpenBiomassPlotPage", m_simulationId, logData);
    }

    void DisplayErrorWindow(int windowID)
    {
        if (GUI.Button(new Rect(105, 125, 50, 20), "OK"))
        {
            m_showErrorWindow = false;
        }
        GUI.TextArea(new Rect(5, 20, 240, 105), m_errorString);
        GUI.DragWindow();
    }

    void DisplayCountLogWindow(int windowID)
    {
        if (GUI.Button(new Rect(330,370,50,20), "Close"))
        {
            m_showCountLogWindow = !m_showCountLogWindow;
        }
        if (m_simulationId != "none")
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), m_countLogString);
        }
        else
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), "No simulation loaded");
        }
        GUI.DragWindow();
    }

    void DisplayAgeLogWindow(int windowID)
    {
        if (GUI.Button(new Rect(330,370,50,20), "Close"))
        {
            m_showAgeLogWindow = !m_showAgeLogWindow;
        }
        if (m_simulationId != "none")
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), m_ageLogString);
        }
        else
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), "No simulation loaded");
        }
        GUI.DragWindow();
    }

    void DisplayBiomassLogWindow(int windowID)
    {
        if (GUI.Button(new Rect(330,370,50,20), "Close"))
        {
            m_showBiomassLogWindow = !m_showBiomassLogWindow;
        }
        if (m_simulationId != "none")
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), m_biomassLogString);
        }
        else
        {
            GUI.TextArea(new Rect(5, 20, 390, 350), "No simulation loaded");
        }
        GUI.DragWindow();
    }

    void DisplayDebugWindow(int windowID)
    {
        GUI.TextArea(new Rect(5, 20, 390, 350), m_debugString);
        GUI.DragWindow();
    }

    string GetCurrentCounts()
    {
        //Generates a string to display the current species counts formatted for the HUD
        StringBuilder currentDataBuilder = new StringBuilder();
        string currentData;
        currentDataBuilder.Append("Gaps: ");
        currentDataBuilder.Append(m_totalSpeciesCounts[m_displayedGeneration, 0].ToString() + "\n");
        for (int i=1; i<m_species; i++)
        {
            currentDataBuilder.Append(m_prototypeNames[m_speciesList[i]] + ": ");
            currentDataBuilder.Append(m_totalSpeciesCounts[m_displayedGeneration, i].ToString() + "\n");
        }
        currentData = currentDataBuilder.ToString();
        return currentData;
    }

    string GetCountLogData(bool isForPlotting)
    {
        //Generates a string of log data suitable for either displaying to humans or
        //for sending out for plotting
        StringBuilder logDataBuilder = new StringBuilder();
        string logData;
        if (isForPlotting)
        {
            //Generate string for sending out for plotting
            logDataBuilder.Append("\"year,");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]] + ",");
            }
            logDataBuilder.Append("Gaps\\\n\" + ");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append("\"");
                logDataBuilder.Append(generation.ToString() + ',');
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_totalSpeciesCounts[generation, i].ToString() + ",");
                }
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 0].ToString() + "\\\n\"");
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append(" + ");
                }
            }
        }
        else
        {
            //Generate string for displaying to humans
            logDataBuilder.Append("Year, ");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]] + ", ");
            }
            logDataBuilder.Append(" Gaps\n");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append(generation.ToString() + ", ");
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_totalSpeciesCounts[generation, i].ToString() + ", ");
                }
                logDataBuilder.Append(m_totalSpeciesCounts[generation, 0].ToString());
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append("\n");
                }
            }
        }
        logData = logDataBuilder.ToString();
        return logData;
    }

    string GetAgeLogData(bool isForPlotting)
    {
        //Generates a string of log data suitable for either displaying to humans or
        //for sending out for plotting
        StringBuilder logDataBuilder = new StringBuilder();
        string logData;
        if (isForPlotting)
        {
            //Generate string for sending out for plotting
            logDataBuilder.Append("\"year,");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != m_species - 1)
                {
                    logDataBuilder.Append(",");
                }
            }
            logDataBuilder.Append("\\\n\" + ");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append("\"");
                logDataBuilder.Append(generation.ToString() + ',');
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_averageSpeciesAges[generation, i].ToString());
                    if (i!= m_species - 1)
                    {
                        logDataBuilder.Append(",");
                    }
                    else
                    {
                        logDataBuilder.Append("\\\n\"");
                    }
                }
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append(" + ");
                }
            }
        }
        else
        {
            //Generate string for displaying to humans
            logDataBuilder.Append("Year, ");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != m_species - 1)
                {
                    logDataBuilder.Append(", ");
                }
            }
            logDataBuilder.Append("\n");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append(generation.ToString() + ", ");
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_averageSpeciesAges[generation, i].ToString());
                    if (i != m_species - 1)
                    {
                        logDataBuilder.Append(", ");
                    }
                }
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append("\n");
                }
            }
        }
        logData = logDataBuilder.ToString();
        return logData;
    }

    string GetBiomassLogData(bool isForPlotting)
    {
        //Generates a string of log data suitable for either displaying to humans or
        //for sending out for plotting
        StringBuilder logDataBuilder = new StringBuilder();
        string logData;
        if (isForPlotting)
        {
            //Generate string for sending out for plotting
            logDataBuilder.Append("\"year,");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != m_species - 1)
                {
                    logDataBuilder.Append(",");
                }
            }
            logDataBuilder.Append("\\\n\" + ");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append("\"");
                logDataBuilder.Append(generation.ToString() + ',');
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_totalSpeciesBiomass[generation, i].ToString());
                    if (i != m_species - 1)
                    {
                        logDataBuilder.Append(",");
                    }
                    else
                    {
                        logDataBuilder.Append("\\\n\"");
                    }
                }
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append(" + ");
                }
            }
        }
        else
        {
            //Generate string for displaying to humans
            logDataBuilder.Append("Year, ");
            for (int i=1; i<m_species; i++)
            {
                logDataBuilder.Append(m_prototypeNames[m_speciesList[i]]);
                if (i != m_species - 1)
                {
                    logDataBuilder.Append(", ");
                }
            }
            logDataBuilder.Append("\n");
            for(int generation=0; generation<m_generations; generation++)
            {
                logDataBuilder.Append(generation.ToString() + ", ");
                for (int i=1; i<m_species; i++)
                {
                    logDataBuilder.Append(m_totalSpeciesBiomass[generation, i].ToString());
                    if (i != m_species - 1)
                    {
                        logDataBuilder.Append(", ");
                    }
                }
                if (generation != m_generations - 1)
                {
                    logDataBuilder.Append("\n");
                }
            }
        }
        logData = logDataBuilder.ToString();
        return logData;
    }

    #endregion

    #region Visualization functions

    void VisualizeGeneration(int generation)
    {
        if (m_simulationId != "none")
        {
            //Update the visualization with plants from a particular generation.
            //Remove old trees
            Terrain.activeTerrain.terrainData.treeInstances = new TreeInstance[0];
            //Add new trees
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
            m_currentDataString = GetCurrentCounts();
        }
        else
        {
            //We can't view a generation if we haven't loaded a simulation.
            m_errorString = "Cannot view.\nNo simulation is loaded.";
            m_showErrorWindow = true;
        }
    }

    void AddTree(Vector3 position, int treeSpecies, int age)
    {
        //Add a tree to the terrain sized according to its age
        if (treeSpecies != 0) // 0 would be a gap (no tree)
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
        //Get simulation parameters from the parameters webapp
        string url = System.IO.Path.Combine(m_parameterPath,"data?id=" + fileName);
        m_www = new WWW(url);
        yield return m_www;
        if (m_www.isDone)
        {
            bool unpackSuccess = UnpackParameters();
            if (unpackSuccess)
            {
                DeleteAllTrees();
                RunSimulation();
                m_countLogString = GetCountLogData(false);
                m_ageLogString = GetAgeLogData(false);
                VisualizeGeneration(0);
                m_chosenGeneration = m_displayedGeneration.ToString();
                m_chosenSimulationId = m_simulationId;
            }
        }
        else
        {
            m_errorString = "Unable to retrieve settings.\nWWW read failed?";
            m_showErrorWindow = true;
        }
    }

    bool UnpackParameters()
    {
        //Unpack the parameter data into appropriate variables
        Dictionary<string, string> newParameters = new Dictionary<string, string>();
        XmlTextReader reader;
        try
        {
            m_debugString += "entered try\n";
            reader = new XmlTextReader(new System.IO.StringReader(m_www.text));
            m_debugString += "Created new XmlTextReader\n";
            reader.WhitespaceHandling = WhitespaceHandling.Significant;
            m_debugString += "Setup whitespace handling\n";
        }
        catch(System.Exception e)
        {
            m_errorString = "Unable to retrieve settings.\nCould not open xml stream?";
            m_showErrorWindow = true;
            m_debugString += e.ToString() + "\n";
            return false;
        }
        try
        {
            while (reader.Read())
            {
                if (reader.Name == "property")
                {
                    string parameterName = reader.GetAttribute("name");
                    string parameterValue = reader.ReadString();
                    newParameters.Add(parameterName, parameterValue);
                }
            }
        }
        catch(System.Exception e)
        {
            m_errorString = "Unable to retrieve settings.\nUnrecognized simulation ID or no connection to webapp?";
            m_showErrorWindow = true;
            m_debugString += e.ToString() + "\n";
            return false;
        }
        try
        {
            m_simulationId = newParameters["id"];
            int waterLevelCode = System.Int32.Parse(newParameters["water_level"]);
            m_waterLevel = m_convertWaterLevel[waterLevelCode];
            int lightLevelCode = System.Int32.Parse(newParameters["light_level"]);
            m_lightLevel = m_convertLightLevel[lightLevelCode];
            int temperatureLevelCode = System.Int32.Parse(newParameters["temperature_level"]);
            m_temperatureLevel = m_convertTemperatureLevel[temperatureLevelCode];
            string[] speciesList = newParameters["plant_types"].Split(',');
            m_species = speciesList.Length + 1;
            m_speciesList = new int[m_species];
            m_speciesList[0] = -1;
            for (int i = 1; i<m_species; i++) //Start at index 1 so index 0 stays "None" to represent gaps
            {
                m_speciesList[i] = System.Int32.Parse(speciesList[i - 1]);
            }
            GenerateReplacementMatrix();
            int ongoingDisturbanceCode = System.Int32.Parse(newParameters["disturbance_level"]);
            m_ongoingDisturbanceRate = m_convertDisturbance[ongoingDisturbanceCode];
            char[]startingPlants = newParameters["starting_matrix"].ToCharArray();
            m_cellStatus = new int[m_generations, m_xCells, m_zCells];
            m_permanentDisturbanceMap = new bool[m_xCells, m_zCells];
            m_age = new int[m_generations, m_xCells, m_zCells];
            m_biomass = new float[m_generations, m_xCells, m_zCells];
            m_totalSpeciesCounts = new int[m_generations, m_species];
            m_averageSpeciesAges = new float[m_generations, m_species];
            m_totalSpeciesBiomass = new float[m_generations, m_species];
            m_cellPositions = new Vector3[m_xCells, m_zCells];
            int[] speciesAgeSums = new int[m_species];
            //Calculate the parts of the health values that will not change during the simulation
            m_fixedHealth = new float[m_species];
            for (int species=1; species<m_species; species++)
            {
                m_fixedHealth[species] = CalculateFixedHealth(species);
            }
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
                    position.y = Terrain.activeTerrain.SampleHeight(new Vector3(position.x * 1000, 0.0f, position.z * 1000));
                    m_cellPositions[x, z] = position;
                    int newSpecies;
                    //The world has a 100x100 matrix of plants but the form to control it
                    //is only 50x50 so we need to make a conversion
                    int startingMatrixCell = ((z/2) * (m_xCells/2)) + (x/2);
                    if (startingPlants[startingMatrixCell] == 'R')
                    {
                        //Randomly select the plant type
                        newSpecies = Random.Range(0,m_species);
                        m_cellStatus[0, x, z] = newSpecies;
                        if (newSpecies != 0)
                        {
                            int prototypeIndex = m_speciesList[newSpecies];
                            int age = Random.Range(0, m_lifespans[prototypeIndex] / 2);
                            m_age[0, x, z] = age;
                            speciesAgeSums[newSpecies] = speciesAgeSums[newSpecies] + age;
                            float health = CalculateHealth(newSpecies, position);
                            float biomass = (m_baseBiomass[prototypeIndex] +
                                             (age * health * m_biomassIncrease[prototypeIndex]));
                            m_biomass[0, x, z] = biomass;
                            m_totalSpeciesBiomass[0, newSpecies] += biomass;
                        }
                        else
                        {
                            m_age[0, x, z] = 0;
                            m_biomass[0, x, z] = 0;
                        }
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
                        if (newSpecies != 0)
                        {
                            int prototypeIndex = m_speciesList[newSpecies];
                            int age = Random.Range(0, m_lifespans[prototypeIndex] / 2);
                            m_age[0, x, z] = age;
                            speciesAgeSums[newSpecies] = speciesAgeSums[newSpecies] + age;
                            float health = CalculateHealth(newSpecies, position);
                            float biomass = (m_baseBiomass[prototypeIndex] +
                                             (age * health * m_biomassIncrease[prototypeIndex]));
                            m_biomass[0, x, z] = biomass;
                            m_totalSpeciesBiomass[0, newSpecies] += biomass;
                        }
                        else
                        {
                            m_age[0, x, z] = 0;
                            m_biomass[0, x, z] = 0;
                        }
                        m_totalSpeciesCounts[0, newSpecies]++;
                    }
                }
            }
            for (int i=0; i<m_species; i++)
            {
                int speciesCount = m_totalSpeciesCounts[0, i];
                //Avoid divide-by-zero errors
                if (speciesCount != 0)
                {
                    m_averageSpeciesAges[0, i] = (float)System.Math.Round((double)speciesAgeSums[i] /
                                                                      (double)speciesCount, 2);
                }
                else
                {
                    m_averageSpeciesAges[0, i] = 0f;
                }
            }
            return true;
        }
        catch(System.Exception e)
        {
            m_errorString = "Unable to retrieve settings.\nError in settings string from webapp?";
            m_showErrorWindow = true;
            m_debugString += e.ToString() + "\n";
            return false;
        }
    }

    void GenerateReplacementMatrix()
    {
        //Pulls from the masterReplacementMatrix to create a replacement matrix
        //for just the species included in this simulation.  In future steps it
        //is much less confusing to work from this smaller matrix where both
        //row and column indices correspond to the species numbers.
        m_replacementMatrix = new float[m_species, m_species];
        int rowPrototype;
        int columnPrototype;
        for (int i=0; i<m_species; i++)
        {
            if (i == 0)
            {
                rowPrototype = -1;
            }
            else
            {
                rowPrototype = m_speciesList[i];
            }
            for (int j=0; j<m_species; j++)
            {
                if (j == 0)
                {
                    columnPrototype = -1;
                }
                else
                {
                    columnPrototype = m_speciesList[j];
                }
                m_replacementMatrix[i, j] = m_masterReplacementMatrix[rowPrototype + 1, columnPrototype + 1];
            }
        }
    }

    void GenerateRandomCommunity()
    {
        //Generate starting matrix with random species and determine the
        //region x,y,z coordinates where each tree will be placed
        //Unity tree prototypes to include in the default community (-1 represents a gap with no tree)
        m_species = 6;
        m_speciesList = new int[6] {-1, 1, 8, 9, 11, 13};
        GenerateReplacementMatrix();
        m_cellStatus = new int[m_generations, m_xCells, m_zCells];
        m_age = new int[m_generations, m_xCells, m_zCells];
        m_biomass = new float[m_generations, m_xCells, m_zCells];
        m_totalSpeciesCounts = new int[m_generations, 6];
        m_averageSpeciesAges = new float[m_generations, 6];
        m_totalSpeciesBiomass = new float[m_generations, 6];
        m_cellPositions = new Vector3[m_xCells, m_zCells];
        m_waterLevel = 0.5f;
        m_lightLevel = 0.5f;
        m_temperatureLevel = 0.5f;
        m_ongoingDisturbanceRate = 0.0f;
        //Make a default disturbance map with no permanent disturbances
        m_permanentDisturbanceMap = new bool[m_xCells, m_zCells];
        int[] speciesAgeSums = new int[6];
        //Calculate the parts of the health values that will not change during the simulation
        m_fixedHealth = new float[m_species];
        for (int species=1; species<m_species; species++)
        {
            m_fixedHealth[species] = CalculateFixedHealth(species);
        }
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
                position.y = Terrain.activeTerrain.SampleHeight(new Vector3(position.x * 1000, 0.0f, position.z * 1000));
                //Store the coordinates of each position so we don't have to recalculate them
                m_cellPositions[x, z] = position;
                //Choose a species at random
                int newSpecies = Random.Range(0,6);
                m_cellStatus[0, x, z] = newSpecies;
                //Assign a random age to each plant so there isn't a massive
                //dieoff early in the simulation, but skew the distribution
                //of ages downward or we will start with a dieoff because a
                //random selection of ages has many more old ages than expected.
                //Also calculate health so we can calculate a starting biomass.
                if (newSpecies != 0)
                {
                    int prototypeIndex = m_speciesList[newSpecies];
                    int age = Random.Range(0, m_lifespans[prototypeIndex] / 2);
                    m_age[0, x, z] = age;
                    speciesAgeSums[newSpecies] = speciesAgeSums[newSpecies] + age;
                    float health = CalculateHealth(newSpecies, position);
                    float biomass = (m_baseBiomass[prototypeIndex] +
                                     (age * health * m_biomassIncrease[prototypeIndex]));
                    m_biomass[0, x, z] = biomass;
                    m_totalSpeciesBiomass[0, newSpecies] += biomass;
                }
                else
                {
                    m_age[0, x, z] = 0;
                    m_biomass[0, x, z] = 0;
                }
                m_totalSpeciesCounts[0, newSpecies]++;
            }
        }
        for (int i=0; i<6; i++)
        {
            int speciesCount = m_totalSpeciesCounts[0, i];
            //Avoid divide-by-zero errors
            if (speciesCount != 0)
            {
                m_averageSpeciesAges[0, i] = (float)System.Math.Round((double)speciesAgeSums[i] /
                                                                      (double)speciesCount, 2);
            }
            else
            {
                m_averageSpeciesAges[0, i] = 0f;
            }
        }
        m_simulationId = "default";
    }

    void RunSimulation()
    {
        //Generate the simulation data
        //Step through each generation
        for (int generation=0; generation<m_generations - 1; generation++)
        {
            int[] speciesAgeSums = new int[m_species];
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
                            //We don't track the age of gaps and gaps have no biomass
                            m_age[nextGeneration, x, z] = 0;
                            m_biomass[nextGeneration, x, z] = 0;
                        }
                        else
                        {
                            //Get species counts of neighbors
                            int[] neighborSpeciesCounts = GetNeighborSpeciesCounts(x, z, rowabove,
                                                                                   rowbelow, colright,
                                                                                   colleft, generation);
                            //Determine plant health and survival based on age and environment
                            float health;
                            bool plantSurvives;
                            if (currentSpecies == 0)
                            {
                                //gaps don't have health or survival ability
                                health = 0f;
                                plantSurvives = false;
                            }
                            else
                            {
                                health = CalculateHealth(currentSpecies, m_cellPositions[x, z]);
                                plantSurvives = CalculateSurvival(currentSpecies, health, m_age[generation, x, z]);
                            }
                            if (plantSurvives)
                            {
                                //Calculate replacement probabilities based on current plant
                                float[] replacementProbability = GetReplacementProbabilities(
                                                                 currentSpecies,
                                                                 neighborSpeciesCounts,
                                                                 generation,
                                                                 m_cellPositions[x, z]);
                                //Determine the next generation plant based on those probabilities
                                int newSpecies = SelectNextGenerationSpecies(replacementProbability,
                                                 currentSpecies);
                                if (newSpecies == -1)
                                {
                                    //The old plant is still there
                                    int age = m_age[generation, x, z] + 1;
                                    m_age[nextGeneration, x, z] = age;
                                    m_cellStatus[nextGeneration, x, z] = currentSpecies;
                                    m_totalSpeciesCounts[nextGeneration, currentSpecies]++;
                                    speciesAgeSums[currentSpecies] = speciesAgeSums[currentSpecies] + age;
                                    int prototypeIndex = m_speciesList[currentSpecies];
                                    float newBiomass = m_biomassIncrease[prototypeIndex] * health;
                                    float currentBiomass = newBiomass + m_biomass[generation, x, z];
                                    m_biomass[nextGeneration, x, z] = currentBiomass;
                                    m_totalSpeciesBiomass[nextGeneration, currentSpecies] += currentBiomass;
                                }
                                else
                                {
                                    //The old plant has been replaced (though possibly by another
                                    //of the same species...)
                                    m_age[nextGeneration, x, z] = 0;
                                    m_cellStatus[nextGeneration, x, z] = newSpecies;
                                    m_totalSpeciesCounts[nextGeneration, newSpecies]++;
                                    int prototypeIndex = m_speciesList[currentSpecies];
                                    float newBiomass = m_baseBiomass[prototypeIndex];
                                    m_biomass[nextGeneration, x, z] = newBiomass;
                                    m_totalSpeciesBiomass[nextGeneration, currentSpecies] += newBiomass;
                                }
                            }
                            else
                            {
                                //Calculate replacement probabilities based on a gap
                                float[] replacementProbability = GetReplacementProbabilities(0,
                                                                 neighborSpeciesCounts,
                                                                 generation,
                                                                 m_cellPositions[x, z]);
                                m_age[nextGeneration, x, z] = 0;
                                //Determine the next generation plant based on those probabilities
                                int newSpecies = SelectNextGenerationSpecies(replacementProbability, 0);
                                if (newSpecies == -1)
                                {
                                    //No new plant was selected.  It will still be a gap. Age and biomass stay zero.
                                    m_cellStatus[nextGeneration, x, z] = 0;
                                    m_totalSpeciesCounts[nextGeneration, 0]++;
                                }
                                else
                                {
                                    //Store the new plant status and update the total species counts. Age stays zero.
                                    m_cellStatus[nextGeneration, x, z] = newSpecies;
                                    m_totalSpeciesCounts[nextGeneration, newSpecies]++;
                                    int prototypeIndex = m_speciesList[newSpecies];
                                    float newBiomass = m_baseBiomass[prototypeIndex];
                                    m_biomass[nextGeneration, x, z] = newBiomass;
                                    m_totalSpeciesBiomass[nextGeneration, newSpecies] += newBiomass;
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
            //Store the average age of each species in this generation
            for (int i=0; i<m_species; i++)
            {
                int speciesCount = m_totalSpeciesCounts[nextGeneration, i];
                //Avoid divide-by-zero errors
                if (speciesCount != 0)
                {
                    m_averageSpeciesAges[nextGeneration, i] = (float)System.Math.Round((double)speciesAgeSums[i] /
                                                                                       (double)speciesCount, 2);
                }
                else
                {
                    m_averageSpeciesAges[nextGeneration, i] = 0f;
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
        int[] neighborSpeciesCounts = new int[m_species];
        int neighborType;
        if (colleft >= 0)
        {
            neighborType = m_cellStatus[generation, colleft, z];
            //Don't count permanent gaps
            if (neighborType != -1)
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

    float CalculateFixedHealth(int species)
    {
        //Returns a value from 1.0 healthy to 0.0 unhealthy representing the health of the
        //plant based on the environmental factors that do not change over the course of a simulation.
        int prototypeIndex = m_speciesList[species];
        float waterHealth = CalculateEnvironmentHealth(m_waterLevel,
                                                       m_waterLevelOptimums[prototypeIndex],
                                                       m_waterLevelEffects[prototypeIndex]);
        float lightHealth = CalculateEnvironmentHealth(m_lightLevel,
                                                       m_lightLevelOptimums[prototypeIndex],
                                                       m_lightLevelEffects[prototypeIndex]);
        float temperatureHealth = CalculateEnvironmentHealth(m_temperatureLevel,
                                                             m_temperatureLevelOptimums[prototypeIndex],
                                                             m_temperatureLevelEffects[prototypeIndex]);
        //Overall fixed health is the product of these separate health values
        float fixedHealth = waterHealth * lightHealth * temperatureHealth;
        return fixedHealth;
    }

    float CalculateHealth(int species, Vector3 coordinates)
    {
        //Returns a value from 1.0 healthy to 0.0 unhealthy representing the health of the
        //plant based on environmental factors that are fixed and those that vary over space.
        int prototypeIndex = m_speciesList[species];
        //Get the portion of health based on factors that don't change over the simulation
        float fixedHealth = m_fixedHealth[species];
        //Generate a float from 0-1.0 representing the health based on elevation
        float elevationHealth = CalculateElevationHealth(coordinates.y,
                               m_elevationOptimums[prototypeIndex],
                               m_elevationEffects[prototypeIndex]);
        //Overall health is the product of these separate health components
        float health = fixedHealth * elevationHealth;
        return health;
    }

    bool CalculateSurvival(int species, float health, int age)
    {
        //Return true if the plant survives or false if it does not
        int prototypeIndex = m_speciesList[species];
        float survivalProbability;
        //Adjust maximum lifespan based on health
        float adjustedMaximumAge = m_lifespans[prototypeIndex] * health;
        //Avoid divide by zero error for really unhealthy plants
        if (adjustedMaximumAge > 0)
        {
            int power = (int)System.Math.Ceiling(adjustedMaximumAge / 20.0) + 2;
            survivalProbability = 1.0f - (float)System.Math.Pow((float)age/adjustedMaximumAge, power);
            //Don't allow health values >1 or <0
            if (survivalProbability > 1.0f)
            {
                survivalProbability = 1.0f;
            }
            if (survivalProbability < 0f)
            {
                survivalProbability = 0f;
            }
        }
        else
        {
            survivalProbability = 0f;
        }
        //Select a random float from 0-1.0.  Plant survives if random number <= survivalProbability
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

    float CalculateElevationHealth(float actual, float optimal, float shape)
    {
        //Returns a value from 0-1.0 representing the health of an individual
        //with an 'actual' value for some environmental parameter given the
        //optimal value and shape. This function works for elevation.  Lower
        //values for shape flatten the 'fitness curve'.
        //With shape <= 0, health will always equal 1.0.
        float health = 1.0f - ((float)System.Math.Abs(System.Math.Pow((optimal - actual) / 210f, 3)) * shape);
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

    float CalculateEnvironmentHealth(float actual, float optimal, float shape)
    {
        //Returns a value from 0-1.0 representing the health of an individual
        //with an 'actual' value for some environmental parameter given the
        //optimal value and shape. This function works for any parameter where
        //the actual values will range from 0-1.0 (or can be converted to that
        //range.  With shape <= 0, health will always equal 1.
        float health = 1.0f - ((float)System.Math.Abs(System.Math.Pow(optimal - actual, 2)) * shape);
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

    float[] GetReplacementProbabilities(int currentSpecies, int[] neighborSpeciesCounts, int generation, Vector3 location)
    {
        //Calculate the probability that the current plant will be replaced by each species.
        //Includes a calculation of health of each species so we don't have the problem of plants
        //replacing other plants even though the new plant is completely maladapted.
        //The first value is always 0 because gaps cannot replace a plant through competition.
        //Gaps arise only when a plant dies and no replacement is selected.
        float[] replacementProbabilities = new float[m_species];
        for (int species=1; species<m_species; species++)
        {
            //Mostly based on what plants are most common in the local area (local dispersal)
            float localComponent = (m_replacementMatrix[species, currentSpecies] *
                                    ((float)neighborSpeciesCounts[species] / 8.0f)) *
                                    0.950f;
            //Partly based on what plants are most common overall (long-distance dispersal)
            float distantComponent = (m_replacementMatrix[species, currentSpecies] *
                                      ((float)m_totalSpeciesCounts[generation, species] /
                                      (m_xCells * m_zCells))) *
                                      0.045f;
            //Slightly based on random events (immigration from outside the simulation area)
            float outOfAreaComponent = 0.005f;
            //Total replacement probability is the sum of these three components, weighted by the health an
            //individual of that species would have at age 0 at that location
            float potentialHealth = CalculateHealth(species, location);
            replacementProbabilities[species] = ((localComponent + distantComponent + outOfAreaComponent) *
                                                 potentialHealth);
        }
        return replacementProbabilities;
    }

    int SelectNextGenerationSpecies(float[] replacementProbability, int currentSpecies)
    {
        //Randomly determine the new species based on the replacement probablilities.
        //We aren't concerned with the probability of replacement by no plant,
        //since we are looking at competition between species here.
        float randomReplacement = (float)m_random.NextDouble();
        float replacementThreshold = 0;
        for (int i=1; i<m_species; i++)
        {
            replacementThreshold += replacementProbability[i];
            if (randomReplacement <= replacementThreshold)
            {
                return i;
            }
        }
        //Indicate that the current plant was not replaced (we use -1 for
        //this because returning the current species integer would indicate
        //that the current individual was replaced by a new member of the
        //same species.
        return -1;
    }

    #endregion
}
