using UnityEngine;
using System.Collections;

public class WalkInHouse : MonoBehaviour {
	GameObject house = null;
	public float moveSpeed = 300.0f;
	public float jumpSpeed = 5.0f;
	public float gravity = 20.0f;
	private Vector3 moveDirection = Vector3.zero;
	private CharacterController cc = null;
	private Transform cameraTransform = null;
	private Vector3 cameraRotation = Vector3.zero;
	private float cameraHeight = 30.0f;
	// Use this for initialization
	void Start () {
        house = GameObject.FindWithTag("House");
		Camera.main.farClipPlane = 5000;
		cameraTransform = Camera.main.transform;
		cameraTransform.localPosition = Vector3.up * cameraHeight;
		cameraRotation = cameraTransform.eulerAngles;

		gameObject.AddComponent<CharacterController> ();
		gameObject.AddComponent<CapsuleCollider> ();

		//transform.position = new Vector3 (-230, 156, -188);
		//transform.position = new Vector3 (-911, 2, -2467);
		//transform.position = new Vector3 (-2264, 2, -98);
		//transform.position = new Vector3 (-4374, 2, -654);
        transform.position = house.GetComponent<BSPData2Unity3D>().GetPlayerStartPosition;
		transform.eulerAngles = new Vector3 (0, -90, 0);

		cc = gameObject.GetComponent<CharacterController> ();
		cc.slopeLimit = 75.0f;
		cc.stepOffset = 0.4f;
	}
	
	// Update is called once per frame
	void Update () {
		// Camera Rotation
		float rh = Input.GetAxis ("Mouse X");
		float rv = Input.GetAxis ("Mouse Y");
		cameraRotation.x -= rv;
		cameraRotation.y += rh;
		cameraTransform.eulerAngles = cameraRotation;

		// Player Rotation come with camera
		transform.eulerAngles = new Vector3(0, cameraRotation.y, 0);

		// Player Walk
		if (cc.isGrounded) {
			float h = Input.GetAxis ("Horizontal") * moveSpeed * Time.deltaTime;
			float v = Input.GetAxis ("Vertical") * moveSpeed * Time.deltaTime;
			moveDirection = new Vector3(h, 0, v);
			if(Input.GetButton("Jump")) {
				moveDirection.y = jumpSpeed;
			}
			moveDirection = transform.TransformDirection(moveDirection);
		}
		moveDirection.y -= gravity * Time.deltaTime;
		cc.Move (moveDirection);

		// Camera Position
		cameraTransform.localPosition = Vector3.up * cameraHeight;

		// Update House Visible Portal
		house.GetComponent<BSPData2Unity3D> ().LoadVisibleModel0 (Camera.main);
	}
}
