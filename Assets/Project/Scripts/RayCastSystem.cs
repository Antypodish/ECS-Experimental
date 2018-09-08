using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;

namespace ECS.Test02
{
    // check if bounding box is intersecting, and get closest hit point of AABB.
    // AABB ( Axis Alligned Bounding Box)

    // [UpdateAfter(typeof(Barrier))]
    public class RayCastSystem : JobComponentSystem
    {

        struct Data
        {            
            [ReadOnly] public EntityArray a_entities ;
            //[ReadOnly] public ComponentDataArray<Health> Health;
            // [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;
            
            // [ReadOnly] public ComponentDataArray <TransformMatrix> a_transformMatrix ;
            [ReadOnly] public ComponentDataArray <Position> a_positions ;
            [ReadOnly] public ComponentDataArray <AllowRayCastingTag> a_allowRayCastingTag ;   
        }

        static private RayCastComponent raycastData ;

        [Inject] private Data data ;         
        [Inject] private Barrier rayCastBarrier ;
        
        // static private Ray ray ;
        static private bool isEntityRayCasted ;
        static private Entity lastRayCastedEntity ;

        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct RayCastJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            // public bool isBool;
            // public bool isIntersecting ;

            [ReadOnly] public EntityArray a_entities;
            // [ReadOnly] public ComponentDataArray <TransformMatrix> a_transformMatrix ;
            [ReadOnly] public ComponentDataArray <Position> a_position ;
            //[ReadOnly] public ComponentDataArray <Unity.Rendering> a_renderer ;

            
            public EntityCommandBuffer commandsBuffer ;

