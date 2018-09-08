using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _TestGravity : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
    Vector3 V3_velocity = Vector3.zero ;

	// Update is called once per frame
	void Update () 
    {
        Vector3 sourcePos = Vector3.zero ;
		Vector3 f3_direction = ( this.transform.position - sourcePos ) ;

        V3_velocity -= ( new Vector3 ( ( f3_direction.x < 0 ? -1 : 1 ) * f3_direction.x * f3_direction.x, ( f3_direction.y < 0 ? -1 : 1 ) * f3_direction.y * f3_direction.y, ( f3_direction.z < 0 ? -1 : 1 ) * f3_direction.z * f3_direction.z ) * 0.001f ) ;
        // V3_velocity += ( new Vector3 ( ( f3_direction.x * f3_direction.x), ( f3_direction.y * f3_direction.y ), ( f3_direction.z * f3_direction.z ) ) ) * 0.001f ;
        this.transform.position += V3_velocity ;
	}
}
