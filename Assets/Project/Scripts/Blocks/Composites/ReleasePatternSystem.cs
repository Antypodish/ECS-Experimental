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
    
    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    [UpdateAfter(typeof(MoveCompositeBarrier))]
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
            public ComponentDataArray <Blocks.PatternPrefab.RequestPatternReleaseTag> a_releasePattern ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.RequestPatternSetupTag> a_notSetupTag ;

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
                    commandBuffer.RemoveComponent <Blocks.PatternPrefab.RequestPatternReleaseTag> ( releasePatternData.a_entities [i] ) ;
                    
                    commandBuffer.AddComponent ( releasePatternData.a_entities [i], new Common.Components.IsNotAssignedTag () ) ;

                    /*
                    CompositePatternComponent compositePatternComponent = movePatternData.a_compositePatternComponent [i] ;
                    int i_ComponentsPatternIndex = compositePatternComponent.i_componentsPatternIndex ; 

                    MovePatternComonent movePattern = movePatternData.a_movePattern [i] ;
                    int i_entityBufferCount = movePatternData.a_entityBuffer [i].Length ;
                                        
                    movePattern.f3_position += new float3 ( random.NextFloat ( -0.01f, 0.01f ) ,0,0 ) ;
                    movePatternData.a_movePattern [i] = movePattern ; // update

                    for ( int i_bufferIndex = 0; i_bufferIndex < i_entityBufferCount; i_bufferIndex ++)
                    {
                        
                        Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_entityBuffer [i][i_bufferIndex] ;
                        
                        Entity compositeEntity = entityBuffer.entity ;
                        if ( compositeEntity.Index != 0 )
                        {
                            Blocks.CompositeComponent compositeComponent = entityManager.GetComponentData <Blocks.CompositeComponent> ( entityBuffer.entity ) ;
                            BlockCompositeBufferElement blockCompositeBufferElement = CompositeSystem._GetCompositeFromPatternPrefab ( compositeComponent.i_inPrefabIndex ) ;
                        
                            Position position = new Position () ;
                            position.Value = blockCompositeBufferElement.f3_position + movePattern.f3_position ;
                            commandBuffer.SetComponent ( compositeEntity, position ) ;
                        }
                    }
                    
                    Debug.Log ( "Empty Exe" ) ; 
                    */
                }
                
                
            }                       
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
                CompositeSystem._ReleaseCompositesFromPatternRequest ( commandBuffer, compositeEntityBuffer ) ;
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
        
    }

    
}

