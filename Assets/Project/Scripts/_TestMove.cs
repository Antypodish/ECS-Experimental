using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _TestMove : MonoBehaviour {

    public Vector3 vel ;
	// Use this for initialization
	void Start () 
    {
		
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		this.transform.position += vel ;
	}
}
