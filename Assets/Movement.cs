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
    public Vector3 Amplitude;

    private bool moving;
    private bool zooming;
    private Vector3 zoomtarget;
    
    private SteamVR_TrackedController movementController;

    private LineRenderer lr;

    public enum MovementType { WALK, FLY, TELEPORT, ZOOM, WALKBOUNCE, WALKFOV };

    public MovementType movementType;

    // Use this for initialization
    void Start ()
    {
        lr = GetComponent<LineRenderer>();
        movementController = controller.GetComponent<SteamVR_TrackedController>();
        movementController.TriggerClicked += Trigger;
        Debug.Log(movementController);
	}
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 direction = head.transform.forward;
        RaycastHit hit;
        Ray r = new Ray(head.transform.position, direction);

        groundPlane.GetComponent<MeshCollider>().Raycast(r, out hit, 100);
        if (movementType == MovementType.TELEPORT || movementType == MovementType.ZOOM)
        {
            marker.transform.position = hit.point;
        }
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
                    cameraRig.transform.position = hit.point;
                    moving = false;
                    break;
                case MovementType.ZOOM:
                    zoomtarget = hit.point;
                    zooming = true;
                    moving = false;
                    break;
                case MovementType.WALKBOUNCE:
                    direction.y = 0;
                    Vector3 pos = Vector3.Lerp(cameraRig.transform.position, cameraRig.transform.position + direction, speed * Time.deltaTime);
                    
                    pos.y = Amplitude.y  * Mathf.Cos(Mathf.PI * Time.time / strideLength);
                    cameraRig.transform.position = pos ;
                    break;

            }
        }
        if (zooming) {
            cameraRig.transform.position = Vector3.Lerp(cameraRig.transform.position, zoomtarget, zoomspeed * Time.deltaTime);
        }
	}

     

    void Trigger(object sender, ClickedEventArgs e) {
        Debug.Log("TRIGGER");
        moving = !moving;
    }
}
