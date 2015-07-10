using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Newtonsoft.Json;

namespace BSPFileParser
{
	enum BSPConstant : int
	{
		MAGIC_STR_LEN = 4,
		DIRENTRY_LEN = 17,
		DIRENTRY_ITEM_NUM = 2,
		TEXTURE_NAME_STR_LEN = 64,
		TEXTURE_SIZE = TEXTURE_NAME_STR_LEN + 4 + 4,
		PLANE_SIZE = 4 * 3 + 4,
		BSPTREENODE_SIZE = 4 + 2 * 4 + 3 * 4 + 3 * 4,
		BSPTREELEAF_SIZE = 4 + 4 + 3 * 4 + 3 * 4 + 4 + 4 + 4 + 4,
		LEAFFACE_SIZE = 4,
		LEAFBRUSH_SIZE = 4,
		MODEL_SIZE = 3 * 4 + 3 * 4 + 4 + 4 + 4 + 4,
		BRUSH_SIZE = 4 + 4 + 4,
		BRUSHSIDE_SIZE = 4 + 4,
		VERTEX_SIZE = 3 * 4 + 2 * 2 * 4 + 3 * 4 + 4 * 1,
		MESHVERT_SIZE = 4,
		EFFECT_NAME_STR_LEN = 64,
		EFFECT_SIZE = EFFECT_NAME_STR_LEN + 4 + 4,
		FACE_SIZE = 4 + 4 + 4 + 4 + 4 + 4 + 4 + 4 + 2 * 4 + 2 * 4 + 3 * 4 + 2 * 3 * 4 + 3 * 4 + 2 * 4,
		LIGHTMAP_SIZE = 128 * 128 * 3 * 1,
		LIGHTVOL_SIZE = 3 * 1 + 3 * 1 + 2 * 1
	}
	
	class BSPHeader
	{
		internal char[] magic;
		internal int version;
		internal int[][] directories;
		private string[] _descriptions = 
		{ 
			"[Entities]Game-related object descriptions",
			"[Textures]Surface descriptions",
			"[Planes]Planes used by map geometry",
			"[Nodes]BSP tree nodes",
			"[Leafs]BSP tree-leaves",
			"[Leaffaces]Lists of face indices, one list per leaf",
			"[Leafbrushes]Lists of brush indices, one list per leaf",
			"[Models]Descriptions of rigid world geometry in map",
			"[Brushes]Convex polyhedra used to describe solid space",
			"[Brushsides]Brush surfaces",
			"[Vertexes]Vertices used to describe faces",
			"[Meshverts]Lists of offsets, one list per mesh",
			"[Effects]List of special map effects",
			"[Faces]Surface geometry",
			"[Lightmaps]Packed lightmap data",
			"[Lightvols]Local illumination data",
			"[Visdata]Cluster-cluster visibility data"
		};
		private Dictionary<string, int> _desc2index = new Dictionary<string, int>();
		public BSPHeader()
		{
			magic = new char[(int)BSPConstant.MAGIC_STR_LEN];
			directories = new int[(int)BSPConstant.DIRENTRY_LEN][];
			for (int i = 0; i < 17; i++)
			{
				directories[i] = new int[(int)BSPConstant.DIRENTRY_ITEM_NUM];
				string name = pickname(_descriptions[i]);
				_desc2index[name] = i;
			}
		}
		public string this[int index]
		{
			get
			{
				if (index >= (int)BSPConstant.DIRENTRY_LEN)
				{
					throw new IndexOutOfRangeException();
				}
				//return pickname(_descriptions[index]);
				return _descriptions[index];
			}
		}
		public int this[string name]
		{
			get
			{
				if (!_desc2index.ContainsKey(name))
				{
					throw new KeyNotFoundException();
				}
				return _desc2index[name];
			}
		}
		private string pickname(string raw)
		{
			return raw.Substring(1, raw.IndexOf(']') - 1);
		}
	}
	
