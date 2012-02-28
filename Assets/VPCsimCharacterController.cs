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
	bool m_isWalking;

	// Use this for initialization
	void Start()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
		{
			rigidbody.freezeRotation = true;
			rigidbody.useGravity = true;
			m_isWalking = true;
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
	
	string buttonText = "Fly";
	string boxText = "Walking...";
	
	void OnGUI()
	{
		Vector3 position = transform.position;
		int displayedSpeed = ((int)m_speed / 10) + 1;
		GUI.Box(new Rect(5, 325, 155, 110), "Movement");
		GUI.Label(new Rect(10, 350, 60, 20), boxText);
		bool walkFlyButton = GUI.Button(new Rect(75, 350, 45, 20), new GUIContent(buttonText));
		GUI.Label(new Rect(10, 375, 100, 25), "Speed: " + displayedSpeed.ToString());
		GUI.Label(new Rect(10, 400, 130, 50), "Position: " + ((int)position.x).ToString() +
											", " + ((int)position.z).ToString() +
											"\nAltitude: " + ((int)position.y).ToString());
		if (walkFlyButton)
		{
			m_isWalking = !m_isWalking;
			rigidbody.useGravity = m_isWalking;
			if (m_isWalking)
			{
				buttonText = "Fly";
				boxText = "Walking...";
			}
			else
			{
				buttonText = "Walk";
				boxText = "Flying...";
			}
		}
	}
}
