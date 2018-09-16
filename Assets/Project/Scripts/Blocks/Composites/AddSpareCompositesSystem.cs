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
    class AddSpareCompositesSystem : JobComponentSystem
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
            // public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            
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
        static private EntityArchetype newCompositeSpareArchetype ;

        //static private EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {
            newCompositeSpareArchetype = EntityManager.CreateArchetype (   
                typeof ( Blocks.CompositeComponent ),
                typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Common.Components.Lod01Tag )
            ) ;

            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {            
            base.OnDestroyManager ( );
        }
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;
             

            // Add required spares, for later assignment into pattern group.
            //if ( !isSpareAssigned2PaternBool && !iSpareBeenAdded )
            //{
            float f_compositeScale = 0.1f ;

            //Debug.Log ( "C" ) ;
            var reqJobHandle = new AddRequiredSpareCompositesJob
            {                
                commandBuffer = commandBuffer,

                requestPatternSetupData = requestPatternSetupData,
                i_spareCompositesCount = spareCompositeData.Length,
                //a_spareCompoisteEntities = spareCompositeData.a_entities, 
                    
                f_compositeScale = f_compositeScale,
            } ;
                
            reqJobHandle.Schedule(inputDeps).Complete () ;

            // iSpareBeenAdded = true  ;


            return inputDeps ;
            
        }


        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct AddRequiredSpareCompositesJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {            
            public EntityCommandBuffer commandBuffer ;
            
            public RequestPatternSetupData requestPatternSetupData ; // primary
            [ReadOnly] public int i_spareCompositesCount ;
            // [ReadOnly] public EntityArray a_spareCompoisteEntities ;
            
            [ReadOnly] public float f_compositeScale ;

            public void Execute ()
            {
                EntityArray a_patternEntities = requestPatternSetupData.a_entities ;

                // int i_spareCompositesCount = i_spareCompositesCount ; 

                // Debug.Log ( "aa: " + requestPatternSetupData.Length ) ;
                // Debug.Log ( "i_spareCompositesCount: " + i_spareCompositesCount ) ;

                int i_totalRequiredCompositesCount = requestPatternSetupData.Length * Pattern.AddPatternPrefabSystem.i_compositesCountPerPatternGroup ;
                int i_need2AddCompositesCount = i_totalRequiredCompositesCount - i_spareCompositesCount ;

                for (int i_newSpareCompositeIndex = 0; i_newSpareCompositeIndex < i_need2AddCompositesCount; ++i_newSpareCompositeIndex )
                {                    
                    _AddNewSpareComposites ( commandBuffer, f_compositeScale ) ;
                                                                    
                } // for
                
                /*
                for (int i_patternGroupIndex = 0; i_patternGroupIndex < requestPatternSetupData.Length; ++i_patternGroupIndex )
                {
                    Entity paternGroupEntity = requestPatternSetupData.a_entities [i_patternGroupIndex] ;
                    commandBuffer.RemoveComponent <Blocks.Pattern.RequestPatternSetupTag> ( paternGroupEntity ) ;
                    commandBuffer.RemoveComponent <Common.Components.IsNotAssignedTag> ( paternGroupEntity ) ;
                } // for
                */
            }

        } // job


        // Add set of new composites, according to selected pattern group
        static private EntityCommandBuffer _AddNewSpareComposites ( EntityCommandBuffer commandBuffer, float f_scale )
        {
            float3 f3_scale = new float3 ( 1,1,1 ) * f_scale ;

            commandBuffer.CreateEntity ( newCompositeSpareArchetype ) ;
                              
            Test02.AddBlockSystem._AddBlockRequestViaCustomBufferNoNewEntity ( 
                commandBuffer,
                //element.f3_position,
                float3.zero, // none initial position                    
                f3_scale,
                float3.zero, new Entity (),
                new float4 (1,1,1,1)     // temp, affects block color                        
            ) ;

            return commandBuffer ;
        }

    }
}

