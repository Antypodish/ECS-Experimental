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

            public ComponentDataArray <Blocks.PatternComponent> a_patternComponent ;
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

            public ComponentDataArray <Blocks.PatternComponent> a_patternComponent ;
            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternMainMeshSetupTag> a_notSetupTag ;            
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;

            // public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod020Tag> a_lodTag ; // test
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }
        

        [Inject] private SparePatternsData sparePatternsData ;  

        // request to assing pattern
        struct SparePatternsData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_patternComponent ;
            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // entity which is not switching, nor requested to be assigned
            public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            // public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
            // public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;
        }

        [Inject] private SpareCompositeData spareCompositeData ;   
                
        // individual smallest composite of the pattern
        struct SpareCompositeData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;
        }

        /*
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
        */

        // [Inject] private ComponentDataFromEntity <Blocks.PatternComponent> a_spareCompositesComponents ;
        // [Inject] private ComponentDataFromEntity <Blocks.PatternComponent> a_sparePatternComponents ;
        // [Inject] private ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
        
        [Inject] private ComponentDataFromEntity <Blocks.Pattern.Components.IsLodSwitchedTag> a_isEntityHaveSwitchTag ;

        [Inject] private LodPatternSwitchBarrier barrier ;

        // static private float3 f3_moveAbout ;
                
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

            // int sparePatternGroupsCount = sparePatternsData.Length ;

            // UnityEngine.Random.Range ( 0, Pattern.AddPatternPrefabSystem.i_currentPrefabsCount ) ; // get random pattern (temp)

            Unity.Mathematics.Random random = Pattern.AddPatternPrefabSystem._Random ( 1234 ) ;

            var lod010PatternSwitchJobHandle = new Lod010PatternSwitchDataJob // for IJobParallelFor
            {   
                commandBuffer = commandBuffer,
                lodPatternData = lod010PatternData,
                // a_patternComponent = sparePatternsData.a_patternComponent,
                a_sparePatternGroupsEntities = sparePatternsData.a_entities,
                // a_sparePatternComponents = a_sparePatternComponents,
                random = random,                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = lod010PatternSwitchJobHandle.Schedule ( inputDeps ) ;


            var lod020PatternSwitchJobHandle = new Lod020PatternSwitchDataJob // for IJobParallelFor
            {   
                commandBuffer = commandBuffer,
                //lodPatternData = lod020PatternData,  
                a_patternComponent = lod020PatternData.a_patternComponent, 
                a_paternEntities = lod020PatternData.a_entities,     
                //sparePatternsData = sparePatternsData,
                a_sparePaternCompositeEntities = spareCompositeData.a_entities,
                //requestPatternSetupData = requestPatternSetupData,
                a_isEntityHaveSwitchTag = a_isEntityHaveSwitchTag,
                
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
            // public SparePatternsData sparePatternsData ;
            // public ComponentDataArray <Blocks.PatternComponent> a_patternComponent ;
            public EntityArray a_sparePatternGroupsEntities ;
            // public ComponentDataFromEntity <Blocks.PatternComponent> a_sparePatternComponents ;

            public Unity.Mathematics.Random random ;

            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                // Iterate through patterns groups, to move its composites
                for ( int i = 0; i < lodPatternData.Length; i++ )
                {       
                    
                    // Debug.Log ( "Lod switch: " + i ) ;

                    // Assign local position
                    Blocks.PatternComponent patternComponent = lodPatternData.a_patternComponent [i] ;

                    int i_newDepthLevel = -1 ; // Go to deeper depth level, with higher details
                    int i_depthLevelChange = patternComponent.i_lodDepth - i_newDepthLevel ;
                    patternComponent.i_lodDepth = i_newDepthLevel ;
                    // patternComponent.f_localPosition = new float3 ( 1, 0, 0 ) * i + new float3 ( 0, 0.1f, 0 ) ;
                    patternComponent.f3_localPosition += new float3 ( i_depthLevelChange * 0.1f, 0, 0 ) ;
                    lodPatternData.a_patternComponent [i] = patternComponent ;

                    Entity paternEntity = lodPatternData.a_entities [i] ;
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                    // Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;                        
                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.Components.Lod050Tag () ) ; // test only 
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                    
                    commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( paternEntity ) ;
                            
                    /*
                    // Add temp new pattern
                    int i_randomPattern = random.NextInt ( 0, Pattern.AddPatternPrefabSystem.i_currentPrefabsCount ) ; // reandom is temp
                    int i_patternIndex = i_randomPattern ; // reandom is temp
                    float f_baseScale = 2f ;
                    float3 f3_localPosition = patternComponent.f3_localPosition ; // new float3 ( 1, 0, 0 ) * i ;
                    int i_lodDepth = 0 ;
                        
                    // Temp test
                    
                    
                    // Check if got enugh spares pattern groups                    
                    if ( a_sparePatternGroupsEntities.Length > i )
                    {
                        Entity sparePatternsEntity = a_sparePatternGroupsEntities [i] ;

                        // patternComponent = a_sparePatternComponents [sparePatternsEntity] ;
                        // patternComponent = ComponentDataFromEntity <Blocks.PatternComponent> ( a_sparePatternGroupsEntities [i] ) ;
                        // Reuse exisiting spare pattern group
                        // Entity  sparePatternsData.a_entities [i] ;
                        // patternComponent = a_patternComponent [i] ;

                        Debug.Log ( "Switch: #" + i + "; sparePatternsEntity #" + sparePatternsEntity.Index ) ;
                        patternComponent.i_patternIndex = i_randomPattern ;
                        patternComponent.f_baseScale = f_baseScale ;
                        patternComponent.f3_localPosition = f3_localPosition ;
                        patternComponent.i_lodDepth = i_lodDepth ;

                        commandBuffer.SetComponent ( sparePatternsEntity, patternComponent ) ;
                        // a_sparePatternComponents [sparePatternsEntity] = patternComponent ;
                        

                        //paternEntity = sparePatternsData.a_entities [i] ;
                        commandBuffer.AddComponent ( sparePatternsEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;                    
                        commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( sparePatternsEntity ) ;
                    }
                    else
                    {
                        Debug.Log ( "Create new #" + i ) ;

                        // Not enough
                        // Create new pattern group
                        InitializePatternSystem._AddNewPatternSystem ( commandBuffer, i_patternIndex, f_baseScale, f3_localPosition, i_lodDepth  ) ;
                    }
                    */

                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                    
                    // commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( paternEntity ) ;

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
            // public Lod020PatternData lodPatternData ;
            //public SparePatternsData sparePatternsData ;

            public ComponentDataArray <Blocks.PatternComponent> a_patternComponent ;

            public EntityArray a_paternEntities ;
            public EntityArray a_sparePaternCompositeEntities ;
            //public RequestPatternSetupData requestPatternSetupData ;
            public ComponentDataFromEntity <Blocks.Pattern.Components.IsLodSwitchedTag> a_isEntityHaveSwitchTag ;

            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                // Iterate through patterns groups, to move its composites
                for ( int i = 0; i < a_paternEntities.Length; i++ )
                {       
                    
                    Entity patternEntity = a_paternEntities [i] ;
                    commandBuffer.AddComponent ( patternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                    commandBuffer.AddComponent ( patternEntity, new Blocks.Pattern.RequestPatternMainMeshSetupTag () ) ;
                    commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( patternEntity ) ;
                    
                    /*
                    Entity spareCompositeEntity = a_sparePaternEntities [i] ;

                    if ( a_isEntityHaveSwitchTag.Exists ( spareCompositeEntity ) )
                    {
                        commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( paternEntity ) ;
                    }


                    
                    */

                    
                    

                    Blocks.PatternComponent patternComponent = a_patternComponent [i] ;
                    patternComponent.i_prefabIndex = 1 ;
                    int i_compositeInPrefabIndex = 2 ; // temp

                    // This composite is different type as previous composite. 
                // This composite mesh will be scaled, to overlap next composite, if the type is the same.
                // Hence next mesh may be not required, hwen type is < 0
                //if ( compositeInPatternPrefab.i_compositePrefabIndex >= 0 )
                //{   
                    
                    // Assign relative references to composite
                    Blocks.CompositeComponent composite = new CompositeComponent ()
                    {
                         blockEntity = patternComponent.blockEntity, // assign grand parent entity to composite
                         patternEntity = patternEntity, // assign parent pattern group entity to composite
                         i_inPrefabIndex = i_compositeInPrefabIndex // used prefab
                    } ;  

                    Entity spareCompositeEntity = a_sparePaternCompositeEntities [i] ; // index is only temp test
                
                    patternComponent.blockEntity = spareCompositeEntity ;
                  
                    commandBuffer.SetComponent ( spareCompositeEntity, composite ) ;

                    Unity.Rendering.MeshInstanceRenderer renderer ;
                    switch ( patternComponent.i_prefabIndex )
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
                    
                    // Position position = new Position () { Value = patternComponent.f3_localPosition } ;
                    //commandBuffer.SetComponent ( spareCompositeEntity, position ) ;
                                        
                    // Scale scale = new Scale () { Value = patternComponent.f_baseScale * 0.1f } ;
                    Scale scale = new Scale () { Value = new float3 ( 1,1,1 ) * 0.1f * patternComponent.f_baseScale } ;
                    commandBuffer.SetComponent ( spareCompositeEntity, scale ) ;

                    // Store composite back in
                    a_patternComponent [i] = patternComponent ;

                    // Composite entity has been assigned
                    // Now is ready for rendering, or other processing
                    commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( spareCompositeEntity ) ;
                                   
               // }

                

                    /*
                    // Debug.Log ( "Lod switch: " + i ) ;
                        
                    // Assign local position
                    Blocks.PatternComponent patternComponent = lodPatternData.a_patternComponent [i] ;

                    int i_newDepthLevel = 0 ; // Go default depth elvel
                    int i_depthLevelChange = patternComponent.i_lodDepth - i_newDepthLevel ;
                    patternComponent.i_lodDepth = i_newDepthLevel ;
                    patternComponent.f3_localPosition += new float3 ( i_depthLevelChange * 1f, 0, 0 ) ;
                    // patternComponent.f_localPosition = new float3 ( 1, 0, 0 ) * i ;
                    lodPatternData.a_patternComponent [i] = patternComponent ;

                    Entity paternEntity = lodPatternData.a_entities [i] ;
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                    // Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;                        
                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.Components.Lod050Tag () ) ; // test only 
                    commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                    
                    commandBuffer.RemoveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> ( paternEntity ) ;
                    */
                                    
                } // for                
                
            } // execute                     
        } // job
        
    }

    
}

