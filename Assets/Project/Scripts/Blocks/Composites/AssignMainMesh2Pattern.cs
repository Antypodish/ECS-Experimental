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
    [UpdateAfter ( typeof ( ReleasePatternBarrier ) ) ]
    // [UpdateAfter ( typeof ( LodPatternSwitchBarrier ) ) ]
    class AssignMainMesh2Pattern : JobComponentSystem
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
            public ComponentDataArray <Blocks.Pattern.RequestPatternMainMeshSetupTag> a_requestPatternSetupTag ; // this tag is removed in this system
            public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ; // this tag is removed in this system

            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_isLodSwitchedTag ;
            
           // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        /*
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
        */
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
            var reqJobHandle = new AssignMainMesh2PatternGroupJob
            {                
                commandBuffer = commandBuffer,

                requestPatternSetupData = requestPatternSetupData,
                // a_spareCompoisteEntities = spareCompositeData.a_entities,
            } ;
                
            reqJobHandle.Schedule (inputDeps).Complete () ;           
                
            return inputDeps ;
            
        }


       /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct AssignMainMesh2PatternGroupJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            // [WriteOnly] public bool isExecutedBool ;
            
            public EntityCommandBuffer commandBuffer ;

            // [ReadOnly] public EntityArray a_entities;
            // [ReadOnly] public ComponentDataArray <BlockSetHighlightTag> a_setBlockHighlight ;
            
            public RequestPatternSetupData requestPatternSetupData ; // primary
            // public SpareCompositeData spareCompositeData ; // secondary
            // public EntityArray a_spareCompoisteEntities ;

            public void Execute ()
            {
                for (int i_patternGroupIndex = 0; i_patternGroupIndex < requestPatternSetupData.Length; ++i_patternGroupIndex )
                {

                    Entity paternGroupEntity = requestPatternSetupData.a_entities [i_patternGroupIndex] ;
                    commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternMainMeshSetupTag> ( paternGroupEntity ) ;
                    commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( paternGroupEntity ) ;

                }
                /*
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
                */
            }

        } // job

    }
}

