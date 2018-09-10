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
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.
    
    public class MoveCompositeBarrier : BarrierSystem {}

    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(ReleaseCompositeBarrier))]
    public class MovePatternCompositesSystem : JobComponentSystem
    {     
       
        [Inject] private MovePatternData movePatternData ;  

        // request to assing pattern
        struct MovePatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;
            
            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }
        
        [Inject] private ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
        [Inject] private MoveCompositeBarrier compositeBarrier ;
                
        protected override void OnCreateManager ( int capacity )
        {
            // commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            // entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
                        
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            // a_compositesPatternPrefabs.Dispose () ;
            base.OnDestroyManager ( );
        }

        // Forum topic discussing, why using IJob, rather IJObPrallelFor for BufferArray
        // https://forum.unity.com/threads/how-can-i-improve-or-jobify-this-system-building-a-list.547324/#post-3614746
        // Prevents potential race condition, of writting into same entities, form differnet prallel jobs
        // August 2018

            

            
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct MovePatternCompositesDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityManager entityManager ;
            // public EntityArray a_entities;     
            
            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public MovePatternData movePatternData ;

            public Unity.Mathematics.Random random ;

            public ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;

                      // Blocks.MovePatternComonent
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                
                // iterate through patterns group, to move its composites
                for ( int i = 0; i < movePatternData.Length; i++ )
                {                    

                    PatternComponent compositePatternComponent = movePatternData.a_compositePatternComponent [i] ;
                    int i_ComponentsPatternIndex = compositePatternComponent.i_patternIndex ; 

                    MovePattern movePattern = movePatternData.a_movePatterns [i] ;
                    int i_entityBufferCount = movePatternData.a_compositeEntities [i].Length ;
                                        
                    movePattern.f3_position += new float3 ( random.NextFloat ( -0.01f, 0.01f ) ,0,0 ) ;
                    movePatternData.a_movePatterns [i] = movePattern ; // update

                    // iterate through pattern's group composites
                    for ( int i_bufferIndex = 0; i_bufferIndex < i_entityBufferCount; i_bufferIndex ++)
                    {
                        
                        Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_compositeEntities [i][i_bufferIndex] ;
                        
                        Entity compositeEntity = entityBuffer.entity ;

                        if ( compositeEntity.Index != 0 )
                        {   
                            // get composite data
                            Blocks.CompositeComponent compositeComponent = a_compositeComponents [compositeEntity] ;                            
                            Blocks.Pattern.CompositeInPatternPrefabComponent blockCompositeBufferElement = Pattern.PatternPrefabSystem._GetCompositeFromPatternPrefab ( compositeComponent.i_inPrefabIndex ) ;
                        
                            // move composite
                            Position position = new Position () ;
                            position.Value = blockCompositeBufferElement.f3_position + movePattern.f3_position ;
                            commandBuffer.SetComponent ( compositeEntity, position ) ;
                        }
                    } // for
                    
                } // for                
                
            }                       
        }
        
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {            
            // Unity.Mathematics.Random random = AssignCompositePatternSystem._Random () ;

            /*
            var compositePatternsJobHandle = new CompositePatternsJob // for IJobParallelFor
            {    
                commandsBuffer = compositeBarrier.CreateCommandBuffer (),
                data = compositePatternsData,
            } ; //.Schedule (inputDeps) ; ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // var mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, mergeLod01JobHandle ) ;
            JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;
            */
            
            var movePatternDataJobHandle = new MovePatternCompositesDataJob // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                movePatternData = movePatternData,
                random = Pattern.PatternPrefabSystem._Random ()
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( inputDeps ) ;
            /*
            var releasePatternDataJobHandle = new ReleasePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                releasePatternData = releasePatternData,
                random = random
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            mergeJobHandle = releasePatternDataJobHandle.Schedule ( mergeJobHandle ) ;
            */
            return mergeJobHandle ;
        
            // var mergeCompositeJobHandle = compositeJob.Schedule ( compositeData.Length, 64, inputDeps ) ;

            // return mergeCompositeJobHandle ; // for IJobParallelFor

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

