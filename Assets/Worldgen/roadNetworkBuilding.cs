using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Poly2Tri;
using ClipperLib;
using System.IO;

public class roadNetworkBuilding : MonoBehaviour {
        
        
    public string GetTempPath()
    {
        string path = System.Environment.GetEnvironmentVariable("TEMP");
        if (!path.EndsWith("\\")) path += "\\";
        return path;
    }

    public void LogMessageToFile(string msg)
    {
        System.IO.StreamWriter sw = System.IO.File.AppendText(
            GetTempPath() + "My Log File.txt");
        try
        {
            //string logLine = System.String.Format(
            //    "{0:G}: {1}", System.DateTime.Now, msg);
            sw.WriteLine(msg);
        }
        finally
        {
            sw.Close();
        }
    }
	public float height = 6.0f;
	public List<List<roadNetworkIntersection>> cornerNodeLists;
	public List<List<roadNetworkIntersection>> HoleNodeLists;
	
	static double floatMultiplier = 100000;
	
	List<Vector3> vertices = new List<Vector3>();
	List<int> faces = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void MakeSection(List<Vector3> verts, List<int> facs, PolyNode polyNode) {
		if(polyNode.IsHole) {
			Debug.LogError("Found a hole where there's not meant to be one.");
			return;
		}
		List<PolygonPoint> polygonPointList = new List<PolygonPoint>();
		foreach(IntPoint Outvertex in polyNode.Contour) {
			polygonPointList.Add(new PolygonPoint(Outvertex.X/floatMultiplier, Outvertex.Y/floatMultiplier));
		}
		MakeWalls(verts, facs, polyNode.Contour);
		Polygon poly = new Polygon(polygonPointList);
		foreach(PolyNode holeNode in polyNode.Childs) {
		    MakeWalls(verts, facs, holeNode.Contour);
			List<PolygonPoint> holePointList = new List<PolygonPoint>();
			foreach(IntPoint Holevertex in holeNode.Contour) {
				holePointList.Add(new PolygonPoint(Holevertex.X/floatMultiplier, Holevertex.Y/floatMultiplier));
			}
            Polygon hole = new Polygon(holePointList);
			poly.AddHole(hole);
		}
		P2T.Triangulate(poly);
		IList<DelaunayTriangle> tris = poly.Triangles;
        IList<TriangulationPoint> points = poly.Points;
        if (poly.Holes != null)
        {
            foreach (Polygon hole in (poly.Holes))
            {
                foreach (TriangulationPoint point in hole.Points)
                {
                    points.Add(point);
                }
            }
        }
		int startingIndex = verts.Count;
		foreach(TriangulationPoint point in points)
        {
			verts.Add(new Vector3(point.Xf, height, point.Yf));
		}
		foreach(DelaunayTriangle tri in tris) {
            facs.Add(points.IndexOf(tri.Points._2) + startingIndex);
			facs.Add(points.IndexOf(tri.Points._1) + startingIndex);
			facs.Add(points.IndexOf(tri.Points._0) + startingIndex);
		}
		foreach(PolyNode holeNode in polyNode.Childs) {
			foreach(PolyNode childNode in holeNode.Childs) {
				MakeSection(verts, facs, childNode);
			}
		}
	}
	
	void MakeWalls(List<Vector3> verts, List<int> facs, List<IntPoint> contour) {
		for(int i = 0; i < contour.Count; i++) {
			int j = i+1;
			if(j == contour.Count) j = 0;
			int trueIndex = verts.Count;
			verts.Add(new Vector3((float)(contour[i].X/floatMultiplier),	0,		(float)(contour[i].Y/floatMultiplier)));
			verts.Add(new Vector3((float)(contour[i].X/floatMultiplier),	height,	(float)(contour[i].Y/floatMultiplier)));
			verts.Add(new Vector3((float)(contour[j].X/floatMultiplier),	0,		(float)(contour[j].Y/floatMultiplier)));
			verts.Add(new Vector3((float)(contour[j].X/floatMultiplier),	height,	(float)(contour[j].Y/floatMultiplier)));
			facs.Add(trueIndex);
			facs.Add(trueIndex+3);
			facs.Add(trueIndex+2);
			facs.Add(trueIndex);
			facs.Add(trueIndex+1);
			facs.Add(trueIndex+3);
		}
	}
	
	public void GenerateMesh() {
		if(cornerNodeLists == null)
			return;
		if(cornerNodeLists.Count == 0)
			return;
		List<roadNetworkIntersection> cornerNodes = cornerNodeLists[0];
		if(cornerNodes.Count < 3)
			return; //it's just too damn small.
		vertices.Clear();
		faces.Clear();
		uvs.Clear();
		
		Vector3 center = new Vector3();
		foreach(roadNetworkIntersection node in cornerNodes) {
			center += node.gameObject.transform.position;
		}
		center /= cornerNodes.Count;
		gameObject.transform.position = center;
		
		Clipper clipper = new Clipper();
		foreach(List<roadNetworkIntersection> wayList in cornerNodeLists) {
			List<IntPoint> contour = new List<IntPoint>();
			foreach(roadNetworkIntersection corner in wayList) {
				contour.Add(new IntPoint((long)((corner.transform.position-center).x*floatMultiplier), (long)((corner.transform.position-center).z*floatMultiplier)));
			}
			clipper.AddPolygon(contour, PolyType.ptSubject);
		}
		if(HoleNodeLists != null) {
			foreach(List<roadNetworkIntersection> wayList in HoleNodeLists) {
				if(wayList != null) {
				List<IntPoint> contour = new List<IntPoint>();
					foreach(roadNetworkIntersection corner in wayList) {
						contour.Add(new IntPoint((long)((corner.transform.position-center).x*floatMultiplier), (long)((corner.transform.position-center).z*floatMultiplier)));
					}
					clipper.AddPolygon(contour, PolyType.ptClip);
				}
			}
		}
		clipper.ForceSimple = true;
		PolyTree polyTree = new PolyTree();
		clipper.Execute(ClipType.ctDifference, polyTree, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
		
		foreach(PolyNode polyNode in polyTree.Childs) {
			MakeSection(vertices, faces, polyNode);
		}
				
		
		
//		//faces = new List<int>(triangulator.Triangulate());
//		
//		int capVerts = vertices2d.Count;
//		
////		for(int i = 0; i < capVerts; i++) {
////			vertices.Add (new Vector3((float)vertices2d[i].Y, height, (float)vertices2d[i].Y));
////		}
//		
//		if(height > 0.001f) {
//			double runningTotal = 0.0;
//			for(int i = 0; i < vertices2d.Count; i++){
//				int j = i+1;
//				if(j >= vertices2d.Count) j = 0;
//				runningTotal += ((vertices2d[j].X - vertices2d[i].X) * (vertices2d[j].Y + vertices2d[i].Y));
//			}
//			int start = 0;
//			int end = capVerts;
//			int step = 1;
//			if(runningTotal <= 0) {
//				start = capVerts-1;
//				end = -1;
//				step = -1;
//			}
//		}
		
		//proper UVs will need to be made, this is just for testing
		foreach(Vector3 vertex in vertices) {
			Vector2 uvert = new Vector2();
			uvert.x = vertex.x/3;
			uvert.y = vertex.z/3;
			uvs.Add(uvert);
		}
		
		MeshFilter mf = GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mf.mesh = mesh;
		
		mesh.vertices = vertices.ToArray();
		mesh.triangles = faces.ToArray();
		mesh.uv = uvs.ToArray();
		
		if(vertices.Count > 0) {		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		}
	}
}
