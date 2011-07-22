var speed : float = 150.0;
 
function Update() {
 
	//is the user pressing left or right (or "a &amp; "d") on the keyboard?
	horMovement = Input.GetAxis("Horizontal");
	//is the user pressing up or down (or "w" &amp; "s") on the keyboard?
	forwardMovement = Input.GetAxis("Vertical");
 
	if (horMovement) {
		transform.Translate(transform.right * horMovement * Time.deltaTime * speed, Space.World);
	} 
 
	if (forwardMovement) {
		transform.Translate(transform.forward * forwardMovement * Time.deltaTime * speed, Space.World);
	}
}