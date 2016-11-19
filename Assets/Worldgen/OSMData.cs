using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System;
using System.Text.RegularExpressions;

public class OSMData : MonoBehaviour {
	public TextAsset loadedXML;
	public GameObject nodeObject;
	public GameObject roadObject;
	public GameObject buildingObject;
	public GameObject waterObject;
	Projection projection = new Projection();
	double latMin;
	double latMax;
	double lonMin;
	double lonMax;

    static float levelHeight = 3;
	
	List<roadNetworkBuilding> buildingList = new List<roadNetworkBuilding>();

	// Use this for initialization
	void Start () {
		float startTime = Time.realtimeSinceStartup;
		LoadXml();
		startTime = Time.realtimeSinceStartup - startTime;
		Debug.Log("Took " + startTime + " seconds to generate the world");
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	enum Units{
		none,
		m,
		feet
	}
	
	static double ConvertToMeters(string input) {
		string[] inputList = Regex.Split(input, @"([-\.\d]+)| ");
		double total = 0.0f;
		double last = 0.0f;
		Units lastUnit = Units.none;
		
		foreach( string found in inputList) {
			double temp;
			if(String.IsNullOrEmpty(found)){
				continue;
			}
			else if(found == " ") {
				continue;
			}
			else if(Double.TryParse(found, out temp)) {
				last = temp;
			}
			else if (String.Compare(found, "m", StringComparison.OrdinalIgnoreCase) == 0) {
				total += last;
				lastUnit = Units.m;
				last = 0.0f;
			}
			else if (String.Compare(found, "mts", StringComparison.OrdinalIgnoreCase) == 0) {
				total += last;
				lastUnit = Units.m;
				last = 0.0f;
			}
			else if (String.Compare(found, "feet", StringComparison.OrdinalIgnoreCase) == 0) {
				total += (last*0.3048f);
				lastUnit = Units.feet;
				last = 0.0f;
			}
			else Debug.LogWarning("Unknown symbol : \"" + found + "\" in " + input);
		}
		if(last > 0.001f){
			if(lastUnit == Units.m)
				last /= 100;
		}
		total += last;
		return total;
	}
	
	class Node {
		public long id;
		public double lat;
		public double lon;
		public Dictionary<string, string> tags = new Dictionary<string, string>();
		public roadNetworkIntersection intersection;
	}
	
	Dictionary<long, Node> nodeList = new Dictionary<long, Node>();
	
	public enum Type {
		none,
		road,
		building
	} 

	public class Way {
		public long id;
		public List<long> nodeIndices;
		public List<long> relationIndices;
		public List<roadNetworkRoad> roadList = new List<roadNetworkRoad>();
		public roadNetworkBuilding building;
		public Dictionary<string, string> tags = new Dictionary<string, string>();
	}
	
	Dictionary<long, Way> wayList = new Dictionary<long, Way>();
	
	enum RelationMemberType {
		none,
		node,
		way,
		relation
	}
	
	enum RelationMemberRole {
		none,
		outer,
		inner
	}
	
	public class Relation {
		public long id;
		public List<long> outerWays = new List<long>();
		public List<long> innerWays = new List<long>();
		public List<long> otherWays = new List<long>();
		public bool isMultipolygon = false;
		public Dictionary<string, string> tags = new Dictionary<string, string>();
		public List<List<long>> outerNodeIndices;
		public List<List<long>> innerNodeIndices;
		public List<List<long>> otherNodeIndices;
		public roadNetworkBuilding building;
	}
	
	void finalizeRelation(Relation relation) {
		//Debug.Log("doing relation: " + relation.id);
		//if there's no tags, we cannibalize the first outer way, for multipolygon stuff.
		if(relation.tags.ContainsValue("multipolygon")) { //but only if it's a multipolygon.
			if(((relation.tags.Count == 1) || ((relation.tags.Count == 2) && (relation.tags.ContainsKey("name"))))
				&& (relation.outerWays.Count > 0) ) {
				Way firstOuterWay = wayList[relation.outerWays[0]];
				foreach(KeyValuePair<string, string> tag in firstOuterWay.tags) {
					if(relation.tags.ContainsKey(tag.Key))
						relation.tags[tag.Key] = tag.Value;
					else relation.tags.Add(tag.Key, tag.Value);
				}
				firstOuterWay.tags.Clear();
			}
		}
		foreach(long id in relation.outerWays) {
			Way way;
			if(wayList.TryGetValue(id, out way)) {
				if(way.relationIndices == null)
					way.relationIndices = new List<long>();
				way.relationIndices.Add(relation.id);
			}
		}
		foreach(long id in relation.innerWays) {
			Way way;
			if(wayList.TryGetValue(id, out way)) {
				if(way.relationIndices == null)
					way.relationIndices = new List<long>();
				way.relationIndices.Add(relation.id);
			}
		}
		foreach(long id in relation.otherWays) {
			Way way;
			if(wayList.TryGetValue(id, out way)) {
				if(way.relationIndices == null)
					way.relationIndices = new List<long>();
				way.relationIndices.Add(relation.id);
			}
		}
		combineWays(relation.outerWays, out relation.outerNodeIndices);
		combineWays(relation.innerWays, out relation.innerNodeIndices);
		combineWays(relation.otherWays, out relation.otherNodeIndices);
	}
	
	void combineWays(List<long> wayInputs, out List<List<long>> wayOutputs) {
		wayOutputs = new List<List<long>>();
		List<bool> inputUsed = new List<bool>();
		foreach(long index in wayInputs) {
			inputUsed.Add(false);
		}
		for(int i = 0; i < wayInputs.Count; i++){// Loop through unused starting ways.
			if(!(inputUsed[i])) {
				List<long> newNodeList = new List<long>();
				if(!wayList.ContainsKey(wayInputs[i]))
					continue;
				foreach(long nodeId in wayList[wayInputs[i]].nodeIndices) {
					newNodeList.Add(nodeId);
				}
				inputUsed[i] = true;
				if(newNodeList[0] != newNodeList[newNodeList.Count-1]) {// if it's not already a loop.
					for(int j = i+1; j < wayInputs.Count; j++) {// loop through the remaining ways
						for(int l = j; l < wayInputs.Count; l++) {// a bunch of times.
							if(!inputUsed[l]) {
								if(wayList.ContainsKey(wayInputs[l])) {
									if(newNodeList[newNodeList.Count-1] == wayList[wayInputs[l]].nodeIndices[0]) {// if the first of this matches the end of that.
										for(int k = 1; k < wayList[wayInputs[l]].nodeIndices.Count; k++) {// Append that way onto this one.
											newNodeList.Add(wayList[wayInputs[l]].nodeIndices[k]);
										}
										inputUsed[l] = true;
									}
									else if(newNodeList[newNodeList.Count-1] == wayList[wayInputs[l]].nodeIndices[wayList[wayInputs[l]].nodeIndices.Count-1]) {// if the first of this matches the end of that.
										for(int k = wayList[wayInputs[l]].nodeIndices.Count-2; k >= 0 ; k--) {// Append that way onto this one. in reverse.
											newNodeList.Add(wayList[wayInputs[l]].nodeIndices[k]);
										}
										inputUsed[l] = true;
									}
								}
							}
						}
					}
				}
				wayOutputs.Add(newNodeList);
			}
		}
	}

	Dictionary<long, Relation> relationList = new Dictionary<long, Relation>();
	
	void MakeRoads (Way way) {
		for(int i = 0; i < way.nodeIndices.Count-1; i++) {
			roadNetworkIntersection tempNode = nodeList[way.nodeIndices[i]].intersection;
			roadNetworkIntersection nextNode = nodeList[way.nodeIndices[i+1]].intersection;
			GameObject newRoad = (GameObject)Instantiate(roadObject, tempNode.gameObject.transform.position, Quaternion.identity);
			if(way.tags.ContainsKey("name"))
				newRoad.name = way.tags["name"];
			else newRoad.name = "way:" + way.id.ToString();
			roadNetworkRoad tempRoad = newRoad.GetComponent<roadNetworkRoad>();
			tempRoad.origin = tempNode;
			tempRoad.destination = nextNode;
			
			double width;
			string widthString;
			string highwayString;
			if(way.tags.TryGetValue("width", out widthString)){
				width = ConvertToMeters(widthString);
			}
            else if (way.tags.TryGetValue("lanes", out widthString) && Double.TryParse(widthString, out width))
            {
                width *= 3;
            }
            else if (way.tags.TryGetValue("highway", out highwayString))
            {
                width = GetDefaultWidth(highwayString);
            }
            else width = 1;
			
			tempRoad.widthLeft = (float)(width/2);
			tempRoad.widthRight = (float)(width/2);
			tempRoad.EnsureConnections();
			way.roadList.Add(tempRoad);
		}
	}
	
	void MakeBuilding (Relation relation, GameObject buildingPrefab) {
		GameObject newBuilding = (GameObject)Instantiate(buildingPrefab, new Vector3(), Quaternion.identity);
		if(relation.tags.ContainsKey("name"))
			newBuilding.name = relation.tags["name"];
		else newBuilding.name = "relation:" + relation.id.ToString();
		roadNetworkBuilding tempBuilding = newBuilding.GetComponent<roadNetworkBuilding>();
		
		tempBuilding.cornerNodeLists = new List<List<roadNetworkIntersection>>();
		foreach(List<long> way in relation.outerNodeIndices) {
			int index = tempBuilding.cornerNodeLists.Count;
			tempBuilding.cornerNodeLists.Add(new List<roadNetworkIntersection>());
			for(int i = 0; i < way.Count-1; i++) { //OSM will always have the first and last nodes repeated.
				roadNetworkIntersection tempNode = nodeList[way[i]].intersection;
				tempBuilding.cornerNodeLists[index].Add(tempNode);
			}
		}
		tempBuilding.HoleNodeLists = new List<List<roadNetworkIntersection>>();
		foreach(List<long> way in relation.innerNodeIndices) {
			int index = tempBuilding.HoleNodeLists.Count;
			tempBuilding.HoleNodeLists.Add(new List<roadNetworkIntersection>());
			for(int i = 0; i < way.Count-1; i++) { //OSM will always have the first and last nodes repeated.
				roadNetworkIntersection tempNode = nodeList[way[i]].intersection;
				tempBuilding.HoleNodeLists[index].Add(tempNode);
			}
		}
        double height;
        string heightString;
        if (relation.tags.TryGetValue("height", out heightString))
        {
            height = ConvertToMeters(heightString);
        }
        else if ((relation.tags.TryGetValue("building:levels", out heightString)) && Double.TryParse(heightString, out height))
        {
            height *= levelHeight;
        }
        else if (relation.tags.ContainsKey("building"))
        {
            height = 6;
        }
        else height = 0.0f;
        tempBuilding.height = (float)height;
		relation.building = tempBuilding;
		buildingList.Add(tempBuilding);
	}
	
	void MakeBuilding (Way way, GameObject buildingPrefab) {
		GameObject newBuilding = (GameObject)Instantiate(buildingPrefab, new Vector3(), Quaternion.identity);
		if(way.tags.ContainsKey("name"))
			newBuilding.name = way.tags["name"];
		else newBuilding.name = "way:" + way.id.ToString();
		roadNetworkBuilding tempBuilding = newBuilding.GetComponent<roadNetworkBuilding>();
	
		tempBuilding.cornerNodeLists = new List<List<roadNetworkIntersection>>();
		tempBuilding.cornerNodeLists.Add(new List<roadNetworkIntersection>());
		for(int i = 0; i < way.nodeIndices.Count-1; i++) { //OSM will always have the first and last nodes repeated.
			roadNetworkIntersection tempNode = nodeList[way.nodeIndices[i]].intersection;
			tempBuilding.cornerNodeLists[0].Add(tempNode);
		}
		double height;
		string heightString;
		if(way.tags.TryGetValue("height", out heightString))
        {
			height = ConvertToMeters(heightString);
		}
        else if ((way.tags.TryGetValue("building:levels", out heightString)) && Double.TryParse(heightString, out height))
        {
            height *= levelHeight;
        }
        else if (way.tags.ContainsKey("building"))
        {
            height = 6;
        }
        else height = 0.0f;
		tempBuilding.height = (float)height;
		way.building = tempBuilding;
		buildingList.Add(tempBuilding);
	}
	
	public Stream GenerateStreamFromString(string s)
	{
	    MemoryStream stream = new MemoryStream();
	    StreamWriter writer = new StreamWriter(stream);
	    writer.Write(s);
	    writer.Flush();
	    stream.Position = 0;
	    return stream;
	}
	
	float GetDefaultWidth(string tagValue) {
		float width;
		if(tagValue == "pedestrian")
			width = 3.0f;
		else if(tagValue == "path")
			width = 3.0f;
		else if(tagValue == "footway")
			width = 1.5f;										
		else if(tagValue == "crossing")
			width = 1.5f;										
		else if(tagValue == "cycleway")
			width = 2.0f;										
		else if(tagValue == "steps")
			width = 1.5f;										
		else if(tagValue == "unclassified")
			width = 6.0f;
        else if (tagValue == "trunk")
            width = 6.5f;
        else if (tagValue == "trunk_link")
            width = 4.5f;
        else if (tagValue == "secondary")
            width = 6.5f;
        else if (tagValue == "tertiary")
			width = 6.0f;										
		else if(tagValue == "residential")
			width = 5.0f;										
		else if(tagValue == "track")
			width = 3.0f;										
		else if(tagValue == "service")
			width = 3.0f;										
		else
		{
			Debug.Log ("Unknown Highway: " + tagValue);
			width = 8.0f;
		}
		return width;
	}
	
	
	
	void LoadXml() {
		using (Stream s = GenerateStreamFromString(loadedXML.text)) {
			XmlTextReader reader = new XmlTextReader(s);
			while(reader.Read()) {
				switch(reader.NodeType) {
				case XmlNodeType.Element:
					if(reader.Name == "node") {
						Node tempNode = new Node();
						bool empty = reader.IsEmptyElement;
						while (reader.MoveToNextAttribute()) {
							if(reader.Name == "id") 
								tempNode.id = XmlConvert.ToInt64(reader.Value);
							else if(reader.Name == "lat")
								tempNode.lat = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
							else if(reader.Name == "lon")
								tempNode.lon = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
						}
						if(!empty) {
							while (reader.Read()) {
								if(reader.NodeType == XmlNodeType.EndElement)
									break;
								if(reader.Name == "tag") {
									string tagName = "";
									string tagValue = "";
									while (reader.MoveToNextAttribute()) {
										if(reader.Name == "k") 
											tagName = reader.Value;
										else if(reader.Name == "v")
											tagValue = reader.Value;
									}
									if(tempNode.tags.ContainsKey(tagName)) {
										tempNode.tags[tagName] = tagValue;
									}
									else {
										tempNode.tags.Add(tagName, tagValue);
									}
								}
							}
						}
						nodeList.Add(tempNode.id, tempNode);
					}
					else if (reader.Name == "way") {
						long wayId = -1;
						bool empty = reader.IsEmptyElement; //really shouldn't, being a way, but you can't be too sure.
						while (reader.MoveToNextAttribute()) {
							if(reader.Name == "id") 
								wayId = XmlConvert.ToInt64(reader.Value);
						}
						Way tempWay;
						if(!(wayList.TryGetValue(wayId, out tempWay))) {
							tempWay = new Way();
						}
						tempWay.id = wayId;
						tempWay.nodeIndices = new List<long>();
						if(!empty) {
							while (reader.Read()) {
								if(reader.NodeType == XmlNodeType.EndElement)
									break;
								if(reader.Name == "tag") {
									string tagName = "";
									string tagValue = "";
									while (reader.MoveToNextAttribute()) {
										if(reader.Name == "k") 
											tagName = reader.Value;
										else if(reader.Name == "v")
											tagValue = reader.Value;
									}
									if(tempWay.tags.ContainsKey(tagName)) {
										tempWay.tags[tagName] = tagValue;
									}
									else {
										tempWay.tags.Add(tagName, tagValue);
									}
								}
								else if(reader.Name == "nd") {
									while (reader.MoveToNextAttribute()) {
										if(reader.Name == "ref") 
											tempWay.nodeIndices.Add(XmlConvert.ToInt64(reader.Value));
									}
								}
							}
						}
						wayList.Add(tempWay.id, tempWay);
					}
					//Highly unfinished.
					else if (reader.Name == "relation") {
						Relation tempRelation = new Relation();
						bool empty = reader.IsEmptyElement;
						while (reader.MoveToNextAttribute()) {
							if(reader.Name == "id") 
								tempRelation.id = XmlConvert.ToInt64(reader.Value);
						}
						if(!empty) {	
							while (reader.Read()) {
								if(reader.NodeType == XmlNodeType.EndElement)
									break;
								if(reader.Name == "tag") {
									string tagName = "";
									string tagValue = "";
									while (reader.MoveToNextAttribute()) {
										if(reader.Name == "k") 
											tagName = reader.Value;
										else if(reader.Name == "v")
											tagValue = reader.Value;
										if(tempRelation.tags.ContainsKey(tagName)) {
											tempRelation.tags[tagName] = tagValue;
										}
										else {
											tempRelation.tags.Add(tagName, tagValue);
										}
									}
								}
								else if(reader.Name == "member") {
									long memberId = -1;
									RelationMemberType tempType = RelationMemberType.none;
									RelationMemberRole tempRole = RelationMemberRole.none;
									while(reader.MoveToNextAttribute()) {
										if(reader.Name == "ref") { 
											memberId = XmlConvert.ToInt64(reader.Value);
										}
										else if(reader.Name == "type") {
											if(reader.Value == "way")
												tempType = RelationMemberType.way;
										}		
										else if(reader.Name == "role") {
											if(reader.Value == "outer")
												tempRole = RelationMemberRole.outer;
											else if(reader.Value == "inner")
												tempRole = RelationMemberRole.inner;
										}
									}
									if(tempType == RelationMemberType.way) {
										if(tempRole == RelationMemberRole.outer) {
											tempRelation.outerWays.Add(memberId);
										}
										else if(tempRole == RelationMemberRole.inner) {
											tempRelation.innerWays.Add(memberId);
										}
										else {
											tempRelation.otherWays.Add(memberId);
										}
									}
								}
							}
						}
						relationList.Add(tempRelation.id, tempRelation);
					}
					else if(reader.Name == "bounds") {
						while (reader.MoveToNextAttribute()) {
							if(reader.Name == "minlat") 
								latMin = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
							else if(reader.Name == "minlon")
								lonMin = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
							else if(reader.Name == "maxlat")
								latMax = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
							else if(reader.Name == "maxlon")
								lonMax = XmlConvert.ToDouble(reader.Value)*Math.PI/180.0;
						}
					}
					break;
				}
			}
		}
		double latMid = latMin + ((latMax - latMin)/2);
		double lonMid = lonMin + ((lonMax - lonMin)/2);
		projection.ensureHasRadius(latMid);
		foreach(KeyValuePair<long, Node> node in nodeList) {
			GameObject newNode = (GameObject)Instantiate(nodeObject, projection.getCoordinatesEquiRectangular(node.Value.lon-lonMid, node.Value.lat - latMid), Quaternion.identity);
			node.Value.intersection = newNode.GetComponent<roadNetworkIntersection>();
			if(node.Value.tags.ContainsKey("name"))
				newNode.name = node.Value.tags["name"];
			else newNode.name = "node:" + node.Value.id.ToString();
		}
		foreach(KeyValuePair<long, Relation> relation in relationList) {
			finalizeRelation(relation.Value);
		}
		foreach(KeyValuePair<long, Way> way in wayList) {
			if(way.Value.tags.ContainsKey("highway"))
				MakeRoads(way.Value);
			else if (way.Value.tags.ContainsKey("building"))
				MakeBuilding(way.Value, buildingObject);
			else if (((way.Value.tags.ContainsKey("landuse")
				&& way.Value.tags["landuse"] == "reservoir")
				|| (way.Value.tags.ContainsKey("natural")
				&& ((way.Value.tags["natural"] == "water") /*|| (way.Value.tags["natural"] == "coastline")*/))
				|| (way.Value.tags.ContainsKey("waterway")
				&& way.Value.tags["waterway"] == "riverbank"))
				&& !((way.Value.tags.ContainsKey("place")
				&& way.Value.tags["place"] == "island"))) {
				MakeBuilding(way.Value, waterObject);
			}
		}
		foreach(KeyValuePair<long, Relation> relation in relationList) {
			if (relation.Value.tags.ContainsKey("building"))
				MakeBuilding(relation.Value, buildingObject);
		}
		foreach(KeyValuePair<long, Node> node in nodeList) {
			node.Value.intersection.GenerateMesh();
		}
		foreach(KeyValuePair<long, Way> way in wayList) {
			if(way.Value.tags.ContainsKey("highway"))
				foreach(roadNetworkRoad road in way.Value.roadList) {
					road.EnsureConnections();
					road.GenerateMesh();
				}
		}
		foreach(roadNetworkBuilding building in buildingList) {
			building.GenerateMesh();
		}
	}
}
