using UnityEngine;
using System.Collections;

public class GenerateHouse : MonoBehaviour {
	// Use this for initialization
	void Start () {

		//GetComponent<BSPData2Unity3D> ().LoadModel0 ();
		GetComponent<BSPData2Unity3D> ().LoadVisibleModel0 (Camera.main);
	}
	
	// Update is called once per frame
	void Update () {


	}
}