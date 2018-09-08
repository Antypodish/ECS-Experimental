using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cam : MonoBehaviour {

    Camera cam ;

	// Use this for initialization
	void Start () 
    {
		cam = Camera.main ;
	}
	
	// Update is called once per frame
	void FixedUpdate () 
    {
        // ray = IterOrbis.Managers.Cameras.currentCamera.ScreenPointToRay ( Input.mousePosition );
        /*
		Camera cam = Camera.main ;
        //Vector3 mousePos = cam.ViewportToWorldPoint ( Input.mousePosition ) ;

        Vector3 V3 = cam.ScreenToViewportPoint ( Input.mousePosition ) ;
        Debug.Log ( V3 ) ;
        // Vector3 V3 = cam.ViewportToScreenPoint ( Input.mousePosition ) ;
        Ray ray = cam.ViewportPointToRay ( V3 ) ;
        Debug.Log ( "blue " + ray ) ;
        Debug.DrawLine ( ray.origin, ray.direction * 100, Color.blue ) ;
        */

        
        Camera cam = Camera.main ;
        // Debug.Log ( "Mouse Pos " + Input.mousePosition ) ;
        // Debug.Log ( "Mouse Pos : Screen To Vieport " + cam.ScreenToViewportPoint ( Input.mousePosition ) ) ;
        Ray ray = cam.ScreenPointToRay ( Input.mousePosition ) ;
        //Debug.Log ( "Ray " + ray ) ;
        //Debug.DrawLine ( ray.origin, ray.origin + ray.direction * 100, Color.red ) ;

        ECS.Test02.PlayerInputSystem.InputPointerData inputPointerData = ECS.Test02.PlayerInputSystem.inputPointerData ;

        // temp assinment
        inputPointerData.rayCastData .f3_origin = ray.origin ;
        inputPointerData.rayCastData.f3_direction = ray.direction ;

        if ( Input.GetKey ( KeyCode .Space ) )
        {
            ECS.Test02.PlayerInputSystem.inputPointerData = inputPointerData ;
        }
        
        //ray = cam.ViewportPointToRay ( Input.mousePosition ) ;
        //Debug.Log ( "Ray blue" + ray ) ;
        //Debug.DrawLine ( ray.origin, ray.direction * 100, Color.red ) ;
	}
}
