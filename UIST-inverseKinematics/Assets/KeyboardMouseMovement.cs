using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeyboardMouseMovement : MonoBehaviour {

    // not needed, just here to visualize how it works.
    public GameObject p;


    public float rotationSpeed;
    public float SensitivityZ = 1;

    private Plane plane;
    private Vector3 planePosition = Vector3.zero;
    private Vector3 rot;


	// Use this for initialization
	void Start () {
        plane = new Plane(Vector3.back, Vector3.zero);
        rot = transform.localRotation.eulerAngles;
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 pos = transform.position;
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        planePosition.z += Input.GetAxis("Mouse ScrollWheel") * SensitivityZ;

        // visualize (debug)
        p.transform.position = planePosition;

        plane.SetNormalAndPosition(Vector3.back, planePosition);
        float distance;
        if(plane.Raycast(mouseRay, out distance))
            transform.position = mouseRay.GetPoint(distance);
         else
            Debug.Log("plane might be behind the camera");


        if (Input.GetKey(KeyCode.W))
            rot.x += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.S))
            rot.x -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.Q))
            rot.y += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            rot.y -= rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.A))
            rot.z += rotationSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.D))
            rot.z -= rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rot);
    }
}
