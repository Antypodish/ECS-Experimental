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
    //[UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    class CompositeSystem : JobComponentSystem
    {

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
            
            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        
        [Inject] private SpareCompositeData spareCompositeData ;   
                
        // individual smallest composite of the pattern
        struct SpareCompositeData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            // [ReadOnly] public ComponentDataArray <Position> a_position ;
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;

            // [ReadOnly] public ComponentDataArray <Common.Components.Lod01Tag> a_compositePatternTag ;
        }
        
        // [Inject] private ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
        [Inject] private Barrier compositeBarrier ;
        

        static private EntityManager entityManager ;
        static private EntityArchetype archetype ;

        protected override void OnCreateManager ( int capacity )
        {
            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            archetype = EntityManager.CreateArchetype (   
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
        
        // static bool isAddingSparesCompositesRequested = false ;
        // static bool isAddingSparesCompositesRequested2 = false ;
        static bool isSpareAssigned2PaternBool = false ;
        static bool iSpareBeenAdded = false ;
        static bool iResetInitiated = false ;

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;
             
            
            // Reset 
            // Finalize task
            if ( isSpareAssigned2PaternBool & iSpareBeenAdded )
            {
                //Debug.Log ( "B: " + requestPatternSetupData.Length ) ;
                if ( requestPatternSetupData.Length == 0 )
                {
                    isSpareAssigned2PaternBool = false ; // reset
                    iSpareBeenAdded = false ; // reset
                    iResetInitiated = false ; // reset

                    World.Active.GetOrCreateManager<CompositeSystem>().Enabled = false;
                    World.Active.GetOrCreateManager<EnableCompositeSystem>().Enabled = true ;

                    //Debug.Log ( "Reset A" ) ;
                }
                else if ( !iResetInitiated ) // one shot
                {
                    // reset request patterns tags
                    
                    for (int i_patternGroupIndex = 0; i_patternGroupIndex < requestPatternSetupData.Length; ++i_patternGroupIndex )
                    {
                        commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( requestPatternSetupData.a_entities [i_patternGroupIndex] ) ;
                    }
                        
                    iResetInitiated = true ;
                }

            }
                

            // Add required spares, for later assignment into pattern group.
            if ( !isSpareAssigned2PaternBool && !iSpareBeenAdded )
            {
                //Debug.Log ( "C" ) ;
                var reqJobHandle = new AddRequiredSpareCompositesJob
                {                
                    commandBuffer = commandBuffer,

                    requestPatternSetupData = requestPatternSetupData,
                    a_spareCompoisteEntities = spareCompositeData.a_entities,                        
                } ;
                
                reqJobHandle.Schedule(inputDeps).Complete () ;

                iSpareBeenAdded = true  ;

                return inputDeps ;
            }


            // assign composite to pattern group
            if ( iSpareBeenAdded && spareCompositeData.Length > 0 && !isSpareAssigned2PaternBool )
            {
                //Debug.Log ( "A" ) ;
                var reqJobHandle2 = new AssignComposites2PatternGroupJob
                {                
                    commandBuffer = commandBuffer,

                    requestPatternSetupData = requestPatternSetupData,
                    a_spareCompoisteEntities = spareCompositeData.a_entities,
                } ;
                
                reqJobHandle2.Schedule (inputDeps).Complete () ;           
                
                isSpareAssigned2PaternBool = true ;

                return inputDeps ;
            }
             
            
            return inputDeps ;
            
        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct AddRequiredSpareCompositesJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            //public bool isBool;
            
            public EntityCommandBuffer commandBuffer ;

            // [ReadOnly] public EntityArray a_entities;
            // [ReadOnly] public ComponentDataArray <BlockSetHighlightTag> a_setBlockHighlight ;
            
            public RequestPatternSetupData requestPatternSetupData ; // primary
            // public SpareCompositeData spareCompositeData ; // secondary
            public EntityArray a_spareCompoisteEntities ;

            public bool iSpareBeenAdded2 ;

            public void Execute ()
            {
                EntityArray a_patternEntities = requestPatternSetupData.a_entities ;

                int i_spareCompositesCount = a_spareCompoisteEntities.Length ; 

                Debug.Log ( "aa: " + requestPatternSetupData.Length ) ;
                Debug.Log ( "Spare: " + i_spareCompositesCount ) ;

                int i_totalRequiredCompositesCount = requestPatternSetupData.Length * Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;

                for (int i_newSpareCompositeIndex = 0; i_newSpareCompositeIndex < i_need2AddCompositesCount; ++i_newSpareCompositeIndex )
                {                    
                    _AddNewSpareComposites ( commandBuffer ) ;
                    
                    // Debug.Log ( "z " + iSpareBeenAdded2 ) ;
                                                
                } // for
                
            }

        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct AssignComposites2PatternGroupJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            // [WriteOnly] public bool isExecutedBool ;
            
            public EntityCommandBuffer commandBuffer ;

            // [ReadOnly] public EntityArray a_entities;
            // [ReadOnly] public ComponentDataArray <BlockSetHighlightTag> a_setBlockHighlight ;
            
            public RequestPatternSetupData requestPatternSetupData ; // primary
            // public SpareCompositeData spareCompositeData ; // secondary
            public EntityArray a_spareCompoisteEntities ;

            public void Execute ()
            {
                isSpareAssigned2PaternBool = false ;

                EntityArray a_patternEntities = requestPatternSetupData.a_entities ;

                int i_spareCompositesCount = a_spareCompoisteEntities.Length ; 

                Debug.Log ( "requestPatternSetupData length: " + requestPatternSetupData.Length ) ;
                Debug.Log ( "i_spareCompositesCount: " + i_spareCompositesCount ) ;

                for (int i_patternGroupIndex = 0; i_patternGroupIndex < a_patternEntities.Length; ++i_patternGroupIndex )
                {
                                            
                    // Debug.Log ( "i_entityIndex: " + i_entityIndex ) ;
                    int i_spareEntitiesOffsetIndex = i_patternGroupIndex * Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
                        
                    if ( i_spareCompositesCount >= i_spareEntitiesOffsetIndex + Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup )
                    {
                                                                
                        //Debug.Log ( "a_spareCompoisteEntities: " + a_spareCompoisteEntities.Length ) ;
                        // got enough composites
                        // Assign now to requested patern group entity
                        _AssignComposites2Pattern ( commandBuffer, requestPatternSetupData, i_patternGroupIndex, a_spareCompoisteEntities ) ;        
                        
                        // Entity patternEntity = a_patternEntities [i_patternGroupIndex] ;
                        // commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( patternEntity ) ;
                        
                    }
                       
                } // for
                
            }

        }


        /// <summary>
        /// Assigns composite patter, to selected entity
        /// </summary>
        /// <param name="entityWithPatern"></param>
        static private EntityCommandBuffer _AssignComposites2Pattern ( EntityCommandBuffer commandBuffer, RequestPatternSetupData requestPatternSetupData, int i_patternGroupIndex, EntityArray a_spareCompositeEntities )
        {           
            //Entity assignComposites2PatternEntity = assignComposite2PatternData.a_entities [i_entityWithPaternIndex] ;
            //CompositeComponent compositeComponent = assignComposite2PatternData.a_compositeEntityRelatives [i_entityWithPaternIndex] ;

            Blocks.PatternComponent pattern = requestPatternSetupData.a_compositesInPattern [i_patternGroupIndex] ;
            BufferArray <Common.BufferElements.EntityBuffer> a_patternsStore = requestPatternSetupData.a_entityBuffer ;
            
            int i_patternIndex = pattern.i_patternIndex ;
            int i_patternOffsetIndex = i_patternIndex * Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;

            Entity requestPatternSetupEntity = requestPatternSetupData.a_entities [i_patternGroupIndex] ;
            

            // clear store for each pattern group entity
            a_patternsStore [i_patternGroupIndex].Clear () ;
            
            int i_spareEntitiesOffsetIndex = i_patternGroupIndex * Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup ;
            // int i_patterGroupOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;
            // assign composite entity to entity with pattern
            for ( int i_spareEntityIndex = 0; i_spareEntityIndex < Pattern.PatternPrefabSystem.i_compositesCountPerPatternGroup; i_spareEntityIndex ++ )
            {
                // get element from patern prefab to copy into group
                int i_compositeInPrefabIndex = i_patternOffsetIndex + i_spareEntityIndex ;
                Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = Pattern.PatternPrefabSystem.a_patternPrefabs [ i_compositeInPrefabIndex ] ;
                //Debug.Log ( "B: " + i_spareEntityIndex ) ;

                // This composite is different type as previous composite. 
                // This composite mesh will be scaled, to overlap next composite, if the type is the same.
                // Hence next mesh may be not required, hwen type is < 0
                if ( compositeInPatternPrefab.i_compositePrefabIndex >= 0 )
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
                    a_patternsStore [i_patternGroupIndex].Add ( spareEntityBuffer ) ;
               
                    commandBuffer.SetComponent ( spareCompositeEntity, composite ) ;

                    MeshInstanceRenderer renderer ;
                    switch ( compositeInPatternPrefab.i_compositePrefabIndex )
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

                    Position position = new Position () { Value = compositeInPatternPrefab.f3_position } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, position ) ;

                    Scale scale = new Scale () { Value = compositeInPatternPrefab.f3_scale } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, scale ) ;

                    // Composite entity has been assigned
                    // Now is ready for rendering, or other processing
                    commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( spareCompositeEntity ) ;
                                     
                }
                else
                {
                    // Iteration reached composite index in the prefab store, which should be ignored.
                    // Any later index for this prefab should also be ignored, as expanded mesh took its place.
                    break ;
                }
            }

            // assigned
            // commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( requestPatternSetupEntity ) ;
            
            return commandBuffer ;
        }



        // Add set of new composites, according to selected pattern group
        static private EntityCommandBuffer _AddNewSpareComposites ( EntityCommandBuffer commandBuffer )
        {
            float3 f3_scale = new float3 ( 1,1,1 ) * 0.1f ;

            commandBuffer.CreateEntity ( archetype ) ;
                              
            Test02.AddBlockSystem._AddBlockRequestViaCustomBufferNoNewEntity ( 
                commandBuffer,
                //element.f3_position,
                float3.zero, // none initial position                    
                f3_scale,
                float3.zero, new Entity (),
                new float4 (1,1,1,1)                            
            ) ;

            return commandBuffer ;
        }

    }
}
