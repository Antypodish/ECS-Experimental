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
        [ReadOnly] static public int i_compositesCountInDepthPerPatternGroup = 10 ; // z
        [ReadOnly] static public int i_compositesCountInColumnPerPatternGroup = 10 ; // y
        [ReadOnly] static public int i_compositesCountInRowPerPatternGroup = 8 ; // x
        [ReadOnly] static public int i_compositesCountPerPatternGroup = i_compositesCountInDepthPerPatternGroup * i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup ;
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

            for ( int z = 0; z < i_compositesCountInDepthPerPatternGroup; z++ )
            {

                int i_zOffsetIndex = i_patternOffsetIndex + z * i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup ;

                for ( int x = 0; x < i_compositesCountInRowPerPatternGroup; x++ )
                {

                    int i_xOffsetIndex = i_zOffsetIndex + x * i_compositesCountInColumnPerPatternGroup ;

                    for ( int y = 0; y < i_compositesCountInColumnPerPatternGroup; y++ )
                    {

                        compositeInPatternPrefab.f3_position = new float3 ( 
                            x * f_compositeScale, 
                            y * f_compositeScale, 
                            z * f_compositeScale
                        ) ;
                    
                        compositeInPatternPrefab.f3_scale = new float3 ( 1,1,1 ) * f_compositeScale ;

                        /*
                        compositeInPatternPrefab.f3_position = new float3 ( 
                            random.NextFloat ( -0.5f, 0.5f ), 
                            random.NextFloat ( -0.5f, 0.5f ), 
                            random.NextFloat ( -0.5f, 0.5f ) 
                        ) ;
                        */

                
                        compositeInPatternPrefab.i_compositePrefabIndex = random.NextInt ( 0, 4 ) ; //  assign composite prefab index (0 is ignored)
                        // compositeInPatternPrefab.i_compositePrefabIndex = random.NextInt ( 0, 5 ) ; //  assign composite prefab index

                        a_requestAddComposites2PatternPrefab [i_xOffsetIndex + y] = compositeInPatternPrefab ;

                    }

                }

            }
            
            // When scale of any axis is 0 no mesh is generated
            // For each axis with scale greater than 1, there is offset applied, 
            // assuming pivot of mesh, is at the position of this composite.
            _GreedyScalling ( i_prefablIndex ) ;

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

            _GreedyScallingZAxis ( i_prefablIndex ) ;

            // Removes composites, which are inside the boundary, and are not visible
            _RemoveInnerComposites ( i_prefablIndex ) ;

        }

        /// <summary>
        /// Remove inner composites, which are not on outside boundary (shell)
        /// </summary>
        /// <param name="i_prefablIndex"></param>
        static private void _RemoveInnerComposites ( int i_prefablIndex )
        {
            int i_discardedCompositesCount = 0 ;
            // store temporarly filtered prefab composites
            //NativeArray<CompositeInPatternPrefabComponent> a_requestAddComposites2PatternPrefabTemp = new NativeArray<CompositeInPatternPrefabComponent> ( a_requestAddComposites2PatternPrefab.Length, Allocator.Temp ) ;

            for ( int i = 0; i < a_requestAddComposites2PatternPrefab.Length; i++ )
            {
                CompositeInPatternPrefabComponent compositeFromPrefab2Filter = a_requestAddComposites2PatternPrefab [i] ;

                
                float3 f3_size = float3.zero ;                
                float3 f3_sizeHalve = float3.zero ;

                bool isCheckRequired = false ;

                if ( compositeFromPrefab2Filter.i_compositePrefabIndex >= 0 && math.lengthSquared ( compositeFromPrefab2Filter.f3_scale ) > 0.00001f )
                {
                    isCheckRequired = true ;

                    //f3_size = compositeFromPrefab2Filter.f3_scale - new float3 ( 1,1,1 ) * f_compositeScale ; // / f_compositeScale ) ;
                    //f3_sizeHalve = f3_size * 0.5f ;
                }

                float3 f3_filterPositionMin = a_requestAddComposites2PatternPrefab [i].f3_position - ( a_requestAddComposites2PatternPrefab [i].f3_scale - f_compositeScale ) * 0.5f ;
                float3 f3_filterPositionMax = new float3 ( i_compositesCountInRowPerPatternGroup, i_compositesCountInColumnPerPatternGroup, i_compositesCountInDepthPerPatternGroup ) ;
                float3 f3_filterPositionMaxDiff  = ( f3_filterPositionMax -1 ) * f_compositeScale - ( a_requestAddComposites2PatternPrefab [i].f3_position + ( a_requestAddComposites2PatternPrefab [i].f3_scale - f_compositeScale ) * 0.5f ) ;

                if ( !isCheckRequired || 
                    (
                    f3_filterPositionMin.x < 0.0001f || // ensure that floatin precision error is not detected
                    f3_filterPositionMin.y < 0.0001f || 
                    f3_filterPositionMin.z < 0.0001f ||
                    f3_filterPositionMaxDiff.x < 0.0001f ||
                    f3_filterPositionMaxDiff.y < 0.0001f || 
                    f3_filterPositionMaxDiff.z < 0.0001f
                    ) )
                {
                    isCheckRequired = false ;
                }

                /*
                float3 f3_filterPositionMax  = ( f_posMax -1 ) * f_compositeScale - a_requestAddComposites2PatternPrefab [i].f3_position ;
                float3 f3_difPosAbs = math.abs ( f3_filterPositionMax ) ;

                if ( f3_difPosAbs.x < 0.00001 ||
                     f3_difPosAbs.y < 0.00001 ||
                     f3_difPosAbs.z < 0.00001 
                    ) // assumes almost 0
                {
                    isCheckRequired = false ;
                    //isHavingNeighbour = false ;
                    //break ;
                }
                */
                //float3 f3_filterPositionMin = math.abs ( compositeFromPrefab2Filter.f3_position - f3_sizeHalve ) ;
                //float3 f3_filterPositionMax = math.abs ( compositeFromPrefab2Filter.f3_position - ( new float3 ( i_compositesCountInRowPerPatternGroup, i_compositesCountInColumnPerPatternGroup, i_compositesCountInDepthPerPatternGroup ) -1 ) * f_compositeScale - f3_sizeHalve ) ;
                // Ignore check of absolute borders, of the boxing area, where composites are allocate located.
                // i.e. when position x, or y, or z are 0, or max
                /*
                if ( !isCheckRequired || 
                    (
                    f3_filterPositionMin.x < 0.0001f || // ensure that floatin precision error is not detected
                    f3_filterPositionMin.y < 0.0001f || 
                    f3_filterPositionMin.z < 0.0001f ||
                    f3_filterPositionMax.x < 0.0001f ||
                    f3_filterPositionMax.y < 0.0001f || 
                    f3_filterPositionMax.z < 0.0001f
                    ) )
                {
                    isCheckRequired = false ;
                }
                else
                {
                    
                }
                       */  
                
                if ( i == 27 )
                {
                    Debug.Log ( "test catch #" + i ) ;
                }

                // Only check main part of composite, from which direction is extended, if box mesh is expanded.
                if ( isCheckRequired )
                {
                    // Absolute boundary checked, now check inner area, of possible naighbours
                    
                    int3 i3_size = (int3) math.round ( compositeFromPrefab2Filter.f3_scale / f_compositeScale ) ; // scalle back to integer units, for easier use as index 
                    CompositeInPatternPrefabComponent neighbourComposite ;

                    bool isHavingNeighbour = true ;

                    // check boundary neighbours, of the merged composite, into bigger block, if applicable
                    for ( int x = 0; x < i3_size.x; x ++ )
                    {
                        
                        for ( int y = 0; y < i3_size.y; y ++ )
                        {

                            for ( int z = 0; z < i3_size.z; z ++ )
                            {

                                //bool isHavingNeighbour = true ;

                                int i_offset ;
                                int i_index = i + y + x * i_compositesCountInColumnPerPatternGroup + z * i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup ;

                                float3 f_posMax = new float3 ( i_compositesCountInRowPerPatternGroup, i_compositesCountInColumnPerPatternGroup, i_compositesCountInDepthPerPatternGroup ) ;
                                float3 f3_difPos  = ( f_posMax -1 ) * f_compositeScale - a_requestAddComposites2PatternPrefab [i_index].f3_position ;
                                float3 f3_difPosAbs = math.abs ( f3_difPos ) ;

                                if ( f3_difPosAbs.x < 0.00001 ||
                                    f3_difPosAbs.y < 0.00001 ||
                                    f3_difPosAbs.z < 0.00001 
                                    ) // assumes almost 0
                                {
                                    isHavingNeighbour = false ;
                                    break ;
                                }

                                // check all 6 sides
                                for ( int i_sides = 0; i_sides < 6; i_sides ++ ) 
                                {
                                    switch (i_sides)
                                    {
                                        case 0:
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_index + 1] ; // +y
                                            break ;
                                        case 1:
                                            // i_index = i_compositesCountInColumnPerPatternGroup ;
                                            i_offset = i_index - 1 ;   
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_offset >= 0 ? i_offset : i] ; // -y
                                            break ;
                                        case 2:
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_index + i_compositesCountInColumnPerPatternGroup] ; // +x
                                            break ;
                                        case 3:                                            
                                            i_offset = i_index - i_compositesCountInColumnPerPatternGroup ;    
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_offset >= 0 ? i_offset : i] ; // -x
                                            break ;
                                        case 4:
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_compositesCountInColumnPerPatternGroup + i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup] ; // +z
                                            break ;
                                        case 5:                                            
                                            i_offset = i_compositesCountInColumnPerPatternGroup - i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup ;                                            
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i_offset >= 0 ? i_offset : i ] ; // -z
                                            break ;
                                        default:
                                            // error
                                            neighbourComposite = a_requestAddComposites2PatternPrefab [i] ;
                                            break ;
                                    }

                                    if ( math.lengthSquared ( neighbourComposite.f3_scale ) < 0.00001f )
                                    {
                                        isHavingNeighbour = false ;
                                        break ;
                                    }
                                } // for

                                if ( !isHavingNeighbour )
                                {
                                    break ;
                                }

                            } // for

                            if ( !isHavingNeighbour )
                            {
                                break ;
                            }

                        } // for

                        if ( !isHavingNeighbour )
                        {
                            break ;
                        }

                    } // for
                    
                    // has no neighbour
                    // means is probably on the boundary, or detached
                    if ( !isHavingNeighbour )
                    {
                        break ;
                    }
                    else
                    {
                        i_discardedCompositesCount ++ ;

                        //int3 i3_ = new int3 ( x,y,z ) ;
                        Debug.Log ( "#" + i_discardedCompositesCount + " discarded composite #" + i + " has all 6 neghbour, at position: " + compositeFromPrefab2Filter.f3_position ) ;

                        float3 f3_maxOffset = new float3 ( i_compositesCountInRowPerPatternGroup, i_compositesCountInColumnPerPatternGroup, i_compositesCountInDepthPerPatternGroup ) -
                            ( compositeFromPrefab2Filter.f3_position + compositeFromPrefab2Filter.f3_scale * 0.5f + ( compositeFromPrefab2Filter.f3_scale - f_compositeScale ) * 0.5f );
                        float3 f3_maxOffsetAbs = math.abs ( f3_maxOffset ) ;
                        /*
                        // check scaling
                        // must not touch borders, even if is expanded
                        if ( f3_maxOffsetAbs.x > 0.0001f &&
                            f3_maxOffsetAbs.y > 0.0001f &&
                            f3_maxOffsetAbs.z > 0.0001f )
                        {
                        */
                        // yes haveing neighbours
                        // hence composite should be removed / deactivated
                        compositeFromPrefab2Filter.i_compositePrefabIndex = -1 ;
                        //compositeFromPrefab2Filter.f3_position = float3.zero ;
                        //compositeFromPrefab2Filter.f3_scale = float3.zero ;   
                                    

                        a_requestAddComposites2PatternPrefab [i] = compositeFromPrefab2Filter ;

                    }

                }

            }

            //a_requestAddComposites2PatternPrefabTemp.Dispose () ;
        }

        static private void _GreedyScallingZAxis ( int i_prefablIndex )
        {

            int i_lastDifferentCompositeTypeIndex = -1 ;
            int i_lastDifferentCompositeTypeId = -1 ;

            int i_initialOffsetIndex = i_prefablIndex * i_compositesCountPerPatternGroup ;
            // int i_maxCount = i_initialOffsetIndex + i_compositesCountPerPatternGroup ;


            // for ( int x = i_initialOffsetIndex * i_currentPrefabsCount; x < i_maxCount; x ++ )            
            for ( int y = 0; y < i_compositesCountInColumnPerPatternGroup; y ++ )
            {

                int i_yOffsetIndex = i_initialOffsetIndex + y ;

                for ( int x = 0; x < i_compositesCountInRowPerPatternGroup; x ++ )
                {

                    int i_xOffsetIndex = i_yOffsetIndex + x * i_compositesCountInColumnPerPatternGroup ;

                    for ( int z = 0; z < i_compositesCountInDepthPerPatternGroup; z ++ )
                    {

                        int i_index = i_xOffsetIndex + z * i_compositesCountInColumnPerPatternGroup * i_compositesCountInRowPerPatternGroup ;

                        //Debug.Log ( i_index ) ;
                                               
                        Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_requestAddComposites2PatternPrefab [ i_index ] ;
                    

                        bool isXaxisChanged = false ;

                        if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                        {
                            isXaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.x - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.x ) > 0.001f ; // check for floating point error
                        }

                        bool isYaxisChanged = false ;

                        if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                        {
                            isYaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.y - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.y ) > 0.001f ; // check for floating point error
                        }
                    

                        if ( isXaxisChanged || isYaxisChanged ||
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
                            bool isScaleMatch = f3_scaleDiffAbs.z < 0.001f ; // float prcision error check, from previous axis iteration

                            if ( isScaleMatch ) // is scale matching of chekced composites?
                            {
                                // composite type is the same as previous one
                                // grow scale in the traversing direction
                                compositeInPatternPrefabMatch.f3_scale += new float3 ( 0, 0, 1 ) * f_compositeScale ;
                                compositeInPatternPrefabMatch.f3_position += new float3 ( 0, 0 ,f_compositeScale * 0.5f ) ;

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

            } // for

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

                    int i_index = i_yOffsetIndex + x * i_compositesCountInColumnPerPatternGroup ;

                    Blocks.Pattern.CompositeInPatternPrefabComponent compositeInPatternPrefab = a_requestAddComposites2PatternPrefab [ i_index ] ;
                    
                    bool isYaxisChanged = false ;

                    if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                    {
                        isYaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.y - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.y ) > 0.001f ; // check for floating point error
                    }

                    bool isZaxisChanged = false ;

                    if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                    {
                        isZaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.z - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.z ) > 0.001f ; // check for floating point error
                    }

                    if ( isYaxisChanged || isZaxisChanged ||
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

                bool isZaxisChanged = false ;

                if ( i_lastDifferentCompositeTypeIndex >= 0 ) // make sure index is none negative
                {
                    isZaxisChanged = math.abs ( compositeInPatternPrefab.f3_position.z - a_requestAddComposites2PatternPrefab [i_lastDifferentCompositeTypeIndex].f3_position.z ) > 0.001f ; // check for floating point error
                }

                if ( isXaxisChanged || isZaxisChanged ||
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
            // int i_int32 = (int) ( System.DateTime.UtcNow.Millisecond + UnityEngine.Time.time * 1000 ) ;
            int i_int32 = 6879794 ;
            Unity.Mathematics.Random random = new Unity.Mathematics.Random ( global::System.Convert.ToUInt32 ( i_int32 ) ) ;

            return random ;
        }

    }
    
}