	struct Texture
	{
		internal char[] name; // [64] texture file name
		internal int flags;
		internal int contents;
		public Texture(char[] name, int flags, int contents)
		{
			this.name = name;
			this.flags = flags;
			this.contents = contents;
		}
	}
	struct Plane
	{
		// Used by map geometry
		internal float[] normal; // [3]
		internal float dist;
		public Plane(float[] normal, float dist)
		{
			this.normal = normal;
			this.dist = dist;
		}
	}
	struct BSPTreeNode
	{
		internal int plane; // Plane Index
		internal int[] children; // Children Indices(left and right children), Negative numbers are leaf indices: -(leaf+1)
		internal int[] mins; // Integer bounding box min coord
		internal int[] maxs; // Integer bounding box max coord
		public BSPTreeNode(int plane, int[] children, int[] mins, int[] maxs)
		{
			this.plane = plane;
			this.children = children;
			this.mins = mins;
			this.maxs = maxs;
		}
	}
	struct BSPTreeLeaf
	{
		// Each leaf is a convex region that contains, 
		// among other things, a cluster index (for determining 
		// the other leafs potentially visible from within the leaf),
		// a list of faces (for rendering), and a list of brushes (for collision detection).
		internal int cluster; // visible data cluster index, If cluster is negative, the leaf is outside the map or otherwise invalid.
		internal int area; // areaportal area
		internal int[] mins; // Integer bounding box min coord
		internal int[] maxs; // Integer bounding box max coord
		internal int leafface; // First leafface for leaf
		internal int n_leaffaces; // Number of leaffaces for leaf
		internal int leafbrush; // First leafbrush for leaf
		internal int n_leafbrushes; // Number of leafbrushes for leaf
		public BSPTreeLeaf(int cluster, int area, int[] mins, int[] maxs,
		                   int leafface, int nleaffaces, int leafbrush, int nleafbrushes)
		{
			this.cluster = cluster;
			this.area = area;
			this.mins = mins;
			this.maxs = maxs;
			this.leafface = leafface;
			this.n_leaffaces = nleaffaces;
			this.leafbrush = leafbrush;
			this.n_leafbrushes = nleafbrushes;
		}
	}
	struct Model
	{
		// The *first* model correponds to the base portion of the map while the remaining models 
		// correspond to *movable* portions of the map, such as the map's doors, platforms, and buttons.
		// Each model has a list of faces and list of brushes; these are especially important for the 
		// movable parts of the map, which (unlike the base portion of the map) do not have BSP trees associated with them.
		internal float[] mins; // [3]
		internal float[] maxs; // [3]
		internal int face;
		internal int n_faces;
		internal int brush;
		internal int n_brushes;
		public Model(float[] mins, float[] maxs, int face, int nfaces, int brush, int nbrushes)
		{
			this.mins = mins;
			this.maxs = maxs;
			this.face = face;
			this.n_faces = nfaces;
			this.brush = brush;
			this.n_brushes = nbrushes;
		}
	}
	struct Brush
	{
		// brushes, which are in turn used for *collision detection*. 
		// Each brush describes a convex volume as defined by its surrounding surfaces
		internal int brushside; // First brushside for brush
		internal int n_brushsides;
		internal int texture; // Texture index
		public Brush(int brushside, int nbrushsides, int texture)
		{
			this.brushside = brushside;
			this.n_brushsides = nbrushsides;
			this.texture = texture;
		}
	}
	struct Brushside
	{
		// descriptions of brush bounding surfaces
		internal int plane; // Plane index;
		internal int texture; // Texture index
		public Brushside(int plane, int texture)
		{
			this.plane = plane;
			this.texture = texture;
		}
	}
	struct Vertex
	{
		// Used to describe faces
		internal float[] position; // [3]
		internal float[][] uv; // [2][2]
		internal float[] normal; // [3]
		internal Byte[] color; // [4]
		public Vertex(float[] position, float[][] uv, float[] normal, Byte[] color)
		{
			this.position = position;
			this.uv = uv;
			this.normal = normal;
			this.color = color;
		}
	}
	struct Effect
	{
		// references to volumetric shaders (typically fog) which affect the 
		// rendering of a particular group of faces
		internal char[] name; // [64] Effect shader
		internal int brush; // Brush that generated this effect
		internal int unknown; // Always 5, except in q3dm8, which has one effect with -1
		public Effect(char[] name, int brush, int unknown)
		{
			this.name = name;
			this.brush = brush;
			this.unknown = unknown;
		}
	}
	struct Face
	{
		// Surface geometry
		internal int texture;
		internal int effect;
		internal int type; // Face type. 1=polygon, 2=patch, 3=mesh, 4=billboard
		internal string[] type2name;
		internal string typename;
		internal int vertex; // First vertex's Index
		internal int n_vertexes; // Number of vertices
		internal int meshvert; // First Meshvert's Index
		internal int n_meshverts; // Number of meshverts
		internal int lm_index; // Lightmap index
		internal int[] lm_start; // [2] 	Corner of this face's lightmap image in lightmap
		internal int[] lm_size; // [2] Size of this face's lightmap image in lightmap.
		internal float[] lm_origin; // [3] World space origin of lightmap
		internal float[][] lm_vecs; // [2][3] 	World space lightmap s and t unit vectors.
		internal float[] normal; // Surface Normal
		internal int[] size; // [2] Patch dimensions.
		/*
         * For type 1 faces (polygons), vertex and n_vertexes describe a set of vertices that form a polygon. 
         * The set always contains a loop of vertices, and sometimes also includes an additional vertex near the center of the polygon.
         * For these faces, meshvert and n_meshverts describe a valid polygon triangulation. Every three meshverts describe a triangle. 
         * Each meshvert is an offset from the first vertex of the face, given by vertex.
         * 
         * For type 2 faces (patches), vertex and n_vertexes describe a 2D rectangular grid of control vertices with dimensions given by size.
         * Within this rectangular grid, regions of 3×3 vertices represent biquadratic Bezier patches. 
         * Adjacent patches share a line of three vertices. There are a total of (size[0] - 1) / 2 by (size[1] - 1) / 2 patches. 
         * Patches in the grid start at (i, j) given by:
            i = 2n, n in [ 0 .. (size[0] - 1) / 2 ), and
            j = 2m, m in [ 0 .. (size[1] - 1) / 2 ).
         *
         * For type 3 faces (meshes), meshvert and n_meshverts are used to describe the independent triangles that form the mesh. 
         * As with type 1 faces, every three meshverts describe a triangle, and each meshvert is an offset from the first vertex of the face, 
         * given by vertex.

         * For type 4 faces (billboards), vertex describes the single vertex that determines the location of the billboard. 
         * Billboards are used for effects such as flares. Exactly how each billboard vertex is to be interpreted has not been investigated.
         *
         * The lm_ variables are primarily used to deal with lightmap data. A face that has a lightmap has a non-negative lm_index. 
         * For such a face, lm_index is the index of the image in the lightmaps lump that contains the lighting data for the face. 
         * The data in the lightmap image can be located using the rectangle specified by lm_start and lm_size.
         *
         * For type 1 faces (polygons) only, lm_origin and lm_vecs can be used to compute the world-space positions corresponding to 
         * lightmap samples. These positions can in turn be used to compute dynamic lighting across the face.
         *
         * None of the lm_ variables are used to compute texture coordinates for indexing into lightmaps. 
         * In fact, lightmap coordinates need not be computed. Instead, lightmap coordinates are simply stored with the vertices used to 
         * describe each face.
         * 
         */
		public Face(int texture, int effect, int type, int vertex,
		            int n_vertexes, int meshvert, int n_meshverts, int lm_index,
		            int[] lm_start, int[] lm_size, float[] lm_origin,
		            float[][] lm_vecs, float[] normal, int[] size)
		{
			this.texture = texture;
			this.effect = effect;
			this.type = type;
			this.vertex = vertex;
			this.n_vertexes = n_vertexes;
			this.meshvert = meshvert;
			this.n_meshverts = n_meshverts;
			this.lm_index = lm_index;
			this.lm_start = lm_start;
			this.lm_size = lm_size;
			this.lm_origin = lm_origin;
			this.lm_vecs = lm_vecs;
			this.normal = normal;
			this.size = size;
			this.type2name = new string[] { "", "polygon", "patch", "mesh", "billboard" };
			this.typename = "";
		}
	}
	struct Lightmap
	{
		internal Byte[][][] map; // Lightmap color data(RGB)
		public Lightmap(Byte[][][] map)
		{
			this.map = map;
		}
	}
	struct Lightvol
	{
		// a uniform grid of lighting information used to illuminate non-map objects
		/*
         Lightvols make up a 3D grid whose dimensions are:
            nx = floor(models[0].maxs[0] / 64) - ceil(models[0].mins[0] / 64) + 1
            ny = floor(models[0].maxs[1] / 64) - ceil(models[0].mins[1] / 64) + 1
            nz = floor(models[0].maxs[2] / 128) - ceil(models[0].mins[2] / 128) + 1
         */
		internal Byte[] ambient; // [3] ambient color rgb
		internal Byte[] directional; // [3] directional color rgb
		internal Byte[] dir; // [2] direction to light. 0=phi, 1=theta
		public Lightvol(Byte[] ambient, Byte[] directional, Byte[] dir)
		{
			this.ambient = ambient;
			this.directional = directional;
			this.dir = dir;
		}
	}
	struct Visibilitydata
	{
		internal int n_vecs; // Number of vectors
		internal int sz_vecs; // Size of each vectors, in bytes
		internal Byte[] vecs; // Visibility data. One bit per cluster per vector
		/*
            Cluster x is visible from cluster y if the (1 << y % 8) bit of vecs[x * sz_vecs + y / 8] is set.
            Note that clusters are associated with leaves.
         */
		public Visibilitydata(int n_vecs, int sz_vecs, Byte[] vecs)
		{
			this.n_vecs = n_vecs;
			this.sz_vecs = sz_vecs;
			this.vecs = vecs;
		}
	}

