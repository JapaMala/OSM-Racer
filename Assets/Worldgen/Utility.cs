using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/*
 * http://www.opensource.org/licenses/lgpl-2.1.php
 * Copyright Defective Studios 2010-2011
 */
///<author>Matt Schoen</author>
///<date>5/21/2011</date>
///<email>schoen@defectivestudios.com</email>
/// <summary>
/// Defective Studios Utility class
/// </summary>
public static class Utility {
	public static bool Intersection2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 result) {
		return Intersection2D(a1, a2, b1, b2, out result, true, true, 0, "");
	}
	public static bool Intersection2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 result, float z) {
		return Intersection2D(a1, a2, b1, b2, out result, true, true, z, "");
	}
	public static bool Intersection2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 result, bool sega, bool segb, float z) {
		return Intersection2D(a1, a2, b1, b2, out result, sega, segb, z, "");
	}
	/// <summary>
	/// Returns whether two line segments intersect.  This ignores the Z-component of the inputs
	/// </summary>
	/// <param name="a1">First point of line a</param>
	/// <param name="a2">Second point of line a</param>
	/// <param name="b1">First point of line b</param>
	/// <param name="b2">Second point of line b</param>
	/// <param name="result">The point of intersection, 0,0,0 if they don't intersect</param>
	/// <param name="sega">Say-gah! Whether to treat line a as a segment</param>
	/// <param name="segb">Whether to treat line b as a segment</param>
	/// <param name="z">Where in Z to put the intersection</param>
	/// <param name="ident">Debug param for identifying which call this is</param>
	/// <returns>True if the lines intersect</returns>
	public static bool Intersection2D(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, out Vector3 result, 
		bool sega, bool segb, float z, string ident = "") {
		//this code from Neil Carter (a.k.a. the man), http://nether.homeip.net:8080/unity/
		//Code butchered from http://flassari.is/2008/11/line-line-intersection-in-cplusplus/
		result = Vector3.zero;
		float x1 = a1.x, x2 = a2.x, x3 = b1.x, x4 = b2.x;
		float y1 = a1.y, y2 = a2.y, y3 = b1.y, y4 = b2.y;

		float denominator = (y4 - y3) * (x2 - x1) - (x4 - x3) * (y2 - y1);

		// If denominator is zero, the lines are parallel:
		if(denominator == 0.0f)
			return false;

		float ua = ((x4 - x3) * (y1 - y3) - (y4 - y3) * (x1 - x3)) / denominator;
		float ub = ((x2 - x1) * (y1 - y3) - (y2 - y1) * (x1 - x3)) / denominator;

		if(sega)	//If "a" is a line segment
			if(ua <= 0 || ua >= 1)
				return false;
		if(segb)	//If "b" is a line segment
			if(ub <= 0 || ub >= 1)
				return false;
		float x = x1 + ua * (x2 - x1);
		float y = y1 + ua * (y2 - y1);
		if(x == 0 || y == 0) {
			Debug.Log(a1 + ", " + a2 + ", " + b1 + ", " + b2 + " - " + ident);
		}
		result = new Vector3(x, y, z);	//This is a function for 2D intersection, the z is just for fun (actually it just puts the intersection at the right spot)
		return true;
	}
	public struct Vector3Pair { public Vector3 a, b; }
	/// <summary>
	/// Point of Closest Approach function.  Works in constant time to determine the minimum between two points on two given line segments.
	/// Distance formula borrowed from:
	/// http://softsurfer.com/Archive/algorithm_0106/algorithm_0106.htm
	/// </summary>
	/// <param name="a1">First point of line a</param>
	/// <param name="a2">Second point of line a</param>
	/// <param name="b1">First point of line b</param>
	/// <param name="b2">Second point of line b</param>
	/// <returns>the pair of points that are closest</returns>
	public static Vector3Pair Dist3DSegToSeg(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2) {
		//Debug.Log("dist between (" + a1 + " - " + a2 + ") and (" + b1 + ", " + b2 + ")");
		Vector3Pair result = new Vector3Pair();
		Vector3 u = a2 - a1;
		Vector3 v = b2 - b1;
		Vector3 w = a1 - b1;
		float a = Vector3.Dot(u, u);
		float b = Vector3.Dot(u, v);
		float c = Vector3.Dot(v, v);
		float d = Vector3.Dot(u, w);
		float e = Vector3.Dot(v, w);
		float D = a * c - b * b;
		float sc, sN, sD = D;
		float tc, tN, tD = D;

		if(D < Mathf.Epsilon) {	//Lines almost parallel
			sN = 0;
			sD = 1;
			tN = e;
			tD = c;
		} else {
			sN = b * e - c * d;
			tN = a * e - b * d;
			if(sN < 0) {
				sN = 0;
				tN = e;
				tD = e;
			} else if(sN > sD) {
				sN = sD;
				tN = e + b;
				tD = c;
			}
		}

		if(tN < 0) {
			tN = 0;
			if(-d < 0)
				sN = 0;
			else if(-d > a)
				sN = sD;
			else {
				sN = -d + b;
				sD = a;
			}
		} else if(tN > tD) {      // tc > 1 => the t=1 edge is visible
			tN = tD;
			// recompute sc for this edge
			if((-d + b) < 0.0)
				sN = 0;
			else if((-d + b) > a)
				sN = sD;
			else {
				sN = (-d + b);
				sD = a;
			}
		}
		sc = Mathf.Abs(sN) < Mathf.Epsilon ? 0 : sN / sD;
		tc = Mathf.Abs(tN) < Mathf.Epsilon ? 0 : tN / tD;
		result.a = a1 + sc * u;
		result.b = b1 + tc * v;
		return result;
	}
	/// <summary>
	/// Does the line segment a1-a2 get within the threshold distance of b1-b2.  Best way to see if two 3D paths "intersect."  They'll probably never actually intersect
	/// </summary>
	/// <param name="a1">First point of line a</param>
	/// <param name="a2">Second point of line a</param>
	/// <param name="b1">First point of line b</param>
	/// <param name="b2">Second point of line b</param>
	/// <param name="thresh">The threshold distance we're checking</param>
	/// <returns>true if within threshold</returns>
	public static bool DistThresh(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2, float thresh) {
		Vector3Pair points = Dist3DSegToSeg(a1, a2, b1, b2);
		if((points.a - points.b).magnitude < thresh)
			return true;
		return false;
	}
	/// <summary>
	/// Returns the full hierarchy path name of a GO. For example, 'BeeSystem/Bee Rig/gibs/bum/Collider'
	/// </summary>
	/// <param name="go">The GameObject we're pathing</param>
	/// <returns>The path</returns>
	public static string GOHierarchyName(GameObject go) {
		string name = go.name;
		Transform t = go.transform;
		while(t = t.parent)
			name = t.gameObject.name + "/" + name;

		return "'" + name + "'";
	}
	#region VECTOR FUNCTIONS
	/// <summary>
	/// Returns the max of the 3 component values
	/// </summary>
	/// <param name="v">a vectir</param>
	/// <returns>the maximum value</returns>
	public static float Vector3Max(Vector3 v) {
		return Mathf.Max(v.x, v.y, v.z);
	}
	#endregion
	/// <summary>
	/// Returns the transformation matrix and quaternion matrix that will turn a unit triangle at the origin into the given triangle
	/// </summary>
	/// <param name="points">Three vec3's that define the triangle</param>
	/// <param name="norm">A quaternion that will be set to the triangle's rotation.  The matrix won't have rotation because it is too skewed by floating point error</param>
	/// <returns>The transformation marix of the triangle</returns>
	public static Matrix4x4 TriMatrix(Vector3[] points, out Quaternion norm) {
		//Lifted from http://stackoverflow.com/questions/3780493/map-points-between-two-triangles-in-3d-space/4679651#4679651
		Matrix4x4 t1 = Matrix4x4.identity;
		norm = Quaternion.identity;
		if(points.Length > 2) {
			Vector3 v1 = points[0];
			Vector3 v2 = points[1];
			Vector3 v3 = points[2];
			Vector3 cross = Vector3.Cross(v2 - v1, v3 - v1);
			norm = Quaternion.LookRotation(v2 - v1, cross);
			Vector3 v4 = v1 + cross;
			t1.SetColumn(0, Pad(v1));
			t1.SetColumn(1, Pad(v2));
			t1.SetColumn(2, Pad(v3));
			t1.SetColumn(3, Pad(v4));
		}
		return t1;
	}
	/// <summary>
	/// Pads a Vector3 with a 1 in the w slot and returns it as a Vector4
	/// </summary>
	/// <param name="input"></param>
	/// <returns></returns>
	public static Vector4 Pad(Vector3 input) { return new Vector4(input.x, input.y, input.z, 1); }

	/// <summary>
	/// The famous "fast inverse square" Also not really in use but I think it's valid
	/// </summary>
	/// <param name="x"></param>
	/// <returns></returns>
	static float ReciprocalSqrt(float x) {
		float xhalf = 0.5f * x;
		int i = System.BitConverter.ToInt32(System.BitConverter.GetBytes(x), 0);
		i = 0x5f3759df - (i >> 1);
		x = System.BitConverter.ToSingle(System.BitConverter.GetBytes(i), 0);
		x = x * (1.5f - xhalf * x * x);
		return x;
	}
	/// <summary>
	/// Swap two ints with the XOR swap trick. Who knows if this is actually faster, or if the function call has more overhead than a tmp variable...
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	public static void XORSwap(ref int x, ref int y) {
		if(x != y) {
			x ^= y;
			y ^= x;
			x ^= y;
		}
	}
	/// <summary>
	/// Figures out the transformation matrix between two triangles.  This function isn't in use so I can't be sure it works
	/// </summary>
	/// <param name="points"></param>
	/// <param name="points2"></param>
	/// <returns></returns>
	public static Matrix4x4 UnitTranslate(Vector3[] points, Vector3[] points2) {
		if(points.Length > 2) {
			//Lifted from http://stackoverflow.com/questions/3780493/map-points-between-two-triangles-in-3d-space/4679651#4679651
			Matrix4x4 m;
			Matrix4x4 t1 = Matrix4x4.identity;
			Matrix4x4 t2 = Matrix4x4.identity;

			Vector3 v1 = points2[0];
			Vector3 v2 = points2[1];
			Vector3 v3 = points2[2];
			Vector3 v4 = v1 + Vector3.Cross(v2 - v1, v3 - v1);

			Vector3 v1p = points[0];
			Vector3 v2p = points[1];
			Vector3 v3p = points[2];
			Vector3 v4p = v1p + Vector3.Cross(v2p - v1p, v3p - v1p);

			t1.SetColumn(0, Pad(v1));
			t1.SetColumn(1, Pad(v2));
			t1.SetColumn(2, Pad(v3));
			t1.SetColumn(3, Pad(v4));

			t2.SetColumn(0, Pad(v1p));
			t2.SetColumn(1, Pad(v2p));
			t2.SetColumn(2, Pad(v3p));
			t2.SetColumn(3, Pad(v4p));

			m = t2 * t1.inverse;
			return m;
		}
		return Matrix4x4.identity;
	}
	public static void SetActive(GameObject go, bool active) { SetActive(go.transform, active); }
	public static void SetActive(Transform t, bool active) {
		t.gameObject.SetActive(active);
		foreach(Transform child in t) {
			SetActive(child, active);
		}
	}
	/// <summary>
	/// Add the item at a certain index, expanding the list if the index exceeds the current maximum
	/// </summary>
	/// <typeparam name="T">The type for the list and item</typeparam>
	/// <param name="list">The list to which we add the item</param>
	/// <param name="item">The item we're adding</param>
	/// <param name="index">The index at which we add the item</param>
	/// <param name="filler">What to fill with if we expand the list</param>
	public static void AddAtIndex<T>(List<T> list, T item, int index, T filler) {
		if(list.Count > index)
			list[index] = item;
		else {
			for(int i = list.Count; i < index; i++)
				list.Add(filler);
			list.Add(item);
		}
	}
	/// <summary>
	///  Add the item at a certain index, expanding the array if the index exceeds the current maximum
	/// </summary>
	/// <typeparam name="T">The type for the list and item</typeparam>
	/// <param name="array">The array to which we add the item</param>
	/// <param name="item">The item we're adding</param>
	/// <param name="index">The index at which we add the item</param>
	/// <param name="filler">What to fill with if we expand the array</param>
	public static void AddAtIndex<T>(ref T[] array, T item, int index, T filler) {
		if(array.Length > index)
			array[index] = item;
		else {
			T[] newArray = new T[index + 1];
			array.CopyTo(newArray, 0);
			for(int i = array.Length; i < index; i++)
				newArray[i] = filler;
			array = newArray;
			Debug.Log(item);
			array[index] = item;
		}
	}
	
	public static string TruncateString(string str, int length){
		if(str.Length > length)
			return str.Substring(0, length - 1);
		else
			return str;
	}
	
	public static string TruncateFloatString(string str, int lengthAfterDecimal){
		string[] strPieces = str.Split('.');
		if(strPieces.Length > 1 && strPieces[1].Length > lengthAfterDecimal)
			return strPieces[0] + "." + strPieces[1].Substring(0, lengthAfterDecimal);
		else
			return str;
	}


	public static Bounds MetaBounds(Transform t) {
		if(t.collider)
			return t.collider.bounds;
		Bounds b = new Bounds(t.position, Vector3.zero);
		foreach(Transform child in t) {
			Bounds tmp = MetaBounds(child);
			b.Encapsulate(tmp.min);
			b.Encapsulate(tmp.max);
		}
		return b;
	}
	public static void SetLayerRecursively(GameObject g, int layer) {
		g.layer = layer;
		foreach(Transform child in g.transform)
			SetLayerRecursively(child.gameObject, layer);
	}
}