using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;

namespace ECS.Time
{
    public struct TimeIntrvalManagerTag : IComponentData {} ;

    // [UpdateBefore(typeof(ECS.Octree.Point.AddBoundingOctreeNodeSystem))]
    // [UpdateBefore(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    public class TimeIntervalSystem : ComponentSystem
    {
        static public EntityArchetype intervalManagerArcheType ;
        [ReadOnly] static public EntityArchetype intervalCustomArcheType ;
        
        struct Data
        {
            public readonly int Length ;
                        
            //[ReadOnly] public EntityArray a_entities ;

            [ReadOnly] public ComponentDataArray <TimeIntrvalManagerTag> a_inputs ;
        }

        [Inject] private Data data ;

        //public struct Data
        //{
           // public InputPointerComponent inputPointer ;            
                       
        //}

        //static public Data data ;

        static private EntityManager entityManager ;
        static private EntityCommandBuffer commandsBuffer ;

        protected override void OnCreateManager ( int capacity )
        {
            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            intervalManagerArcheType = entityManager.CreateArchetype 
            ( 
                typeof ( TimeIntrvalManagerTag ) 
            ) ;

            Entity entity = EntityManager.CreateEntity ( intervalManagerArcheType ) ;

            intervalCustomArcheType = entityManager.CreateArchetype 
            (          
                typeof ( TimeIntrvalCustomTag )
            ) ;

           

            // base.OnCreateManager ( capacity );

            //Entity entity = EntityManager.CreateEntity ( ) ;

            //EntityManager.AddComponent ( entity, typeof ( TimeIntrvalTag ) ) ;
            //EntityManager.AddComponent ( entity, typeof ( Render ) ) ;            
        }

        private float f_nextT ;
        protected override void OnUpdate ()
        {
            // Debug.Log ( "aa" ) ;
            // float dt = Time.deltaTime;

            if ( UnityEngine.Time.time >= f_nextT )
            {
                f_nextT = UnityEngine.Time.time + 5 ;

                Entity entity = EntityManager.CreateEntity ( intervalCustomArcheType ) ;
            }

            
        }
        
    }
}
