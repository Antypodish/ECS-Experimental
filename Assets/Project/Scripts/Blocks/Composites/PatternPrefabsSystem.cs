using System;
using System.Collections.Generic;
using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Blocks.PatternPrefab
{
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.
    

    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierB))]
    public class PatternPrefabSystem : JobComponentSystem
    {     
        /*
        [Inject] private PatternPrefabData patternPrefabData ;   
                
        // individual smallest composite of the pattern
        struct PatternPrefab
        {
            public readonly int Length ;

            public EntityArray a_entities ;
              
            // [ReadOnly] public ComponentDataArray <Position> a_position ;
            [ReadOnly] public ComponentDataArray <Blocks.CompositeComponent> a_compositeEntityRelatives ;

            // [ReadOnly] public ComponentDataArray <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            
            // [ReadOnly] public ComponentDataArray <Common.Components.Lod01Tag> a_compositePatternTag ;
        }
        */
        [Inject] private RequestAddPatternPrefabData requestAddPatternPrefabData ;  

        // request to assing pattern
        struct RequestAddPatternPrefabData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            // public ComponentDataArray <Blocks.CompositePatternComponent> a_compositePattern ;
            // public BufferArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> a_requestAddComposites2PatternPrefab ;

            public ComponentDataArray <Blocks.PatternPrefab.RequestAddPrefabTag> a_requestAddPrefabTag ;
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            // public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
        
        /// <summary>
        /// Temp storage, until request is excuted, by adding request prefabs, to actuall prefab store. After which, store is emptied.
        /// </summary>
        static private NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefab = new NativeArray<CompositeInPatternPrefabComponent> ( 0, Allocator.Persistent ) ;



        // [Inject] private ComponentDataFromEntity <Blocks.CompositePatternComponent> a_compositeComponents ;
        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;

        
        static private EntityArchetype archetype ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;
        

        [ReadOnly] static public int i_compositesCountPerPatternGroup = 10 ;
        [ReadOnly] static public int i_currentPrefabsCount = 0 ;
        
        // store position of composites in a pattern
        static public NativeArray <ECS.Blocks.PatternPrefab.CompositeInPatternPrefabComponent> a_patternPrefabs ; // default

        protected override void OnCreateManager ( int capacity )
        {
            //commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
            archetype = entityManager.CreateArchetype (   
                //typeof ( Blocks.CompositeComponent ),
                //typeof ( Common.Components.IsNotAssignedTag ),
                //typeof ( Common.Components.Lod01Tag )

                // typeof ( Position ),
                // typeof ( Common.Components.Lod01Tag )

//                typeof ( Common.BufferElements.EntityBuffer ),
//                typeof ( Blocks.RequestPatternSetupTag ),
//                typeof ( Blocks.MovePattern )

                typeof ( Blocks.PatternPrefab.RequestAddPrefabTag )
                // typeof ( BufferArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> )

            ) ;
                        
            // a_requestAddComposites2PatternPrefab = new BufferDataFromEntity <RequestAddPrefabBufferElement> () ;
            
            // int i_componentsPatternPrefabIndex = 0 ;

            int i_prefabs2AddCount = 4 ;
            
            int i_initialIndex = a_requestAddComposites2PatternPrefab.Length ;
            if ( i_initialIndex == 0 )
            {
                a_requestAddComposites2PatternPrefab.Dispose () ; // dispose old array
                a_requestAddComposites2PatternPrefab = new NativeArray <CompositeInPatternPrefabComponent> ( i_prefabs2AddCount * i_compositesCountPerPatternGroup, Allocator.Persistent ) ;
            }
            // Copy and extend an array
            NativeArray<CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefabTemp = new NativeArray <CompositeInPatternPrefabComponent> ( i_initialIndex + i_prefabs2AddCount * i_compositesCountPerPatternGroup, Allocator.Temp ) ;
            
            // Coppy array elements, from smaller to bigger array
            for ( int i = 0; i < i_prefabs2AddCount; i ++ )
            {
                // int i_componentsPatternPrefabIndex = _GenerateNewPatternPrefab () ;
                a_requestAddComposites2PatternPrefabTemp [i] = a_requestAddComposites2PatternPrefab [i] ;
            }

            // arrays are qual now
            a_requestAddComposites2PatternPrefab.CopyFrom ( a_requestAddComposites2PatternPrefabTemp ) ;

            random = _Random () ;

            // add some new patter prefabs
            for ( int i = 0; i < i_prefabs2AddCount; i ++ )
            {
                // int i_componentsPatternPrefabIndex = _GenerateNewPatternPrefab () ;
                _GenerateNewPatternPrefab ( i_initialIndex ) ;
            }

            Entity requestAddNewPrefabEntity = entityManager.CreateEntity ( archetype ) ;

            a_requestAddComposites2PatternPrefabTemp.Dispose () ;
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            a_requestAddComposites2PatternPrefab.Dispose () ;
            a_patternPrefabs.Dispose () ;
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
        struct AddPatternPrefabJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityArray a_entities;     
            
            public RequestAddPatternPrefabData requestAddPatternPrefabData ;    
            public NativeArray <PatternPrefab.CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefab ;
                    
            //public RequestPatternSetupData requestPatternSetupData ;

            
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {    

                //Debug.Log ( "aa" ) ;
                //NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> a_compositesInPatternPrefab ; // = new NativeArray<CompositeInPatternPrefabComponent> () ;

                for ( int i_entityIndex = 0; i_entityIndex < requestAddPatternPrefabData.Length; i_entityIndex++ )
                {
                    Entity requestEntity = requestAddPatternPrefabData.a_entities [i_entityIndex] ;

                    //int i_composites2AddLength = a_requestAddComposites2PatternPrefab.Length ;
                    //a_compositesInPatternPrefab = new NativeArray<PatternPrefab.CompositeInPatternPrefabComponent> ( i_composites2AddLength, Allocator.TempJob ) ;

                    // Iterate through composites to add
                    //for ( int i_compositeIndex = 0; i_compositeIndex < i_composites2AddLength; i_compositeIndex++ )
                    //{
                    //    Blocks.PatternPrefab.CompositeInPatternPrefabComponent requestAddcomposite2Prefab = a_requestAddComposites2PatternPrefab [i_compositeIndex] ;
                                                
                        // PatternPrefab.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_compositesInPatternPrefab [ i_compositeIndex ] ;
                    //    compositeInPatternPrefab.f3_position = requestAddcomposite2Prefab.f3_position ;
                    //    compositeInPatternPrefab.i_compositePrefabIndex = requestAddcomposite2Prefab.i_compositePrefabIndex ;
                    //    a_compositesInPatternPrefab [ i_compositeIndex ] = compositeInPatternPrefab ;
                        
                    //} // for
                    
                    _AssignComponents2PatternPrefab ( a_requestAddComposites2PatternPrefab ) ;

                    

                    // Addition request complete
                    // Prefab data is stored in Buffer Array
                    commandBuffer.DestroyEntity ( requestEntity ) ;

                } // for

                // a_requestAddComposites2PatternPrefab = new NativeArray<CompositeInPatternPrefabComponent> ( 0, Allocator.Persistent ) ;
                //a_compositesInPatternPrefab.Dispose () ;
                //a_requestAddComposites2PatternPrefab.Dispose () ;
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

            var addPatternPrefabJobHandle = new AddPatternPrefabJob // for IJobParallelFor
            {    
                commandBuffer = compositeBarrier.CreateCommandBuffer (),
                requestAddPatternPrefabData = requestAddPatternPrefabData,
                a_requestAddComposites2PatternPrefab = a_requestAddComposites2PatternPrefab,
                
                //spareCompositeData = spareCompositeData,
                
                
            }.Schedule ( inputDeps ) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = assignCompositePatternJobHandle.Schedule ( assignCompositePatternData.Length, 64, inputDeps ) ;

            return addPatternPrefabJobHandle ;
        
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
        /// Returns prefab index
        /// </summary>
        /// <returns></returns>
        private void _GenerateNewPatternPrefab ( int i_initialIndex )
        {
            // NativeArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> a_compositesInPatternPrefab = new NativeArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> ( i_compositesCountPerPatternGroup, Allocator.Temp ) ;
            
            Debug.Log ( "Need get composite prefab index" ) ;

            
            // entityManager.AddComponent ( requestAddNewPrefabEntity,  ) ;



            // store temp data
            Blocks.PatternPrefab.CompositeInPatternPrefabComponent compositeInPatternPrefab = new Blocks.PatternPrefab.CompositeInPatternPrefabComponent () ;

            for ( int i = i_initialIndex; i < i_compositesCountPerPatternGroup; i++ )
            {
                // Blocks.PatternPrefab.RequestAddPrefabBufferElement compositeInPatternPrefab = new Blocks.PatternPrefab.RequestAddPrefabBufferElement () ;
                // blockCompositeBufferElement.f3_position = new float3 (1,1,1) * i * 0.1f + CompositeSystem.i_currentPrefabsCount ;
                // compositeInPatternPrefab.f3_position = new float3 ( random.NextFloat ( -0.5f, 0.5f ), random.NextFloat ( -0.5f, 0.5f ), random.NextFloat ( -0.5f, 0.5f ) ) ;
                // compositeInPatternPrefab.i_compositePrefabIndex = ... // incomplete composite prefabs

                // a_compositesInPatternPrefab [i] = compositeInPatternPrefab ;

                compositeInPatternPrefab.f3_position = new float3 ( 
                    random.NextFloat ( -0.5f, 0.5f ), 
                    random.NextFloat ( -0.5f, 0.5f ), 
                    random.NextFloat ( -0.5f, 0.5f ) 
                ) ;
                compositeInPatternPrefab.i_compositePrefabIndex = 0 ; //  assign composite prefab index
                a_requestAddComposites2PatternPrefab [i] = compositeInPatternPrefab ;
            }

            // BufferArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> a_requestAddPrefabComposites ;

                        
            //a_requestAddComposites2PatternPrefab
            //  Common.BufferElements.EntityBuffer a = new Common.BufferElements.EntityBuffer () ;
            // entityManager.SetComponentData ( requestAddNewPrefabEntity, a_requestAddPrefabComposites[1][0] ) ;
            // assing composites to the prefab store
            // Assigning composites to prefab
            // int i_prefabIndex = _AssignComponents2PatternPrefab ( a_compositesInPatternPrefab ) -1 ;

            // a_compositesInPatternPrefab.Dispose () ;

            // return i_prefabIndex ;
        }
               
        /// <summary>
        /// Size of the array should be of multipler by i_compositesCountPerPatternGroup
        /// Assigning composites to prefab
        /// </summary>
        /// <param name="a_compositesInPatternPrefab"></param>
        static private int _AssignComponents2PatternPrefab ( NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> a_compositesInPatternPrefab )
        {
            int i_prefabOffsetIndex = i_currentPrefabsCount * i_compositesCountPerPatternGroup ;
            
            // expand storage if needed
            if ( a_patternPrefabs.Length <= i_prefabOffsetIndex + i_compositesCountPerPatternGroup )
            {
                // it multiplies minimum size of patter storage, by given number, of empty pattern storages
                int i_capacityExpanderMultipler = 10 ;
                // add extra space
                NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> a_compositesPatternPrefabsExpand = new NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> ( a_patternPrefabs.Length + i_capacityExpanderMultipler * i_compositesCountPerPatternGroup, Allocator.Temp ) ;

                // copy old array to new bigger aray
                for ( int i_copyIndex = 0; i_copyIndex < a_patternPrefabs.Length; i_copyIndex ++ )
                {
                    a_compositesPatternPrefabsExpand [i_copyIndex] = a_patternPrefabs [i_copyIndex] ;
                }
                // a_compositesPatternPrefabs.Dispose () ;
                a_patternPrefabs = new NativeArray <Blocks.PatternPrefab.CompositeInPatternPrefabComponent> ( a_compositesPatternPrefabsExpand.Length, Allocator.Persistent ) ;
                // assign back to old array, but now with bigger capacity
                a_patternPrefabs.CopyFrom ( a_compositesPatternPrefabsExpand ) ;
                a_compositesPatternPrefabsExpand.Dispose () ;
            }

            // assign components to prefab
            for ( int i = 0; i < a_compositesInPatternPrefab.Length; i ++ )
            {
                a_patternPrefabs [ i_prefabOffsetIndex + i ] = a_compositesInPatternPrefab [i] ;                
            }
                        
            i_currentPrefabsCount ++ ;
             
            return i_currentPrefabsCount ;
        }

        static public Blocks.PatternPrefab.CompositeInPatternPrefabComponent _GetCompositeFromPatternPrefab ( int i_index )
        {
            Blocks.PatternPrefab.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_patternPrefabs [ i_index ] ;

            return compositeInPatternPrefab ;
        }

        
        static public Unity.Mathematics.Random _Random ()
        {
            int i_int32 = (int) ( UnityEngine.Time.deltaTime * 1000 ) ;
            Unity.Mathematics.Random random = new Unity.Mathematics.Random ( global::System.Convert.ToUInt32 ( i_int32 ) ) ;

            return random ;
        }

    }
    
}