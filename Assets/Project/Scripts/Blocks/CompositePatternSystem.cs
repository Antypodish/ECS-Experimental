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
    // [UpdateAfter(typeof(BarrierB))]
    public class CompositePatternSystem : JobComponentSystem
    {     
       
        [Inject] private MovePatternData movePatternData ;  

        // request to assing pattern
        struct MovePatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.CompositePatternComponent> a_compositePatternComponent ;
            //public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;

            public ComponentDataArray <Blocks.MovePatternComonent> a_movePattern ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.RequestPatternSetupTag> a_notSetupTag ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
        
        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;

        static private EntityArchetype archetype ;

        //static private int i_compositesCountPerPatternGroup = 10 ;
        
        //static private NativeArray <ECS.Blocks.BlockCompositeBufferElement> a_compositesPatternPrefabs ; // default

        protected override void OnCreateManager ( int capacity )
        {
            //commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
            archetype = entityManager.CreateArchetype (   
                //typeof ( Blocks.CompositeComponent ),
                //typeof ( Common.Components.IsNotAssignedTag ),
                //typeof ( Common.Components.Lod01Tag )

                // typeof ( Position ),
                // typeof ( Common.Components.Lod01Tag )
                typeof ( Common.BufferElements.EntityBuffer ),
                typeof ( Blocks.RequestPatternSetupTag ),
                typeof ( Blocks.MovePatternComonent )

            ) ;
                        
            int i_componentsPatternPrefabIndex = 0 ;
            i_componentsPatternPrefabIndex = _AddNewPatternPrefab () ;
            Entity entity = entityManager.CreateEntity ( archetype ) ; // store data about composite patterns groups
            entityManager.AddComponentData ( entity, new Blocks.CompositePatternComponent () { 
                i_componentsPatternIndex = i_componentsPatternPrefabIndex 
            } ) ;
            
            i_componentsPatternPrefabIndex = _AddNewPatternPrefab () ;
            entity = entityManager.CreateEntity ( archetype ) ; // store data about composite patterns groups
            entityManager.AddComponentData ( entity, new Blocks.CompositePatternComponent () { 
                i_componentsPatternIndex = i_componentsPatternPrefabIndex 
            } ) ;

            i_componentsPatternPrefabIndex = _AddNewPatternPrefab () ;
            entity = entityManager.CreateEntity ( archetype ) ; // store data about composite patterns groups
            entityManager.AddComponentData ( entity, new Blocks.CompositePatternComponent () { 
                i_componentsPatternIndex = i_componentsPatternPrefabIndex 
            } ) ;
            
                        
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
        struct MovePatternDataJob : IJob
        {
            [ReadOnly] public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            
            // public EntityArray a_entities;     
            
            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public MovePatternData movePatternData ;

                      // Blocks.MovePatternComonent
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {                    
                      
                for ( int i = 0; i < movePatternData.Length; i++ )
                {
                    CompositePatternComponent compositePatternComponent = movePatternData.a_compositePatternComponent [i] ;
                    int i_ComponentsPatternIndex = compositePatternComponent.i_componentsPatternIndex ; 

                    MovePatternComonent movePattern = movePatternData.a_movePattern [i] ;
                    int i_entityBufferCount = movePatternData.a_entityBuffer [i].Length ;

                    for ( int i_bufferIndex = 0; i_bufferIndex < i_entityBufferCount; i_bufferIndex ++)
                    {
                        
                        Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_entityBuffer [i][i_bufferIndex] ;

                        Entity compositeEntity = entityBuffer.entity ;
                        if ( compositeEntity.Index != 0 )
                        {
                            Position position = entityManager.GetComponentData <Position> ( compositeEntity ) ;
                            position.Value += new float3 ( 1,2,3 ) ;
                        }
                    }
                    
                    Debug.Log ( "Empty Exe" ) ; 
                }
                
                
            }                       
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            /*
            var compositePatternsJobHandle = new CompositePatternsJob // for IJobParallelFor
            {    
                commandsBuffer = compositeBarrier.CreateCommandBuffer (),
                data = compositePatternsData,
            } ; //.Schedule (inputDeps) ; ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // var mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, mergeLod01JobHandle ) ;
            JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;
            */

            var movePatternDataJobHandle = new MovePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                movePatternData = movePatternData,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            }.Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;

            return movePatternDataJobHandle ;
        
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
        
        private int _AddNewPatternPrefab ()
        {
            NativeArray <BlockCompositeBufferElement> a_blockCompositeBufferElement = new NativeArray<BlockCompositeBufferElement> ( CompositeSystem.i_compositesCountPerPatternGroup, Allocator.Temp ) ;

            for ( int i = 0; i < CompositeSystem.i_compositesCountPerPatternGroup; i++ )
            {
                BlockCompositeBufferElement blockCompositeBufferElement = new BlockCompositeBufferElement () ;
                blockCompositeBufferElement.f3_position = new float3 (1,1,1) * i * 0.1f + CompositeSystem.i_prefabsCount ;
                // blockCompositeBufferElement.i_prefabId = i_prefabId ;
                a_blockCompositeBufferElement [i] = blockCompositeBufferElement ;
            }

            // assing array to the prefab store
            int i_prefabsCount = CompositeSystem._AddNewPatternPrefab ( a_blockCompositeBufferElement ) ;

            a_blockCompositeBufferElement.Dispose () ;

            // a_compositesPatternPrefabs = new NativeArray<BlockCompositeBufferElement> ( 100, Allocator.Persistent ) ;

            return i_prefabsCount ;
        }
    }
    
}

