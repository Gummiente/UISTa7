using UnityEngine;

public class Movement : MonoBehaviour {

    public GameObject groundPlane;
    public GameObject controller;
    public GameObject cameraRig;
    public GameObject head;
    public float speed;
    public float zoomspeed;
    public GameObject marker;
    public float strideLength;
    public Vector3 amplitude;

    private bool moving;
    private bool zooming;
    private Vector3 zoomtarget;
    private float timeStartedLerping;
    private float timeSinceLerpStarted;
    private float lerpProgress;
    
    private SteamVR_TrackedController movementController;

    private LineRenderer lr;

    public enum MovementType { WALK, FLY, TELEPORT, ZOOM, WALKBOUNCE, WALKFOV };
    public MovementType movementType;

    void Start ()
    {
        lr = GetComponent<LineRenderer>();

        // Add our own method to to be called, when TriggerClicked event occurs
        movementController = controller.GetComponent<SteamVR_TrackedController>();
        movementController.TriggerClicked += Trigger;

        Debug.Log(movementController);
	}
	
	void Update ()
    {
        // For keyboard debugging
        if (Input.GetKeyDown(KeyCode.W)) {
            moving = !moving;
        }

        // Setup view data (direction, hitpoint)
        Vector3 direction = head.transform.forward;
        RaycastHit hit;
        Ray r = new Ray(head.transform.position, direction);
        groundPlane.GetComponent<MeshCollider>().Raycast(r, out hit, 100);
        if (movementType == MovementType.TELEPORT || movementType == MovementType.ZOOM)
        {
            marker.transform.position = hit.point;
        }

        // Move camerarig by type
        if (moving)
        {
            Debug.Log("Moving");
            switch (movementType)
            {
                case MovementType.WALK:
                    direction.y = 0;
                    cameraRig.transform.position = Vector3.Lerp(cameraRig.transform.position, cameraRig.transform.position + direction, speed * Time.deltaTime);
                    break;
                case MovementType.FLY:
                    cameraRig.transform.position = Vector3.Lerp(cameraRig.transform.position, cameraRig.transform.position + direction, speed * Time.deltaTime);
                    break;
                case MovementType.TELEPORT:
                    cameraRig.transform.position = hit.point + Vector3.up*2;
                    moving = false;
                    break;
                case MovementType.ZOOM:
                    zoomtarget = hit.point + Vector3.up*2;
                    zooming = true;
                    timeStartedLerping = Time.time;
                    moving = false;
                    break;
                case MovementType.WALKBOUNCE:
                    direction.y = 0;
                    Vector3 pos = Vector3.Lerp(cameraRig.transform.position, cameraRig.transform.position + direction, speed * Time.deltaTime);
                    
                    pos.y = amplitude.y * Mathf.Cos(Mathf.PI * Time.time / strideLength);
                    cameraRig.transform.position = pos + Vector3.up*2;
                    break;

            }
        }
        if (zooming) {
            timeSinceLerpStarted = Time.time - timeStartedLerping;
            lerpProgress = timeSinceLerpStarted / (speed/5);
            cameraRig.transform.position = Vector3.Lerp(cameraRig.transform.position, zoomtarget, lerpProgress);
          if(lerpProgress >= 1.0f)
            {
                Debug.Log("Lerping OVER");
                zooming = false;
            }
        }
	}

     

    void Trigger(object sender, ClickedEventArgs e) {
        Debug.Log("TRIGGER");
        moving = !moving;
    }
}
