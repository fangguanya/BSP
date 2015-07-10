using UnityEngine;
using System;
using System.IO;
using System.Collections;
using BSPFileParser;

public class BSPData2Unity3D  : MonoBehaviour {
	private BSPParser parser = null;
	private BSPFileParser.Texture[] _textures;
	private BSPFileParser.Vertex[] _vertexes;
	private BSPFileParser.Plane[] _planes;
	private BSPFileParser.Face[] _faces;
	private BSPFileParser.Model[] _models;
	private BSPFileParser.BSPTreeNode[] _nodes;
	private BSPFileParser.BSPTreeLeaf[] _leafs;
	private BSPFileParser.Visibilitydata _visdata;
    private string _entities;
    private float[] _playerstart;
    private BSPFileParser.Brush[] _brushes;
    private BSPFileParser.Brushside[] _brushsides;
	private int[] _meshverts;
	private int[] _leaffaces;
    private int[] _leafbrushes;
	private int lastCluster = -1;
	private ArrayList lastVisibleLeafs = null;

	public static UnityEngine.Texture2D[] textures = null;
	public static UnityEngine.Material[] materials = null;
	public static UnityEngine.Material[] _materials = null;
	public static UnityEngine.Mesh mesh = null;
	public static UnityEngine.Plane[] planes = null;
    private string RESOURCESPATH = @"Assets\Data\";
	public string FILENAME = "ssjj.bsp";
    
    // Buffer meshes, game objects, and something else
    public static UnityEngine.Mesh[] meshes = null;  // A GameObject keep one mesh, correspond a BSP's face(a triangle list)
	public static UnityEngine.GameObject[] objects = null;
	Vector3[] vertices = null;
	Vector3[] normals = null;
	Vector2[] uvs = null;
	Vector2[] uv2s = null;
	Color[] colors = null;

	private const float BACKCULLINGFACTOR = 0.7f;  // backface culling threshold
    private bool dataBeLoad = false;


    public enum GizmosDrawType {GizmosDrawTypeCameraInLeaf, GizmosDrawTypeFrustumCulling, GizmosDrawTypeLeafBrushes, GizmosDrawTypeLeaves, GizmosDrawTypeBrush};
    public int gizmosDrawWhichLeaf = 1;
    public int gizmosDrawWhichBrush = 1;
    public bool gizmosIsSelected = true;
    public GizmosDrawType gizmosDrawType = GizmosDrawType.GizmosDrawTypeCameraInLeaf;
    public enum ModelColliderType { MeshCollider, BoxCollider, RealtimeCollider};
    public ModelColliderType colliderType = ModelColliderType.BoxCollider;
    void Awake() {
        parser = new BSPParser(RESOURCESPATH + FILENAME);
        _textures = parser.Textures;
        _vertexes = parser.Vertexes;
        _planes = parser.Planes;
        _faces = parser.Faces;
        _models = parser.Models;
        _meshverts = parser.Meshverts;
        _nodes = parser.Nodes;
        _leafs = parser.Leafs;
        _leaffaces = parser.Leaffaces;
        _leafbrushes = parser.Leafbrushes;
        _brushes = parser.Brushes;
        _brushsides = parser.Brushsides;
        _visdata = parser.Visdata;
        _playerstart = parser.GetPlayerStartPosition();
    }
    
    void Start() {
		LoadData ();
	}

    void LoadData() {
        if (!dataBeLoad) {
            LoadTextures();
            LoadPlanes();
            int len = _vertexes.Length;
            vertices = new Vector3[len];
            normals = new Vector3[len];
            uvs = new Vector2[len];
            uv2s = new Vector2[len];
            colors = new Color[len];

            for (int i = 0; i < len; i++) {
                BSPFileParser.Vertex v = _vertexes[i];
                // Notice: Convert Vertex from right hand coordinate into left coordinate
                vertices[i] = Right2Left(new Vector3(v.position[0], v.position[1], v.position[2]));
                // Notice: Convert Normal from right hand coordinate into left coordinate
                normals[i] = Right2Left(new Vector3(v.normal[0], v.normal[1], v.normal[2]));
                uvs[i] = new Vector2(v.uv[0][0], v.uv[0][1]);
                uv2s[i] = new Vector2(v.uv[1][0], v.uv[1][1]);
                colors[i] = new Color(((float)v.color[0]) / 255, ((float)v.color[1]) / 255, ((float)v.color[2]) / 255);
            }
            LoadAllMeshAndObject(gameObject, _models[0], "HouseChild");
            dataBeLoad = true;
        }
    }

