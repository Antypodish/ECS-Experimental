using UnityEngine ;
using Unity.Entities ;
// using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Rendering ;

namespace ECS.Test02
{

    public class AddGravitySystem : ComponentSystem
    {
        //static public EntityArchetype objectArchetype;

        struct Data
        {
            [ReadOnly] public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            [ReadOnly] public ComponentDataArray <AddGravitySourceTag> a_addGravityTags ;
            // public ComponentDataArray <EntityComponent> a_entities ;

            //public ComponentDataArray <TransformMatrix> a_transformMatrix ;
            //public ComponentDataArray <Position> a_position ;
            //public ComponentDataArray <PlayerInputComponent> a_playerInputs ;
            //public ComponentDataArray <IsBlockTag> a_isBlock ;

            [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }


        [Inject] private Data data ;

        [Inject] private Barrier addGravityBarrier ;

        static EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {
            commandsBuffer = addGravityBarrier.CreateCommandBuffer () ;

            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            float f_offset = 10 ;

            //_AddGravityRequest ( new float3 ( 0,0,1 ) * 2 ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
            //_AddGravityRequest ( new float3 ( UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value) * f_offset - new float3 ( 1, 1, 1) * f_offset * 0.5f ) ;
        }

        static private EntityCommandBuffer commandsBuffer ;

        protected override void OnUpdate ()
        {
            commandsBuffer = addGravityBarrier.CreateCommandBuffer () ;
            
            for (int i = 0; i < data.Length; ++i)
            {
                // commandsBuffer.DestroyEntity ( blockData.a_entities [i].entity ) ;

                // entityManager.DestroyEntity ( blockData ) ;
                _AddGravity (i);
                
            }
                        
        }

        private void _AddGravity (int i)
        {

            // Entity entity = blockData.a_entities [i].entity ;
            Entity entity = data.a_entities [i] ;
            // RigidTransform rt = new RigidTransform () ;
            // rt = RigidTransform.scale ( 5 ) ;
            // RigidTransform.scale = new System.Func<float, RigidTransform> (5) ;

            float4x4 f4x4 = float4x4.identity ; // set default position/rotation/scale matrix
            // commandsBuffer.AddComponent ( entity, new TransformMatrix { Value = f4x4 } ) ;
            //commandsBuffer.AddComponent ( entity, new RigidTransform { }  ) ;
                        
            commandsBuffer.AddComponent ( entity, new Position { Value = data.a_addGravityTags [i].f3_position } ) ;    

            // tags
            commandsBuffer.AddComponent ( entity, new GravitySourceTag { } ) ;     
            // commandsBuffer.AddComponent ( entity, new AllowRayCastingTag { } ) ;  

            // renderer
            MeshInstanceRenderer renderer = Bootstrap.gravitySourceRenderer ;
            // renderer.material.SetColor ( "_Color", Color.blue ) ;
            commandsBuffer.AddSharedComponent ( entity, renderer ) ;

            //commandsBuffer.SetComponent <> () ;
            commandsBuffer.RemoveComponent <AddGravitySourceTag> ( entity ) ; // block added. Remove tag

        }

        /// <summary>
        /// Call it from whatever place
        /// </summary>
        static public void _AddGravityRequest ( float3 f3_position )
        {
            // Create an entity based on the archetype. It will get default-constructed
            // defaults for all the component types we listed.
            // Entity entity = entityManager.CreateEntity ( objectArchetype ) ;
            Entity entity = entityManager.CreateEntity ( ) ;
            // Entity entity = entityManager.CreateEntity ( typeof ( AddBlockTag ), typeof ( EntityComponent ) ) ;
            // entityManager.AddComponentData ( entity, new EntityStruct { entity = entity } ) ;
            entityManager.AddComponentData ( entity, new AddGravitySourceTag { f3_position = f3_position } ) ; // tag it as new block. This tag will be removed after block added

            /*             
            commandsBuffer = m_RemoveDeadBarrier.CreateCommandBuffer () ;

            commandsBuffer.CreateEntity ( objectArchetype ) ;
            // commandsBuffer.AddComponent ( new EntityComponent () ) ;
            commandsBuffer.AddComponent ( new AddBlockTag () ) ;
            commandsBuffer.AddSharedComponent ( Bootstrap.playerRenderer ) ;
            */
        }


    }
}