            public void Execute ()
            {
                float3 f3_lastClosesIntersection = new float3 () ;
                float f_closestSqrDist = 999999999 ;
                Entity rayCastedEntity = new Entity () ;
                bool isAtLeastOneHit = false ;

                float3 f3_hitAABBCenter = new float3 () ;

                for ( int i = 0; i < a_entities.Length; ++i )
                {
                    Entity entity = a_entities [i] ;
                           
                    // float4x4 transformMatrix = a_transformMatrix [i].Value ;
                    //float3 pos = new float3 ( transformMatrix.c3.x, transformMatrix.c3.y, transformMatrix.c3.z ) ;
                    float3 pos = a_position [i].Value ;
                    
                    //Bounds bounds = new Bounds () ;
                    //bounds.center = pos ;
                    //bounds.size = Vector3.one ;

                    Vector3 V3_hitPoint = Vector3.zero ;
                    // isIntersecting = bounds.IntersectRay ( ray ) ;  

                    // AABB bounding
                    float3 f3_boundMin = pos - new float3 (1,1,1) * 0.5f ;
                    float3 f3_boundMax = pos + new float3 (1,1,1) * 0.5f ;

                    // check if bounding box is intersecting, and get closest hit point of AABB.
                    // AABB ( Axis Alligned Bounding Box)

                    float f_closestHitPointSqrDistance = 0 ;
                    bool isIntersecting = TestCollision01._TestAABB_Collision._AABBIntersectionTest ( pos, f3_boundMin, f3_boundMax, raycastData.f3_origin, raycastData.f3_direction * 10000, ref V3_hitPoint, ref f_closestHitPointSqrDistance ) ;
                    
                    if ( isIntersecting )
                    {
                        
                        // Vector3 f3_diff = new float3 ( V3_hitPoint.x, V3_hitPoint.y, V3_hitPoint.z ) - f3_lastClosesIntersection ;
                        // Vector3 V3_diff = V3_hitPoint - new Vector3 ( f3_lastClosesIntersection.x, f3_lastClosesIntersection.y, f3_lastClosesIntersection.z ) ;
                        // Vector3 V3_diff = raycastData.f3_origin - V3_hitPoint ;

                        // float f_sqrDist = V3_diff.sqrMagnitude ;

                        // check for closest hitpoint occurence
                        if ( f_closestHitPointSqrDistance < f_closestSqrDist )
                        {
                            isAtLeastOneHit = true ;

                            f_closestSqrDist = f_closestHitPointSqrDistance ;
                            rayCastedEntity = entity ;
                            f3_lastClosesIntersection = V3_hitPoint ;
                            f3_hitAABBCenter = pos ;
                            
                        }
                                  
                    }
                    
                } // for

                
                if ( isAtLeastOneHit )
                {
                    Debug.Log ( "Hovered Over Entity #" + lastRayCastedEntity.Index ) ;

                    if ( !isEntityRayCasted )
                    {
                        isEntityRayCasted = true ;

                        lastRayCastedEntity = rayCastedEntity ;

                        // prepare for highlighting component
                        commandsBuffer.AddComponent ( rayCastedEntity, new BlockSetHighlightTag () ) ;
                                               
                        // Debug.Log ( "New intersection." ) ;

                    }
                    else if ( rayCastedEntity.Index != lastRayCastedEntity.Index || rayCastedEntity.Version != lastRayCastedEntity.Version )
                    {
                        // prepare for unhighlighting last component
                        commandsBuffer.AddComponent ( lastRayCastedEntity, new BlockResetHighlightTag () ) ;
                                                    
                        lastRayCastedEntity = rayCastedEntity ;

                        // prepare for highlighting new component
                        commandsBuffer.AddComponent ( rayCastedEntity, new BlockSetHighlightTag () ) ;

                        // Debug.Log ( "Different entity intersection" ) ;
                    }

                    // raycastData.f3_hitpoint = f3_lastClosesIntersection ;
                    PlayerInputSystem.inputPointerData.rayCastData.entityHit = rayCastedEntity ;                    
                    PlayerInputSystem.inputPointerData.rayCastData.isHitpoint = true ;
                    PlayerInputSystem.inputPointerData.rayCastData.f3_hitpoint = f3_lastClosesIntersection ;
                    PlayerInputSystem.inputPointerData.rayCastData.f3_objectCenter = f3_hitAABBCenter ;
                    
                }
                // is not intersecting
                // revert previous raycast, if was highlighted
                else if ( !isAtLeastOneHit && isEntityRayCasted )
                {
                    isEntityRayCasted = false ;

                    // prepare for unhighlighting component
                    commandsBuffer.AddComponent ( lastRayCastedEntity, new BlockResetHighlightTag () ) ;

                    f3_lastClosesIntersection = new float3 (0,0,0) ;
                    // raycastData.f3_hitpoint = f3_lastClosesIntersection ;
                    PlayerInputSystem.inputPointerData.rayCastData.f3_hitpoint = f3_lastClosesIntersection ;
                    PlayerInputSystem.inputPointerData.rayCastData.f3_objectCenter = f3_hitAABBCenter ;
                    PlayerInputSystem.inputPointerData.rayCastData.isHitpoint = false ;

                    // Debug.Log ( "Not intersecting" ) ;
                }
                                               
            }           
            
        }
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            PlayerInputSystem.InputPointerData inputPointerData = PlayerInputSystem.inputPointerData ;
            
            raycastData.f3_origin = inputPointerData.rayCastData.f3_origin ;
            raycastData.f3_direction = inputPointerData.rayCastData.f3_direction ;
            raycastData.entityHit = inputPointerData.rayCastData.entityHit ;
            

            Debug.DrawLine ( raycastData.f3_origin, raycastData.f3_origin + raycastData.f3_direction * 100, Color.red ) ;
            
            return new RayCastJob
            {
                //isBool = true,
                a_entities = data.a_entities,
                // a_transformMatrix = data.a_transformMatrix,
                a_position = data.a_positions,

                commandsBuffer = rayCastBarrier.CreateCommandBuffer (),

            }.Schedule(inputDeps) ;

        }
    }
    
}

