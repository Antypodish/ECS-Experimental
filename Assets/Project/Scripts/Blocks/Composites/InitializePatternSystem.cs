﻿using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Blocks.Pattern
{
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.
    
    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(ReleaseCompositeBarrier))]
    public class InitializePatternSystem : JobComponentSystem
    {     

        [Inject] private InitializePatternSetupData initializePatternSetupData ;  

        // request to assing pattern
        struct InitializePatternSetupData
        {
            //public readonly int Length ;

            public EntityArray a_entities ;

            // public ComponentDataArray <Blocks.PatternComponent> a_compositePattern ;
            // public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;
            // public ComponentDataArray <Common.Components.InitializeTag> a_initializeTag ;
            public ComponentDataArray <Blocks.Pattern.InitializePrefabTag> a_initializePrefabTag ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            // public ComponentDataArray <Blocks.Pattern.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
            
        [Inject] private MoveCompositeBarrier compositeBarrier ;

        // test temp
        // add some pattern groups from pattern prefabs
        static private int i_patternGroups = 8 ;

        static private EntityArchetype archetype ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;

        

        protected override void OnCreateManager ( int capacity )
        {
            Debug.Log ( "InitializePatternSystem" ) ;
            EntityManager entityManager = World.Active.GetOrCreateManager <EntityManager>() ;      
            
            archetype = entityManager.CreateArchetype 
            (   
                //typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Common.BufferElements.EntityBuffer ),
                typeof ( Blocks.Pattern.RequestPatternSetupTag ),
                typeof ( Common.Components.IsNotAssignedTag ),
                typeof ( Blocks.MovePattern ),
                typeof ( Blocks.Pattern.Components.Lod010Tag ),
                typeof ( Blocks.Pattern.Components.IsLodActiveTag )
                //typeof ( Blocks.CompositeComponent )
            ) ;


            Entity entity = EntityManager.CreateEntity ( ) ;            
            EntityManager.AddComponentData ( entity, new Blocks.Pattern.InitializePrefabTag () ) ;

            
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

                    
        static bool isInitialized = false ;

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {         

            if ( !isInitialized )
            {
                // prefabs must exist
                // keep checking
                if ( Pattern.AddPatternPrefabSystem.i_currentPrefabsCount > 0 )
                {

                    isInitialized = true ;
                
                    // initialzed
                    // remove initialization entity
                    // system go to sleep
                    Entity initializationEntity = initializePatternSetupData.a_entities [0] ;
                    EntityManager.DestroyEntity ( initializationEntity ) ;


                    // Debug.Log ( "rand2 (random pattern getting not working) : " + Pattern.AddPatternPrefabSystem.i_currentPrefabsCount ) ;

                    EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;

                    // test temp
                    // add some pattern groups from pattern prefabs
                    // int i_patternGroups = 1 ;
                    for ( int i = 0; i < i_patternGroups; i ++ )
                    {
               
                        //random = _Random ( i ) ;
                        //random.NextInt ( 1, 10 ) ;
                        //Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint) i + 1);

                        // Debug.Log ( "rand2 (random pattern getting not working) : " + random.NextInt ( 0, i_patternGroups ) ) ;

                        // Entity entity = EntityManager.CreateEntity ( archetype ) ; // store data about composite patterns groups

                        
                        int i_randomPattern = UnityEngine.Random.Range ( 0, Pattern.AddPatternPrefabSystem.i_currentPrefabsCount ) ; // get random pattern (temp)
                        int i_patternIndex = i_randomPattern ; // random is temp
                        float f_baseScale = i < 4 ? 2f : 8f ;
                        float3 f3_localPosition = new float3 ( 1, 0, 0 ) * i ;
                        int i_lodDepth = i < 4 ? 0 : 1 ;
                        i_randomPattern = UnityEngine.Random.Range ( 0, Pattern.AddPatternPrefabSystem.i_currentPrefabsCount ) ; // get random pattern (temp)
                        int i_prefabIndex = i_randomPattern ; // used for lower level of details
                        
                        _AddNewPatternSystem ( commandBuffer, i_patternIndex, f_baseScale, f3_localPosition, i_lodDepth, i_prefabIndex ) ;

                            /*
                        commandBuffer.CreateEntity ( archetype ) ; // store data about composite patterns groups
                        
                        PatternComponent patternComponent = new Blocks.PatternComponent () { 
                            //i_patternIndex = random.NextInt ( 0, Pattern.PatternPrefabSystem.i_currentPrefabsCount ), // get random prefab pattern
                            i_patternIndex = i_randomPattern,            
                            //i_patternIndex = random.NextInt ( 0, 3 )
                            f_baseScale = 2f,
                            f3_localPosition = new float3 ( 1, 0, 0 ) * i,
                            i_lodDepth = 0 // set default depth level

                        } ;

                        commandBuffer.AddComponent <PatternComponent> ( patternComponent ) ;
                        */
                        

                        /*
                        if ( i == 5 )
                        {
                            // temp test
                           entityManager.AddComponent ( entity, typeof ( Blocks.Pattern.RequestPatternReleaseTag ) ) ;
                        }
                        */
                    }    

                }

            }


            return inputDeps ;
       }


        static public void _AddNewPatternSystem ( EntityCommandBuffer commandBuffer, int i_patternIndex, float f_baseScale, float3 f3_localPosition, int i_lodDepth, int i_prefabIndex  )
        {
            commandBuffer.CreateEntity ( archetype ) ; // store data about composite patterns groups
                        
            PatternComponent patternComponent = new Blocks.PatternComponent () { 
                //i_patternIndex = random.NextInt ( 0, Pattern.PatternPrefabSystem.i_currentPrefabsCount ), // get random prefab pattern
                i_patternIndex = i_patternIndex,            
                //i_patternIndex = random.NextInt ( 0, 3 )
                f_baseScale = f_baseScale,
                f3_localPosition = f3_localPosition,
                i_lodDepth = 0, // set default depth level
                i_prefabIndex = i_prefabIndex
            } ;

            commandBuffer.AddComponent <PatternComponent> ( patternComponent ) ;
        }
    }

    
}

