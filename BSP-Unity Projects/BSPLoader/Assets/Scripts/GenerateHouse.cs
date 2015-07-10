using UnityEngine;
using System.Collections;

public class GenerateHouse : MonoBehaviour {
	// Use this for initialization
	void Start () {
		gameObject.AddComponent<MeshFilter> ();
		gameObject.AddComponent<MeshRenderer> ();
		gameObject.AddComponent<MeshCollider> ();
		//GetComponent<BSPData2Unity3D> ().LoadModels0 ();
		GetComponent<BSPData2Unity3D> ().LoadVisibleModels (Camera.main);
	}
	
	// Update is called once per frame
	void Update () {


	}
}