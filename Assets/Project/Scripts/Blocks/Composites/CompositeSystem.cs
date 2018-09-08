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
    public class CompositeSystem : JobComponentSystem
    {     
        
        [Inject] private SpareCompositeData spareCompositeData ;   
                
        // individual smallest composite of the pattern
        struct SpareCompositeData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            // [ReadOnly] public ComponentDataArray <Position> a_position ;
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            
            // [ReadOnly] public ComponentDataArray <Common.Components.Lod01Tag> a_compositePatternTag ;
        }

        [Inject] private RequestPatternSetupData requestPatternSetupData ;  

        // request to assing pattern
        struct RequestPatternSetupData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.CompositePatternComponent> a_compositePattern ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
        

        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        // static private EntityManager entityManager ;

        static private EntityArchetype archetype ;

        [ReadOnly] static public int i_compositesCountPerPatternGroup = 10 ;
        [ReadOnly] static public int i_prefabsCount = 0 ;
        // static public BufferArray <ECS.Blocks.BlockCompositeBufferElement> a_blockCompositesPatterns ;
        static private NativeArray <ECS.Blocks.BlockCompositeBufferElement> a_compositesPatternPrefabs ; // default

        protected override void OnCreateManager ( int capacity )
        {
            //commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
            archetype = entityManager.CreateArchetype (   
                typeof ( Blocks.CompositeComponent ),
                typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Common.Components.Lod01Tag )

                // typeof ( Position ),
                // typeof ( Common.Components.Lod01Tag )
            ) ;
            
            a_compositesPatternPrefabs = new NativeArray<BlockCompositeBufferElement> ( 100, Allocator.Persistent ) ;
            //public ComponentDataArray <Blocks.CompositePatternComponent> a_compositePatternComponent ;
            //public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;
            
            // Entity entity = entityManager.CreateEntity ( ) ; // test
            // ComponentDataArray <ECS.Common.Components.AddNewTag> a_entities = new ComponentDataArray<Common.Components.AddNewTag> () ; //( 10, Allocator.Persistent ) ;

            
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            a_compositesPatternPrefabs.Dispose () ;
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
        struct RequestPatternSetupJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;     
            
            public SpareCompositeData spareCompositeData ;            
            public RequestPatternSetupData requestPatternSetupData ;
            
            // [ReadOnly] public TargetsData targetsData ;

            // public BufferDataFromEntity <Blocks.BlockCompositeBufferElement> a_compositesGroups ;
            //public SharedComponentDataArray <Components.Half3SharedComponent> a_lodTargetPosition ;

            
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {    
                
                int i_spareCompositesCount = spareCompositeData.Length ;
                
                if ( requestPatternSetupData.Length > 0 )
                {
                    int i_entitiesThatCanBeAssignedCount = UnityEngine.Mathf.FloorToInt ( i_spareCompositesCount / i_compositesCountPerPatternGroup ) ;

                    // iterate through entities, which request to have pattern data, to be assigned
                    for ( int i_entityIndex = 0; i_entityIndex < i_entitiesThatCanBeAssignedCount; i_entityIndex ++ )
                    {   
                        // got enough composites
                        // assign now to requested entity
                        _AssignComposites2Pattern ( commandBuffer, requestPatternSetupData, i_entityIndex, spareCompositeData.a_entities ) ;
                    } // for

                }

                int i_totalRequiredCompositesCount = (requestPatternSetupData.Length) * i_compositesCountPerPatternGroup ;
                int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;
                int i_nextEntitiesThatCanBeAssignedCount = i_need2AddCompositesCount / i_compositesCountPerPatternGroup ;

                for ( int i_nextEntity2AssignIndex = 0; i_nextEntity2AssignIndex < i_nextEntitiesThatCanBeAssignedCount; i_nextEntity2AssignIndex ++ )
                {               
                    int i_componentsPatternIndex = requestPatternSetupData.a_compositePattern [i_nextEntity2AssignIndex].i_componentsPatternIndex ;
                    
                    // add required composites   
                    // they will be assigned in next job execution
                    _AddNewSpareComposites ( commandBuffer, i_componentsPatternIndex ) ;
                } // for
                    
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

            var requestPatternSetupDataJobHandle = new RequestPatternSetupJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                requestPatternSetupData = requestPatternSetupData,
                spareCompositeData = spareCompositeData,
                
                
            }.Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;

            return requestPatternSetupDataJobHandle ;
        
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
        


        /// <summary>
        /// Assigns composite patter, to selected entity
        /// </summary>
        /// <param name="entityWithPatern"></param>
        static private void _AssignComposites2Pattern ( EntityCommandBuffer commandBuffer, RequestPatternSetupData requestPatternSetupData, int i_entityWithPaternIndex, EntityArray a_spareCompositeEntities )
        {           
            //Entity assignComposites2PatternEntity = assignComposite2PatternData.a_entities [i_entityWithPaternIndex] ;
            //CompositeComponent compositeComponent = assignComposite2PatternData.a_compositeEntityRelatives [i_entityWithPaternIndex] ;

            
            Blocks.CompositePatternComponent compositePattern = requestPatternSetupData.a_compositePattern [i_entityWithPaternIndex] ;
            BufferArray <Common.BufferElements.EntityBuffer> a_patternsStore = requestPatternSetupData.a_entityBuffer ;
            
            int i_componentsPatternIndex = compositePattern.i_componentsPatternIndex ;
            int i_componentsPatternOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;

            Entity requestPatternSetupEntity = requestPatternSetupData.a_entities [i_entityWithPaternIndex] ;
            

            // clear store for each pattern group entity
            a_patternsStore [i_entityWithPaternIndex].Clear () ;

            int i_spareEntitiesOffsetIndex = i_entityWithPaternIndex * i_compositesCountPerPatternGroup ;
            // int i_patterGroupOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;
            // assign composite entity to entity with pattern
            for ( int i_spareEntityIndex = 0; i_spareEntityIndex < i_compositesCountPerPatternGroup; i_spareEntityIndex ++ )
            {
                // get element from patern prefab to copy into group
                //BlockCompositeBufferElement element = a_compositesPatternPrefabs [i_componentsPatternOffsetIndex + i_spareEntityIndex] ;
                int i_compositeInPrefabIndex = i_componentsPatternOffsetIndex + i_spareEntityIndex ;
                Blocks.BlockCompositeBufferElement patternPrefab = a_compositesPatternPrefabs [ i_compositeInPrefabIndex ] ;
                //element.f3_position = patternPrefab.f3_position ; // assing new position
                //element.i_prefabId = patternPrefab.i_prefabId ;

                Entity spareCompositeEntity = a_spareCompositeEntities [i_spareEntitiesOffsetIndex + i_spareEntityIndex] ;
                
                // assign relative references to composite
                Blocks.CompositeComponent composite = new CompositeComponent ()
                {
                     blockEntity = compositePattern.blockEntity, // assign grand parent entity to composite
                     patternEntity = requestPatternSetupEntity, // assign parent pattern group entity to composite
                     i_inPrefabIndex = i_compositeInPrefabIndex // used prefab
                } ;  

                Common.BufferElements.EntityBuffer spareEntityBuffer = new Common.BufferElements.EntityBuffer () { 
                    entity = spareCompositeEntity 
                } ;
                
                // expand buffer array if is too small
                a_patternsStore [a_patternsStore.Length - 1].Add ( spareEntityBuffer ) ;
               
                commandBuffer.SetComponent ( spareCompositeEntity, composite ) ;

                Position position = new Position () { Value = patternPrefab.f3_position } ;
                commandBuffer.SetComponent ( spareCompositeEntity, position ) ;

                // Composite entity has been assigned
                // Now is ready for rendering, or other processing
                commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( spareCompositeEntity ) ;
            }

            // assigned
            commandBuffer.RemoveComponent <Blocks.RequestPatternSetupTag> ( requestPatternSetupEntity ) ;
        }

        
        static public void _ReleaseCompositesFromPatternRequest ()
        {

        }
               

        // Add set of new composites, according to selected pattern group
        static private void _AddNewSpareComposites ( EntityCommandBuffer commandBuffer, int i_patternGroupIndex )
        {
            float3 f3_scale = new float3 ( 1,1,1 ) * 0.1f ;

            // BlockCompositeBufferElement element = a_compositesPatternPrefabs [i_patternGroupIndex] ;
            // Add composites
            for ( int i_index = 0; i_index < i_compositesCountPerPatternGroup; i_index ++ )           
            {
                commandBuffer.CreateEntity ( archetype ) ;
                                
                Test02.AddBlockSystem._AddBlockRequestViaCustomBufferNoCreateEntity ( 
                    commandBuffer,
                    //element.f3_position,
                    float3.zero, // none pinitial osition
                    f3_scale,
                    float3.zero, new Entity (),
                    new float4 (1,1,1,1)                            
                ) ;
                              
            } // for
        }

        /// <summary>
        /// Size of the array should be of multipler by i_compositesCountPerPatternGroup
        /// </summary>
        /// <param name="a_blockCompositeBufferElement"></param>
        static public int _AddNewPatternPrefab ( NativeArray <BlockCompositeBufferElement> a_blockCompositeBufferElement )
        {
            for ( int i = 0; i < a_blockCompositeBufferElement.Length; i ++ )
            {
                a_compositesPatternPrefabs [ i_prefabsCount * i_compositesCountPerPatternGroup + i ] = a_blockCompositeBufferElement [i] ;                
            }
                        
            i_prefabsCount ++ ;
             
            return i_prefabsCount ;
        }

        static public BlockCompositeBufferElement _GetCompositeFromPatternPrefab ( int i_index )
        {
            BlockCompositeBufferElement blockCompositeBufferElement = a_compositesPatternPrefabs [ i_index ] ;

            return blockCompositeBufferElement ;
        }

    }
    
}