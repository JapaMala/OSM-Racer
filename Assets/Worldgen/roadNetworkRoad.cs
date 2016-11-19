using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class roadNetworkRoad : MonoBehaviour {
	public float widthLeft = 0.5f;
	public float widthRight = 0.5f;
	public roadNetworkIntersection origin;
	public roadNetworkIntersection destination;
	float laneWidth = 3;
	Vector3 originLeftPoint;
	Vector3 originMidPoint;
	Vector3 originRightPoint;
	Vector3 destinationLeftPoint;
	Vector3 destinationMidPoint;
	Vector3 destinationRightPoint;
	public float lanewidth = 1;
//	Vector3 lastOriginPoint;
//	Vector3 lastDestinationPoint;

	List<Vector3> vertices;
	List<int> faces;
	List<Vector2> uvs;
	
	public roadNetworkRoad(roadNetworkIntersection creator) {
		origin = creator;
	}
	
	// Use this for initialization
	void Start () {
		vertices = new List<Vector3>();
		faces = new List<int>();
		uvs = new List<Vector2>();				
	}
	
	// Update is called once per frame
	void Update () {
		//GenerateMesh();
	}
	
	public void EnsureConnections() {
		if(origin && (!origin.isConnected(this))){
			origin.connectedRoads.Add(this);
		}
		if(destination && (!destination.isConnected(this))){
			destination.connectedRoads.Add(this);
		}
	}
	
	public void GenerateMesh() {
		if(!origin)
			return;
		if(!destination)
			return;
//		if((lastOriginPoint == origin.gameObject.transform.position) && (lastDestinationPoint == destination.gameObject.transform.position))
//			return;
//		lastOriginPoint = origin.gameObject.transform.position;
//		lastDestinationPoint = destination.gameObject.transform.position;
		gameObject.transform.position = Vector3.Lerp(origin.gameObject.transform.position, destination.gameObject.transform.position, 0.25f);
		
		MeshFilter mf = GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mf.mesh = mesh;
		
		if(vertices == null) vertices = new List<Vector3>();
		if(faces == null) faces = new List<int>();
		if(uvs == null) uvs = new List<Vector2>();				
		vertices.Clear();
		faces.Clear();
		uvs.Clear();
		
		Vector3 primaryRoadVector = (destinationMidPoint + destination.gameObject.transform.position) - (originMidPoint + origin.gameObject.transform.position);
		float roadAngle = (float)(Math.Atan2(primaryRoadVector.z, primaryRoadVector.x)*180.0f/Math.PI);
		
		laneWidth = 3;
		Vector3 tempVert;
		tempVert = (originLeftPoint + origin.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, tempVert.z));
		tempVert = (originMidPoint + origin.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, tempVert.z));
		tempVert = (originRightPoint + origin.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, -tempVert.z));
		tempVert = (destinationLeftPoint + destination.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, tempVert.z));
		tempVert = (destinationMidPoint + destination.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, tempVert.z));
		tempVert = (destinationRightPoint + destination.gameObject.transform.position) - gameObject.transform.position;
		vertices.Add (tempVert);
		tempVert = (Quaternion.AngleAxis(roadAngle, Vector3.up) * tempVert) / (laneWidth * 2);
		uvs.Add (new Vector2(tempVert.x, -tempVert.z));

		faces.Add(0);
		faces.Add(3);
		faces.Add(1);
		
		faces.Add(1);
		faces.Add(3);
		faces.Add(4);
		
		faces.Add(1);
		faces.Add(4);
		faces.Add(2);
		
		faces.Add(2);
		faces.Add(4);
		faces.Add(5);
		
		for(int i = 0; i < vertices.Count; i++) {
			Vector3 temp = vertices[i];
			bool corrected = false;
			if(float.IsNaN(temp.x)){
				temp.x = 0;
				corrected = true;
			}
			if(float.IsPositiveInfinity(temp.x)){
				temp.x = 1;
				corrected = true;
			}
			if(float.IsNegativeInfinity(temp.x)){
				temp.x = -1;
				corrected = true;
			}
			if(float.IsNaN(temp.y)){
				temp.y = 0;
				corrected = true;
			}
			if(float.IsPositiveInfinity(temp.y)){
				temp.y = 1;
				corrected = true;
			}
			if(float.IsNegativeInfinity(temp.y)){
				temp.y = -1;
				corrected = true;
			}
			if(float.IsNaN(temp.z)){
				temp.z = 0;
				corrected = true;
			}
			if(float.IsPositiveInfinity(temp.z)){
				temp.z = 1;
				corrected = true;
			}
			if(float.IsNegativeInfinity(temp.z)){
				temp.z = -1;
				corrected = true;
			}
			if(corrected)
				//Debug.Log("Invalid vertex " + i + ": " + vertices[i] + " in " + gameObject);
			vertices[i] = temp;
		}
						
		mesh.vertices = vertices.ToArray();
		mesh.triangles = faces.ToArray();
		mesh.uv = uvs.ToArray();
		
		if(vertices.Count > 0) {		
		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		}
	}
	
	public roadNetworkIntersection getOtherIntersection( roadNetworkIntersection input ) {
		if(input == origin) return destination;
		if(input == destination) return origin;
		return null;
	}
	
	public void setEndPoints(Vector3 leftPoint, Vector3 midPoint,Vector3 rightPoint, roadNetworkIntersection source) {
		if(source == origin) {
			originLeftPoint = leftPoint;
			originMidPoint = midPoint;
			originRightPoint = rightPoint;
		}
		if(source == destination) {
			destinationLeftPoint = rightPoint;
			destinationMidPoint = midPoint;
			destinationRightPoint = leftPoint;
		}
	}
	
	public float getWidthLeft(roadNetworkIntersection requester) {
		if(requester == destination)
			return widthRight;
		return widthLeft;
	}
	public float getWidthRight(roadNetworkIntersection requester) {
		if(requester == destination)
			return widthLeft;
		return widthRight;
	}
	
	public bool IsConnected(roadNetworkIntersection inter) {
		return (inter == origin || inter == destination);
	}	
}
