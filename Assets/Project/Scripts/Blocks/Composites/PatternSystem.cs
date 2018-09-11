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
    // [UpdateAfter(typeof(ReleaseCompositeBarrier))]
    public class PatternSystem : JobComponentSystem
    {     
        
        [Inject] private PatternData patternData ;   
                
        // individual smallest composite of the pattern
        struct PatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            // [ReadOnly] public ComponentDataArray <Position> a_position ;
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            // [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            
            // [ReadOnly] public ComponentDataArray <Common.Components.Lod01Tag> a_compositePatternTag ;
        }

        [Inject] private RequestPatternSetupData requestPatternSetupData ;  

        // request to assing pattern
        struct RequestPatternSetupData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePattern ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            public ComponentDataArray <Blocks.Pattern.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
        

        /*
        [Inject] private AssignPatternData assignPatternData ;  

        // request to assing pattern
        struct AssignPatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.CompositePatternComponent> a_compositePatternComponent ;
            
            //public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            //public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            //public SubtractiveComponent <Blocks.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }
        
        [Inject] private ComponentDataFromEntity <Blocks.CompositePatternComponent> a_compositeComponents ;
        [Inject] private MoveCompositeBarrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;
        */

        static private EntityArchetype archetype ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;

        protected override void OnCreateManager ( int capacity )
        {
            EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;            
            
            
            archetype = entityManager.CreateArchetype (   
                //typeof ( Blocks.CompositeComponent ),
                //typeof ( Common.Components.IsNotAssignedTag ),
                //typeof ( Common.Components.Lod01Tag )

                // typeof ( Position ),
                // typeof ( Common.Components.Lod01Tag )
                typeof ( Common.BufferElements.EntityBuffer ),
                typeof ( Blocks.Pattern.RequestPatternSetupTag ),
                typeof ( Blocks.MovePattern )

            ) ;

            Debug.Log ( "PAttern System Disabled adding new groups" ) ;

            Debug.Log ( "need reuse released entities" ) ;
            // public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            // test temp
            // add some pattern groups from pattern prefabs
            for ( int i = 0; i < 2; i ++ )
            {

                random = Pattern.PatternPrefabSystem._Random () ;

                Entity entity = entityManager.CreateEntity ( archetype ) ; // store data about composite patterns groups
                entityManager.AddComponentData ( entity, new Blocks.PatternComponent () { 
                    i_patternIndex = random.NextInt ( 0, Pattern.PatternPrefabSystem.i_currentPrefabsCount ), // get random prefab pattern
                } ) ;

                
                if ( i == 5 )
                {
                    // temp test
                   entityManager.AddComponent ( entity, typeof ( Blocks.Pattern.RequestPatternReleaseTag ) ) ;
                }
                
            }

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

            

            /*
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct AssignPatternDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityManager entityManager ;
            // public EntityArray a_entities;     
            
            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public MovePatternData movePatternData ;

            public Unity.Mathematics.Random random ;

            public ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;

                      // Blocks.MovePatternComonent
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {                      
                
            }                       
        }
        */
        
        
       // protected override JobHandle OnUpdate ( JobHandle inputDeps )
       // {            
       //     random = _Random () ;
            
            // var mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, mergeLod01JobHandle ) ;
            //JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;
            
            
            /*
            var assignPatternDataJobHandle = new AssignPatternDataJob // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                // movePatternData = movePatternData,
                //random = random
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( inputDeps ) ;
            */

            /*
            var releasePatternDataJobHandle = new ReleasePatternDataJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                releasePatternData = releasePatternData,
                random = random
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            mergeJobHandle = releasePatternDataJobHandle.Schedule ( mergeJobHandle ) ;
            */
            
      //      return mergeJobHandle ;
        
      //  }
        


    }

    
}