    public Vector3 GetPlayerStartPosition {
        get {
            return Right2Left(new Vector3(_playerstart[0], _playerstart[1], _playerstart[2]));
        }
    }

	void LoadTextures() {
		if (textures == null) {
			int len = _textures.Length;
			textures = new UnityEngine.Texture2D[len];
			_materials = new UnityEngine.Material[len];
			for(int i=0; i<len; i++) {
				string[] parts = new string(_textures[i].name).Split('/');
				textures[i] = Resources.Load(parts[parts.Length-1]) as Texture2D;
				_materials[i] = new UnityEngine.Material(Shader.Find ("Diffuse"));
				_materials[i].mainTexture = textures[i];
			}
		}
	}

	void LoadPlanes() {
		if (planes == null) {
			int len = _planes.Length;
			planes = new UnityEngine.Plane[len];
			for(int i=0; i<len; i++) {
				BSPFileParser.Plane p = _planes[i];
                // Notice: Convert Plane from right hand coordinate into left coordinate
				UnityEngine.Plane plane = new UnityEngine.Plane(new Vector3(p.normal[0], p.normal[1], p.normal[2]), p.dist);
				planes[i] = Right2Left(plane);
			}
		}
	}

    // Load all of meshes to construct game objects
    // and as children be add to gmObject
	void LoadAllMeshAndObject(GameObject gmObject, BSPFileParser.Model m, string childName) {
        gmObject.transform.DetachChildren();

		meshes = new UnityEngine.Mesh[m.face+m.n_faces];
		objects = new UnityEngine.GameObject[m.face+m.n_faces];
		for (int faceIndex=m.face; faceIndex<m.face+m.n_faces; faceIndex++) {
			BSPFileParser.Face f = _faces [faceIndex];
            
            // Construct Mesh
            UnityEngine.Mesh mesh = new UnityEngine.Mesh();
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.uv2 = uv2s;
            mesh.colors = colors;
            int[] triangles = new int[f.n_meshverts];
            for (int j = f.meshvert; j < f.meshvert + f.n_meshverts; j++) {
                triangles[j - f.meshvert] = f.vertex + _meshverts[j];
            }
            mesh.triangles = triangles;

            // Construct GameObject
            GameObject childObject = new GameObject(childName);
            childObject.AddComponent<MeshFilter>().mesh = mesh;
            childObject.AddComponent<MeshRenderer>().material = _materials[f.texture];

            if (colliderType == ModelColliderType.MeshCollider) { 
                childObject.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            
            childObject.transform.SetParent(gmObject.transform);

            meshes[faceIndex] = mesh;
            objects[faceIndex] = childObject;
		}
        
        if (colliderType == ModelColliderType.BoxCollider) { 
            LoadCollider(gmObject);
        }
	}

    void LoadCollider(GameObject gmObject) {
        for (int i = 0; i < _leafs.Length; i++) {
            for (int j = _leafs[i].leafbrush; j < _leafs[i].leafbrush + _leafs[i].n_leafbrushes; j++) { 
                int brushIndex = _leafbrushes[j];
                BSPFileParser.Brush bsh = _brushes[brushIndex];

                if (bsh.n_brushsides == 6) { 
                    // Generate Box Collider directly by the 6 bounding planes
                    UnityEngine.Plane[] ps = new UnityEngine.Plane[bsh.n_brushsides];                   
                    for (int k = bsh.brushside; k < bsh.brushside + bsh.n_brushsides; k++) {
                        UnityEngine.Plane plane = planes[_brushsides[k].plane];
                        ps[k-bsh.brushside] = new UnityEngine.Plane(plane.normal, plane.distance);
                    }
                    Bounds b = GenerateBoxFromPlanes(ps);

                    GameObject obj = new GameObject();
                    obj.name = "BoxCollider";
                    obj.AddComponent<BoxCollider>();
                    obj.GetComponent<BoxCollider>().center = b.center;
                    obj.GetComponent<BoxCollider>().size = b.size;
                    obj.transform.SetParent(gmObject.transform);
                }
                else { 
                    // Generate Box Collider by leaf bounding box which brush in it
                    for (int m = 0; m < _leafs.Length; m++) {
                        BSPFileParser.BSPTreeLeaf lf = _leafs[m];
                        for (int n = lf.leafbrush; n < lf.leafbrush + lf.n_leafbrushes; n++) {
                            if (_leafbrushes[n] == brushIndex) {
                                Bounds b = new Bounds();
                                b.SetMinMax(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]),
                                               new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]));
                                b = Right2Left(b);
                                GameObject obj = new GameObject();
                                obj.name = "BoxCollider";
                                obj.AddComponent<BoxCollider>();
                                obj.GetComponent<BoxCollider>().center = b.center;
                                obj.GetComponent<BoxCollider>().size = b.size;
                                obj.transform.SetParent(gmObject.transform);
                            }
                        }
                    }

                }
               
            }
        }
    }

    // Set game objects status base on responding BSP face's visible
    void LoadObjects(bool[] faces, int first, int len) {
        for (int faceIndex = first; faceIndex < first + len; faceIndex++) {
            objects[faceIndex].SetActive(faces[faceIndex]);
        }
    }

	public void LoadModel0() {
		BSPFileParser.Model m = _models [0];
		bool[] temp = new bool[m.face+m.n_faces];
		for (int i=m.face; i<m.face+m.n_faces; i++) {
			temp[i] = true;
		}
		LoadObjects (temp, m.face, m.n_faces);
	}

	public void LoadVisibleModel0(Camera cam) {
		ArrayList visibleLeafs = findVisibleLeafs (cam);
		ArrayList visibleFaces = findVisibleFaces (visibleLeafs);
		visibleFaces = backfaceCulling (cam, visibleFaces);
		BSPFileParser.Model m = _models [0];
		bool[] temp = new bool[m.face+m.n_faces];
		for (int i=0; i<visibleFaces.Count; i++) {
			temp[(int)visibleFaces[i]] = true;
		}
		LoadObjects (temp, m.face, m.n_faces);
	}

	ArrayList findVisibleLeafs(Camera cam) {
		int leafIndex = findLeafIndex (cam.transform.position);
		int cluster = _leafs [leafIndex].cluster;
		if (cluster != lastCluster || lastVisibleLeafs == null) {
			lastVisibleLeafs = occlusionCulling (cluster);
			lastCluster = cluster;
		}

		return frustumCulling (cam, lastVisibleLeafs);
	}

	ArrayList findVisibleFaces(ArrayList visibleLeafs) {
		if (visibleLeafs == null) {
			throw new ArgumentException();
		}
		bool[] alreadyVisibleFaces = new bool[_faces.Length];
		ArrayList visibleFaces = new ArrayList();
		foreach(int i in visibleLeafs) {
			BSPTreeLeaf lf = _leafs[i];
			for(int j=lf.leafface; j<lf.leafface+lf.n_leaffaces; j++) {
				int faceIndex = _leaffaces[j];
				if(!alreadyVisibleFaces[faceIndex]) {
					alreadyVisibleFaces[faceIndex] = true;
					visibleFaces.Add(faceIndex);
				}
			}
		}
		return visibleFaces;
	}
	
	ArrayList occlusionCulling(int cluster) {
		ArrayList visibleLeafs = new ArrayList();
		for (int i=0; i<_leafs.Length; i++) {
			BSPFileParser.BSPTreeLeaf lf = _leafs [i];
			if(isClusterVisible(cluster, lf.cluster)) {
				visibleLeafs.Add(i);
			}
		}
		return visibleLeafs;
	}

	ArrayList frustumCulling(Camera cam, ArrayList visibleLeafs) {
		ArrayList visible = new ArrayList ();
		UnityEngine.Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes (cam);
		foreach (int i in visibleLeafs) {
			BSPFileParser.BSPTreeLeaf lf = _leafs[i];
			for(int face = lf.leafface; face<lf.leafface+lf.n_leaffaces; face++) {
				int faceIndex = _leaffaces[face];
				//if(GeometryUtility.TestPlanesAABB(cameraPlanes, objects[faceIndex].GetComponent<MeshCollider>().bounds)) {
				if(GeometryUtility.TestPlanesAABB(cameraPlanes, objects[faceIndex].GetComponent<MeshRenderer>().bounds)) {
					visible.Add(i);
					break;  // if any of leaf's face is visible, meaning the leaf is visible(no need to check other faces)
				}
			}
		}
		return visible;
	}

	ArrayList backfaceCulling(Camera cam, ArrayList visibleFaces) {
		ArrayList visible = new ArrayList ();
		foreach (int i in visibleFaces) {
			BSPFileParser.Face f = _faces[i];
			Vector3 normal = Right2Left(new Vector3(f.normal[0], f.normal[1], f.normal[2]));
			if(Vector3.Dot(cam.transform.forward, normal) < BACKCULLINGFACTOR) {
				visible.Add(i);
			}
		}
		return visible;
	}
	
	int findLeafIndex(Vector3 position) {
		if (position == null) {
			throw new ArgumentException();
		}

		int index = 0;
		while(index >= 0) {
			BSPFileParser.BSPTreeNode node = _nodes[index];
			UnityEngine.Plane plane = planes[node.plane];
			if(plane.GetSide(position)) {
				index = node.children[0];
			}
			else {
				index = node.children[1];
			}
		}
		return -index - 1;
	}

	bool isClusterVisible(int cluster, int testCluster) {
		if(cluster == -1 || testCluster == -1) {
			return false;
		}
		if(_visdata.vecs == null || cluster < 0 || testCluster < 0)
		{
			return true;
		}
		int index = cluster * _visdata.sz_vecs + (testCluster >> 3);
		Byte temp = _visdata.vecs[index];
		return (temp & (1 << (testCluster % 8))) != 0;
	}

	
	// Convert BSP coordinate into Unity3D coordinate
	// BSP File data based coordinate system(Right-hand):
	// 	x-axis points East, y-axis points South, and z-axis points vertically downward
	// Unity3D based coordinate system(Left-hand):
	//  x-axis points East, y-axis points vertically upward, and z-axis points North
    // You must convert BSP's vertex, normal, plane, bounding box into Unity3D coordinate
	UnityEngine.Plane Right2Left(UnityEngine.Plane plane) {
		plane.normal = Right2Left (plane.normal);
		// Notice: While Right hand coordinate convert into left coordinate,
		// you must negative plane.distance.
		plane.distance = - plane.distance;
		return plane;
	}
	
	UnityEngine.Vector3 Right2Left(UnityEngine.Vector3 v) {
		if(v == null)
		{
			throw new ArgumentException();
		}
		float temp = v.y;
		v.y = v.z;
		v.z = -temp;
		v.x = -v.x;
		return v;
	}

    UnityEngine.Bounds Right2Left(UnityEngine.Bounds b) {
        // Notice: Convert Bounds is a little incomprehensible
        // you should convert the bounding box's center and diagonal
        // rather than convert it's min and max.
        Vector3 boxmin = b.min;
        Vector3 boxmax = b.max;
        Vector3 center = Right2Left((boxmin + boxmax) / 2);
        Vector3 direction = Right2Left(b.min - b.max);
        direction.x = direction.x > 0 ? direction.x : -direction.x;
        direction.y = direction.y > 0 ? direction.y : -direction.y;
        direction.z = direction.z > 0 ? direction.z : -direction.z;
        return new UnityEngine.Bounds(center, direction);
    }
	
	float[] Right2Left(float[] v) {
		if(v == null || v.Length != 3)
		{
			throw new ArgumentException();
		}
		float temp = v [1];  // y
		v [1] = v [2];		 // y = z
		v [2] = -temp;	    // z = -y
		v [0] = -v [0];		// x = -x
		return v;
	}
	
	int[] Right2Left(int[] v) {
		if(v == null || v.Length != 3)
		{
			throw new ArgumentException();
		}
		int temp = v [1]; // y
		v [1] = v [2];	  // y = z
		v [2] = -temp;	  // z = -y
		v [0] = -v [0];	  // x = -x
		return v;
	}

    // For Debug Visualization
	void OnDrawGizmos() {
		if (gizmosIsSelected)
			OnDrawGizmosSelected();
	}
	
	void OnDrawGizmosSelected() {
		// Draw World Space Origin
		Gizmos.DrawWireCube (Vector3.zero, new Vector3 (100, 100, 100));
        switch (gizmosDrawType) { 
            case GizmosDrawType.GizmosDrawTypeCameraInLeaf:
                // Draw the leaf which camera in side
                GizmosDrawCameraInsideLeaf(Camera.main);
                break;
            case GizmosDrawType.GizmosDrawTypeFrustumCulling:
                // Draw camera frustum culling process
                UnityEngine.Bounds[] bs = new UnityEngine.Bounds[lastVisibleLeafs.Count];
                for (int i=0; i<bs.Length; i++) {
                    BSPFileParser.BSPTreeLeaf leaf = _leafs[i];
                    Bounds b = new Bounds();
                    b.SetMinMax(new Vector3(leaf.mins[0], leaf.mins[1], leaf.mins[2]),
                                new Vector3(leaf.maxs[0], leaf.maxs[1], leaf.maxs[2]));
                    b = Right2Left(b);
                    bs[i] = b;
                }
                GizmosDrawCameraFrustum(Camera.main, Color.green);
                GizmosDrawFrustumCulling(Camera.main, Color.yellow, bs);
                break;
            case GizmosDrawType.GizmosDrawTypeLeaves:
                GizmosDrawLeaves(gizmosDrawWhichLeaf);
                //GizmosDrawLeaves(-1);
                break;
            case GizmosDrawType.GizmosDrawTypeLeafBrushes:
                GizmosDrawLeafBrushes(gizmosDrawWhichLeaf);
                GizmosDrawLeaves(gizmosDrawWhichLeaf);
                //GizmosDrawLeafBrushes(-1);
                break;
            case GizmosDrawType.GizmosDrawTypeBrush:
                GizmosDrawABrushOfLeaf(gizmosDrawWhichBrush);
                break;
        }
	}

    void GizmosDrawCameraInsideLeaf(Camera cam) {
        Color tc = Gizmos.color;
        Color[] cs = new Color[] { Color.gray, Color.yellow, Color.magenta, Color.red };  // from light to dark
        int colorIndex = 0;

        Vector3 position = cam.transform.position;
        // Camera
        Gizmos.DrawCube(position, new Vector3(200, 200, 200));
        int index = 0;
        while (index >= 0) {
            Gizmos.color = cs[colorIndex++ % cs.Length];
            BSPFileParser.BSPTreeNode node = _nodes[index];
            UnityEngine.Plane plane = planes[node.plane];
            Bounds b = new Bounds();
            b.SetMinMax(new Vector3(node.mins[0], node.mins[1], node.mins[2]),
                        new Vector3(node.maxs[0], node.maxs[1], node.maxs[2]));
            b = Right2Left(b);
            // Draw Sub-Space(BSP Tree Node bounding box)
            Gizmos.DrawWireCube(b.center, b.size);
            // Draw Split-Plane
            GizmosDrawPlane(GetOnePointOnPlane(plane), plane.normal, 5000.0f);
            if (plane.GetSide(position)) {
                index = node.children[0];
                //Debug.Log("Front");
            }
            else {
                index = node.children[1];
                //Debug.Log("Back");
            }
        }

        // Draw BSP Tree Leaf bounding box
        BSPFileParser.BSPTreeLeaf lf = _leafs[-index - 1];
        Bounds temp = new Bounds();
        temp.SetMinMax(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]),
                       new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]));
        temp = Right2Left(temp);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(temp.center, temp.size);

        Gizmos.color = tc;
    }

    void GizmosDrawCameraFrustum(Camera cam, Color color)
    {
        Color tc = Gizmos.color;
        Matrix4x4 tm = Gizmos.matrix;
        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.TRS(cam.transform.position, cam.transform.rotation, Vector3.one);
        Gizmos.DrawFrustum(Vector3.zero, cam.fieldOfView, cam.farClipPlane, cam.nearClipPlane, cam.aspect);
        Gizmos.matrix = tm;
        Gizmos.color = tc;
    }

    void GizmosDrawFrustumCulling(Camera cam, Color color, UnityEngine.Bounds[] bs) {
        Color tc = Gizmos.color;
        UnityEngine.Plane[] cameraPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
        foreach (UnityEngine.Bounds b in bs) {
            if (GeometryUtility.TestPlanesAABB(cameraPlanes, b)) {
                Gizmos.color = color;
            }
            else {
                Gizmos.color = InvertColor(color);
            }
            Gizmos.DrawWireCube(b.center, b.size);
        }
        Gizmos.color = tc;
    }

    void GizmosDrawLeaves(int whichLeaf) {
        Color tc = Gizmos.color;
        if (whichLeaf >= 0) {
            BSPFileParser.BSPTreeLeaf lf = _leafs[whichLeaf];
            Bounds temp = new Bounds();
            temp.SetMinMax(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]),
                           new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]));
            temp = Right2Left(temp);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(temp.center, temp.size);
            Gizmos.color = tc;
            return;
        }
        // Draw all of leaves
        for (int i = 0; i < _leafs.Length; i++) {
            BSPFileParser.BSPTreeLeaf lf = _leafs[i];
            Bounds temp = new Bounds();
            temp.SetMinMax(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]),
                           new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]));
            temp = Right2Left(temp);
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(temp.center, temp.size);
        }
        Gizmos.color = tc;
    }

    void GizmosDrawLeafBrushes(int whichLeaf) {
        Color tc = Gizmos.color;
        Color[] cs = new Color[] { Color.gray, Color.yellow, Color.magenta, Color.red };  // from light to dark
        int colorIndex = 0;
        if (whichLeaf >= 0) {
            BSPFileParser.BSPTreeLeaf lf = _leafs[whichLeaf];
            for (int j = lf.leafbrush; j < lf.leafbrush + lf.n_leafbrushes; j++) {
                int brushIndex = _leafbrushes[j];
                BSPFileParser.Brush bsh = _brushes[brushIndex];
                Gizmos.color = cs[colorIndex++ % cs.Length];
                
                for (int k = bsh.brushside; k < bsh.brushside + bsh.n_brushsides; k++) {
                    BSPFileParser.Brushside bsd = _brushsides[k];
                    UnityEngine.Plane plane = planes[bsd.plane];
                    GizmosDrawPlane(GetOnePointOnPlane(plane), plane.normal, 5000.0f);
                }
            }
            Gizmos.color = tc;
            return;
        }
        // Draw All brushes for leaves
        int len = _leafs.Length;
        for (int i = 0; i < len; i++) {
            BSPFileParser.BSPTreeLeaf lf = _leafs[i];
            for (int j = lf.leafbrush; j < lf.leafbrush + lf.n_leafbrushes; j++) { 
                int brushIndex = _leafbrushes[j];
                BSPFileParser.Brush bsh = _brushes[brushIndex];
                Gizmos.color = cs[colorIndex++ % cs.Length];
                for (int k = bsh.brushside; k < bsh.brushside + bsh.n_brushsides; k++) {
                    BSPFileParser.Brushside bsd = _brushsides[k];
                    UnityEngine.Plane plane = planes[bsd.plane];
                    GizmosDrawPlane(GetOnePointOnPlane(plane), plane.normal, 5000.0f);
                }
            }
        }
        Gizmos.color = tc;
    }

    void GizmosDrawABrushOfLeaf(int brushIndex) { 
        Color tc = Gizmos.color;
        // Draw Brush
        Gizmos.color = Color.white;
        BSPFileParser.Brush bsh = _brushes[brushIndex];
        for (int k = bsh.brushside; k < bsh.brushside + bsh.n_brushsides; k++) {
            BSPFileParser.Brushside bsd = _brushsides[k];
            UnityEngine.Plane plane = planes[bsd.plane];
            GizmosDrawPlane(GetOnePointOnPlane(plane), plane.normal, 5000.0f);
        }
        // Draw Leaf which hold the brushIndex
        Gizmos.color = Color.red;
        for (int i = 0; i < _leafs.Length; i++) {
            BSPFileParser.BSPTreeLeaf lf = _leafs[i];
            for (int j = lf.leafbrush; j < lf.leafbrush + lf.n_leafbrushes; j++) {
                if (_leafbrushes[j] == brushIndex) {
                    Bounds temp = new Bounds();
                    temp.SetMinMax(new Vector3(lf.mins[0], lf.mins[1], lf.mins[2]),
                                   new Vector3(lf.maxs[0], lf.maxs[1], lf.maxs[2]));
                    temp = Right2Left(temp);
                    Gizmos.DrawWireCube(temp.center, temp.size);
                }
            }
        }
        Gizmos.color = tc;
    }

	Vector3 GetOnePointOnPlane(UnityEngine.Plane plane) {
		Ray r = new Ray (Vector3.zero, plane.normal);
		//Debug.Log(plane.GetDistanceToPoint(r.GetPoint(-plane.distance)));
		return r.GetPoint (-plane.distance);
		// or
		//float distance = 0.0f;
		//plane.Raycast (r, out distance);
		//return r.GetPoint (distance);
	}

	void GizmosDrawPlane(Vector3 position, Vector3 normal, float diagonal) {

		Vector3 temp;
		if (Vector3.Cross(normal.normalized, Vector3.forward) != Vector3.zero) {
			temp = Vector3.Cross (normal, Vector3.forward).normalized * diagonal;
		} else {
			temp = Vector3.Cross(normal, Vector3.up).normalized * diagonal;
		}

		Vector3 corner0 = position + temp;
		Vector3 corner1 = position - temp;
		Quaternion q = Quaternion.AngleAxis (90.0f, normal);
		temp = q * temp;
		Vector3 corner2 = position + temp;
		Vector3 corner3 = position - temp;

		Gizmos.DrawLine(corner0, corner2);
		Gizmos.DrawLine(corner1, corner3);
		Gizmos.DrawLine(corner0, corner1);
		Gizmos.DrawLine(corner2, corner3);
		Gizmos.DrawLine(corner0, corner3);
		Gizmos.DrawLine(corner2, corner1);

		Gizmos.DrawLine (position, normal * 2 * diagonal + position);
	}

    Color InvertColor(Color color) {
        return new Color(1.0f - color.r, 1.0f - color.g, 1.0f - color.b);
    }

    Vector3 PositiveVector3(Vector3 v) { 
        v.x = v.x > 0 ? v.x : -v.x;
        v.y = v.y > 0 ? v.y : -v.y;
        v.z = v.z > 0 ? v.z : -v.z;
        return v;
    }

    UnityEngine.Plane[] SortPlanes(UnityEngine.Plane[] planes) {
        if (planes.Length != 6) { 
            throw new ArgumentException();
        }
        UnityEngine.Plane[] ps = new UnityEngine.Plane[planes.Length];
        for (int i = 0; i < ps.Length; i++) {
            if (Vector3.Cross(Vector3.right, planes[i].normal) == Vector3.zero) {
                if (Vector3.Dot(Vector3.right, planes[i].normal) > 0) { 
                    ps[0] = planes[i];
                }
                else { 
                    ps[1] = planes[i];
                }
            }
            else if (Vector3.Cross(Vector3.up, planes[i].normal) == Vector3.zero) {
                if (Vector3.Dot(Vector3.up, planes[i].normal) > 0) {
                    ps[2] = planes[i];
                }
                else {
                    ps[3] = planes[i];
                }
            }
            else if (Vector3.Cross(Vector3.forward, planes[i].normal) == Vector3.zero) {
                if (Vector3.Dot(Vector3.forward, planes[i].normal) > 0) {
                    ps[4] = planes[i];
                }
                else {
                    ps[5] = planes[i];
                }
            }
        }
        return ps;
    }

    bool ThreePlanesIntersection(UnityEngine.Plane plane1, 
                                UnityEngine.Plane plane2, 
                                UnityEngine.Plane plane3,
                                out Vector3 point) {
        float temp = Vector3.Dot(Vector3.Cross(plane1.normal, plane2.normal), plane3.normal);
        if (temp == 0) { 
            point = Vector3.zero;
            return false;
        }
        Vector3 v1 = Vector3.Cross(plane1.normal, plane2.normal) * plane3.distance;
        Vector3 v2 = Vector3.Cross(plane2.normal, plane3.normal) * plane1.distance;
        Vector3 v3 = Vector3.Cross(plane3.normal, plane1.normal) * plane2.distance;
        point = -(v1 + v2 + v3) / temp;
        return true;
    }

    Bounds GenerateBoxFromPlanes(UnityEngine.Plane[] planes) {
        if (planes.Length != 6) {
            throw new ArgumentException();
        }
        Vector3[] vertices = new Vector3[8];
        planes = SortPlanes(planes);
        
        Vector3 bounding1 = new Vector3();
        Vector3 bounding2 = new Vector3();
        ThreePlanesIntersection(planes[0], planes[2], planes[4], out bounding1);
        ThreePlanesIntersection(planes[1], planes[3], planes[5], out bounding2);
        
        Bounds bounds = new Bounds();
        bounds.max = bounding1;
        bounds.min = bounding2;
        return bounds;
    }
}