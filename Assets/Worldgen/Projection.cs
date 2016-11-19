using System;
using System.Collections;
using UnityEngine;

public class Projection {
	double currentRadius = 0;
	double cosLatitude = 0;
	
	public void ensureHasRadius(double centralLatitude) {
		if(cosLatitude < 0.0001)
			cosLatitude = Math.Cos(centralLatitude);
		if(currentRadius < 0.001)
			currentRadius = getLocalRadius(currentRadius);
	}
	
	public double getLocalRadius(double centralLatitude) {
		double equatorialRadius = 6378137.00; //In Meters. taken from Wikipedia
		double polarRadius = 6356752.30; //likewise
		return Math.Sqrt((Math.Pow(Math.Pow(equatorialRadius,2) * Math.Cos(centralLatitude), 2) +
				Math.Pow(Math.Pow(polarRadius,2) * Math.Sin(centralLatitude), 2)) /
			(Math.Pow(equatorialRadius * Math.Cos(centralLatitude), 2) +
				Math.Pow(polarRadius * Math.Sin(centralLatitude), 2)));
	}
	
	public Vector3 getCoordinatesEquiRectangular(double longitude, double latitude) {
		ensureHasRadius(latitude);
		return new Vector3((float)(currentRadius * longitude * cosLatitude), 0.0f, (float)(currentRadius * latitude));
	}
}
