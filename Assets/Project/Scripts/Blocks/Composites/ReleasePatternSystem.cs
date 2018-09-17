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
    
    public class ReleasePatternBarrier : BarrierSystem {} // required for conflicts avoidance (race condition)

    //[UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    [UpdateAfter ( typeof ( LodPatternSwitchBarrier ) ) ]
    [UpdateAfter ( typeof ( MoveCompositeBarrier ) ) ] // ensures no conflict
    public class ReleasePatternSystem : JobComponentSystem
    {     
        [Inject] private ReleasePatternData releasePatternData ;  

        // request to assing pattern
        struct ReleasePatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;
            
            public ComponentDataArray <Blocks.MovePattern> a_movePattern ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // release composites from the group and the grup itself
            public ComponentDataArray <Blocks.Pattern.RequestPatternReleaseTag> a_releasePattern ;

        }

        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;
        

        protected override void OnCreateManager ( int capacity )
        {
            //commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            //entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
                 
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            // a_compositesPatternPrefabs.Dispose () ;
            base.OnDestroyManager ( );
        }

        
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {     

            var releasePatternDataJobHandle = new ReleasePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                releasePatternData = releasePatternData,               
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = releasePatternDataJobHandle.Schedule ( inputDeps ) ;

            return mergeJobHandle ;
        }
        
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct ReleasePatternDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            
            public ReleasePatternData releasePatternData ;

            
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {       

                for ( int i = 0; i < releasePatternData.Length; i++ )
                {

                    releasePatternData.a_compositeEntities = _ReleaseCompositesFromPatternGroup ( commandBuffer, releasePatternData.a_compositeEntities, i ) ;
                    
                    // released, remove tag
                    commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternReleaseTag> ( releasePatternData.a_entities [i] ) ;
                    
                    // pattern now is not assigned
                    // can be reused later
                    commandBuffer.AddComponent ( releasePatternData.a_entities [i], new Common.Components.IsNotAssignedTag () ) ;

                }
            }                       
        }

        
        static private BufferArray <Common.BufferElements.EntityBuffer> _ReleaseCompositesFromPatternGroup ( EntityCommandBuffer commandBuffer, BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities, int i_prefabIndex )
        {
            // get number of composites in this patter group
            int i_compositesCount = a_compositeEntities [i_prefabIndex].Length ;

            // iterate through owned composites, to detach them
            for ( int i = 0; i < i_compositesCount; i ++ )
            {
                Common.BufferElements.EntityBuffer compositeEntityBuffer = a_compositeEntities [i_prefabIndex][i] ;

                // Set as not assigned
                // And reset position
                _ReleaseCompositesFromPatternRequest ( commandBuffer, compositeEntityBuffer ) ;
            }

            // fiinally clear store of detached compoenents
            a_compositeEntities [i_prefabIndex].Clear () ;

            return a_compositeEntities ;
        }
        
         /// <summary>
        /// Set as not assigned
        /// And reset position
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <param name="compositeEntityBuffer"></param>
        static public void _ReleaseCompositesFromPatternRequest ( EntityCommandBuffer commandBuffer, Common.BufferElements.EntityBuffer compositeEntityBuffer )
        {            
            // set as not assigned
            commandBuffer.AddComponent ( compositeEntityBuffer.entity, new Common.Components.IsNotAssignedTag () ) ;
            // reset position
            commandBuffer.SetComponent ( compositeEntityBuffer.entity, new Position () { Value = new float3 (0,5,5) }  ) ;

            commandBuffer.SetComponent ( compositeEntityBuffer.entity, new Scale () { Value = new float3 (1,1,1) * 0.1f }  ) ;
        }
    }

    
}

