using UnityEngine ;
using Unity.Entities ;
// using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Rendering ;

namespace ECS.Time
{
    public struct TimeIntrvalCustomTag : IComponentData {} ;

    public class TimeIntervalCustomSystem : ComponentSystem
    {
        //static public EntityArchetype objectArchetype;

        struct Data
        {
            [ReadOnly] public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            //[ReadOnly] public ComponentDataArray <TimeIntrvalManagerTag> a_timeIntervalCustomTag ;
            [ReadOnly] public ComponentDataArray <TimeIntrvalCustomTag> a_timeIntervalTag ;
        }


        [Inject] private Data data ;

        [Inject] private Barrier addGravityBarrier ;

        static EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {
            // commandsBuffer = addGravityBarrier.CreateCommandBuffer () ;

            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            // float f_offset = 10 ;

            //_AddGravityRequest ( new float3 ( 0,0,1 ) * 2 ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
        }

        static private EntityCommandBuffer commandsBuffer ;

        protected override void OnUpdate ()
        {
            for (int i = 0; i < data.Length; ++i)
            {
                Entity entity = data.a_entities [i] ;

                entityManager.DestroyEntity ( entity ) ;

                Debug.Log ( "Test: Custom time interval. Based on entity component add/remove." ) ;
            }

            /*
            commandsBuffer = addGravityBarrier.CreateCommandBuffer () ;
            
            for (int i = 0; i < data.Length; ++i)
            {
                // commandsBuffer.DestroyEntity ( blockData.a_entities [i].entity ) ;

                // entityManager.DestroyEntity ( blockData ) ;
                _AddGravity (i);
                
            }
            */
                        
        }
        
    }
}

