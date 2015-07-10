using UnityEngine;
using System.Collections;

public class Trash : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	//	void LoadMeshes(ArrayList faceIndices) {
	//		gameObject.transform.DetachChildren ();
	//		for (int i=0; i<faceIndices.Count; i++) {
	//			int faceIndex = (int)faceIndices[i];
	//			BSPFileParser.Face f = _faces[faceIndex];
	//			if(meshes[faceIndex] == null) {
	//				meshes[faceIndex] = new UnityEngine.Mesh();
	//				meshes[faceIndex].vertices = vertices;
	//				meshes[faceIndex].normals = normals;
	//				meshes[faceIndex].uv = uvs;
	//				meshes[faceIndex].uv2 = uv2s;
	//				meshes[faceIndex].colors = colors;
	//				int [] triangles = new int[f.n_meshverts];
	//				for (int j=f.meshvert; j<f.meshvert+f.n_meshverts; j++) {
	//					triangles [j - f.meshvert] = f.vertex + _meshverts [j];
	//				}
	//				meshes[faceIndex].triangles = triangles;
	//			}
	//			
	//			if(objects[faceIndex] == null) {
	//				GameObject childObject = new GameObject("HouseChild");
	//				childObject.AddComponent<MeshFilter>();
	//				childObject.GetComponent<MeshFilter>().mesh = meshes[faceIndex];
	//				childObject.AddComponent<MeshRenderer>();
	//				childObject.GetComponent<MeshRenderer>().material = _materials[f.texture];
	//				childObject.AddComponent<MeshCollider>();
	//				childObject.GetComponent<MeshCollider>().sharedMesh = meshes[faceIndex];
	//				objects[faceIndex] = childObject;
	//			}
	//
	//			objects[faceIndex].transform.SetParent(gameObject.transform);
	//		}
	//	}
//	void LoadObjects(ArrayList faceIndices) {
//		//gameObject.transform.DetachChildren ();
//		BSPFileParser.Model m = _models [0];
//		for (int faceIndex=m.face; faceIndex<m.face+m.n_faces; faceIndex++) {
//			objects[faceIndex].SetActive(false);
//		}
//		for (int i=0; i<faceIndices.Count; i++) {
//			int faceIndex = (int)faceIndices[i];
//			objects[faceIndex].SetActive(true);
//		}
//	}
//	public void LoadModel0() {
//		BSPFileParser.Model m = _models [0];
//		ArrayList temp = new ArrayList ();
//		for (int i=m.face; i<m.face+m.n_faces; i++) {
//			temp.Add(i);
//		}
//		LoadObjects (temp);
//	}
	//	ArrayList frustumCulling(Camera cam, ArrayList visibleLeafs) {
	//		ArrayList visible = new ArrayList ();
	//		UnityEngine.Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes (cam);
	//		foreach (int i in visibleLeafs) {
	//			BSPFileParser.BSPTreeLeaf lf = _leafs[i];
	//
	//			Bounds b = new Bounds ();
	//			//				b.SetMinMax(m.MultiplyVector(right2left(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]))),
	//			//				            m.MultiplyVector(right2left(new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]))));
	//			// Notice: Because Convert Right hang coordinate to left coordinate
	//			// the bounding box min and max should be swap
	//			b.SetMinMax(right2left(new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2])),
	//						right2left(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2])));
	//			if(GeometryUtility.TestPlanesAABB(cameraPlanes, b)) {
	//				visible.Add(i);
	//			}
	//		}
	//		return visible;
	//	}
	//	ArrayList findVisibleLeafs(Camera cam, int leafIndex) {
	//		if(cam == null) {
	//			throw new ArgumentException();
	//		}
	//		ArrayList visibleLeafs = new ArrayList();
	//
	//		int cluster = _leafs[leafIndex].cluster;
	//		UnityEngine.Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes (cam);
	//		for(int i=0; i<_leafs.Length; i++) {
	//			BSPFileParser.BSPTreeLeaf lf = _leafs[i];
	//
	//			// Occlusion Culling
	//			// Now, there is some problem, so always return true, frustum return true too.
	//
	//			Debug.Log("Occlusion Culling, Frustum Culling should be modified");
	//
	//			if(isClusterVisible(cluster, lf.cluster)) {
	//				// Frustum Culling
	//				Bounds b = new Bounds ();
	////				Matrix4x4 m = transform.worldToLocalMatrix;
	////				b.SetMinMax(m.MultiplyVector(right2left(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]))),
	////				            m.MultiplyVector(right2left(new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]))));
	//				b.SetMinMax(right2left(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2])),
	//				            right2left(new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2])));
	//				// Now, there is some problem, so always return true
	//				if(GeometryUtility.TestPlanesAABB(cameraPlanes, b)) {
	//					visibleLeafs.Add(i);
	//				}
	//			}
	//		}
	//		return visibleLeafs;
	//	}
