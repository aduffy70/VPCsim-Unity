using UnityEngine;
using System.Collections;



public class TestMovementScript3 : MonoBehaviour
{

	// Use this for initialization
	void Start()
	{
		// Make the rigid body not change rotation
		if (rigidbody)
			rigidbody.freezeRotation = true;

	}
	
	public float sensitivityY = 1f;
	public float minimumY = -90f;
	public float maximumY = 90f;

	float rotationY = 0F;
	
	
	float speed = 10.0f;
	float rotationSpeed = 100.0f;
	
	// Update is called once per frame
	void Update()
	{
		float translation = Input.GetAxis("Vertical") * speed;
        float rotation = Input.GetAxis("Horizontal") * rotationSpeed;
        translation *= Time.deltaTime;
        rotation *= Time.deltaTime;
        transform.Translate(0, 0, translation);
        transform.Rotate(0, rotation, 0);
        
        if (Input.GetKey("e"))
		{
			rotationY += sensitivityY;
		}
		else if (Input.GetKey("x"))
		{
			rotationY -= sensitivityY;
		}
		rotationY = Mathf.Clamp (rotationY, minimumY, maximumY);
			
		transform.localEulerAngles = new Vector3(-rotationY, transform.localEulerAngles.y, 0);	
	}
}
