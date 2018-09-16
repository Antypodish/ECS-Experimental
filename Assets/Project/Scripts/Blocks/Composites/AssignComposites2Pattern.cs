using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

// TODO: this ssystem probably runs continously. Should run only based on Request of pattern. Not based on available spares
namespace ECS.Blocks.Pattern
{
    // [UpdateAfter ( typeof ( ReleasePatternBarrier ) ) ]
    // [UpdateAfter ( typeof ( LodPatternSwitchBarrier ) ) ]
    class AssignComposites2Pattern : JobComponentSystem
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
            public ComponentDataArray <Blocks.Pattern.RequestPatternSetupTag> a_requestPatternSetupTag ; // this tag is removed in this system
            public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ; // this tag is removed in this system

            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_isLodSwitchedTag ;
            
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
            
            // [ReadOnly] public ComponentDataArray <Common.Components.Lod01Tag> a_compositePatternTag ;

            // public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
        }

        // [Inject] private ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
        [Inject] private Barrier compositeBarrier ;
        

        //static private EntityManager entityManager ;
        //static private EntityArchetype newCompositeSpareArchetype ;

        //static private EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {
            /*
            newCompositeSpareArchetype = EntityManager.CreateArchetype (   
                typeof ( Blocks.CompositeComponent ),
                typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Common.Components.Lod01Tag )
            ) ;
            */

            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {            
            base.OnDestroyManager ( );
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;
             
            //Debug.Log ( "A" ) ;
            var reqJobHandle2 = new AssignComposites2PatternGroupJob
            {                
                commandBuffer = commandBuffer,

                requestPatternSetupData = requestPatternSetupData,
                a_spareCompoisteEntities = spareCompositeData.a_entities,
            } ;
                
            reqJobHandle2.Schedule (inputDeps).Complete () ;           
                
            return inputDeps ;
            
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

                int i_spareCompositesCount = a_spareCompoisteEntities.Length ; 

                // Debug.Log ( "requestPatternSetupData length: " + requestPatternSetupData.Length ) ;
                // Debug.Log ( "i_spareCompositesCount: " + i_spareCompositesCount ) ;

                int i_totalRequiredCompositesCount = requestPatternSetupData.Length * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
                int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;

                // Ensure I have enough spares, before assigning them.
                // Of not enough, wait, untill ienough will be created.
                if ( i_need2AddCompositesCount <= 0 )
                {

                    for (int i_patternGroupIndex = 0; i_patternGroupIndex < requestPatternSetupData.Length; ++i_patternGroupIndex )
                    {
                                            
                        // Debug.Log ( "i_entityIndex: " + i_entityIndex ) ;
                        // get composites spares start index in patterns, for current indexx
                        int i_spareEntitiesOffsetIndex = i_patternGroupIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
                        
                        if ( i_spareCompositesCount >= i_spareEntitiesOffsetIndex + Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup )
                        {
                            // got enough composites
                            // Assign now to requested patern group entity
                            _AssignComposites2Pattern ( commandBuffer, requestPatternSetupData, i_patternGroupIndex, a_spareCompoisteEntities ) ;                              
                        }
                       
                        // assumes assignemnt of spares to pattern group has been successful

                        Entity paternGroupEntity = requestPatternSetupData.a_entities [i_patternGroupIndex] ;
                        commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( paternGroupEntity ) ;
                        commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( paternGroupEntity ) ;

                    } // for

                }
                
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
            int i_patternOffsetIndex = i_patternIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;

            Entity requestPatternSetupEntity = requestPatternSetupData.a_entities [i_patternGroupIndex] ;
            

            // clear store for each pattern group entity
            a_patternsStore [i_patternGroupIndex].Clear () ;
            
            int i_spareEntitiesOffsetIndex = i_patternGroupIndex * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
            // int i_patterGroupOffsetIndex = i_componentsPatternIndex * i_compositesCountPerPatternGroup ;
            // assign composite entity to entity with pattern
            for ( int i_spareEntityIndex = 0; i_spareEntityIndex < Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup; i_spareEntityIndex ++ )
            {
                // get element from patern prefab to copy into group
                int i_compositeInPrefabIndex = i_patternOffsetIndex + i_spareEntityIndex ;
                Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = Pattern.AddPatternPrefabSystem.a_patternPrefabs [ i_compositeInPrefabIndex ] ;

                // This composite is different type as previous composite. 
                // This composite mesh will be scaled, to overlap next composite, if the type is the same.
                // Hence next mesh may be not required, hwen type is < 0
                if ( compositeInPatternPrefab.i_compositePrefabIndex >= 0 )
                {   
                    
                    // Assign relative references to composite
                    Blocks.CompositeComponent composite = new CompositeComponent ()
                    {
                         blockEntity = pattern.blockEntity, // assign grand parent entity to composite
                         patternEntity = requestPatternSetupEntity, // assign parent pattern group entity to composite
                         i_inPrefabIndex = i_compositeInPrefabIndex // used prefab
                    } ;  

                    Entity spareCompositeEntity = a_spareCompositeEntities [i_spareEntitiesOffsetIndex + i_spareEntityIndex] ;
                
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
                                        
                    Scale scale = new Scale () { Value = compositeInPatternPrefab.f3_scale * pattern.f_baseScale } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, scale ) ;

                    // Store composite back in
                    //Pattern.AddPatternPrefabSystem.a_patternPrefabs [ i_compositeInPrefabIndex ] = compositeInPatternPrefab ;

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
                        
            return commandBuffer ;
        }

    }
}

