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

            // Excludes entities that contain a MeshCollider from the group
            // public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  

            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }

        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;

        //static private EntityArchetype archetype ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;
        

        //static private int i_compositesCountPerPatternGroup = 10 ;
        
        //static private NativeArray <ECS.Blocks.BlockCompositeBufferElement> a_compositesPatternPrefabs ; // default

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
            /*
            var movePatternDataJobHandle = new MovePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                movePatternData = movePatternData,
                random = random
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor
            */
            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( inputDeps ) ;

            var releasePatternDataJobHandle = new ReleasePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                releasePatternData = releasePatternData,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
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
            
            // public EntityArray a_entities;     
            
            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public ReleasePatternData releasePatternData ;

            // public Unity.Mathematics.Random random ;

                      // Blocks.MovePatternComonent
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
                // set as not assigned
                //commandBuffer.AddComponent ( compositeEntityBuffer.entity, new Common.Components.IsNotAssignedTag () ) ;
                // reset position
                //commandBuffer.SetComponent ( compositeEntityBuffer.entity, new Position () { Value = new float3 (2,1,1) }  ) ;

                //Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_compositeEntities [i][i_bufferIndex] ;
                        
                  //      Entity compositeEntity = entityBuffer.entity ;
            }
            
            //BlockCompositeBufferElement blockCompositeBufferElement = CompositeSystem._GetCompositeFromPatternPrefab ( i_prefabIndex ) ;
            //blockCompositeBufferElement.
            // CompositeSystem._ReleaseCompositesFromPrefab ( i_prefabIndex ) ;

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