//	void LoadAllMeshAndObject1(BSPFileParser.Model m) {
//		gameObject.transform.DetachChildren ();
//		for (int leafIndex=0; leafIndex<_leafs.Length; leafIndex++) {
//			UnityEngine.Mesh mesh = new UnityEngine.Mesh();
//			mesh.vertices = vertices;
//			mesh.normals = normals;
//			mesh.uv = uvs;
//			mesh.uv2 = uv2s;
//			mesh.colors = colors;
//			
//			BSPFileParser.BSPTreeLeaf lf = _leafs[leafIndex];
//			materials = new UnityEngine.Material[lf.n_leaffaces];
//			mesh.subMeshCount = lf.n_leaffaces;
//			for(int faceIndex=lf.leafface; faceIndex<lf.leafface+lf.n_leaffaces; faceIndex++) {
//				BSPFileParser.Face f = _faces[_leaffaces[faceIndex]];
//				
//				materials[faceIndex-lf.leafface] = _materials[f.texture];
//				
//				int [] triangles = new int[f.n_meshverts];
//				for(int j=f.meshvert; j<f.meshvert+f.n_meshverts; j++) {
//					triangles[j-f.meshvert] = f.vertex + _meshverts[j];
//				}
//				mesh.SetTriangles(triangles, faceIndex-lf.leafface);
//			}
//			
//			GameObject childObject = new GameObject ("HouseChild");
//			childObject.AddComponent<MeshFilter> ();
//			childObject.GetComponent<MeshFilter> ().mesh = mesh;
//			childObject.AddComponent<MeshRenderer> ();
//			childObject.GetComponent<MeshRenderer> ().materials = materials;
//			childObject.AddComponent<MeshCollider> ();
//			childObject.GetComponent<MeshCollider> ().sharedMesh = mesh;
//			
//			childObject.transform.SetParent(gameObject.transform);
//		}
//	}
//	public void LoadVisibleModel01(Camera cam) {
//		int leafIndex = findLeafIndex (cam.transform.position);
//		int cluster = _leafs [leafIndex].cluster;
//		//Debug.Log (leafIndex);
//		//Debug.Log (cluster);
//		if (cluster != lastCluster || lastVisibleLeafs == null) {
//			//Debug.Log(cluster);
//			lastVisibleLeafs = occlusionCulling (cluster);
//			//Debug.Log(lastVisibleLeafs.Count);
//			lastCluster = cluster;
//		}
//		
//		//		foreach (UnityEngine.Transform child in transform) {
//		//			child.gameObject.SetActive(false);
//		//		}
//		
//		ArrayList visible = new ArrayList ();
//		UnityEngine.Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes (cam);
//		foreach (int i in lastVisibleLeafs) {
//			BSPFileParser.BSPTreeLeaf lf = _leafs[i];
//			if(GeometryUtility.TestPlanesAABB(cameraPlanes, gameObject.transform.GetChild(i).GetComponent<MeshCollider>().bounds)) {
//				gameObject.transform.GetChild(i).gameObject.SetActive(true);
//			}
//		}
//	}

}
