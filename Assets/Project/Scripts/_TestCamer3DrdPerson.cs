/*
// using System.Collections.Generic;
// using Unity.Burst;
// using Unity.Collections;
using Unity.Entities;
// using Unity.Jobs;
// using Unity.Transforms;
// using Unity.Mathematics;
// using Unity.Transforms2D;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;
 
[UpdateAfter(typeof(PreLateUpdate))]
public class CameraSystem : ComponentSystem
{
    public struct Player : IComponentData
    {
    }
 
    public class PlayerComponent : ComponentDataWrapper<Player> { }

    public struct Data
    {
        public readonly int Length;
        public GameObjectArray GameObject;
        public ComponentDataArray<Player> Player;
    }
 
    public class ClipPlanePoints
    {
        public Vector3[] points;
        public float hitDistance;
        public bool didCollide;
    }
 
    [Inject]private Data data;
    public LayerMask collisionLayers;
    private float sensitivity = 200;
    private float inputX;
    private float inputY;
    public float minRotatonY = -30;
    public float maxRotationY = 50;
    private float defaultDistance = 5;
 
    protected override void OnUpdate()
    {
        if (data.Length == 0)
            return;
 
        var dt = Time.deltaTime;
        var transform = Camera.main.transform;
        var target = data.GameObject[0].transform.GetChild(0).transform;
        collisionLayers = ~0;
 
 
        //Rotate
        inputX += Input.GetAxisRaw("RightHorizontal") * dt * sensitivity;
        inputY += Input.GetAxisRaw("RightVertical") * dt * sensitivity;
        inputY = Mathf.Clamp(inputY,minRotatonY,maxRotationY);
        transform.eulerAngles = new Vector3(inputY,inputX);
       
        //Move To Default Position
        transform.position = (target.position) - transform.forward * defaultDistance;
 
        //Collision
        ClipPlanePoints nearClipPlanePoints = GetCameraClipPlanePoints (target);
        DetectCollision(nearClipPlanePoints,target);
 
        //Move To Position based on collision
        transform.position = (target.position) - transform.forward * ((nearClipPlanePoints.didCollide) ? nearClipPlanePoints.hitDistance : defaultDistance);
    }
 
    private ClipPlanePoints GetCameraClipPlanePoints(Transform target)
    {
        //Variables
        ClipPlanePoints clipPlanePoints = new ClipPlanePoints();
        Transform transform = Camera.main.transform;
 
        float length = Camera.main.nearClipPlane;
        float height = Mathf.Tan((Camera.main.fieldOfView) * Mathf.Deg2Rad) * length;
        float width = height * Camera.main.aspect;
        clipPlanePoints.points = new Vector3[5];
 
        //Get Points
        clipPlanePoints.points[0] = (transform.position + transform.forward * length) + (transform.right * width) - (transform.up * height);
        clipPlanePoints.points[1] = (transform.position + transform.forward * length) - (transform.right * width) - (transform.up * height);
        clipPlanePoints.points[2] = (transform.position + transform.forward * length) + (transform.right * width) + (transform.up * height);
        clipPlanePoints.points[3] = (transform.position + transform.forward * length) - (transform.right * width) + (transform.up * height);
        clipPlanePoints.points[4] = (transform.position + transform.forward * length);
   
        return clipPlanePoints;
    }
 
    public void DetectCollision(ClipPlanePoints clipPlanePoints, Transform target)
    {
        RaycastHit hit;
        clipPlanePoints.hitDistance = -1f;
        for(int i = 0; i < clipPlanePoints.points.Length; i++)
        {
            if (Physics.Raycast (target.position, (clipPlanePoints.points[i] - target.position), out hit, Vector3.Distance(target.position,clipPlanePoints.points[i]),collisionLayers))
            {
                Debug.DrawLine (target.position, hit.point, Color.red);
                clipPlanePoints.didCollide = true;
                if (clipPlanePoints.hitDistance < 0 || hit.distance < clipPlanePoints.hitDistance)
                    clipPlanePoints.hitDistance = hit.distance;
            }
            else
                Debug.DrawLine (target.position, clipPlanePoints.points[i], Color.yellow);  
        }          
    }
}
*/