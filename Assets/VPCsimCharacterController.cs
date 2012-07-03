using UnityEngine;
using System.Collections;

/// VPCsimCharacterController provides first person character movement:
///		Up & down arrows (or 'w' & 's') move forward and back in the
///		direction being faced
///   	Left & right arrows (or 'a' & 'd') rotate left and right.
///   	'e' & 'x' rotate up and down
///     'q' & 'z' to accelerate and decelerate

public class VPCsimCharacterController : MonoBehaviour
{
	Vector3 m_homePosition = new Vector3(500.5f, 50, 500.5f);
	Vector3 m_homeRotation = new Vector3(0, 0, 0);
	float m_rotationSpeedY = 0.75f;
	float m_minimumY = -90f;
	float m_maximumY = 90f;
	float m_rotationY = 0;
	float m_speed = 11.0f;
	float m_minimumSpeed = 1.0f;
	float m_maximumSpeed = 91.0f;
	float m_acceleration = 10.0f;
	float m_rotationSpeedHorizontal = 50.0f;
	string m_movementMode = "Walking...";
	float m_distanceToGround = 0f;
	float m_sampleHeight = 50f;
    //Whether to display the window with movement instructions
	bool m_showHelpWindow = true;
    Rect m_helpWindow = new Rect(285, 175, 400, 255);
    string m_helpString = "\n\t\t\t\t\t\t\t** Movement Instructions **\n\n" +
    					  "Walk and Fly modes:\n" +
    					  "    Arrow keys  -  Forward / Back / Turn left / Turn right\n" +
    					  "    e / x  -  Look up / Look down\n" +
    					  "    q / z  -  Speed up / Slow down\n\n" +
    					  "Sample mode:\n" +
    					  "    Arrow keys  -  North / South / West / East\n" +
    					  "    e / x  -  Increase / Decrease Height Above Terrain (HAT)\n" +
    					  "    q / z  -  Speed up / Slow down";
    //Compass related variables
    public Texture2D m_compassRose;
    public Texture2D m_compassMarker;
    float m_compassRadius = 20f;
    Vector2 m_compassCenter = new Vector2(125, 570);
    Vector2 m_compassSize = new Vector2(40, 40);
    Vector2 m_compassMarkerSize = new Vector2(9, 9);
    float m_cameraRot;
    float m_compassMarkerX;
    float m_compassMarkerY;
    Rect m_compassRect;


