using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Blocks
{
    // public class BarrierA : BarrierSystem {}

    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierB))]
    public class BlocksCompositeSystem : JobComponentSystem
    {        
        // Targets from which distance of LOD is calcualted
        [Inject] private CompositeData compositeData ;   
                
        struct CompositeData
        {
            public readonly int Length ;

            public EntityArray a_entities ;


            public ComponentDataArray <Common.Components.DisableSystemTag> a_disableTag ;
        }

        /*
       [Inject] private Lod01Data CompositeData ;      

        struct Lod01Data
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public BufferArray <ECS.Common.Components.FloatBuffernElementComponent> a_floatBuffernElementComponent ;

            // [ReadOnly] public ComponentDataArray <Position> a_position ;            
            public ComponentDataArray <Position> a_position ; // temporarly withouth [ReadOnly]
            
            public ComponentDataArray <ECS.Common.Components.LodComponent> a_lod ;
            
            public ComponentDataArray <ECS.Common.Components.Lod01Tag> a_lodTag ;


            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }
        // [Inject] private SharedComponentDataArray <Common.Components.Half3SharedComponent> a_lodTargetPosition ;
        [Inject] private Lod02Data lod02Data ;     

        struct Lod02Data
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            // [ReadOnly] public ComponentDataArray <Position> a_position ;            
            public ComponentDataArray <Position> a_position ; // temporarly withouth [ReadOnly]   

            public ComponentDataArray <ECS.Common.Components.LodComponent> a_lod ;
            
            public ComponentDataArray <ECS.Common.Components.Lod02Tag> a_lodTag ;

            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }
        */
        // 
        [Inject] private Barrier lodBarrier ;

        static private EntityArchetype archetype ;

        protected override void OnCreateManager ( int capacity )
        {
            // EntityCommandBuffer commandBuffer = lodBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            archetype = entityManager.CreateArchetype (
                
                // typeof (ComponentDataArray <Position>),            
                // typeof (Position),            
                // typeof (Common.Components.LodComponent),
                // typeof (SharedComponentDataArray <Common.Components.Half3SharedComponent>)
                // typeof (Common.Components.Half3SharedComponent),
                typeof (Common.BufferElements.FloatBufferElement) 
            ) ;

            Entity entity = entityManager.CreateEntity ( archetype ) ;
            // entityManager.AddBuffer <Common.Components.FloatBuffernElementComponent> ( entity ) ;
            // entityManager.GetBufferDataFromEntity <Common.BufferElements.FloatBufferElement> ( 0 ) ;
            //EntityCommandBuffer ecb = new EntityCommandBuffer () ;
            //ecb.SetBuffer <Common.BufferElements.FloatBufferElement> ( entity ) ;

            // ecb.SetBuffer <NativeArraySharedValues <Common.Components.FloatBuffernElementComponent>> ( entity ) ;
            //NativeArraySharedValues <float> nativeArraySharedValues ;
            //SharedComponentDataArray <Common.SharedComponents.FloatSharedComponent> a_f_shared ;

            /*
            Entity entity = entityManager.CreateEntity ( typeof ( Common.Components.Float3Component ), typeof ( Common.Components.LodTargetTag ) ) ;
            entityManager.SetComponentData ( entity, new Common.Components.Float3Component { 
                f3 = new half3 ( 2, 0, 0 ) 
            } ) ;

            entity = entityManager.CreateEntity ( typeof ( Common.Components.Float3Component ), typeof ( Common.Components.LodTargetTag ) ) ;
            entityManager.SetComponentData ( entity, new Common.Components.Float3Component { 
                f3 = new half3 ( -1, 0, 0 ) 
            } ) ;



            entity = entityManager.CreateEntity ( archetype ) ;
            entityManager.AddComponentData ( entity, new ECS.Common.Components.Lod01Tag { } ) ;
            // ComponentDataArray<Components.IntComponent> a_meshID = new ComponentDataArray<Components.IntComponent> () ; // ( 50, Allocator.Persistent) ;
            
            entityManager.SetComponentData ( entity, new Common.Components.LodComponent {  
                i4_meshID                       = new int4 ( 0, 1, 2, 3 ),
                f4_switch2NextLodDistance       = new float4 ( 1, 2, 0, 0 ), // distance, at which next LOD is triggerend
                f4_switch2PreviousLodDistance   = new float4 ( 1.5f, 0.5f, 0, 0 ), // distance, at which previous LOD is triggerend
                
                i_triggerID = 0                 
            }) ;

            entityManager.SetComponentData ( entity, new Position { Value = new float3 ( 2.5f, 0, 0 ) } ) ;
            
            Debug.Log ( "comm") ;
            */

            base.OnCreateManager ( capacity );
        }
        
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct CompositeJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent commandsBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;            
            // public Lod02Data data ;  
            [ReadOnly] public CompositeData data ;

            public BufferDataFromEntity <Common.BufferElements.FloatBufferElement> options ;
            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {

                for ( int i_index = 0; i_index < options [data.a_entities [i]].Length; i_index ++ )
                {
                    // options [i_index].Add
                }

                /*
                Debug.Log ( "Switch Lod02 for entity #" + data.a_entities [i].Index ) ;
                
                 Common.Components.LodComponent lodData = data.a_lod [i] ;

                int i_triggerID = lodData.i_triggerID ;

                float3 f3_targetPosition = targetsData.a_lodTargetPosition [0].f3 ;                               
                float3 f3_position = data.a_position [i].Value ;

                float3 h3_positionDiff = new float3 ( 
                    f3_targetPosition.x - f3_position.x, 
                    f3_targetPosition.y - f3_position.y, 
                    f3_targetPosition.z - f3_position.z );
                
                float f_distance = UnityEngine.Mathf.Sqrt ( 
                    h3_positionDiff.x * h3_positionDiff.x +
                    h3_positionDiff.y * h3_positionDiff.y + 
                    h3_positionDiff.z * h3_positionDiff.z 
                ) ;

                Debug.Log ( f_distance ) ;
                float f_switch2NextLodDistance = lodData.f4_switch2NextLodDistance.y ;
                float f_switch2PreviousLodDistance = lodData.f4_switch2PreviousLodDistance.y ;
                            
                // test
                // Position pos = data.a_position [i] ;
                f3_position -= new float3 ( 0.01f, 0, 0 ) ;
                // Entity entity = data.a_entities [i] ;
                //commandsBuffer.SetComponent ( entity, pos ) ;
                data.a_position [i] = new Position () { Value = f3_position } ;

                if ( f_distance >= f_switch2NextLodDistance )
                {
                    // TODO: switch to next LOD
                    // there is no next LOD yet                
                    //commandsBuffer.RemoveComponent <Common.Components.Lod01Tag> ( a_entities [i] ) ;
                    //commandsBuffer.RemoveComponent <Common.Components.Lod02Tag> ( a_entities [i] ) ;
                }
                else if ( f_distance <= f_switch2PreviousLodDistance )
                {
                    // Switch to lower LOD
                    commandsBuffer.RemoveComponent <Common.Components.Lod02Tag> ( a_entities [i] ) ;
                    commandsBuffer.AddComponent <Common.Components.Lod01Tag> ( a_entities [i], new Components.Lod01Tag () ) ;
                }
                */
                
            }                       
        }

        struct SourceData
        {            
            [ReadOnly] public EntityArray a_entities ;
            //[ReadOnly] public ComponentDataArray<Health> Health;
            // [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;
            [ReadOnly] public ComponentDataArray <Position> a_positions ;
            //[ReadOnly] public ComponentDataArray <MassCompnent> a_mass ;
            //[ReadOnly] public ComponentDataArray <grav> a_gravitySource ;  
            //[ReadOnly] public ComponentDataArray <GravitySourceTag> a_gravitySourceTag ;   


        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {

            /// var copyObstaclePositionsJobHandle = copyObstaclePositionsJob.Schedule(obstaclePositions.Length, 2, inputDeps);
            
            var compositeJob = new CompositeJob // for IJobParallelFor
            {      
                options = GetBufferArrayFromEntity<Common.BufferElements.FloatBufferElement>(false),
                // commandsBuffer = lodBarrier.CreateCommandBuffer (),
                data = compositeData,
                // targetsData = targetsData,
                //a_lodTargetPosition = a_lodTargetPosition
            } ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            var mergeCompositeJobHandle = compositeJob.Schedule ( compositeData.Length, 64, inputDeps ) ;

            return mergeCompositeJobHandle ; // for IJobParallelFor

            /*
            var mergeLod01JobHandle = lod01Job.Schedule( lod01Data.Length, 64, inputDeps ) ;

            // return new MoveInstanceJob // for IJob
            var lod02Job = new Lod02Job // for IJobParallelFor
            {
                // commandsBuffer = lodBarrier.CreateCommandBuffer (),
                a_entities = lod02Data.a_entities,
                data = lod02Data,
                targetsData = targetsData,
                //a_lodTargetPosition = a_lodTargetPosition
            } ; // .Schedule( lod02Data.Length, 64, inputDeps) ; // IJobParallelFor
            
            var mergeLod02JobHandle = lod02Job.Schedule( lod02Data.Length, 64, mergeLod01JobHandle ) ;

            return mergeLod02JobHandle ; // for IJobParallelFor
            */
        }

    }
    
}

