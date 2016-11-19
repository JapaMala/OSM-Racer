using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ClipperLib;

public static class NodeList
{
	static Dictionary<long, Node> nodeDictionary;
	static long minimumNodeId = 0;
	static long maximumNodeId = 0;
	public class Node
	{
		long id;
		Dictionary<string, string> tags;
		IntPoint position;
	}
}
