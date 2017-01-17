using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class KeyboardMouseMovement : MonoBehaviour {

    // not needed, just here to visualize how it works.
    public GameObject p;


    public float rotationSpeed;
    public float SensitivityZ = 1;

    private Plane plane;
    private Vector3 planePosition = Vector3.zero;
    private Vector3 rot;

    private bool performSpiral;
    public float spiralSpeed;
    public float radiusDecrease;
    public float spiralSize;
    private float currentRadius;

    public BioIK.KinematicJoint[] kinematicJoints;
    private GameObject tool;
    private string jointLogFormat = "{0,-11} {1,-12:F4} {2,-12:F4} {3,-12:F4} {4,-12:F4} {5,-12:F4} {6,-12:F4} {7,-12:F4}";
    private string coordinateLogFormat = "{0,-8}\nPosition:   X = {1,-12:F4} Y = {2,-12:F4} Z = {3,-12:F4}\nRotation:   X = {4,-12:F4} Y = {5,-12:F4} Z = {6,-12:F4}";

	// Use this for initialization
	void Start () {
        plane = new Plane(Vector3.back, Vector3.zero);
        rot = transform.localRotation.eulerAngles;
        performSpiral = false;

        tool = GameObject.Find("Hammer");
        kinematicJoints = new BioIK.KinematicJoint[7];
        string jointName = "joint_a{0}";
        GameObject joint;
        for (int i = 1; i <= 7; i++) {
            joint = GameObject.Find(String.Format(jointName, i));
            kinematicJoints[i-1] = joint.GetComponent<BioIK.KinematicJoint>();
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKey(KeyCode.Space)) {
            performSpiral = true;
            currentRadius = spiralSize;
            if (File.Exists(@"valueDump.txt"))
                File.Delete(@"valueDump.txt");
        }

        if (performSpiral) {
            oneSpiralStep();
            dumpTargetAndRobotValues();
        }
        else {
            Vector3 pos = transform.position;
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            planePosition.z += Input.GetAxis("Mouse ScrollWheel") * SensitivityZ;

            // visualize (debug)
            p.transform.position = planePosition;

            plane.SetNormalAndPosition(Vector3.back, planePosition);
            float distance;
            if (plane.Raycast(mouseRay, out distance))
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

    private void oneSpiralStep() {
        Vector3 pos = transform.position;

        float xPos = Mathf.Sin(Time.time * spiralSpeed) * currentRadius;
        float zPos = Mathf.Cos(Time.time * spiralSpeed) * currentRadius + 1;
        pos.x = xPos;
        pos.z = zPos;
        transform.position = pos;

        currentRadius -= radiusDecrease * Time.deltaTime;
        if (currentRadius <= 0)
            performSpiral = false;
    }

    private void dumpTargetAndRobotValues() {
        

        using (System.IO.StreamWriter file = 
            new System.IO.StreamWriter(@"valueDump.txt", true))
        {
            file.WriteLine(String.Format(jointLogFormat, "Joints:",
                                                         kinematicJoints[0].XMotion.GetTargetValue(),
                                                         kinematicJoints[1].XMotion.GetTargetValue(),
                                                         kinematicJoints[2].XMotion.GetTargetValue(),
                                                         kinematicJoints[3].XMotion.GetTargetValue(),
                                                         kinematicJoints[4].XMotion.GetTargetValue(),
                                                         kinematicJoints[5].XMotion.GetTargetValue(),
                                                         kinematicJoints[6].XMotion.GetTargetValue()
                                                          ));
            file.WriteLine(String.Format(coordinateLogFormat, "Target:",
                                                          transform.position.x,
                                                          transform.position.y,
                                                          transform.position.z,
                                                          transform.rotation.x,
                                                          transform.rotation.y,
                                                          transform.rotation.z
                                                          ));
            if (tool != null)
                file.WriteLine(String.Format(coordinateLogFormat, "Tool:",
                                                          tool.transform.position.x,
                                                          tool.transform.position.y,
                                                          tool.transform.position.z,
                                                          tool.transform.position.x,
                                                          tool.transform.position.y,
                                                          tool.transform.position.z
                                                          ));
        }
    }
}
