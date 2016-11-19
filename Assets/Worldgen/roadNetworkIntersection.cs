using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class roadNetworkIntersection : MonoBehaviour {
	//public List<GameObject> connectedIntersections;
	public List<roadNetworkRoad> connectedRoads;
	
	class roadDirection {
		public roadNetworkRoad connectedRoad;
		public Vector3 totalDistance;
		public Vector3 totalDistanceFlat;
		public double absoluteAngle;
		public double nextCornerAngle;
		public float nextCornerDistance;
		public float nextStreetDistance;
		public float prevStreetDistance;
		public int prevStreetVertex;
		public int prevAngleVertex;
		public int centerStreetVertex;
		public int nextStreetVertex;
		public int nextAngleVertex;
	}
	List<roadDirection> directionList = new List<roadDirection>();
	
	List<Vector3> vertices = new List<Vector3>();
	List<int> faces = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	
	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
		//GenerateMesh();
	}
	
	public bool isConnected(roadNetworkRoad input) {
		foreach(roadNetworkRoad road in connectedRoads) {
			if(road == input)
				return true;
		}
		return false;
	}
		
	static int compareVectorAngle(Vector2 a, Vector2 b) {
		double aa = AngleClamp(Math.Atan2(a.y, a.x));
		double bb = AngleClamp(Math.Atan2(b.y, b.x));
		double difference = (bb - aa);
		if (difference < 0) return 1;
		if (difference > 0) return -1;
		return 0;
	}
	static int compareVectorAngle(Vector3 a, Vector3 b) {
		Vector2 aa = new Vector2(a.x, a.z);
		Vector2 bb = new Vector2(b.x, b.z);
		return compareVectorAngle(aa, bb);
	}
	
	int compareVectorAngle(GameObject a, GameObject b) {
		return compareVectorAngle(a.transform.position - this.transform.position, b.transform.position - this.transform.position);
	}
	int compareVectorAngle(roadNetworkRoad a, roadNetworkRoad b) {
		if(!a.getOtherIntersection(this) || !b.getOtherIntersection(this))
			return 0;
		return compareVectorAngle(
			a.getOtherIntersection(this).gameObject.transform.position - this.transform.position,
			b.getOtherIntersection(this).gameObject.transform.position - this.transform.position);
	}
	
	void sortRoads() {
		connectedRoads.Sort(this.compareVectorAngle);
	}
	
	/*
	//find a road connecting this intersection to another, if any. If none is found, create one.
	roadNetworkRoad getRoadTo(roadNetworkIntersection connection) {
		int index = connectedIntersections.FindIndex(connection.isThis); 
		if(index < 0)
			return null;
		if(connectedRoads == null) {
			connectedRoads = new List<roadNetworkRoad>(connectedIntersections.Capacity);
		}
		if(!connectedRoads[index])
			connectedRoads[index] = new roadNetworkRoad(this);
		return connectedRoads[index];
	}
	*/
	
	static double AngleClamp(double input){
		bool high = true;
		bool low = true;
		while(high || low) {
			if(input > Math.PI*2.0)
				input -= Math.PI*2.0;
			else high = false;
			if(input < 0)
				input += Math.PI*2.0;
			else low = false;
		}
		return input;
	}
	
	bool isThis(GameObject input) {
		if(input.GetComponent<roadNetworkIntersection>() == this)
			return true;
		else return false;
	}
	
	public void GenerateMesh() {
		if(connectedRoads.Count < 1) //If there's no other intersection, we can't do it.
			return; //Fixme: make it actually work.
		sortRoads(); //Just to make sure everything's in order.
		MeshFilter mf = GetComponent<MeshFilter>();
		Mesh mesh = new Mesh();
		mf.mesh = mesh;
		
		vertices.Clear();
		faces.Clear();
		uvs.Clear();
		
		if(connectedRoads.Count == 1)
			GenerateEndPoint();
		else if(connectedRoads.Count == 2)
			GenerateCorner();
		else
			GenerateBasicIntersection();

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
	
	void GenerateBasicIntersection() {
		
		Vector3 centerpoint = new Vector3(0,0,0);
		vertices.Add(centerpoint);
		directionList.Clear();
		for(int i = 0; i <  connectedRoads.Count; i++) {
			roadNetworkRoad road = connectedRoads[i];

			if(road.getOtherIntersection(this)) {
				
				roadDirection direction = new roadDirection();
				direction.connectedRoad = road;
				direction.totalDistance = road.getOtherIntersection(this).gameObject.transform.position - this.transform.position;
				
				direction.totalDistanceFlat = new Vector3(direction.totalDistance.x, 0, direction.totalDistance.z);
								
				direction.absoluteAngle = AngleClamp(Math.Atan2(direction.totalDistanceFlat.z, direction.totalDistanceFlat.x));
				directionList.Add(direction);
			}
		}
		for(int i = 0; i <  directionList.Count; i++) {
			int j = i+1;
			if( j >= directionList.Count) j = 0;
			roadDirection currentDirection = directionList[i];
			roadDirection nextDirection = directionList[j];
			
			//find the vectors to the elbows between the streets.
			double innerAngle = AngleClamp(nextDirection.absoluteAngle-currentDirection.absoluteAngle);
			if(innerAngle == Math.PI) {
				currentDirection.nextCornerAngle = AngleClamp(Math.PI/2 + currentDirection.absoluteAngle);
				currentDirection.nextCornerDistance = -1.0f;
			}
			else {
				double angleMid = AngleClamp(Math.Atan2(
					((currentDirection.connectedRoad.getWidthLeft(this)/nextDirection.connectedRoad.getWidthRight(this)) * Math.Sin(innerAngle)) ,
					1+ ((currentDirection.connectedRoad.getWidthLeft(this)/nextDirection.connectedRoad.getWidthRight(this)) * Math.Cos(innerAngle))));
				currentDirection.nextCornerAngle = AngleClamp(angleMid + currentDirection.absoluteAngle);
				currentDirection.nextCornerDistance = currentDirection.connectedRoad.getWidthLeft(this)/((float)Math.Sin(angleMid));
			}
		}
		for(int i = 0; i <  directionList.Count; i++) {
			int k = i-1;
			if( k < 0 ) k = directionList.Count -1;
			roadDirection prevDirection = directionList[k];
			roadDirection currentDirection = directionList[i];
			
			currentDirection.nextStreetDistance = (float)(Math.Sqrt((currentDirection.nextCornerDistance*currentDirection.nextCornerDistance) - (currentDirection.connectedRoad.getWidthLeft(this)*currentDirection.connectedRoad.getWidthLeft(this))));
			currentDirection.prevStreetDistance = (float)(Math.Sqrt((prevDirection.nextCornerDistance*prevDirection.nextCornerDistance) - (currentDirection.connectedRoad.getWidthRight(this)*currentDirection.connectedRoad.getWidthRight(this))));
			double forwardAngle = AngleClamp(currentDirection.nextCornerAngle - currentDirection.absoluteAngle);
			double backwardsAngle = AngleClamp(currentDirection.absoluteAngle - prevDirection.nextCornerAngle);
			
			if(backwardsAngle > Math.PI)
				backwardsAngle -= Math.PI;
			if(forwardAngle > Math.PI)
				forwardAngle -= Math.PI;
			
			if(forwardAngle > (Math.PI/2.0)){
				currentDirection.nextStreetDistance = -currentDirection.nextStreetDistance;
			}
			if(backwardsAngle > (Math.PI/2.0)){
				currentDirection.prevStreetDistance = -currentDirection.prevStreetDistance;
			}
			
			float distance = currentDirection.nextStreetDistance;
			if(distance < currentDirection.prevStreetDistance)
				distance = currentDirection.prevStreetDistance;
			
			Vector3 endpoint = currentDirection.totalDistanceFlat.normalized * distance;
			Vector3 nextMidpoint = new Vector3(currentDirection.nextCornerDistance * (float)Math.Cos(currentDirection.nextCornerAngle), 0, currentDirection.nextCornerDistance * (float)Math.Sin(currentDirection.nextCornerAngle));
							
			int tempIndex = vertices.Count;
			vertices.Add(endpoint);
			vertices.Add(nextMidpoint);

			currentDirection.nextAngleVertex = tempIndex+1;
			currentDirection.centerStreetVertex = tempIndex;
		}
		//corner mirroring
		for(int i = 0; i <  directionList.Count; i++) {
			int k = i-1;
			if( k < 0 ) k = directionList.Count -1;
			int j = i+1;
			if( j >= directionList.Count) j = 0;
			roadDirection prevDirection = directionList[k];
			roadDirection currentDirection = directionList[i];
			currentDirection.prevAngleVertex = prevDirection.nextAngleVertex;
			if(currentDirection.nextStreetDistance > currentDirection.prevStreetDistance){
				int tempIndex = vertices.Count;
				float distanceDifference = currentDirection.nextStreetDistance - currentDirection.prevStreetDistance;
				Vector3 newStreetVert = (currentDirection.totalDistanceFlat.normalized * distanceDifference) + vertices[currentDirection.prevAngleVertex];
				currentDirection.nextStreetVertex = currentDirection.nextAngleVertex;
				currentDirection.prevStreetVertex = tempIndex;
				vertices.Add(newStreetVert);
				faces.Add(0);
				faces.Add(currentDirection.prevStreetVertex);
				faces.Add(currentDirection.prevAngleVertex);
				
			}
			else{
				int tempIndex = vertices.Count;
				float distanceDifference = currentDirection.prevStreetDistance - currentDirection.nextStreetDistance;
				Vector3 newStreetVert = (currentDirection.totalDistanceFlat.normalized * distanceDifference) + vertices[currentDirection.nextAngleVertex];
				currentDirection.prevStreetVertex = currentDirection.prevAngleVertex;
				currentDirection.nextStreetVertex = tempIndex;
				vertices.Add(newStreetVert);
				faces.Add(0);
				faces.Add(currentDirection.nextAngleVertex);
				faces.Add(currentDirection.nextStreetVertex);				
			}
			currentDirection.connectedRoad.setEndPoints(vertices[currentDirection.nextStreetVertex], vertices[currentDirection.centerStreetVertex], vertices[currentDirection.prevStreetVertex], this);
		}
		//finally make all the tris now that we have the verts.
		for(int i = 0; i <  directionList.Count; i++) {
			faces.Add(0);
			faces.Add(directionList[i].nextStreetVertex);
			faces.Add(directionList[i].centerStreetVertex);
			faces.Add(0);
			faces.Add(directionList[i].centerStreetVertex);
			faces.Add(directionList[i].prevStreetVertex);
		}

		//proper UVs will need to be made, this is just for testing
		foreach(Vector3 vertex in vertices) {
			Vector2 uvert = new Vector2();
			uvert.x = vertex.x/3;
			uvert.y = vertex.z/3;
			uvs.Add(uvert);
		}
	}
	
	void GenerateCorner() {
		for(int i = 0; i <  connectedRoads.Count; i++) {
			roadNetworkRoad road = connectedRoads[i];

			if(road.getOtherIntersection(this)) {
				
				roadDirection direction = new roadDirection();
				direction.connectedRoad = road;
				direction.totalDistance = road.getOtherIntersection(this).gameObject.transform.position - this.transform.position;
				
				direction.totalDistanceFlat = new Vector3(direction.totalDistance.x, 0, direction.totalDistance.z);
								
				direction.absoluteAngle = AngleClamp(Math.Atan2(direction.totalDistanceFlat.z, direction.totalDistanceFlat.x));
				directionList.Add(direction);
			}
		}
		for(int i = 0; i <  directionList.Count; i++) {
			int j = i+1;
			if( j >= directionList.Count) j = 0;
			roadDirection currentDirection = directionList[i];
			roadDirection nextDirection = directionList[j];
			
			float cornerDistance = (currentDirection.connectedRoad.getWidthLeft(this) + nextDirection.connectedRoad.getWidthRight(this)) / 2.0f;
			
			//find the vectors to the elbows between the streets.
			double innerAngle = AngleClamp(nextDirection.absoluteAngle-currentDirection.absoluteAngle);
			currentDirection.nextCornerAngle = AngleClamp(innerAngle/2 + currentDirection.absoluteAngle);
			if(innerAngle > Math.PI) innerAngle = (Math.PI*2) - innerAngle;
			if(innerAngle > Math.PI/4)
				currentDirection.nextCornerDistance = cornerDistance/(float)(Math.Sin(innerAngle/2.0));
			else
				currentDirection.nextCornerDistance = cornerDistance;
		}
		for(int i = 0; i <  directionList.Count; i++) {
			int k = i-1;
			if( k < 0 ) k = directionList.Count -1;
			roadDirection prevDirection = directionList[k];
			roadDirection currentDirection = directionList[i];
			
			currentDirection.nextStreetDistance = (float)(Math.Sqrt((currentDirection.nextCornerDistance*currentDirection.nextCornerDistance) - (currentDirection.connectedRoad.getWidthLeft(this)*currentDirection.connectedRoad.getWidthLeft(this))));
			currentDirection.prevStreetDistance = (float)(Math.Sqrt((prevDirection.nextCornerDistance*prevDirection.nextCornerDistance) - (currentDirection.connectedRoad.getWidthRight(this)*currentDirection.connectedRoad.getWidthRight(this))));
			double forwardAngle = AngleClamp(currentDirection.nextCornerAngle - currentDirection.absoluteAngle);
			double backwardsAngle = AngleClamp(currentDirection.absoluteAngle - prevDirection.nextCornerAngle);
			
			if(backwardsAngle > Math.PI)
				backwardsAngle -= Math.PI;
			if(forwardAngle > Math.PI)
				forwardAngle -= Math.PI;
			
			if(forwardAngle > (Math.PI/2.0)){
				currentDirection.nextStreetDistance = -currentDirection.nextStreetDistance;
			}
			if(backwardsAngle > (Math.PI/2.0)){
				currentDirection.prevStreetDistance = -currentDirection.prevStreetDistance;
			}

			Vector3 nextMidpoint = new Vector3(currentDirection.nextCornerDistance * (float)Math.Cos(currentDirection.nextCornerAngle), 0, currentDirection.nextCornerDistance * (float)Math.Sin(currentDirection.nextCornerAngle));
							
			int tempIndex = vertices.Count;
			vertices.Add(nextMidpoint);

			currentDirection.nextAngleVertex = tempIndex;
		}
		for(int i = 0; i <  directionList.Count; i++) {
			int k = i-1;
			if( k < 0 ) k = directionList.Count -1;
			int j = i+1;
			if( j >= directionList.Count) j = 0;
			roadDirection prevDirection = directionList[k];
			roadDirection currentDirection = directionList[i];
			currentDirection.prevAngleVertex = prevDirection.nextAngleVertex;
			currentDirection.connectedRoad.setEndPoints(vertices[currentDirection.nextAngleVertex], new Vector3(), vertices[currentDirection.prevAngleVertex], this);
		}
		vertices.Clear();
	}
	
	void GenerateEndPoint() {
		roadNetworkRoad road = connectedRoads[0];

		if(road.getOtherIntersection(this)) {
			
			roadDirection direction = new roadDirection();
			direction.connectedRoad = road;
			direction.totalDistance = road.getOtherIntersection(this).gameObject.transform.position - this.transform.position;
			
			direction.totalDistanceFlat = new Vector3(direction.totalDistance.x, 0, direction.totalDistance.z);
							
			direction.absoluteAngle = AngleClamp(Math.Atan2(direction.totalDistanceFlat.z, direction.totalDistanceFlat.x));
			Vector3 leftPoint = new Vector3(direction.connectedRoad.widthLeft * (float)Math.Cos(direction.absoluteAngle + (Math.PI/2.0f)), 0, direction.connectedRoad.widthLeft * (float)Math.Sin(direction.absoluteAngle + (Math.PI/2.0f)));
			Vector3 rightPoint = new Vector3(direction.connectedRoad.widthRight * (float)Math.Cos(direction.absoluteAngle - (Math.PI/2.0f)), 0, direction.connectedRoad.widthRight * (float)Math.Sin(direction.absoluteAngle - (Math.PI/2.0f)));
			direction.connectedRoad.setEndPoints(leftPoint, new Vector3(), rightPoint, this);
		}
	}
}
