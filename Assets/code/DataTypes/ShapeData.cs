using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
struct ShapeData {
    public Vector4 colour;
    public float emission;
    public float transmitance;
    public float smoothness;
    public Vector4 specularColour;
    public int triangle_begin;
    public int triangle_count;
    public Vector3 bounds_min;
    public Vector3 bounds_max;

    public ShapeData(Vector4 colour, Vector4 specularColour, float emission, float transmitance, float smoothness, int triangle_begin, int triangle_count, Vector3 bounds_min, Vector3 bounds_max)
	{
        this.colour = colour;
        this.emission = emission;
        this.transmitance = transmitance;
        this.smoothness = smoothness;
        this.specularColour = specularColour;
        this.triangle_begin = triangle_begin;
        this.triangle_count = triangle_count;
        this.bounds_min = bounds_min;
        this.bounds_max = bounds_max;

	}

    public static int GetSize () {
        return sizeof (float) * 17 + sizeof (int) * 2;
    }
}