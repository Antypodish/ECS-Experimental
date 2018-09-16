using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS.Blocks.Pattern
{
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.

    public class LodPatternSwitchBarrier : BarrierSystem {}

    // [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    [UpdateAfter ( typeof ( LodPatternBarrier ) ) ]
    public class LodPatternSwitchSystem : JobComponentSystem
    {     
       
        [Inject] private Lod010PatternData lod010PatternData ;  

        // request to assing pattern
        struct Lod010PatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;

            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            // public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;

            public ComponentDataArray <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod010Tag> a_lodTag ; // test
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }

        [Inject] private Lod020PatternData lod020PatternData ;  

        // request to assing pattern
        struct Lod020PatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;

            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;

            public ComponentDataArray <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod020Tag> a_lodTag ; // test
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }
        
        
        [Inject] private ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
        [Inject] private LodPatternSwitchBarrier barrier ;
        static private float3 f3_moveAbout ;
                
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

            

             
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {            
                    
            EntityCommandBuffer commandBuffer = barrier.CreateCommandBuffer () ;

            var lod010PatternSwitchJobHandle = new Lod010PatternSwitchDataJob // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                //a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = commandBuffer,
                lodPatternData = lod010PatternData,
                // random = Pattern.AddPatternPrefabSystem._Random ( 1 ),
                // f3_moveAbout = f3_moveAbout,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = lod010PatternSwitchJobHandle.Schedule ( inputDeps ) ;


            var lod020PatternSwitchJobHandle = new Lod020PatternSwitchDataJob // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                //a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = commandBuffer,
                lodPatternData = lod020PatternData,
                // random = Pattern.AddPatternPrefabSystem._Random ( 1 ),
                // f3_moveAbout = f3_moveAbout,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            mergeJobHandle = lod020PatternSwitchJobHandle.Schedule ( mergeJobHandle ) ;

            return mergeJobHandle ;
        
        }

            
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct Lod010PatternSwitchDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job 
            
            public Lod010PatternData lodPatternData ;

            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                // Iterate through patterns groups, to move its composites
                for ( int i = 0; i < lodPatternData.Length; i++ )
                {       
                    
                    Debug.Log ( "Lod switch: " + i ) ;
                        
                    Entity paternEntity = lodPatternData.a_entities [i] ;
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                    // Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;                        
                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.Components.Lod050Tag () ) ; // test only 
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                    
                    commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( paternEntity ) ;
                                        
                } // for                
                
            } // execute                     
        } // job


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct Lod020PatternSwitchDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job 
            
            public Lod020PatternData lodPatternData ;

            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                // Iterate through patterns groups, to move its composites
                for ( int i = 0; i < lodPatternData.Length; i++ )
                {       
                    
                    Debug.Log ( "Lod switch: " + i ) ;
                        
                    Entity paternEntity = lodPatternData.a_entities [i] ;
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                    // Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;                        
                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.Components.Lod050Tag () ) ; // test only 
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                    
                    commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( paternEntity ) ;
                                        
                } // for                
                
            } // execute                     
        } // job
        
    }

    
}

