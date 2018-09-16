using System;
using System.Collections.Generic;
using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Blocks.Pattern
{
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.
    
    public class LodPatternBarrier : BarrierSystem {} // required for conflicts avoidance (race condition)

    //[UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    [UpdateAfter ( typeof ( MoveCompositeBarrier ) ) ]
    public class LodPatternSystem : JobComponentSystem
    {     

        // Targets from which distance of LOD is calcualted
        [Inject] private TargetsData targetsData ;  
                
        struct TargetsData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
                      
            // public ComponentDataArray <ECS.Common.Components.LodComponent> a_lod ;     
            // The position, from where LOD is calculated, based on distance. Is accessesd by ID of an array
            [ReadOnly] public ComponentDataArray <Common.Components.Float3Component> a_targetPosition ;
            [ReadOnly] public ComponentDataArray <Blocks.Pattern.Components.LodTargetTag> a_lodTargetTag ;
            [ReadOnly] public ComponentDataArray <Blocks.Pattern.Components.IsLodActiveTag> a_isLodActive ;
        }

        [Inject] private Lod01Data lod01Data ;     

        struct Lod01Data
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;

            // [ReadOnly] public ComponentDataArray <Position> a_position ;            
            //public ComponentDataArray <Position> a_position ; // temporarly withouth [ReadOnly]     
            [ReadOnly] public ComponentDataArray <Blocks.PatternComponent> a_compositesInPattern ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod010Tag> a_lodTag ;
            [ReadOnly] public ComponentDataArray <Blocks.Pattern.Components.IsLodActiveTag> a_isLodActive ;

            public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;
            

            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }
        // [Inject] private SharedComponentDataArray <Common.Components.Half3SharedComponent> a_lodTargetPosition ;
        [Inject] private Lod02Data lod02Data ;     

        struct Lod02Data
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;

            // [ReadOnly] public ComponentDataArray <Position> a_position ;            
            //public ComponentDataArray <Position> a_position ; // temporarly withouth [ReadOnly]       
            [ReadOnly] public ComponentDataArray <Blocks.PatternComponent> a_compositesInPattern ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod020Tag> a_lodTag ;
            [ReadOnly] public ComponentDataArray <Blocks.Pattern.Components.IsLodActiveTag> a_isLodActive ;

            public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;

            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        // 
        [Inject] private LodPatternBarrier lodBarrier ;

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

             // distance, at which next LOD is triggerend
            a_switch2NextLodDistances [0] = 3 ;
            a_switch2NextLodDistances [1] = 7 ; // not utilised atm. No more LOD (TODO)

            // distance, at which previous LOD is triggerend
            a_switch2PreviousLodDistances [0] = 0.5f ; // not utilised
            a_switch2PreviousLodDistances [1] = 2.5f ;

            // EntityCommandBuffer commandBuffer = lodBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            // EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            archetype = EntityManager.CreateArchetype (
                
                // typeof (ComponentDataArray <Position>),            
                // typeof (Position),            
                typeof ( Common.Components.Float3Component ),
                //typeof (Common.Components.LodComponent),
                //typeof (SharedComponentDataArray <Common.Components.Half3SharedComponent>)
                // typeof (Common.SharedComponents.Half3SharedComponent),
                typeof ( Blocks.Pattern.Components.LodTargetTag ), 
                typeof ( Blocks.Pattern.Components.IsLodActiveTag ) 
            ) ;



            // temp
            
            // create test target (soruce from which LOD is calculating a distance)
            Entity entity = EntityManager.CreateEntity ( archetype ) ;

            EntityManager.SetComponentData ( entity, new Common.Components.Float3Component { f3 = new float3 ( 0f, 0, 0f ) } ) ;
            /*
            // create test entities to apply LOD
            entity = EntityManager.CreateEntity ( 
                typeof ( Position ),
                typeof ( Blocks.Pattern.Components.Lod010Tag ), 
                typeof ( Blocks.Pattern.Components.IsLodActiveTag )  
            ) ;
            */

            //EntityManager.SetComponentData ( entity, new Position { Value = new float3 ( 0f, 0, 3 ) } ) ;
            //EntityManager.AddComponent ( entity, typeof ( Common.Components.EntityComponent ) ) ;

            //Common.Components.EntityComponent EntityStruct = new Common.Components.EntityComponent { entity = new Entity() } ;
            //EntityManager.SetComponentData ( entity, EntityStruct ) ;
            


            base.OnCreateManager ( capacity );
        }


        protected override void OnDestroyManager ( )
        {
            a_switch2NextLodDistances.Dispose () ;
            a_switch2PreviousLodDistances.Dispose () ;
            base.OnDestroyManager ( );
        }





        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {

            /// var copyObstaclePositionsJobHandle = copyObstaclePositionsJob.Schedule(obstaclePositions.Length, 2, inputDeps);
            
            float3 f3_targetPosition = targetsData.a_targetPosition [0].f3 ;

            var lod010Job = new Lod010Job // for IJobParallelFor
            {                
                commandsBuffer = lodBarrier.CreateCommandBuffer (),
                data = lod01Data,
                //targetsData = targetsData,
                f3_targetPosition = f3_targetPosition
                //a_lodTargetPosition = a_lodTargetPosition
            } ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            var mergeLod01JobHandle = lod010Job.Schedule( lod01Data.Length, 64, inputDeps ) ;

            // return new MoveInstanceJob // for IJob
            var lod020Job = new Lod020Job // for IJobParallelFor
            {
                commandsBuffer = lodBarrier.CreateCommandBuffer (),
                data = lod02Data,
                f3_targetPosition = f3_targetPosition
                //a_lodTargetPosition = a_lodTargetPosition
            } ; // .Schedule( lod02Data.Length, 64, inputDeps) ; // IJobParallelFor
            
            var mergeLod02JobHandle = lod020Job.Schedule( lod02Data.Length, 64, mergeLod01JobHandle ) ;

            return mergeLod02JobHandle ; // for IJobParallelFor

        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct Lod010Job : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent commandsBuffer ; // concurrent is required for parallel job
            // public EntityCommandBuffer commandsBuffer ;
            // public EntityArray a_entities;
            public Lod01Data data ;          
            // [ReadOnly] public TargetsData targetsData ;             
            //[ReadOnly] public ComponentDataArray <Position> a_targetPosition ;
            [ReadOnly] public float3 f3_targetPosition ;

            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;
            

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {                
                
                float3 f3_position = data.a_movePatterns [i].f3_position ;
                float3 f3_positionDiff = f3_targetPosition - f3_position ;
                
                float f_distance = math.sqrt ( math.lengthSquared ( f3_positionDiff ) ) ; // TODO: Optimise sqrt, for lookup
                
                float f_switch2NextLodDistance = a_switch2NextLodDistances [0] ;
                float f_switch2PreviousLodDistance = a_switch2PreviousLodDistances [0] ;

                if ( f_distance >= f_switch2NextLodDistance )
                {
                    Debug.Log ( "Switch Lod020 for entity #" + data.a_entities [i].Index ) ;
                    Debug.Log ( f_distance ) ;

                    Entity jobEntity = data.a_entities [i] ;
                    // switch to next LOD
                    commandsBuffer.RemoveComponent <Blocks.Pattern.Components.Lod010Tag> ( jobEntity ) ;
                    commandsBuffer.AddComponent ( jobEntity, new Blocks.Pattern.Components.Lod020Tag () ) ;
                    commandsBuffer.AddComponent ( jobEntity, new Blocks.Pattern.Components.IsLodSwitchedTag () ) ;
                }
                else if ( f_distance <= f_switch2PreviousLodDistance )
                {
                    // there is no lower LOD
                }
                
                //}

            }                       
        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct Lod020Job : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent commandsBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;            
            public Lod02Data data ;  
            //[ReadOnly] public TargetsData targetsData ;                
            //[ReadOnly] public ComponentDataArray <Position> a_targetPosition ;
            [ReadOnly] public float3 f3_targetPosition ;

            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {
                
                // Common.Components.LodComponent lodData = data.a_lod [i] ;

                //int i_triggerID = lodData.i_triggerID ;

                // float3 f3_targetPosition = targetsData.a_targetPosition [i].f3 ; // currently considering only one entity target                        
                float3 f3_position = data.a_movePatterns [i].f3_position ;
                float3 f3_positionDiff = f3_targetPosition - f3_position ;
                
                float f_distance = math.sqrt ( math.lengthSquared ( f3_positionDiff ) ) ; // TODO: Optimise sqrt, for lookup
                                
                float f_switch2NextLodDistance = a_switch2NextLodDistances [1] ;
                float f_switch2PreviousLodDistance = a_switch2PreviousLodDistances [1] ;
                        
                if ( f_distance >= f_switch2NextLodDistance )
                {
                    // TODO: switch to next LOD
                    // there is no next LOD yet                
                    //commandsBuffer.RemoveComponent <Common.Components.Lod01Tag> ( a_entities [i] ) ;
                    //commandsBuffer.RemoveComponent <Common.Components.Lod02Tag> ( a_entities [i] ) ;
                }
                else if ( f_distance <= f_switch2PreviousLodDistance )
                {
                    Debug.Log ( "Switch Lod020 for entity #" + data.a_entities [i].Index ) ;
                    Debug.Log ( f_distance ) ;

                    Entity jobEntity = data.a_entities [i] ;
                    // Switch to lower LOD
                    commandsBuffer.RemoveComponent <Blocks.Pattern.Components.Lod020Tag> ( jobEntity ) ;
                    commandsBuffer.AddComponent <Blocks.Pattern.Components.Lod010Tag> ( jobEntity, new Blocks.Pattern.Components.Lod010Tag () ) ;
                    commandsBuffer.AddComponent ( jobEntity, new Blocks.Pattern.Components.IsLodSwitchedTag () ) ;
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


    }

    
}

