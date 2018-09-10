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

            public ComponentDataArray <Blocks.PatternComponent> a_compositesInPattern ;
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

        
        // static public BufferArray <ECS.Blocks.BlockCompositeBufferElement> a_blockCompositesPatterns ;
        // static private NativeArray <ECS.Blocks.BlockCompositeBufferElement> a_compositesPatternPrefabs ; // default

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
            
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {            
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

            
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {    
                
                int i_spareCompositesCount = spareCompositeData.Length ;
                
                if ( requestPatternSetupData.Length > 0 )
                {
                    int i_entitiesThatCanBeAssignedCount = UnityEngine.Mathf.FloorToInt ( i_spareCompositesCount / PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ) ;

                    EntityArray a_spareCompoisteEntities = spareCompositeData.a_entities ;

                    // iterate through entities, which request to have pattern data, to be assigned
                    for ( int i_entityIndex = 0; i_entityIndex < i_entitiesThatCanBeAssignedCount; i_entityIndex ++ )
                    {   
                        
                        int i_spareEntitiesOffsetIndex = i_entityIndex * PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                        
                        if ( PatternPrefab.PatternPrefabSystem.a_patternPrefabs.Length >= i_spareEntitiesOffsetIndex + PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup )
                        {
                            // got enough composites
                            // assign now to requested entity
                            _AssignComposites2Pattern ( commandBuffer, requestPatternSetupData, i_entityIndex, a_spareCompoisteEntities ) ;
                        }
                        else
                        {
                            Debug.Log ( "Pattern prefab index is out of range: " + ( i_spareEntitiesOffsetIndex + PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup)  ) ;
                           //  Debug.Log ( "Not enough spare composites, to assign to pattern." ) ;

                            break ;
                        }

                    } // for

                }

                int i_totalRequiredCompositesCount = (requestPatternSetupData.Length) * PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;
                int i_nextEntitiesThatCanBeAssignedCount = i_need2AddCompositesCount / PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ;

                for ( int i_nextEntity2AssignIndex = 0; i_nextEntity2AssignIndex < i_nextEntitiesThatCanBeAssignedCount; i_nextEntity2AssignIndex ++ )
                {               
                    int i_componentsPatternIndex = requestPatternSetupData.a_compositesInPattern [i_nextEntity2AssignIndex].i_patternIndex ;
                    
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

            
            Blocks.PatternComponent pattern = requestPatternSetupData.a_compositesInPattern [i_entityWithPaternIndex] ;
            BufferArray <Common.BufferElements.EntityBuffer> a_patternsStore = requestPatternSetupData.a_entityBuffer ;
            
            int i_patternIndex = pattern.i_patternIndex ;
            int i_patternOffsetIndex = i_patternIndex * PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ;

            Entity requestPatternSetupEntity = requestPatternSetupData.a_entities [i_entityWithPaternIndex] ;
            

            // clear store for each pattern group entity
            a_patternsStore [i_entityWithPaternIndex].Clear () ;

            int i_spareEntitiesOffsetIndex = i_entityWithPaternIndex * PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
            // int i_patterGroupOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;
            // assign composite entity to entity with pattern
            for ( int i_spareEntityIndex = 0; i_spareEntityIndex < PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup; i_spareEntityIndex ++ )
            {
                // get element from patern prefab to copy into group
                int i_compositeInPrefabIndex = i_patternOffsetIndex + i_spareEntityIndex ;
                Blocks.PatternPrefab.CompositeInPatternPrefabComponent patternPrefab = PatternPrefab.PatternPrefabSystem.a_patternPrefabs [ i_compositeInPrefabIndex ] ;

                Entity spareCompositeEntity = a_spareCompositeEntities [i_spareEntitiesOffsetIndex + i_spareEntityIndex] ;
                
                // assign relative references to composite
                Blocks.CompositeComponent composite = new CompositeComponent ()
                {
                     blockEntity = pattern.blockEntity, // assign grand parent entity to composite
                     patternEntity = requestPatternSetupEntity, // assign parent pattern group entity to composite
                     i_inPrefabIndex = i_compositeInPrefabIndex // used prefab
                } ;  

                Common.BufferElements.EntityBuffer spareEntityBuffer = new Common.BufferElements.EntityBuffer () { 
                    entity = spareCompositeEntity 
                } ;
                
                // expand buffer array if is too small
                a_patternsStore [i_entityWithPaternIndex].Add ( spareEntityBuffer ) ;
               
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
            commandBuffer.SetComponent ( compositeEntityBuffer.entity, new Position () { Value = new float3 (1,0,0) }  ) ;
        }
             /*
        static public void _ReleaseCompositesFromPrefab ( int i_prefabIndex )
        {
            int i_prefabMaxIndex = i_prefabIndex * i_compositesCountPerPatternGroup ;
            for ( int i_index = i_prefabIndex; i_index < i_prefabMaxIndex ; i_index ++ )           
            {
                BlockCompositeBufferElement composite = a_compositesPatternPrefabs[i_index] ;
                
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
            // CompositeSystem._ReleaseCompositesFromPrefab () ;
        }
        */

        // Add set of new composites, according to selected pattern group
        static private void _AddNewSpareComposites ( EntityCommandBuffer commandBuffer, int i_patternGroupIndex )
        {
            float3 f3_scale = new float3 ( 1,1,1 ) * 0.1f ;

            // BlockCompositeBufferElement element = a_compositesPatternPrefabs [i_patternGroupIndex] ;
            // Add composites
            for ( int i_index = 0; i_index < PatternPrefab.PatternPrefabSystem.i_compositesCountPerPatternGroup; i_index ++ )           
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



    }
    
}