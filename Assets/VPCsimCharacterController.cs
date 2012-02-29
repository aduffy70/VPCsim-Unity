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


	// Use this for initialization
	void Start()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
		{
			rigidbody.freezeRotation = true;
			rigidbody.useGravity = true;
		}
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
		}
		else
		{	
			//Sampling-mode movement
			//Facing down with North at top of screen.
			//Arrow keys move N,S,E,W while maintaining fixed height above terrain.
			//If you move off the edge of the terrain you maintain altitude.
			if (Input.GetKey("e"))
			{
				m_sampleHeight += 0.05f;
			}
			else if (Input.GetKey("x"))
			{
				m_sampleHeight -= 0.05f;
			}
			m_sampleHeight = Mathf.Clamp(m_sampleHeight, 10, 500.5f);
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
	}
		
	void OnGUI()
	{
		Vector3 position = transform.position;
		int displayedSpeed = ((int)m_speed / 10) + 1;
		GUI.Box(new Rect(5, 315, 155, 135), "Movement");
		GUI.Label(new Rect(10, 340, 150, 20), "Mode: " + m_movementMode);
		bool walkButton = false;
		bool flyButton = false;
		bool samplingButton = false;
		if (m_movementMode != "Walking...")
		{
			walkButton = GUI.Button(new Rect(10, 360, 40, 20), "Walk");
		}
		if (m_movementMode != "Flying..." )
		{
			flyButton = GUI.Button(new Rect(55, 360, 40, 20), "Fly");
		}
		if (m_movementMode != "Sampling...")
		{
			samplingButton = GUI.Button(new Rect(100, 360, 55, 20), "Sample");
		}
		else
		{
			GUI.Label(new Rect(100, 415, 130, 50), "\nHAT: " + ((int)m_distanceToGround).ToString());
		}
		GUI.Label(new Rect(10, 390, 100, 25), "Speed: " + displayedSpeed.ToString());
		GUI.Label(new Rect(10, 415, 130, 50), "Position: " + ((int)position.x).ToString() +
											", " + ((int)position.z).ToString() +
											"\nAltitude: " + ((int)position.y).ToString());
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
	}
}
