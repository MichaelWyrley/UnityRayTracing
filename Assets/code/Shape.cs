using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Shape : MonoBehaviour
{
    public Color colour = Color.white;

    [Range(0.5f,2.5f)]
    public float transmitance = 1.0f;

    // [Range(0.0f,1.0f)]
    public float emission;

    [Range(0.0f,1.0f)]
    public float smoothness;

    public Color specularColour;
    


    public Vector3 Position {
        get {
            return transform.position;
        }
    }

    public Vector3 Scale {
        get {
            return transform.localScale;
        }
    }

    public Quaternion Rotation {
        get {
            return transform.rotation.normalized;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
