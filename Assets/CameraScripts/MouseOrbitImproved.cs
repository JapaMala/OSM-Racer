using UnityEngine;
using System.Collections;
 
[AddComponentMenu("Camera-Control/Mouse Orbit with zoom")]
public class MouseOrbitImproved : MonoBehaviour {
 
    public Transform target;
    public float distance = 5.0f;
    public float xSpeed = 120.0f;
    public float ySpeed = 120.0f;
 
    public float yMinLimit = -20f;
    public float yMaxLimit = 80f;
 
    public float distanceMin = .5f;
    public float distanceMax = 15f;
	
	public float zoomSpeed = 5.0f;
	
	public float moveSpeedX = 10.0f;
	public float moveSpeedY = 10.0f;
 
    float x = 0.0f;
    float y = 0.0f;
 
	// Use this for initialization
	void Start () {
        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;
 
        // Make the rigid body not change rotation
        if (rigidbody)
            rigidbody.freezeRotation = true;
	}
 
    void LateUpdate () {
		if (Input.GetKeyDown("escape"))
			Screen.lockCursor = false;
		if (Input.GetKeyDown("mouse 0"))
			Screen.lockCursor = true;
		
	    if (target) {
			if(Screen.lockCursor) {
		        x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
		        y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
			}
	 
	        y = ClampAngle(y, yMinLimit, yMaxLimit);
	 
	        Quaternion rotation = Quaternion.Euler(y, x, 0);
	 		Quaternion targetRotation = Quaternion.Euler(0, x, 0);
	
	        distance = Mathf.Clamp(distance - Input.GetAxis("Mouse ScrollWheel")*zoomSpeed*distance*0.1f, distanceMin, distanceMax);
	 
	        RaycastHit hit;
	        if (Physics.Linecast (target.position, transform.position, out hit)) {
	                distance -=  hit.distance;
	        }
	        Vector3 negDistance = new Vector3(0.0f, 0.0f, -distance);
	        Vector3 position = rotation * negDistance + target.position;
	 
	        transform.rotation = rotation;
	        transform.position = position;
			
			int multiplier = 1;
			if(Input.GetButton("Fire2"))
				multiplier = 10;
			
			target.transform.rotation = targetRotation;
			target.Translate(((Vector3.forward*Time.deltaTime*multiplier*moveSpeedY*Input.GetAxis("Vertical")) + (Vector3.right*Time.deltaTime*multiplier*moveSpeedY*Input.GetAxis("Horizontal"))) * distance * 0.01f);
	    }
	}
 
    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
        return Mathf.Clamp(angle, min, max);
    }
 
 
}