	// Use this for initialization
	void Start()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
		{
			GoToHomePosition();
			rigidbody.freezeRotation = true;
			rigidbody.useGravity = true;
		}
        //Set up GUI location for compass. Calculate it here so we don't redo it every GUI cycle
        m_compassRect = new Rect(m_compassCenter.x - m_compassSize.x / 2,
                                 m_compassCenter.y - m_compassSize.y / 2,
                                 m_compassSize.x,
                                 m_compassSize.y);
	}

	// Update is called once per frame
	void Update()
	{
		//'q' & 'z' acceleration
		if (Input.GetKeyDown("q"))
		{
			m_speed += m_acceleration;
		}
		else if (Input.GetKeyDown("z"))
		{
			m_speed -= m_acceleration;
		}
		m_speed = Mathf.Clamp(m_speed, m_minimumSpeed, m_maximumSpeed);
		if (m_movementMode != "Sampling...")
		{
			//Non-sampling-mode (flying and walking) movement
			//Arrow key movement and rotation
			float translation = Input.GetAxis("Vertical") * m_speed;
        	float rotation = Input.GetAxis("Horizontal") * m_rotationSpeedHorizontal;
        	translation *= Time.deltaTime;
        	rotation *= Time.deltaTime;
        	transform.Translate(0, 0, translation);
        	transform.Rotate(0, rotation, 0);
        	//'e' & 'x' rotation
        	if (Input.GetKey("e"))
			{
				m_rotationY += m_rotationSpeedY;
			}
			else if (Input.GetKey("x"))
			{
				m_rotationY -= m_rotationSpeedY;
			}
			m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);
			transform.localEulerAngles = new Vector3(-m_rotationY, transform.localEulerAngles.y, 0);
			if (m_movementMode == "Walking...")
			{
				if ((-1 > transform.position.x) || (transform.position.x > 1001) ||
					(-1 > transform.position.z) || (transform.position.z > 1001))
				{
					GoToHomePosition();
				}
			}
		}
		else
		{
			//Sampling-mode movement
			//Facing down with North at top of screen.
			//Arrow keys move N,S,E,W while maintaining fixed height above terrain.
			//If you move off the edge of the terrain you maintain elevation.
			if (Input.GetKey("e"))
			{
				m_sampleHeight += 0.05f;
			}
			else if (Input.GetKey("x"))
			{
				m_sampleHeight -= 0.05f;
			}
			//Sampling above 150m HAT is useless because you can't see smaller plants.
			//Sampling below 10m is useless because trees are further apart than what you can see on screen at once.
			m_sampleHeight = Mathf.Clamp(m_sampleHeight, 10, 150.5f);
			float xTranslation = Input.GetAxis("Horizontal") * m_speed;
			float zTranslation = Input.GetAxis("Vertical") * m_speed;
			float yTranslation = 0;
			xTranslation *= Time.deltaTime;
			zTranslation *= Time.deltaTime;
			RaycastHit hit;
        	if (Physics.Raycast(transform.position, -Vector3.up, out hit))
        	{
            	m_distanceToGround = hit.distance;
            	yTranslation = m_distanceToGround - m_sampleHeight;
        	}
			transform.Translate(xTranslation, zTranslation, yTranslation);
			transform.eulerAngles = new Vector3(90, 0, 0);
		}
        //Calculations for compass
        m_cameraRot = (-90 + transform.eulerAngles.y)* Mathf.Deg2Rad;
        m_compassMarkerX = m_compassRadius * Mathf.Cos(m_cameraRot);
        m_compassMarkerY = m_compassRadius * Mathf.Sin(m_cameraRot);
	}

	void OnGUI()
	{
		Vector3 position = transform.position;
		int displayedSpeed = ((int)m_speed / 10) + 1;
		GUI.Box(new Rect(5, 410, 165, 130), "Movement");
		GUI.Label(new Rect(10, 430, 150, 20), "Mode: " + m_movementMode);
		bool walkButton = false;
		bool flyButton = false;
		bool samplingButton = false;
		bool homeButton = false;
		if (m_movementMode != "Walking...")
		{
			walkButton = GUI.Button(new Rect(10, 450, 44, 20), new GUIContent("Walk",
									"Enter 'Walking' mode"));
		}
		if (m_movementMode != "Flying..." )
		{
			flyButton = GUI.Button(new Rect(59, 450, 42, 20), new GUIContent("Fly",
								   "Enter 'Flying' mode"));
		}
		if (m_movementMode != "Sampling...")
		{
			samplingButton = GUI.Button(new Rect(107, 450, 57, 20), new GUIContent("Sample",
										"Enter 'Sampling' mode"));
			homeButton = GUI.Button(new Rect(90, 515, 75, 20), new GUIContent("Go home",
									"Return to the starting position"));
		}
		else
		{
			GUI.Label(new Rect(100, 475, 130, 50), "\nHAT: " + ((int)m_distanceToGround).ToString() + "m");
		}
		GUI.Label(new Rect(10, 515, 100, 25), "Speed: " + displayedSpeed.ToString());
		GUI.Label(new Rect(10, 475, 145, 50), "Position: N" + ((int)position.z).ToString() + "  E" +
                                              ((int)position.x).ToString() +
											  "\nElevation: " + ((int)position.y).ToString() + "m");
		bool helpButton = GUI.Button(new Rect(10, 555, 60, 20), new GUIContent("Help", "Movement instructions"));
		if (!System.String.IsNullOrEmpty(GUI.tooltip))
		{
        	GUI.Box(new Rect(175 , 430, 185, 20),"");
		}
		GUI.Label(new Rect(180, 430, 210, 20), GUI.tooltip);
		if (walkButton)
		{
			m_movementMode = "Walking...";
			rigidbody.useGravity = true;
		}
		if (flyButton)
		{
			m_movementMode = "Flying...";
			rigidbody.useGravity = false;
		}
		if (samplingButton)
		{
			m_movementMode = "Sampling...";
			rigidbody.useGravity = false;
		}
		if (homeButton)
		{
			GoToHomePosition();
		}
		if (m_showHelpWindow)
        {
            //Setup the help message window
            m_helpWindow = GUI.Window(0, m_helpWindow, DisplayHelpWindow, "Help");
        }
        if (helpButton)
        {
            //Show or hide the help message window
            m_showHelpWindow = !m_showHelpWindow;
        }
        //Display compass
        GUI.DrawTexture(m_compassRect, m_compassRose);
        GUI.DrawTexture(new Rect(m_compassCenter.x + m_compassMarkerX - m_compassMarkerSize.x / 2,
                                 m_compassCenter.y + m_compassMarkerY - m_compassMarkerSize.y/2,
                                 m_compassMarkerSize.x, m_compassMarkerSize.y), m_compassMarker);
	}

	void DisplayHelpWindow(int windowID)
    {
    	if (GUI.Button(new Rect(175,230,50,20), "OK"))
        {
            m_showHelpWindow = !m_showHelpWindow;
        }
        GUI.TextArea(new Rect(5, 20, 390, 205), m_helpString);
        GUI.DragWindow();
    }

	void GoToHomePosition()
	{
		transform.position = m_homePosition;
		transform.eulerAngles = m_homeRotation;
		m_rotationY = 0;
	}
}
