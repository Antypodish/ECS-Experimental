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
    

    // [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierB))]
    public class CompositeSystem2 : JobComponentSystem
    {     
        
        [Inject] private SpareCompositeData spareCompositeData ;   
                
        // individual smallest composite of the pattern
        struct SpareCompositeData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            // [ReadOnly] public ComponentDataArray <Position> a_position ;
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            // [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;

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
            public ComponentDataArray <Blocks.Pattern.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            
            [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }
        
        // [Inject] private ComponentDataFromEntity

        [Inject] private ComponentDataFromEntity <Common.Components.IsNotAssignedTag> componentDataFromEntity;
        [Inject] private Barrier compositeBarrier ;

        // static private EntityCommandBuffer commandBuffer ;
        // static private EntityManager entityManager ;

        static private EntityArchetype archetype ;
        // static private EntityArchetype requestPatternSetupArchetype ;

        
        // static public BufferArray <ECS.Blocks.BlockCompositeBufferElement> a_blockCompositesPatterns ;
        // static private NativeArray <ECS.Blocks.BlockCompositeBufferElement> a_compositesPatternPrefabs ; // default
        //static EntityManager entityManager ;
        protected override void OnCreateManager ( int capacity )
        {
            Debug.Log ( "Initiate CompositeSystem." ) ;
            // commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;

            //entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
            archetype = EntityManager.CreateArchetype (   
                typeof ( Blocks.CompositeComponent ),
                typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Common.Components.Lod01Tag )

                // typeof ( Position ),
                // typeof ( Common.Components.Lod01Tag )
            ) ;
            
            Debug.Log ( "PAttern System Disabled adding new groups" ) ;

            Debug.Log ( "need reuse released entities" ) ;
            // public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

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

        static int i_jobIterationCounter ; // test
            
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct RequestPatternSetupJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;     
            
            //public NativeArray <Entity> a_e ;
            public SpareCompositeData spareCompositeData ;            
            public RequestPatternSetupData requestPatternSetupData ;
            public ComponentDataFromEntity <Common.Components.IsNotAssignedTag> componentDataFromEntity ;
            
            
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {    
                Debug.Log ( "requestPatternSetupData.Length " + requestPatternSetupData.Length ) ;
                for (int i = 0; i < requestPatternSetupData.a_entities.Length; ++i )
                {
                    Entity entity = requestPatternSetupData.a_entities [i] ;

                    commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( entity ) ; 

                    int i_spareCompositesCount = spareCompositeData.Length ;        
                // Debug.Log ( "i_spareCompositesCount " + i_spareCompositesCount) ;
                
                
       //         if ( requestPatternSetupData.Length > 0 )
        //        {
                     /*                   
                   int i_length = requestPatternSetupData.Length ;
                   for ( int i_entityIndex = 0; i_entityIndex < i_length; i_entityIndex ++ )
                    { 
                        Entity e = requestPatternSetupData.a_entities [i_entityIndex] ;
                
                        commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( e ) ;
                        //entityManager.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> (e) ;

                        Debug.Log ( "-- i " + i_entityIndex) ;
                    } 
                    */


                    int i_totalRequiredCompositesCount = requestPatternSetupData.Length * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
                    int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;

                    Debug.Log ( "requestPatternSetupData.Length " + requestPatternSetupData.Length) ;

                    int i_patternsGroupsThatCanBeAssignedCount = UnityEngine.Mathf.FloorToInt ( i_spareCompositesCount / Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ) ;

                    if ( i_spareCompositesCount > 0 && i_patternsGroupsThatCanBeAssignedCount > 0 )
                    {

                        EntityArray a_spareCompoisteEntities = spareCompositeData.a_entities ;

                        Debug.Log ( "i_entitiesThatCanBeAssignedCount " + i_patternsGroupsThatCanBeAssignedCount) ;

                        // iterate through entities, which request to have pattern data, to be assigned
                        for ( int i_entityIndex = 0; i_entityIndex < i_patternsGroupsThatCanBeAssignedCount; i_entityIndex ++ )
                        {   
                        
                            Debug.Log ( "i_entityIndex: " + i_entityIndex ) ;
                            int i_spareEntitiesOffsetIndex = i_entityIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
                        
                            if ( Pattern.AddPatternPrefabSystem.a_patternPrefabs.Length >= i_spareEntitiesOffsetIndex + Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup )
                            {
                                                                
                                //Debug.Log ( "a_spareCompoisteEntities: " + a_spareCompoisteEntities.Length ) ;
                                // got enough composites
                                // assign now to requested entity
                                commandBuffer = _AssignComposites2Pattern ( commandBuffer, requestPatternSetupData, i_entityIndex, a_spareCompoisteEntities, componentDataFromEntity ) ;                                
                            }
                            else
                            {
                                Debug.Log ( "Pattern prefab index is out of range: " + ( i_spareEntitiesOffsetIndex + Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup)  ) ;
                               //  Debug.Log ( "Not enough spare composites, to assign to pattern." ) ;
                               
                                break ;
                            }
                                                        
                            // Request complete.
                            
                        } // for
                                                
                        
                    }
                    else if ( i_patternsGroupsThatCanBeAssignedCount == 0 )   
                    {

                        // int i_totalRequiredCompositesCount = (requestPatternSetupData.Length) * Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                        
                        //int i_nextPatternGroupsThatCanBeAssignedToCount = i_need2AddCompositesCount / Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                    
                        //Debug.Log ( "i_nextPatternGroupsThatCanBeAssignedToCount " + i_nextPatternGroupsThatCanBeAssignedToCount) ;
                        //for ( int i_nextPatterngGroup2AssignToIndex = 0; i_nextPatterngGroup2AssignToIndex < i_nextPatternGroupsThatCanBeAssignedToCount; i_nextPatterngGroup2AssignToIndex ++ )
                        //{               
                           // Debug.Log ( "- i_nextPatterngGroup2AssignToIndex " + i_nextPatterngGroup2AssignToIndex) ;
                            // int i_componentsPatternIndex = requestPatternSetupData.a_compositesInPattern [i_nextEntity2AssignIndex].i_patternIndex ;
                    
                            // Add required spare composites   
                            // they will be assigned in next job execution
                        _AddNewSpareComposites ( commandBuffer ) ;
                        //commandBuffer.ShouldPlayback = true ;
                            Debug.Log ( "** i_spareCompositesCount " + spareCompositeData.Length) ;

                        //} // for
                    
                    
                        Debug.Log ( ">> i_spareCompositesCount " + spareCompositeData.Length) ;

                        i_jobIterationCounter ++ ;
                        Debug.Log ( "ik " + i_jobIterationCounter) ;

                    }
                    
                
                
                  Debug.Log ( "H: " ) ;  

                } // for loop


            }          
        }

        RequestPatternSetupJob requestPatternSetupDataJobHandle = new RequestPatternSetupJob () ;

        static bool isInitialized = false ;

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {         

            if ( !isInitialized )
            {
                isInitialized = true ;
            
            int i_length = requestPatternSetupData.Length ;
            Debug.Log ( "-- spareCompositeData.Length " + i_length) ;
            
            // EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;

            //NativeArray <Entity> a_e = new NativeArray <Entity> ( 10, Allocator.TempJob ) ;
            //requestPatternSetupData.a_entities.CopyTo ( a_e ) ;
            /*
            for ( int i_entityIndex = 0; i_entityIndex < i_length; i_entityIndex ++ )
            { 
                Entity e = a_e [i_entityIndex] ;
                
                //commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( e ) ;
                entityManager.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> (e) ;

                Debug.Log ( "-- i " + i_entityIndex) ;
            } 

            a_e.Dispose () ;

            return inputDeps ;
            */
            //commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;

            /*
            var compositePatternsJobHandle = new CompositePatternsJob // for IJobParallelFor
            {    
                commandsBuffer = compositeBarrier.CreateCommandBuffer (),
                data = compositePatternsData,
            } ; //.Schedule (inputDeps) ; ;// .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // var mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, mergeLod01JobHandle ) ;
            JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;
            */
            //EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;
            
                Debug.Log ( "M:" ) ;
                var requestPatternSetupDataJobHandle = new RequestPatternSetupJob // for IJobParallelFor
                {    
                    //a_e = a_e,

                    commandBuffer = compositeBarrier.CreateCommandBuffer (),
                    requestPatternSetupData = requestPatternSetupData,
                    spareCompositeData = spareCompositeData, 
                    componentDataFromEntity = componentDataFromEntity,
                
                } ; //.Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

                // JobHandle mergeJobHandle = releasePatternDataJobHandle.Schedule ( inputDeps ) ;
                // requestPatternSetupDataJobHandle.Complete () ;
                Debug.Log ( "O: " ) ;

                //a_e.Dispose () ;

                //commandBuffer.Playback ( entityManager ) ;

                // JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;
                // requestPatternSetupDataJobHandle ;

            }

            JobHandle mergeJobHandle = requestPatternSetupDataJobHandle.Schedule ( inputDeps ) ;

            return mergeJobHandle ;
            

            /*
            return new RequestPatternSetupJob
            {
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                requestPatternSetupData = requestPatternSetupData,

                //commandsBuffer = setBlockHiglightBarrier.CreateCommandBuffer (),

            }.Schedule(inputDeps) ;
        */
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
        static private EntityCommandBuffer _AssignComposites2Pattern ( EntityCommandBuffer commandBuffer, RequestPatternSetupData requestPatternSetupData, int i_entityWithPaternIndex, EntityArray a_spareCompositeEntities, ComponentDataFromEntity<Common.Components.IsNotAssignedTag> componentDataFromEntity )
        {           
            //Entity assignComposites2PatternEntity = assignComposite2PatternData.a_entities [i_entityWithPaternIndex] ;
            //CompositeComponent compositeComponent = assignComposite2PatternData.a_compositeEntityRelatives [i_entityWithPaternIndex] ;

            Blocks.PatternComponent pattern = requestPatternSetupData.a_compositesInPattern [i_entityWithPaternIndex] ;
            BufferArray <Common.BufferElements.EntityBuffer> a_patternsStore = requestPatternSetupData.a_entityBuffer ;
            
            int i_patternIndex = pattern.i_patternIndex ;
            int i_patternOffsetIndex = i_patternIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;

            Entity requestPatternSetupEntity = requestPatternSetupData.a_entities [i_entityWithPaternIndex] ;
            

            // clear store for each pattern group entity
            a_patternsStore [i_entityWithPaternIndex].Clear () ;
            Debug.Log ( "A" ) ;
            int i_spareEntitiesOffsetIndex = i_entityWithPaternIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
            // int i_patterGroupOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;
            // assign composite entity to entity with pattern
            for ( int i_spareEntityIndex = 0; i_spareEntityIndex < Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup; i_spareEntityIndex ++ )
            {
                // get element from patern prefab to copy into group
                int i_compositeInPrefabIndex = i_patternOffsetIndex + i_spareEntityIndex ;
                Blocks.Pattern.CompositeInPatternPrefabComponent patternPrefab = Pattern.AddPatternPrefabSystem.a_patternPrefabs [ i_compositeInPrefabIndex ] ;
                Debug.Log ( "B: " + i_spareEntityIndex ) ;
                // This composite is different type as previous composite. 
                // This composite mesh will be scaled, to overlap next composite, if the type is the same.
                // Hence next mesh may be not required, hwen type is < 0
                if ( patternPrefab.i_compositePrefabIndex >= 0 )
                {   
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

                    MeshInstanceRenderer renderer ;
                    switch ( patternPrefab.i_compositePrefabIndex )
                    {
                        case 1:
                            renderer = Bootstrap.octreeCenter02 ;
                        break ;
                        case 2:
                            renderer = Bootstrap.octreeCenter03 ;
                        break ;
                        case 3:
                            renderer = Bootstrap.octreeCenter04 ;
                        break ;
                        case 4:
                            renderer = Bootstrap.octreeCenter05 ;
                        break ;
                        case 5:
                            renderer = Bootstrap.octreeCenter06 ;
                        break ;
                        case 6:
                            renderer = Bootstrap.octreeCenter07 ;
                        break ;

                        default:
                            renderer = Bootstrap.octreeCenter01 ;
                        break ;
                    }

                    commandBuffer.SetSharedComponent ( spareCompositeEntity, renderer ) ;

                    Position position = new Position () { Value = patternPrefab.f3_position } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, position ) ;

                    Scale scale = new Scale () { Value = patternPrefab.f3_scale } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, scale ) ;

                    // Composite entity has been assigned
                    // Now is ready for rendering, or other processing
                    commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( spareCompositeEntity ) ;

                    /*
                    // ComponentDataFromEntity <Common.Components.IsNotAssignedTag> m_positionFromEntity = new ComponentDataFromEntity<Common.Components.IsNotAssignedTag> () ; 
                    bool isExists = componentDataFromEntity.Exists ( spareCompositeEntity ) ;

                    Debug.Log ( isExists ?"Yes" : "No") ;

                    if ( !isExists )
                    {
                        Debug.LogWarning ( "Error" ) ;
                    }
                    else
                    {
                        
                    }
                    */
                 
                }
                else
                {
                    // Iteration reached composite index in the prefab store, which should be ignored.
                    // Any later index for this prefab should also be ignored, as expanded mesh took its place.
                    break ;
                }
            }

            Debug.Log ( "C" ) ;
            
            //entityManager.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( requestPatternSetupEntity ) ;
            // assigned
            commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( requestPatternSetupEntity ) ;
            Debug.Log ( "D" ) ;
            return commandBuffer ;
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
        static private EntityCommandBuffer _AddNewSpareComposites ( EntityCommandBuffer commandBuffer )
        {
            float3 f3_scale = new float3 ( 1,1,1 ) * 0.1f ;

            Debug.Log ( "i_entitiesThatCanBeAssignedCount " + Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup) ;

            // BlockCompositeBufferElement element = a_compositesPatternPrefabs [i_patternGroupIndex] ;
            // Add composites
            for ( int i_index = 0; i_index < Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup; i_index ++ )           
            {
                Debug.Log ( "i_index " + i_index) ;
                // Debug.Log ( "i_spareCompositesCount " + spareCompositeData.Length) ;

                commandBuffer.CreateEntity ( archetype ) ;
                                
                Test02.AddBlockSystem._AddBlockRequestViaCustomBufferNoNewEntity ( 
                    commandBuffer,
                    //element.f3_position,
                    float3.zero, // none initial position                    
                    f3_scale,
                    float3.zero, new Entity (),
                    new float4 (1,1,1,1)                            
                ) ;
                         
            } // for

            return commandBuffer ;
        }



    }
    
}