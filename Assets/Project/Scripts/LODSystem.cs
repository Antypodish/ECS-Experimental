using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Common
{
    // public class BarrierA : BarrierSystem {}

    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierB))]
    public class LODSystem : JobComponentSystem
    {        
        // Targets from which distance of LOD is calcualted
        [Inject] private TargetsData targetsData ;  
                
        struct TargetsData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
                      
            // public ComponentDataArray <ECS.Common.Components.LodComponent> a_lod ;     
            // The position, from where LOD is calculated, based on distance. Is accessesd by ID of an array
            [ReadOnly] public ComponentDataArray <ECS.Common.Components.Float3Component> a_targetPosition ;
            [ReadOnly] public ComponentDataArray <ECS.Common.Components.LodTargetTag> a_lodTargetTag ;
            [ReadOnly] public ComponentDataArray <ECS.Common.Components.IsLodActiveTag> a_isLodActive ;
        }

        [Inject] private Lod01Data lod01Data ;     

        struct Lod01Data
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            // [ReadOnly] public ComponentDataArray <Position> a_position ;            
            public ComponentDataArray <Position> a_position ; // temporarly withouth [ReadOnly]         
            public ComponentDataArray <ECS.Common.Components.Lod01Tag> a_lodTag ;
            [ReadOnly] public ComponentDataArray <ECS.Common.Components.IsLodActiveTag> a_isLodActive ;

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
            public ComponentDataArray <ECS.Common.Components.Lod02Tag> a_lodTag ;
            [ReadOnly] public ComponentDataArray <ECS.Common.Components.IsLodActiveTag> a_isLodActive ;

            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        // 
        [Inject] private Barrier lodBarrier ;

        static private EntityArchetype archetype ;
        // distances for each index per LOD
        static public NativeArray <float> a_switch2NextLodDistances ;
        static public NativeArray <float> a_switch2PreviousLodDistances ;

        protected override void OnCreateManager ( int capacity )
        {
            // assumming up to 10 LOD
            // this can be extended later
            a_switch2NextLodDistances = new NativeArray<float> ( 10, Allocator.Persistent ) ;
            a_switch2PreviousLodDistances = new NativeArray<float> ( 10, Allocator.Persistent ) ;

            a_switch2NextLodDistances [0] = 1 ; // distance, at which next LOD is triggerend
            a_switch2NextLodDistances [1] = 2 ;

            a_switch2PreviousLodDistances [0] = 1.5f ; // distance, at which previous LOD is triggerend
            a_switch2PreviousLodDistances [1] = 0.5f ;

            // EntityCommandBuffer commandBuffer = lodBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            archetype = entityManager.CreateArchetype (
                
                // typeof (ComponentDataArray <Position>),            
                // typeof (Position),            
                typeof ( ECS.Common.Components.Float3Component ),
                //typeof (Common.Components.LodComponent),
                //typeof (SharedComponentDataArray <Common.Components.Half3SharedComponent>)
                // typeof (Common.SharedComponents.Half3SharedComponent),
                typeof ( Common.Components.LodTargetTag ), 
                typeof ( Common.Components.IsLodActiveTag ) 
            ) ;

            /*
            // create test target (soruce from which LOD is calculating a distance)
            Entity entity = entityManager.CreateEntity ( archetype ) ;

            // create test entities to apply LOD
            entity = entityManager.CreateEntity ( 
                typeof ( Position ),
                typeof ( Common.Components.Lod02Tag ), 
                typeof ( Common.Components.IsLodActiveTag )  
            ) ;

            entityManager.SetComponentData ( entity, new Position { Value = new float3 ( 3f, 0, 0 ) } ) ;
            entityManager.AddComponent ( entity, typeof ( Common.Components.EntityComponent ) ) ;

            Common.Components.EntityComponent EntityStruct = new Common.Components.EntityComponent { entity = new Entity() } ;
            entityManager.SetComponentData ( entity, EntityStruct ) ;
            */

            /*
            entity = entityManager.CreateEntity (                 
                typeof ( Position ),
                typeof ( Common.Components.Lod02Tag ),
                typeof ( Common.Components.IsLodActiveTag )                 
            ) ;
            

            entity = entityManager.CreateEntity ( archetype ) ;
            entityManager.AddComponentData ( entity, new ECS.Common.Components.Lod01Tag { } ) ;
            // ComponentDataArray<Components.IntComponent> a_meshID = new ComponentDataArray<Components.IntComponent> () ; // ( 50, Allocator.Persistent) ;
            */
            /*
            entityManager.SetComponentData ( entity, new Common.Components.LodComponent {  
                i4_meshID                       = new int4 ( 0, 1, 2, 3 ),
                f4_switch2NextLodDistance       = new float4 ( 1, 2, 0, 0 ), // distance, at which next LOD is triggerend
                f4_switch2PreviousLodDistance   = new float4 ( 1.5f, 0.5f, 0, 0 ), // distance, at which previous LOD is triggerend
                
                i_triggerID = 0                 
            }) ;
            */
            
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            a_switch2NextLodDistances.Dispose () ;
            a_switch2PreviousLodDistances.Dispose () ;
            base.OnDestroyManager ( );
        }

        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct Lod01Job : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent commandsBuffer ; // concurrent is required for parallel job
            // public EntityCommandBuffer commandsBuffer ;
            // public EntityArray a_entities;
            public Lod01Data data ;          
            [ReadOnly] public TargetsData targetsData ; 
            //[ReadOnly] public ComponentDataArray <Position> a_targetPosition ;
            //[ReadOnly] public float3 f3_targetPosition ;

            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;
            

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {                
                Debug.Log ( "Switch Lod01 for entity #" + data.a_entities [i].Index ) ;
                
                // Common.Components.LodComponent lodData = data.a_lod [i] ;

                // int i_triggerID = lodData.i_triggerID ;

                float3 f3_targetPosition = targetsData.a_targetPosition [i].f3 ; // currently considering only one entity target 
                float3 f3_position = data.a_position [i].Value ;
                float3 f3_positionDiff = f3_targetPosition - f3_position ;
                
                float f_distance = UnityEngine.Mathf.Sqrt ( 
                    f3_positionDiff.x * f3_positionDiff.x +
                    f3_positionDiff.y * f3_positionDiff.y + 
                    f3_positionDiff.z * f3_positionDiff.z 
                ) ;
                
                Debug.Log ( (float)f_distance ) ;
                float f_switch2NextLodDistance = a_switch2NextLodDistances [0] ;
                float f_switch2PreviousLodDistance = a_switch2PreviousLodDistances [0] ;

                // test
                // Position pos = data.a_position [i] ;
                f3_position += new float3 ( 0.01f, 0, 0 ) ;
                data.a_position [i] = new Position () { Value = f3_position } ;
                // Entity entity = data.a_entities [i] ;
                //commandsBuffer.SetComponent ( entity, pos ) ;


                if ( f_distance >= f_switch2NextLodDistance )
                {
                    Entity jobEntity = data.a_entities [i] ;
                    // switch to next LOD
                    commandsBuffer.RemoveComponent <Common.Components.Lod01Tag> ( jobEntity ) ;
                    commandsBuffer.AddComponent <Common.Components.Lod02Tag> ( jobEntity, new Components.Lod02Tag () ) ;
                }
                else if ( f_distance <= f_switch2PreviousLodDistance )
                {
                    // there is no lower LOD
                }
                
            }                       
        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct Lod02Job : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent commandsBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;            
            public Lod02Data data ;  
            [ReadOnly] public TargetsData targetsData ;                
            //[ReadOnly] public ComponentDataArray <Position> a_targetPosition ;
            //[ReadOnly] public float3 f3_targetPosition ;

            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {
                Debug.Log ( "Switch Lod02 for entity #" + data.a_entities [i].Index ) ;
                
                // Common.Components.LodComponent lodData = data.a_lod [i] ;

                //int i_triggerID = lodData.i_triggerID ;

                float3 f3_targetPosition = targetsData.a_targetPosition [i].f3 ; // currently considering only one entity target                        
                float3 f3_position = data.a_position [i].Value ;
                float3 f3_positionDiff = f3_targetPosition - f3_position ;
                
                float f_distance = UnityEngine.Mathf.Sqrt ( 
                    f3_positionDiff.x * f3_positionDiff.x +
                    f3_positionDiff.y * f3_positionDiff.y + 
                    f3_positionDiff.z * f3_positionDiff.z 
                ) ;

                Debug.Log ( f_distance ) ;
                float f_switch2NextLodDistance = a_switch2NextLodDistances [1] ;
                float f_switch2PreviousLodDistance = a_switch2PreviousLodDistances [1] ;
                            
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
                    Entity jobEntity = data.a_entities [i] ;
                    // Switch to lower LOD
                    commandsBuffer.RemoveComponent <Common.Components.Lod02Tag> ( jobEntity ) ;
                    commandsBuffer.AddComponent <Common.Components.Lod01Tag> ( jobEntity, new Components.Lod01Tag () ) ;
                }
                
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
            
            var lod01Job = new Lod01Job // for IJobParallelFor
            {                
                commandsBuffer = lodBarrier.CreateCommandBuffer (),
                data = lod01Data,
                //targetsData = targetsData,
                targetsData = targetsData
                //a_lodTargetPosition = a_lodTargetPosition
            } ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            var mergeLod01JobHandle = lod01Job.Schedule( lod01Data.Length, 64, inputDeps ) ;

            // return new MoveInstanceJob // for IJob
            var lod02Job = new Lod02Job // for IJobParallelFor
            {
                commandsBuffer = lodBarrier.CreateCommandBuffer (),
                data = lod02Data,
                targetsData = targetsData
                //a_lodTargetPosition = a_lodTargetPosition
            } ; // .Schedule( lod02Data.Length, 64, inputDeps) ; // IJobParallelFor
            
            var mergeLod02JobHandle = lod02Job.Schedule( lod02Data.Length, 64, mergeLod01JobHandle ) ;

            return mergeLod02JobHandle ; // for IJobParallelFor

        }

    }
    
}