    class BSPEntity
    { 
        public Hashtable Properties = new Hashtable();
        public string ClassName
        { 
            get
            { 
                return (string)Properties["classname"];
            }
        }
    }
	class BSPParser
	{
		static String FILENAME = "ssjj.bsp";
		private int length = 0;
		private int current = -1;
		private BinaryReader reader = null;
		private BSPHeader header = null;
		private string _entities = null;
        private ArrayList BSPEntities = new ArrayList();
		private Texture[] _textures = null;
		private Plane[] _planes = null;
		private BSPTreeNode[] _nodes = null;
		private BSPTreeLeaf[] _leafs = null;
		private int[] _leaffaces = null; // list of face indices
		private int[] _leafbrushes = null; // list of brush indices
		private Model[] _models = null;
		private Brush[] _brushes = null;
		private Brushside[] _brushsides = null;
		private Vertex[] _vertexes = null;
		private int[] _meshverts = null; // lists of vertex offsets, used to describe generalized triangle meshes
		private Effect[] _effects = null;
		private Face[] _faces = null;
		private Lightmap[] _lightmaps = null; // light map textures used make surface lighting look more realistic, Lightmap color data RGB
		private Lightvol[] _lightvols = null;
		private Visibilitydata? _visdata = null;
		private bool r2l = false; // Right Hand Coordinate System Convert into Left Hand Coordinate System Flag
		
		public string Entities
		{
			get
			{
                //if (_entities == null)
                //{
                //    reader.BaseStream.Seek(header.directories[header["Entities"]][0], SeekOrigin.Begin);
                //    _entities = new String(reader.ReadChars(header.directories[header["Entities"]][1]));
                //    //Console.WriteLine(header.directories[header["Entities"]][0]);
                //    //Console.WriteLine(header.directories[header["Entities"]][1]);
                //}
				return _entities;
			}
		}

        public float[] GetPlayerStartPosition()
        {
            float[] pos = new float[3];
            foreach (BSPEntity entity in BSPEntities)
            {
                if (entity.ClassName == "info_player_start" ||
                    entity.ClassName == "info_player_start_T" ||
                    entity.ClassName == "info_player_start_CT" ||
                    entity.ClassName == "info_player_start_I")
                {
                    string origin = (string)entity.Properties["origin"];
                    string[] pos_s = origin.Split(' ');
                    pos[0] = Convert.ToSingle(pos_s[0].Trim());
                    pos[1] = Convert.ToSingle(pos_s[1].Trim());
                    pos[2] = Convert.ToSingle(pos_s[2].Trim());
                    break;
                }
            }
            return pos;
        }

