using UnityEngine;
using System.Collections;

public class WalkInHouse : MonoBehaviour {
	CharacterController cc = null;
	GameObject house = null;
	public float moveSpeed = 300.0f;
	public float jumpSpeed = 8.0f;
	public float gravity = 20.0f;
	public float cameraHeight = 30.0f;
	Vector3 cameraEulerAngles = Vector3.zero;
	private Vector3 moveDirection = Vector3.zero;
	// Use this for initialization
	void Start () {
        house = GameObject.FindWithTag("House");
        Camera.main.farClipPlane = 10000;
		//Camera.main.transform.localPosition = Vector3.up * cameraHeight;
		Camera.main.transform.localPosition = Vector3.zero;
		cameraEulerAngles = Camera.main.transform.eulerAngles;

		gameObject.AddComponent<CharacterController> ();
		gameObject.AddComponent<CapsuleCollider> ();

		//transform.position = new Vector3 (-230, 156, -188);
        transform.position = house.GetComponent<BSPData2Unity3D> ().GetPlayerStartPosition;
		transform.eulerAngles = new Vector3 (0, -90, 0);

		cc = gameObject.GetComponent<CharacterController> ();
	}
	
	// Update is called once per frame
	void Update () {
		float rh = Input.GetAxis ("Mouse X");
		float rv = Input.GetAxis ("Mouse Y");
		cameraEulerAngles.x -= rv;
		cameraEulerAngles.y += rh;
		Camera.main.transform.eulerAngles = cameraEulerAngles;
		transform.eulerAngles = new Vector3 (0, cameraEulerAngles.y, 0);

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

		//Camera.main.transform.localPosition = Vector3.up * cameraHeight;

		cc.Move (moveDirection);

		house.GetComponent<BSPData2Unity3D> ().LoadVisibleModels (Camera.main);
        //house.GetComponent<MeshCollider>().sharedMesh = BSPData2Unity3D.mesh;
	}
}
