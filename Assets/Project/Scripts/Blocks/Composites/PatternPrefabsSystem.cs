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

namespace ECS.Blocks.Pattern
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

            public ComponentDataArray <Blocks.Pattern.RequestAddPrefabTag> a_requestAddPrefabTag ;
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            // public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
        }
        
        /// <summary>
        /// Temp storage, until request is excuted, by adding request prefabs, to actuall prefab store. After which, store is emptied.
        /// </summary>
        static private NativeArray <Blocks.Pattern.CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefab = new NativeArray<CompositeInPatternPrefabComponent> ( 0, Allocator.Persistent ) ;



        // [Inject] private ComponentDataFromEntity <Blocks.CompositePatternComponent> a_compositeComponents ;
        [Inject] private Barrier compositeBarrier ;

        //static private EntityCommandBuffer commandBuffer ;
        static private EntityManager entityManager ;

        
        static private EntityArchetype archetype ;

        static private Unity.Mathematics.Random random = new Unity.Mathematics.Random () ;
        
        [ReadOnly] static public int i_compositesCountInRowPerPatternGroup = 5 ;
        [ReadOnly] static public int i_compositesCountInColumnPerPatternGroup = 5 ;
        [ReadOnly] static public int i_compositesCountPerPatternGroup = i_compositesCountInRowPerPatternGroup * i_compositesCountInColumnPerPatternGroup ;
        [ReadOnly] static public int i_currentPrefabsCount = 0 ;
        [ReadOnly] static public float f_compositeScale = 0.1f ;
        
        // store position of composites in a pattern
        static public NativeArray <ECS.Blocks.Pattern.CompositeInPatternPrefabComponent> a_patternPrefabs ; // default

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

                typeof ( Blocks.Pattern.RequestAddPrefabTag )
                // typeof ( BufferArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> )

            ) ;
                        
            // a_requestAddComposites2PatternPrefab = new BufferDataFromEntity <RequestAddPrefabBufferElement> () ;
            
            // int i_componentsPatternPrefabIndex = 0 ;

            int i_prefabs2AddCount = 2 ;
            
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

            // add some new pattern prefabs
            for ( int i = 0; i < i_prefabs2AddCount; i ++ )
            {
                // int i_componentsPatternPrefabIndex = _GenerateNewPatternPrefab () ;
                _GenerateNewPatternPrefab ( i ) ;
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
            public NativeArray <Pattern.CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefab ;
                    
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
        private void _GenerateNewPatternPrefab ( int i_prefablIndex )
        {
            // NativeArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> a_compositesInPatternPrefab = new NativeArray <Blocks.PatternPrefab.RequestAddPrefabBufferElement> ( i_compositesCountPerPatternGroup, Allocator.Temp ) ;
            
            Debug.Log ( "Need get composite prefab index" ) ;

            
            // entityManager.AddComponent ( requestAddNewPrefabEntity,  ) ;



            // store temp data
            Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = new Blocks.Pattern.CompositeInPatternPrefabComponent () ;

            //int i_maxCount = i_initialIndex * i_currentPrefabsCount + i_compositesCountPerPatternGroup ;
            
            int i_patternOffsetIndex = i_prefablIndex * i_compositesCountPerPatternGroup ;

            for ( int x = 0; x < i_compositesCountInRowPerPatternGroup; x++ )
            {

                int i_xOffsetIndex = i_patternOffsetIndex + x * i_compositesCountInRowPerPatternGroup ;

                for ( int y = 0; y < i_compositesCountInColumnPerPatternGroup; y++ )
                //for ( int i = i_initialIndex * i_currentPrefabsCount; i < i_maxCount; i++ )
                {
  
                    compositeInPatternPrefab.f3_position = new float3 ( 
                        x * f_compositeScale, 
                        y * f_compositeScale, 
                        0 
                    ) ;
                    
                    compositeInPatternPrefab.f3_scale = new float3 ( 1,1,1 ) * f_compositeScale ;

                    /*
                    compositeInPatternPrefab.f3_position = new float3 ( 
                        random.NextFloat ( -0.5f, 0.5f ), 
                        random.NextFloat ( -0.5f, 0.5f ), 
                        random.NextFloat ( -0.5f, 0.5f ) 
                    ) ;
                    */

                
                    compositeInPatternPrefab.i_compositePrefabIndex = random.NextInt ( 0, 2 ) ; //  assign composite prefab index (0 is ignored)
                    // compositeInPatternPrefab.i_compositePrefabIndex = random.NextInt ( 0, 5 ) ; //  assign composite prefab index

                    a_requestAddComposites2PatternPrefab [i_xOffsetIndex + y] = compositeInPatternPrefab ;

                }

            }
            
            // When scale of any axis is 0 no mesh is generated
            // For each axis with scale greater than 1, there is offset applied, 
            // assuming pivot of mesh, is at the position of this composite.
            _GreedyScalling ( i_prefablIndex ) ;

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
        /// Equivalent to Greedy Meshing, only that do not merges meshes.
        /// Rather it evaluates optimal scale of cube meshes, while reducing number of required cubes.
        /// Scalled cubes become boxes
        /// </summary>
        static private void _GreedyScalling ( int i_prefablIndex )
        {
            _GreedyScallingYAxis ( i_prefablIndex ) ; // optional, but must be executed before GreedyScallingXAxis

            _GreedyScallingXAxis ( i_prefablIndex ) ;

        }

        static private void _GreedyScallingXAxis ( int i_prefablIndex )
        {
            int i_lastDifferentCompositeTypeIndex = -1 ;
            int i_lastDifferentCompositeTypeId = -1 ;

            int i_initialOffsetIndex = i_prefablIndex * i_compositesCountPerPatternGroup ;
            // int i_maxCount = i_initialOffsetIndex + i_compositesCountPerPatternGroup ;


            // for ( int x = i_initialOffsetIndex * i_currentPrefabsCount; x < i_maxCount; x ++ )            
            for ( int y = 0; y < i_compositesCountInColumnPerPatternGroup; y ++ )
            {

                int i_yOffsetIndex = i_initialOffsetIndex + y ; // * i_compositesCountInColumnPerPatternGroup ;

                for ( int x = 0; x < i_compositesCountInRowPerPatternGroup; x ++ )
                {

                    int i_index = i_yOffsetIndex + x * i_compositesCountInRowPerPatternGroup ;

                    Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_requestAddComposites2PatternPrefab [ i_index ] ;
                    
                     bool isYaxisChanged = false ;

                    if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                    {
                        isYaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.y - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.y ) > 0.001f ; // check for floating point error
                    }

                    if ( isYaxisChanged ||
                        compositeInPatternPrefab.i_compositePrefabIndex < 0 ||
                        //compositeInPatternPrefab.i_compositePrefabIndex >= 0 &&
                        i_lastDifferentCompositeTypeId != compositeInPatternPrefab.i_compositePrefabIndex
                        )
                    {
                        i_lastDifferentCompositeTypeIndex = i_index ;
                        i_lastDifferentCompositeTypeId = compositeInPatternPrefab.i_compositePrefabIndex ;
                    }
                    else
                    {
                        Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefabMatch = a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex] ;

                        float3 f3_scaleDiffAbs = math.abs (  compositeInPatternPrefab.f3_scale - compositeInPatternPrefabMatch.f3_scale ) ;

                        // bool isScaleMatch = f3_scaleDiffAbs.x < 0.001f && f3_scaleDiffAbs.y < 0.001f && f3_scaleDiffAbs.z < 0.001f ; // float prcision error check
                        bool isScaleMatch = f3_scaleDiffAbs.y < 0.001f ; // float prcision error check, from previous axis iteration

                        if ( isScaleMatch ) // is scale matching of chekced composites?
                        {
                            // composite type is the same as previous one
                            // grow scale in the traversing direction
                            compositeInPatternPrefabMatch.f3_scale += new float3 ( 1, 0, 0 ) * f_compositeScale ;
                            compositeInPatternPrefabMatch.f3_position += new float3 ( f_compositeScale * 0.5f, 0 ,0 ) ;

                            // first consecutive composite index, with mathching type
                            a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex] = compositeInPatternPrefabMatch ;


                            // This composite is same type as previous composite. 
                            // Previous composite mesh will be scaled, to overlap this composite.
                            // hence mesh is not required.
                            compositeInPatternPrefab.i_compositePrefabIndex = -1 ; // 

                            // a_requestAddComposites2PatternPrefab [i] = compositeInPatternPrefab ;
                        }
                        else
                        {
                            // is diferent
                            i_lastDifferentCompositeTypeIndex = i_index ;
                            i_lastDifferentCompositeTypeId = compositeInPatternPrefab.i_compositePrefabIndex ;
                        }

                    } // for
                
                    a_requestAddComposites2PatternPrefab [i_index] = compositeInPatternPrefab ;


                } // for           

            } // for 

        }

        static private void _GreedyScallingYAxis ( int i_prefablIndex )
        {
                        
            // Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab ;

            int i_lastDifferentCompositeTypeIndex = -1 ;
            int i_lastDifferentCompositeTypeId = -1 ;

            int i_initialOffsetIndex = i_prefablIndex * i_compositesCountPerPatternGroup ;
            int i_maxCount = i_initialOffsetIndex + i_compositesCountPerPatternGroup ;
                        

            for ( int i = i_initialOffsetIndex * i_currentPrefabsCount; i < i_maxCount; i ++ )
            {
                Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_requestAddComposites2PatternPrefab [i] ;

                bool isXaxisChanged = false ;

                if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                {
                    isXaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.x - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.x ) > 0.001f ; // check for floating point error
                }

                if ( isXaxisChanged ||
                    i_lastDifferentCompositeTypeId != compositeInPatternPrefab.i_compositePrefabIndex )
                {
                    // composite type has changed from previous one

                    i_lastDifferentCompositeTypeIndex = i ;
                    i_lastDifferentCompositeTypeId = compositeInPatternPrefab.i_compositePrefabIndex ;
                    
                    // compositeInPatternPrefab.f3_scale = new float3 ( 1,1,1 ) * f_compositeScale ;
                }
                else
                {
                    Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefabMatch = a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex] ;

                    // composite type is the same as previous one
                    // grow scale in the traversing direction
                    compositeInPatternPrefabMatch.f3_scale += new float3 ( 0, 1, 0 ) * f_compositeScale ;
                    compositeInPatternPrefabMatch.f3_position += new float3 ( 0, f_compositeScale * 0.5f ,0 ) ;

                    // first consecutive composite index, with mathching type
                    a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex] = compositeInPatternPrefabMatch ;


                    // This composite is same type as previous composite. 
                    // Previous composite mesh will be scaled, to overlap this composite.
                    // hence mesh is not required.
                    compositeInPatternPrefab.i_compositePrefabIndex = -1 ; // 

                    // a_requestAddComposites2PatternPrefab [i] = compositeInPatternPrefab ;
                }
                
                a_requestAddComposites2PatternPrefab [i] = compositeInPatternPrefab ;

            } // for 

        }
        /// <summary>
        /// Size of the array should be of multipler by i_compositesCountPerPatternGroup
        /// Assigning composites to prefab
        /// </summary>
        /// <param name="a_compositesInPatternPrefab"></param>
        static private int _AssignComponents2PatternPrefab ( NativeArray <Blocks.Pattern.CompositeInPatternPrefabComponent> a_compositesInPatternPrefab )
        {
            int i_prefabOffsetIndex = i_currentPrefabsCount * i_compositesCountPerPatternGroup ;
            
            // expand storage if needed
            if ( a_patternPrefabs.Length <= i_prefabOffsetIndex + i_compositesCountPerPatternGroup )
            {
                // it multiplies minimum size of patter storage, by given number, of empty pattern storages
                int i_capacityExpanderMultipler = 10 ;
                // add extra space
                NativeArray <Blocks.Pattern.CompositeInPatternPrefabComponent> a_compositesPatternPrefabsExpand = new NativeArray <Blocks.Pattern.CompositeInPatternPrefabComponent> ( a_patternPrefabs.Length + i_capacityExpanderMultipler * i_compositesCountPerPatternGroup, Allocator.Temp ) ;

                // copy old array to new bigger aray
                for ( int i_copyIndex = 0; i_copyIndex < a_patternPrefabs.Length; i_copyIndex ++ )
                {
                    a_compositesPatternPrefabsExpand [i_copyIndex] = a_patternPrefabs [i_copyIndex] ;
                }
                // a_compositesPatternPrefabs.Dispose () ;
                a_patternPrefabs = new NativeArray <Blocks.Pattern.CompositeInPatternPrefabComponent> ( a_compositesPatternPrefabsExpand.Length, Allocator.Persistent ) ;
                // assign back to old array, but now with bigger capacity
                a_patternPrefabs.CopyFrom ( a_compositesPatternPrefabsExpand ) ;
                a_compositesPatternPrefabsExpand.Dispose () ;
            }

            int i_ignoredOffsetIndex = 0 ;
            // assign components to prefab
            for ( int i = 0; i < i_compositesCountPerPatternGroup; i ++ )
            {
                if ( a_compositesInPatternPrefab [i].i_compositePrefabIndex >= 0 )
                {
                    // Ensuring all valid composites are stored next to each other in array, rahter tha being scattered in the array storage
                    // Allows to discard early (break;) iteration through, when first invalid composite is found, 
                    // withouth accidentally discarding valid composites (per pattern)
                    a_patternPrefabs [ i_prefabOffsetIndex + i - i_ignoredOffsetIndex ] = a_compositesInPatternPrefab [i] ;                
                }
                else
                {
                    i_ignoredOffsetIndex ++ ;

                    // Add component to the end of store, for given prefab
                    // These are not used anymnore, but stored, just in case are needed at some point.
                    // TODO: Potentially shrinking array store cane be beneficial
                    
                    CompositeInPatternPrefabComponent tempComposite = a_patternPrefabs [ i_prefabOffsetIndex + i_compositesCountPerPatternGroup - i_ignoredOffsetIndex ] ;
                    tempComposite.i_compositePrefabIndex = a_compositesInPatternPrefab [i].i_compositePrefabIndex ;
                    a_patternPrefabs [ i_prefabOffsetIndex + i_compositesCountPerPatternGroup - i_ignoredOffsetIndex ] = tempComposite ;
                    // other properties like position and scale is ignored
                }
            }
                        
            i_currentPrefabsCount ++ ;
             
            return i_currentPrefabsCount ;
        }

        static public Blocks.Pattern.CompositeInPatternPrefabComponent _GetCompositeFromPatternPrefab ( int i_index )
        {
            Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_patternPrefabs [ i_index ] ;

            return compositeInPatternPrefab ;
        }

        
        static public Unity.Mathematics.Random _Random ()
        {
            // int i_int32 = (int) ( UnityEngine.Time.time * 1000 ) + 1 ;
            int i_int32 = (int) ( System.DateTime.UtcNow.Millisecond + UnityEngine.Time.time * 1000 ) ;
            Unity.Mathematics.Random random = new Unity.Mathematics.Random ( global::System.Convert.ToUInt32 ( i_int32 ) ) ;

            return random ;
        }

    }
    
}