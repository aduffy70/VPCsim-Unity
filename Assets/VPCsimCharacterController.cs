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
	public float rotationSpeedY = 1f;
	public float minimumY = -90f;
	public float maximumY = 90f;
	float rotationY = 0;
	public float speed = 10.0f;
	public float minimumSpeed = 5f;
	public float maximumSpeed = 500f;
	public float acceleration = 1f;
	public float rotationSpeedHorizontal = 100.0f;

	// Use this for initialization
	void Start()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
		{
			rigidbody.freezeRotation = true;
		}
	}
		
	// Update is called once per frame
	void Update()
	{
		//'q' & 'z' acceleration
		if (Input.GetKey("q"))
		{
			speed += acceleration;
		}
		else if (Input.GetKey("z"))
		{
			speed -= acceleration;
		}
		speed = Mathf.Clamp(speed, minimumSpeed, maximumSpeed);
		//Arrow key movement and rotation
		float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeedHorizontal;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);
        //'e' & 'x' rotation
        if (Input.GetKey("e"))
		{
			rotationY += rotationSpeedY;
		}
		else if (Input.GetKey("x"))
		{
			rotationY -= rotationSpeedY;
		}
		rotationY = Mathf.Clamp(rotationY, minimumY, maximumY);	
		transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);	
	}
}