		public Texture[] Textures
		{
			get
			{
				if(_textures == null)
				{
					reader.BaseStream.Seek(header.directories[header["Textures"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Textures"]][1] / (int)BSPConstant.TEXTURE_SIZE;
					_textures = new Texture[len];
					for(int i=0; i<len; i++)
					{
						char[] name = reader.ReadChars((int)BSPConstant.TEXTURE_NAME_STR_LEN);
						int flags = reader.ReadInt32();
						int contents = reader.ReadInt32();
						_textures[i] = new Texture(name, flags, contents);
						//_textures[i].name = reader.ReadChars((int)BSPConstant.TEXTURE_NAME_STR_LEN);
						//_textures[i].flags = reader.ReadInt32();
						//_textures[i].contents = reader.ReadInt32();
					}
				}
				return _textures;
			}
		}
		public Plane[] Planes
		{
			get
			{
				if(_planes == null)
				{
					reader.BaseStream.Seek(header.directories[header["Planes"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Planes"]][1] / (int)BSPConstant.PLANE_SIZE;
					_planes = new Plane[len];
					for(int i=0; i<len; i++)
					{
						float[] normal = new float[3] { 
							reader.ReadSingle(),
							reader.ReadSingle(),
							reader.ReadSingle()
						};
						float dist = reader.ReadSingle();
						if(r2l)
						{
							right2left(normal);
						}
						_planes[i] = new Plane(normal, dist);
						//_planes[i].normal = new float[3];
						//_planes[i].normal[0] = reader.ReadSingle();
						//_planes[i].normal[1] = reader.ReadSingle();
						//_planes[i].normal[2] = reader.ReadSingle();
						//_planes[i].dist = reader.ReadSingle();
					}
				}
				return _planes;
			}
		}
		public BSPTreeNode[] Nodes
		{
			get
			{
				if(_nodes == null)
				{
					reader.BaseStream.Seek(header.directories[header["Nodes"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Nodes"]][1] / (int)BSPConstant.BSPTREENODE_SIZE;
					_nodes = new BSPTreeNode[len];
					for(int i=0; i<len; i++)
					{
						int plane = reader.ReadInt32();
						int[] children = new int[2]{
							reader.ReadInt32(),
							reader.ReadInt32()
						};
						int[] mins = new int[3] { 
							reader.ReadInt32(),
							reader.ReadInt32(),
							reader.ReadInt32()
						}; 
						int[] maxs = new int[3] { 
							reader.ReadInt32(),
							reader.ReadInt32(),
							reader.ReadInt32()
						};
						if(r2l)
						{
							right2left(mins);
							right2left(maxs);
						}
						_nodes[i] = new BSPTreeNode(plane, children, mins, maxs);
						//_nodes[i].plane = reader.ReadInt32();
						//_nodes[i].children = new int[2];
						//_nodes[i].children[0] = reader.ReadInt32();
						//_nodes[i].children[1] = reader.ReadInt32();
						//_nodes[i].mins = new int[3];
						//_nodes[i].mins[0] = reader.ReadInt32();
						//_nodes[i].mins[1] = reader.ReadInt32();
						//_nodes[i].mins[2] = reader.ReadInt32();
						//_nodes[i].maxs = new int[3];
						//_nodes[i].maxs[0] = reader.ReadInt32();
						//_nodes[i].maxs[1] = reader.ReadInt32();
						//_nodes[i].maxs[2] = reader.ReadInt32();
					}
				}
				return _nodes;
			}
		}
		public BSPTreeLeaf[] Leafs
		{
			get
			{
				if(_leafs == null)
				{
					reader.BaseStream.Seek(header.directories[header["Leafs"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Leafs"]][1] / (int)BSPConstant.BSPTREELEAF_SIZE;
					_leafs = new BSPTreeLeaf[len];
					for(int i=0; i<len; i++)
					{
						int cluster = reader.ReadInt32();
						int area = reader.ReadInt32();
						int[] mins = new int[3] { 
							reader.ReadInt32(),
							reader.ReadInt32(),
							reader.ReadInt32()
						};
						int[] maxs = new int[3] { 
							reader.ReadInt32(),
							reader.ReadInt32(),
							reader.ReadInt32()
						};
						int leaface = reader.ReadInt32();
						int nleaffaces = reader.ReadInt32();
						int leafbrush = reader.ReadInt32();
						int nleafbrushes = reader.ReadInt32();
						if(r2l)
						{
							right2left(mins);
							right2left(maxs);
						}
						_leafs[i] = new BSPTreeLeaf(cluster, area, mins, maxs, leaface, 
						                            nleaffaces, leafbrush, nleafbrushes);
						//_leafs[i].cluster = reader.ReadInt32();
						//_leafs[i].area = reader.ReadInt32();
						//_leafs[i].mins = new int[3];
						//_leafs[i].mins[0] = reader.ReadInt32();
						//_leafs[i].mins[1] = reader.ReadInt32();
						//_leafs[i].mins[2] = reader.ReadInt32();
						//_leafs[i].maxs = new int[3];
						//_leafs[i].maxs[0] = reader.ReadInt32();
						//_leafs[i].maxs[1] = reader.ReadInt32();
						//_leafs[i].maxs[2] = reader.ReadInt32();
						//_leafs[i].leafface = reader.ReadInt32();
						//_leafs[i].n_leaffaces = reader.ReadInt32();
						//_leafs[i].leafbrush = reader.ReadInt32();
						//_leafs[i].n_leafbrushes = reader.ReadInt32();
					}
				}
				return _leafs;
			}
		}
		public int[] Leaffaces
		{
			get
			{
				if(_leaffaces == null)
				{
					reader.BaseStream.Seek(header.directories[header["Leaffaces"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Leaffaces"]][1] / (int) BSPConstant.LEAFFACE_SIZE;
					_leaffaces = new int[len];
					for(int i=0; i<len; i++)
					{
						_leaffaces[i] = reader.ReadInt32();
					}
				}
				return _leaffaces;
			}
		}
		public int[] Leafbrushes
		{
			get
			{
				if(_leafbrushes == null)
				{
					reader.BaseStream.Seek(header.directories[header["Leafbrushes"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Leafbrushes"]][1] / (int)BSPConstant.LEAFBRUSH_SIZE;
					_leafbrushes = new int[len];
					for (int i = 0; i < len; i++)
					{
						_leafbrushes[i] = reader.ReadInt32();
					}
				}
				return _leafbrushes;
			}
		}
		public Model[] Models
		{
			get
			{
				if(_models == null)
				{
					reader.BaseStream.Seek(header.directories[header["Models"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Models"]][1] / (int)BSPConstant.MODEL_SIZE;
					//Console.WriteLine(header.directories[header["Models"]][1]);
					_models = new Model[len];
					for(int i=0; i<len; i++)
					{
						float[] mins = new float[3]
						{
							reader.ReadSingle(),
							reader.ReadSingle(),
							reader.ReadSingle()
						};
						float[] maxs = new float[3]
						{
							reader.ReadSingle(),
							reader.ReadSingle(),
							reader.ReadSingle()
						};
						int face = reader.ReadInt32();
						int nfaces = reader.ReadInt32();
						int brush = reader.ReadInt32();
						int nbrushes = reader.ReadInt32();
						if(r2l)
						{
							right2left(mins);
							right2left(maxs);
						}
						_models[i] = new Model(mins, maxs, face, nfaces, brush, nbrushes);
					}
				}
				return _models;
			}
		}
		public Brush[] Brushes
		{
			get
			{
				if(_brushes == null)
				{
					reader.BaseStream.Seek(header.directories[header["Brushes"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Brushes"]][1] / (int)BSPConstant.BRUSH_SIZE;
					_brushes = new Brush[len];
					for(int i=0; i<len; i++)
					{
						int brushside = reader.ReadInt32();
						int nbrushsides = reader.ReadInt32();
						int textture = reader.ReadInt32();
						_brushes[i] = new Brush(brushside, nbrushsides, textture);
					}
				}
				return _brushes;
			}
		}
		public Brushside[] Brushsides
		{
			get
			{
				if(_brushsides == null)
				{
					reader.BaseStream.Seek(header.directories[header["Brushsides"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Brushsides"]][1] / (int)BSPConstant.BRUSHSIDE_SIZE;
					_brushsides = new Brushside[len];
					for(int i=0; i<len; i++)
					{
						int plane = reader.ReadInt32();
						int texture = reader.ReadInt32();
						_brushsides[i] = new Brushside(plane, texture);
					}
				}
				return _brushsides;
			}
		}
		public Vertex[] Vertexes
		{
			get
			{
				if(_vertexes == null)
				{
					reader.BaseStream.Seek(header.directories[header["Vertexes"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Vertexes"]][1] / (int)BSPConstant.VERTEX_SIZE;
					_vertexes = new Vertex[len];
					for(int i=0; i<len; i++)
					{
						float[] position = new float[3]
						{
							reader.ReadSingle(),
							reader.ReadSingle(),
							reader.ReadSingle()
						};
						float[][] uv = new float[2][]
						{
							new float[2],
							new float[2]
						};
						uv[0][0] = reader.ReadSingle();
						uv[0][1] = reader.ReadSingle();
						uv[1][0] = reader.ReadSingle();
						uv[1][1] = reader.ReadSingle();
						float[] normal = new float[3] 
						{
							reader.ReadSingle(),
							reader.ReadSingle(),
							reader.ReadSingle()
						};
						Byte[] color = new Byte[4] 
						{ 
							reader.ReadByte(),
							reader.ReadByte(),
							reader.ReadByte(),
							reader.ReadByte()
						};
						if(r2l)
						{
							right2left(normal);
							right2left(position);
						}
						_vertexes[i] = new Vertex(position, uv, normal, color);
					}
				}
				return _vertexes;
			}
		}
		public int[] Meshverts
		{
			get
			{
				if(_meshverts == null)
				{
					reader.BaseStream.Seek(header.directories[header["Meshverts"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Meshverts"]][1] / (int)BSPConstant.MESHVERT_SIZE;
					_meshverts = new int[len];
					for(int i=0; i<len; i++)
					{
						_meshverts[i] = reader.ReadInt32();
					}
				}
				return _meshverts;
			}
		}
		public Effect[] Effects
		{
			get
			{
				if(_effects == null)
				{
					//Console.WriteLine("Effect Length: " + header.directories[header["Effects"]][1]);
					reader.BaseStream.Seek(header.directories[header["Effects"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Effects"]][1] / (int)BSPConstant.EFFECT_SIZE;
					_effects = new Effect[len];
					for(int i=0; i<len; i++)
					{
						char[] name = reader.ReadChars((int)BSPConstant.EFFECT_NAME_STR_LEN);
						int brush = reader.ReadInt32();
						int unknow = reader.ReadInt32();
						_effects[i] = new Effect(name, brush, unknow);
					}
				}
				return _effects;
			}
		}
		public Face[] Faces
		{
			get
			{
				if(_faces == null)
				{
					reader.BaseStream.Seek(header.directories[header["Faces"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Faces"]][1] / (int)BSPConstant.FACE_SIZE;
					_faces = new Face[len];
					for(int i=0; i<len; i++)
					{
						int texture = reader.ReadInt32();
						int effect = reader.ReadInt32();
						int type = reader.ReadInt32();
						int vertex = reader.ReadInt32();
						int nvertexes = reader.ReadInt32();
						int meshvert = reader.ReadInt32();
						int nmeshverts = reader.ReadInt32();
						int lm_index = reader.ReadInt32();
						int[] lm_start = new int[2] { reader.ReadInt32(), reader.ReadInt32() };
						int[] lm_size = new int[2] { reader.ReadInt32(), reader.ReadInt32() };
						float[] lm_origin = new float[3] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
						float[][] lm_vecs = new float[2][] 
						{ 
							new float[3] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}, 
							new float[3] {reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()}
						};
						float[] normal = new float[3] { reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() };
						int[] size = new int[2] { reader.ReadInt32(), reader.ReadInt32() };
						if(r2l)
						{
							//right2left(lm_origin);
							//right2left(lm_vecs[0]);
							//right2left(lm_vecs[1]);
							//right2left(normal);
						}
						_faces[i] = new Face(texture, effect, type, vertex, nvertexes, meshvert, nmeshverts,
						                     lm_index, lm_start, lm_size, lm_origin, lm_vecs, normal, size);
						_faces[i].typename = _faces[i].type2name[_faces[i].type];
					}
				}
				return _faces;
			}
		}
		public Lightmap[] Lightmaps
		{
			get
			{
				if(_lightmaps == null)
				{
					reader.BaseStream.Seek(header.directories[header["Lightmaps"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Lightmaps"]][1] / (int)BSPConstant.LIGHTMAP_SIZE;
					_lightmaps = new Lightmap[len];
					for(int m=0; m<len; m++)
					{
						Byte[][][] temp = new Byte[128][][];
						for(int i=0; i<128; i++)
						{
							if(temp[i] == null)
							{temp[i] = new Byte[128][];}
							for(int j=0; j<128; j++)
							{
								temp[i][j] = new Byte[3] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
							}
						}
						_lightmaps[m] = new Lightmap(temp);
					}
				}
				return _lightmaps;
			}
		}
		public Lightvol[] Lightvols
		{
			get
			{
				if(_lightvols == null)
				{
					//Console.WriteLine("Lightvols Length: " + header.directories[header["Lightvols"]][1]);
					reader.BaseStream.Seek(header.directories[header["Lightvols"]][0], SeekOrigin.Begin);
					int len = header.directories[header["Lightvols"]][1] / (int)BSPConstant.LIGHTVOL_SIZE;
					_lightvols = new Lightvol[len];
					for(int i=0; i<len; i++)
					{
						Byte[] ambient = new Byte[3] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
						Byte[] directional = new Byte[3] { reader.ReadByte(), reader.ReadByte(), reader.ReadByte() };
						Byte[] dir = new Byte[2] { reader.ReadByte(), reader.ReadByte() };
						_lightvols[i] = new Lightvol(ambient, directional, dir);
					}
				}
				return _lightvols;
			}
		}
		public Visibilitydata Visdata
		{
			get
			{
				if(_visdata == null)
				{
					reader.BaseStream.Seek(header.directories[header["Visdata"]][0], SeekOrigin.Begin);
					int n_vecs = reader.ReadInt32();
					int sz_vecs = reader.ReadInt32();
					int len = n_vecs * sz_vecs;
					Byte[] vecs = new Byte[len];
					for(int i=0; i<len; i++)
					{
						vecs[i] = reader.ReadByte();
					}
					_visdata = new Visibilitydata(n_vecs, sz_vecs, vecs);
				}
				return (Visibilitydata)_visdata;
			}
		}
		private int findLeaf(float[] position)
		{
			if (position == null || position.Length != 3)
			{
				throw new ArgumentException();
			}
			int index = 0;
			BSPTreeNode[] nodes = Nodes;
			Plane[] planes = Planes;
			while(index >= 0)
			{
				BSPTreeNode node = nodes[index];
				Plane plane = planes[node.plane];
				float distance = vector3dot(position, plane.normal) - plane.dist;
				if(distance >= 0)
				{
					index = node.children[0];
				}
				else
				{
					index = node.children[1];
				}
			}
			return -index - 1;
		}
		private ArrayList findVisibleLeafs(float[] camera)
		{
			if(camera == null || camera.Length != 3)
			{
				throw new ArgumentException();
			}
			ArrayList visibleLeafs = new ArrayList();
			BSPTreeLeaf[] leafs = Leafs;
			int leafIndex = findLeaf(camera);
			int cluster = leafs[leafIndex].cluster;
			for(int i=0; i<leafs.Length; i++)
			{
				BSPTreeLeaf lf = leafs[i];
				// Occlusion Culling
				if(isClusterVisible(cluster, lf.cluster))
				{
					// Frustum Culling, If you get camera frustum, you can do that before Add(i)
					// Frustum Culling, something like this: Camera.IsVisiable(leaf.min, leaf.max)
					visibleLeafs.Add(i);
				}
			}
			return visibleLeafs;
		}
		private ArrayList findVisibleFaces(float[] camera)
		{
			if (camera == null || camera.Length != 3)
			{
				throw new ArgumentException();
			}
			BSPTreeLeaf[] leafs = Leafs;
			Face[] faces = Faces;
			int[] leaffaces = Leaffaces;
			ArrayList visibleLeafs = findVisibleLeafs(camera);
			bool[] alreadyVisibleFaces = new bool[faces.Length];
			ArrayList visibleFaces = new ArrayList();
			foreach(int i in visibleLeafs)
			{
				BSPTreeLeaf lf = leafs[i];
				for(int j=lf.leafface; j<lf.leafface+lf.n_leaffaces; j++)
				{
					int faceIndex = leaffaces[j];
					if(!alreadyVisibleFaces[faceIndex])
					{
						alreadyVisibleFaces[faceIndex] = true;
						visibleFaces.Add(faceIndex);
					}
				}
			}
			return visibleFaces;
		}
		private bool isClusterVisible(int cluster, int testCluster)
		{
			Visibilitydata visdata = Visdata;
			if(visdata.vecs == null)
			{
				return true;
			}
			int index = cluster * visdata.sz_vecs + (testCluster >> 3);
			Byte temp = visdata.vecs[index];
			return (temp & (1 << (testCluster & 7))) != 0;
		}
		// Vector3
		private float vector3dot(float[] v1, float[] v2)
		{
			if(v1 == null || v1.Length != 3)
			{
				throw new ArgumentException();
			}
			if (v2 == null || v2.Length != 3)
			{
				throw new ArgumentException();
			}
			return v1[0] * v2[0] + v1[1] * v2[1] + v1[2] * v2[2];
		}
		private void right2left(float[] v)
		{
			// right coordinate system convert into left coordinate system
			if(v == null || v.Length != 3)
			{
				throw new ArgumentException();
			}
			//float temp = v[1]; // y
			//v[1] = v[2];  // y=z
			//v[2] = -temp; // z=-y
			float temp = v [1];
			v [1] = v [2];
			v [2] = -temp;
			v [0] = -v [0];
		}
		private void right2left(int[] v)
		{
			// right coordinate system convert into left coordinate system
			if(v == null || v.Length != 3)
			{
				throw new ArgumentException();
			}
			//int temp = v[1]; // y
			//v[1] = v[2];  // y=z
			//v[2] = -temp; // z=-y
			int temp = v [1];
			v [1] = v [2];
			v [2] = -temp;
			v [0] = -v [0];
		}
		private bool isInvaild(String filename)
		{
			return !File.Exists(filename);
		}
		private void parserHeader(BinaryReader reader)
		{
			header.magic = reader.ReadChars((int)BSPConstant.MAGIC_STR_LEN);
			header.version = reader.ReadInt32();
			for (int i = 0; i < (int)BSPConstant.DIRENTRY_LEN; i++)
			{
				header.directories[i][0] = reader.ReadInt32();
				header.directories[i][1] = reader.ReadInt32();
			}
		}
        private void parserEntities(BinaryReader reader)
        {
            if (_entities == null)
            {
                reader.BaseStream.Seek(header.directories[header["Entities"]][0], SeekOrigin.Begin);
                _entities = new String(reader.ReadChars(header.directories[header["Entities"]][1]));
                string[] ents = findEntities(_entities);
                foreach (string ent in ents)
                {
                    BSPEntity entity = new BSPEntity();
                    string[] props = readEntityProperties(ent);
                    for (int i = 0; i < props.Length / 2; i++)
                        entity.Properties[props[i * 2]] = props[i * 2 + 1];
                    BSPEntities.Add(entity);
                }
            }
        }

        private static string[] findEntities(string entity)
        {
            ArrayList results = new ArrayList();

            int begin = -1;
            int end = -1;

            for (int i = 0; i < entity.Length; i++)
            {
                if ((entity.Substring(i, 1) == "{") && (begin == -1))
                    begin = i;
                if ((entity.Substring(i, 1) == "}") && (end == -1))
                    end = i;

                if ((begin != -1) && (end != -1))
                {
                    results.Add(entity.Substring(begin, end - begin + 1));
                    begin = -1;
                    end = -1;
                }
            }

            return (string[])results.ToArray(typeof(string));
        }

        /// <summary>
        /// 读取实体属性
        /// </summary>
        /// <param name="entity">实体字符串</param>
        /// <returns>属性列表</returns>
        private static string[] readEntityProperties(string entity)
        {
            ArrayList results = new ArrayList();

            int begin = -1;
            int end = -1;

            for (int i = 0; i < entity.Length; i++)
            {
                if ((entity.Substring(i, 1) == "\"") && (begin == -1))
                {
                    begin = i;
                    continue;
                }
                if ((entity.Substring(i, 1) == "\"") && (end == -1))
                {
                    end = i;

                    if ((begin != -1) && (end != -1))
                    {
                        results.Add(entity.Substring(begin + 1, end - begin - 1));
                        begin = -1;
                        end = -1;
                    }
                    continue;
                }
            }

            return (string[])results.ToArray(typeof(string));
        }
		private void parser(BinaryReader reader)
		{
			parserHeader(reader);
            parserEntities(reader);
		}
		public BSPParser(String filename = null)
		{
			if (filename != null)
			{
				FILENAME = filename;
			}
			if (isInvaild(FILENAME))
			{
				throw new FileNotFoundException();
			}
			header = new BSPHeader();
			reader = new BinaryReader(File.Open(FILENAME, FileMode.Open));
			length = (int)reader.BaseStream.Length;
			current = 0;
			parser(reader);
		}
		~BSPParser()
		{
			reader.Close();
		}
		public void display()
		{
			// Header/Directory
			Console.WriteLine("Header/Directory");
			Console.Write(" Magic: ");
			Console.WriteLine(header.magic);
			Console.WriteLine(" Version: " + header.version);
			Console.WriteLine(" [offset, length]");
			for (int i = 0; i < (int)BSPConstant.DIRENTRY_LEN; i++)
			{
				Console.Write(header[i]);
				Console.WriteLine(" (" + header.directories[i][0] + "," + header.directories[i][1] + ")");
			}
			
			// Entitis(game-related objects in the map) descriptions
			Console.WriteLine("Entities");
			Console.WriteLine(Entities);
			
			// Textures
			Console.WriteLine("Textures");
			Texture[] textures = Textures;
			for(int i=0; i<textures.Length; i++)
			{
				Console.Write(" Name: ");
				Console.WriteLine(textures[i].name);
				Console.WriteLine(" Flags: " + textures[i].flags);
				Console.WriteLine(" Contents: " + textures[i].contents);
			}
			
			// Planes
			Console.WriteLine("Planes");
			Console.WriteLine("     Notice: planes are paired. The pair of planes with indices i and i ^ 1 are coincident planes with opposing normals");
			Plane[] planes = Planes;
			for(int i=0; i<planes.Length; i++)
			{
				Console.WriteLine(" Normal: (" + planes[i].normal[0] + ", " + planes[i].normal[1] + ", " + planes[i].normal[2] +")");
				Console.WriteLine(" Dist: " + planes[i].dist);
			}
			
			// BSP Tree Nodes
			Console.WriteLine("BSP Tree Nodes");
			Console.WriteLine("     Notice: Children Node,Negative numbers are leaf indices: -(leaf+1)");
			BSPTreeNode[] nodes = Nodes;
			for(int i=0; i<nodes.Length; i++)
			{
				Console.WriteLine(" Plane Index: " + nodes[i].plane);
				Console.WriteLine(" Children Node: (" + nodes[i].children[0] + ", " + nodes[i].children[1] + ")");
				Console.WriteLine(" Box Min Coord: (" + nodes[i].mins[0] + ", " + nodes[i].mins[1] + ", " + nodes[i].mins[2] + ")");
				Console.WriteLine(" Box Max Coord: (" + nodes[i].maxs[0] + ", " + nodes[i].maxs[1] + ", " + nodes[i].maxs[2] + ")");
			}
			
			// BSP Tree Leafs
			Console.WriteLine("BSP Tree Leafs");
			Console.WriteLine("     Notice: If cluster is negative, the leaf is outside the map or otherwise invalid.");
			BSPTreeLeaf[] leafs = Leafs;
			for(int i=0; i<leafs.Length; i++)
			{
				Console.WriteLine(" Cluster Index: " + leafs[i].cluster);
				Console.WriteLine(" Area: " + leafs[i].area);
				Console.WriteLine(" Box Min Coord: (" + leafs[i].mins[0] + ", " + leafs[i].mins[1] + ", " + leafs[i].mins[2] + ")");
				Console.WriteLine(" Box Max Coord: (" + leafs[i].maxs[0] + ", " + leafs[i].maxs[1] + ", " + leafs[i].maxs[2] + ")");
				Console.WriteLine(" First Leaf Face: " + leafs[i].leafface);
				Console.WriteLine(" Number of Leaffaces: " + leafs[i].n_leaffaces);
				Console.WriteLine(" Firt Leaf Brush: " + leafs[i].leafbrush);
				Console.WriteLine(" Number of Leafbrushes: " + leafs[i].n_leafbrushes);
			}
			
			// Leaffaces
			Console.WriteLine("Leaffaces");
			int[] leaffaces = Leaffaces;
			for(int i=0; i<leaffaces.Length; i++)
			{
				Console.WriteLine(" Leaf Face Index: " + leaffaces[i]);
			}
			
			// Leafbrushes
			Console.WriteLine("Leafbrushes");
			int[] leafbrushes = Leafbrushes;
			for(int i=0; i<leafbrushes.Length; i++)
			{
				Console.WriteLine(" Leaf Brush Index: " + leafbrushes[i]);
			}
			
			// Models
			Console.WriteLine("Rigid Models In the world geometry");
			Model[] models = Models;
			for(int i=0; i<models.Length; i++)
			{
				Console.WriteLine(" Box Min Coord: (" + models[i].mins[0] + ", " + models[i].mins[1] + ", " + models[i].mins[2] + ")");
				Console.WriteLine(" Box Max Coord: (" + models[i].maxs[0] + ", " + models[i].maxs[1] + ", " + models[i].maxs[2] + ")");
				Console.WriteLine(" First Face Index: " + models[i].face);
				Console.WriteLine(" Number of Faces: " + models[i].n_faces);
				Console.WriteLine(" First Brush Index: " + models[i].brush);
				Console.WriteLine(" Number of Brushes: " + models[i].n_brushes);
			}
			
			// Brushes
			Console.WriteLine("Brushes");
			Brush[] brushes = Brushes;
			for(int i=0; i<brushes.Length; i++)
			{
				Console.WriteLine(" Brushside Index: " + brushes[i].brushside);
				Console.WriteLine(" Number of Brushsides: " + brushes[i].n_brushsides);
				Console.WriteLine(" Texture Index: " + brushes[i].texture);
			}
			
			// Brushsides
			Console.WriteLine("Brushsides");
			Brushside[] brushsides = Brushsides;
			for(int i=0; i<brushsides.Length; i++)
			{
				Console.WriteLine(" Plane Index: " + brushsides[i].plane);
				Console.WriteLine(" Texture Index: " + brushsides[i].texture);
			}
			
			// Vertexes
			Console.WriteLine("Vertexes");
			Vertex[] vertexes = Vertexes;
			for(int i=0; i<vertexes.Length; i++)
			{
				Console.WriteLine(" Position: (" + vertexes[i].position[0] + ", " + vertexes[i].position[1] + ", " + vertexes[i].position[2] + ")");
				Console.WriteLine(" Texture Coordiate:[{0}, {1}]", vertexes[i].uv[0][0], vertexes[i].uv[0][1]);
				Console.WriteLine("                   [{0}, {1}]", vertexes[i].uv[1][1], vertexes[i].uv[1][1]);
				Console.WriteLine(" Normal: ({0}, {1}, {2})", vertexes[i].normal[0], vertexes[i].normal[1], vertexes[i].normal[2]);
				Console.WriteLine(" Color:　({0}, {1}, {2})", vertexes[i].color[0], vertexes[i].color[1], vertexes[i].color[2]);
			}
			
			// Meshverts
			Console.WriteLine("Triangle Meshes");
			int[] meshverts = Meshverts;
			for(int i=0; i<meshverts.Length; i++)
			{
				Console.WriteLine(meshverts[i]);
			}
			
			// Effects
			Console.WriteLine("Effects");
			Effect[] effects = Effects;
			for(int i=0; i<effects.Length; i++)
			{
				Console.WriteLine(" Effect Name: ");
				Console.WriteLine(effects[i].name);
				Console.WriteLine(" Brush Index: " + effects[i].brush);
				Console.WriteLine(" Unknow: " + effects[i].unknown);
			}
			
			// Faces
			Console.WriteLine("Faces");
			Face[] faces = Faces;
			for(int i=0; i<faces.Length; i++)
			{
				Console.WriteLine(" Texture Index: " + faces[i].texture);
				Console.WriteLine(" Effect Index: " + faces[i].effect);
				Console.WriteLine(" Face Type: " + faces[i].typename);
				Console.WriteLine(" First Vertex Index: " + faces[i].vertex);
				Console.WriteLine(" Number of Vertexes: " + faces[i].n_vertexes);
				Console.WriteLine(" First Meshvert Index: " + faces[i].meshvert);
				Console.WriteLine(" Number of Meshverts: " + faces[i].n_meshverts);
				Console.WriteLine(" Lightmap Index: " + faces[i].lm_index);
				Console.WriteLine(" Lightmap image start:[{0}, {1}]", faces[i].lm_start[0], faces[i].lm_start[1]);
				Console.WriteLine(" Lightmap image size:[{0}, {1}]", faces[i].lm_size[0], faces[i].lm_size[1]);
				Console.WriteLine(" World space origin of lightmap: [{0}, {1}, {2}]", 
				                  faces[i].lm_origin[0], faces[i].lm_origin[1], faces[i].lm_origin[2]);
				// Don't display: float[2][3] lm_vecs 	World space lightmap s and t unit vectors.
				Console.WriteLine(" Normal: [{0}, {1}, {2}]", faces[i].normal[0], faces[i].normal[1], faces[i].normal[2]);
				Console.WriteLine(" Patch dimensions: [{0}, {1}]", faces[i].size[0], faces[i].size[1]);
			}
			
			// Lightmaps
			Console.WriteLine("Lightmaps");
			Lightmap[] lightmaps = Lightmaps;
			for(int m=0; m<lightmaps.Length; m++)
			{
				for (int i = 0; i < lightmaps[m].map.Length; i++)
				{
					for(int j=0; j<lightmaps[m].map[i].Length; j++)
					{
						Console.WriteLine(" Color(RGB): [{0}, {1}, {2}]", 
						                  lightmaps[m].map[i][j][0], lightmaps[m].map[i][j][1], lightmaps[m].map[i][j][2]);
					}
				}
			}
			
			// Lightvols
			Console.WriteLine("Lightvols");
			Lightvol[] lightvols = Lightvols;
			for(int i=0; i<lightvols.Length; i++)
			{
				Console.WriteLine(" Ambient(RGB): [{0}, {1}, {2}]", 
				                  lightvols[i].ambient[0], lightvols[i].ambient[1], lightvols[i].ambient[2]);
				Console.WriteLine(" Directional(RGB): [{0}, {1}, {2}]", 
				                  lightvols[i].directional[0], lightvols[i].directional[1], lightvols[i].directional[2]);
				Console.WriteLine(" Direction to Light: [{0}, {1}]", lightvols[i].dir[0], lightvols[i].dir[1]);
			}
			
			// Visdata
			Console.WriteLine("Visdata");
			Visibilitydata vd = Visdata;
			for(int i=0; i<vd.sz_vecs*vd.n_vecs; i++)
			{
				Console.WriteLine(vd.vecs[i]);
			}
		}
	}
//	class Program
//	{
//		static void Main(string[] args)
//		{
//			BSPParser parser = new BSPParser();
//			parser.display();
//		}
//	}
}
