using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
// using Samples.Common;

namespace ECS.Octree.Point
{    

    // [UpdateBefore(typeof(TransformInputBarrier))]
    // [UpdateBefore(typeof(WaitForFixedUpdate))]
    // [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    // [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.Initialization ) )]
    public class BoundingOctreeSystem : JobComponentSystem
    {
        
        [ReadOnly] static public EntityArchetype archetype ;

        // [ReadOnly] static public float f_initialBoundRegion = 3 ;
        // [ReadOnly] static public float f_minNodeSize = 1f ;
        
            
        [Inject] private InitializeData initializeData ;

        struct InitializeData
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            [ReadOnly] public ComponentDataArray <OctreeTag> a_octreeTags ;    
            [ReadOnly] public ComponentDataArray <Common.Components.InitializeTag> a_InitializeTags ;  
            
            // public ComponentDataArray <DisableSystemTag> a_disabledSystem ;  
        }


        [Inject] private Data data ;

        struct Data
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            [ReadOnly] public ComponentDataArray <OctreeTag> a_octreeTags ;    
            
            // public ComponentDataArray <DisableSystemTag> a_disabledSystem ;  
        }



        [Inject] private Barrier octrreBarrier ;

        static private EntityManager entityManager ;
        static private EntityCommandBuffer commandsBuffer ;
        

        static private EntityInstance emptyEntityStruct = new EntityInstance () ;
        static private Entity lastRayCastedEntity = new Entity () ;
        static private bool isEntityRayCasted = false ;
        
        /// <summary>
        /// Stores list of octrees, with owned node, default shoulld be 1 octree
        /// </summary>
        static public List <BoundingOctreeAddNodeSystem.Nodes> l_octrees = new List <BoundingOctreeAddNodeSystem.Nodes> ();

        protected override void OnDestroyManager ( )
        {
            BoundingOctreeAddNodeSystem.Nodes octree = l_octrees [0] ;
            octree.a_bounds.Dispose () ;
            octree.a_rootNodeIndex.Dispose () ;
            octree.a_childrenNodeIndex.Dispose () ;
            octree.a_childrenNodesCount.Dispose () ;
            octree.a_entitiesInNodeCount.Dispose () ;
            octree.a_entitiesInNodes.Dispose () ;
            octree.a_baseLength.Dispose () ;
            octree.a_actualLength.Dispose () ;

            base.OnDestroyManager ( ) ;
        }
        protected override void OnCreateManager (int capacity)
        {
            // commandsBuffer = addNodeBarrier.CreateCommandBuffer () ; // this may be not required

            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

            archetype = entityManager.CreateArchetype 
            (
                typeof (OctreeTag),   
                typeof (Common.Components.InitializeTag)

                // typeof (ECS.Test02.RayCastComponent)
            ) ;          

            
            // create new main root octree with no leafs
            // Form this point octree will grow, or shrink, when new entities are added to the tree.
            // Contains initial octree settings
            _AddOctree ( ) ;
            
            base.OnStartRunning ( );

        }
        
        static private void _Initialize ()
        {

            Debug.Log ( "Initialize" ) ;
            //Nodes octree = l_octrees [0] ;

            _CreateTestNodes () ;

        }
        
        /*
        struct Initialize : IComponentSystem 
        {
            public EntityCommandBuffer commandsBuffer ;

            [ReadOnly] public InitializeData initializeData ;

            public void Execute ()
            {
                Debug.Log ( "Initialize2" ) ;

                for (int i = 0; i < initializeData.Length; ++i )
                {
                    Entity entity = initializeData.a_entities [i] ; // not the Octree Node

                    _Initialize () ;

                    // is initialized
                    commandsBuffer.RemoveComponent <InitializeData> (entity) ;
                }
            }
        }
        */

        // [BurstCompile]
        // IJobParallelFor // Adding nodes should NOT be parallerised, as they are referenced between each other 
        struct AddNode : IJob 
        {
            // [ReadOnly] public float f_nextT ;
            [ReadOnly] public bool b_executeNow ;
            [ReadOnly] public Data data ;
            // [ReadOnly] public EntityArray a_entities;
            // public ComponentDataArray <AddNodeTag> a_nodeTags ;
            
            static private Ray previousRay = new Ray () ;

            public EntityCommandBuffer commandsBuffer ;

            public void Execute ()
            {
                // test frequency only
                if ( b_executeNow )
                {

                    ECS.Test02.PlayerInputSystem.InputPointerData inputPointerData = ECS.Test02.PlayerInputSystem.inputPointerData ;
            
                    Ray ray = new Ray ( inputPointerData.rayCastData.f3_origin, inputPointerData.rayCastData.f3_direction ) ;
                    //Debug.DrawLine ( ray.origin, ray.direction * 100, Color.green, 10 ) ;
        
                    // Debug.Log ( "ray: " + ray.ToString ( "F4" ) ) ;
                    // test only
                    EntityInstance entityInstanceOut ;

                    // check if ray changed
                    // then adjust highlight
                    if ( ray.direction.sqrMagnitude != 0 && !previousRay.Equals ( ray ) )
                    {
                        
                        float f_intersectDistance = -1 ; 
                        previousRay = ray ;
                        bool isRayColliding = BoundingOctreeAddNodeSystem._IsRayColliding ( 0, ray, 9999999, out f_intersectDistance, out entityInstanceOut ) ;
                        // if ( isRayColliding ) Debug.Log ( "Ray: " + ray.origin.ToString ( "F4" ) + "; " + ray.direction.ToString ( "F4" ) ) ;
                            
                        // isRayColliding = false ;
                        // is not empty
                        if ( isRayColliding )
                        {
                            // prepare for highlighting new component
                            
                            // Debug.Log ( entityInstanceOut.entity.Index ) ;

                            Entity rayCastedEntity = entityInstanceOut.entity ;
                         
                            if ( !isEntityRayCasted )
                            {
                                isEntityRayCasted = true ;

                                lastRayCastedEntity = rayCastedEntity ;

                                // prepare for highlighting component
                                commandsBuffer.AddComponent ( rayCastedEntity, new ECS.Test02.BlockSetHighlightTag () ) ;
                                               
                                // Debug.Log ( "New intersection." ) ;

                            }
                            else if ( rayCastedEntity.Index != lastRayCastedEntity.Index || rayCastedEntity.Version != lastRayCastedEntity.Version )
                            {
                                // prepare for unhighlighting last component
                                commandsBuffer.AddComponent ( lastRayCastedEntity, new ECS.Test02.BlockResetHighlightTag () ) ;
                                                    
                                lastRayCastedEntity = rayCastedEntity ;

                                // prepare for highlighting new component
                                commandsBuffer.AddComponent ( rayCastedEntity, new ECS.Test02.BlockSetHighlightTag () ) ;

                                // Debug.Log ( "Different entity intersection" ) ;
                            }
                        
                        }
                        else if ( !isRayColliding && isEntityRayCasted ) // ray not colliding with bounding boxes of octree
                        {
                            isEntityRayCasted = false ;

                            // prepare for unhighlighting component
                            commandsBuffer.AddComponent ( lastRayCastedEntity, new ECS.Test02.BlockResetHighlightTag () ) ;
                        }
                    
                    
                        for (int i = 0; i < data.Length; ++i )
                        {
                            //Entity octreeGroupEntity = data.a_entities [i] ; // not the Octree Node

                            //OctreeTag octreeTag = entityManager.GetComponentData <OctreeTag> ( octreeGroupEntity ) ;
                            //BoundingOctreeAddNodeSystem.Nodes octreeGroup = l_octrees [octreeTag.i_octreeID] ;

                            //octreeGroup.
                            // Debug.Log ("Remove") ;
                            //commandsBuffer.RemoveComponent <AddNodeTag> ( entity2Remove ) ;
                            //commandsBuffer.DestroyEntity ( entity2Remove ) ; 
                  

                        }

                    }
                    // EntityInstance entityInstanceOut ;

                    // bool isColliding = _IsColliding ( 0, new Bounds { f3_center = new float3 (2f, 2f, 4f), f_size = 1 }, out entityInstanceOut ) ;
                    // Debug.LogWarning ( isColliding ? "yes" : "No" ) ;

                }
            }  

        }
        
        private bool isInitialized = false ;
        private float f_nextT ;
        // protected override JobHandle OnUpdate ( JobHandle inputDeps )
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
               
            if ( !isInitialized )
            {
                isInitialized = true ;
                _Initialize () ;
            }
            /*
            // run once
            Initialize initialize = new Initialize ()
            {
                // a_entities = data.a_entities,
                initializeData = initializeData,
                
                commandsBuffer = octrreBarrier.CreateCommandBuffer ()
            };

            initialize.Schedule ().Complete () ; // see also SheduleBatch ( a, b ).Complete ;
            */


            // ECS.Test02.PlayerInputSystem.InputPointerData inputPointerData = ECS.Test02.PlayerInputSystem.inputPointerData ;
            
            //Ray ray2 = new Ray ( inputPointerData.rayCastData.f3_origin, inputPointerData.rayCastData.f3_direction ) ;
            //Debug.DrawLine ( ray2.origin, ray2.origin + ray2.direction * 100, Color.green ) ;

            bool b_executeNow ;

            // run periodcally, at given frequence
            // only test
            if ( UnityEngine.Time.time >= f_nextT )
            {

                f_nextT = UnityEngine.Time.time + 0.05f ; // sec

                b_executeNow = true ;
            }
            else
            {
                b_executeNow = false ;
            }

            //return jobHandle ; // for IJobParallelFor

            
            // NativeArray <int> data = new NativeArray <int> (50, Allocator.TempJob) ;

            return new AddNode
            {
                b_executeNow = b_executeNow,

                // a_entities = data.a_entities,
                data = data,
                
                commandsBuffer = octrreBarrier.CreateCommandBuffer ()

            }.Schedule(inputDeps) ;

            //addNode.Schedule (  data.Length, 64, inputDeps ).Complete () ; // see also SheduleBatch ( a, b ).Complete ;
            
            // return base.OnUpdate ( inputDeps );
            

        }




        /* ***************************************** 
        * Methods
        * ***************************************** 
        */


        
        /// <summary>
        /// Adds new octree and creates first initial node at given origing
        /// </summary>
        /// <returns></returns>
        static private void _AddOctree ( )
        {
            BoundingOctreeAddNodeSystem.Nodes octree = new BoundingOctreeAddNodeSystem.Nodes () ;  
            
            //octree.NUM_ENTITIES_ALLOWED     = 0 ; 
            octree.NUM_ENTITIES_ALLOWED     = 1 ;  
            octree.f_minNodeSize            = 1f ; 

            int i_nodeArrayAllocationSize   = 1000000 ;// 800 ;
            
            // dispose on manager exit
            octree.a_bounds                 = new NativeArray<Bounds> ( i_nodeArrayAllocationSize, Allocator.Persistent ) ;            
            octree.a_rootNodeIndex          = new NativeArray<int> ( i_nodeArrayAllocationSize, Allocator.Persistent ) ;   
            octree.a_childrenNodeIndex      = new NativeArray<int> ( i_nodeArrayAllocationSize * 8, Allocator.Persistent ) ;
            octree.a_childrenNodesCount     = new NativeArray<byte> ( i_nodeArrayAllocationSize, Allocator.Persistent ) ;
            octree.a_entitiesInNodeCount    = new NativeArray<byte> ( i_nodeArrayAllocationSize * octree.NUM_ENTITIES_ALLOWED, Allocator.Persistent ) ;
            octree.a_entitiesInNodes        = new NativeArray<EntityInstance> ( i_nodeArrayAllocationSize * octree.NUM_ENTITIES_ALLOWED, Allocator.Persistent ) ; // kep buffer bigger than node capcity
            octree.a_baseLength             = new NativeArray<float> ( i_nodeArrayAllocationSize, Allocator.Persistent ) ;
            octree.a_actualLength           = new NativeArray<float> ( i_nodeArrayAllocationSize, Allocator.Persistent ) ;
            
            octree.f_looseness              = 1.0f ; // range 1 to 2. Shuld be clamped // range 1.0f to 2.0f // where 1 is no loosness (ordinary octree)
            octree.f_initialSize            = 1 ;
            octree.i_lastNodeIndex          = 0 ;
            octree.i_objectsCount           = 0 ;
            octree.i_rootNodeIndex          = 0 ;

            // octree = _SetValues ( octree, 0, octree.f_minNodeSize, new float3 (1,1,1) ) ;
            octree.a_baseLength [0] = octree.f_initialSize ;
            octree.a_actualLength [0] = octree.f_initialSize ;
            octree.a_bounds [0] = new Bounds () { f3_center = new float3 (1,1,1) * 0.5f, f_size = octree.f_initialSize } ;

                
            Debug.Log ( "Add initial new node to new octree" ) ;

            int i_initialNodeIndex = 0 ;
            // EntityInstance initialEntityInstance = new EntityInstance () { f3_position = new float3 (1,1,1) * 0, entity = new Entity () } ;
            // _AddNode ( 0, i_initialNodeIndex, initialEntityInstance ) ;
                      

            Debug.Log ( "Add new octree to the list" ) ;
            Debug.Log ( "Review octree id structure, as it do not considers, when octree group is deleted from the list." ) ;
            // Add octree
            l_octrees.Add ( octree ) ;

            OctreeTag octreeTag = new OctreeTag () { i_octreeID = l_octrees.Count } ;

            Entity newEntity = entityManager.CreateEntity ( archetype ) ;
            entityManager.SetComponentData <OctreeTag> ( newEntity, octreeTag ) ;

            // Add initial node with index, at origin position 0,0,0
            // _AddNodeNow ( octree ) ;
           
        }


        static private void _CreateTestNodes ()
        {

            // create new main root octree with no leafs
            // Form this point octree will grow, or shrink, when new entities are added to the tree.
            // Contains initial octree settings

            // Nodes octree = BoundingOctreeSystem.l_octrees [0] ;

            Entity testEntity ;
            EntityInstance testEntityInstance ;
                      
            
            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -3.4f, -2f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            
                        
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 0f, -2f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 0f, 8f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 0f, 0f ) * 5 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 5f, 0f, 11f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 2f, 0f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -6f, -2f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
                                               
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -3.4f, 2.5f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
                                   
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 0f, 4f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
                                    
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 0f, 0f ) * 3 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            */


            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -3.4f, -2f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
                                    
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 0f, 8f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 0f, 0f ) * 5 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
                        
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 5f, 0f, 11f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 2f, 0f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -3.4f, 3.5f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
                                   
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 0f, 4f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
                    
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 7f, 0f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
                        
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 1f, 0f ) * 3 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 1f, 0f ) * 4 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 1f, 0f ) * 5 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 2f, 1f, 0f ) * -1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 2f, 2f, 4f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, -3f, 0f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

            */
            
            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 1f, 0f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 2f, 0f ) * 1 } ;
            BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            */

            /*
            // System.Random rand = new System.Random () ;
            for ( int i = 0; i < 10; i ++ )
            {
                float x = rand.Next ( -10, 10 ) * 1f ;
                float y = rand.Next ( -10, 10 ) * 1f ;
                float z = rand.Next ( -10, 10 ) * 1f ;

                testEntity = entityManager.CreateEntity () ;
                testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( x, y, z ) * 1 } ;
                BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;
            }
            */

            
            /*
            int i_copyArraySize = 107 ;
            int i_indexOffset = 0 ;
            NativeArray <EntityInstance> a_entitiesInstancesCopy = new NativeArray<EntityInstance> ( i_copyArraySize, Allocator.Temp ) ;
            
            
            float y = 0 ;
            float z = 0 ;
            for ( int x = -4; x < 50; x ++ )
            {
                testEntity = entityManager.CreateEntity () ;
                //testEntity = commandsBuffer.CreateEntity () ;
                // test example entity instance
                testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( x +0.5f, -y +0.5f, z +0.5f ) * 1 } ;
                BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

                i_indexOffset++ ;

                // Debug.Log ( i_indexOffset ) ;
                a_entitiesInstancesCopy [i_indexOffset] = testEntityInstance ;
            }

            a_entitiesInstancesCopy.Dispose () ;
            */

            /*
            // int i_copyArraySize = 5 * 11 * 21 ;
            int i_copyArraySize = 100 * 50 * 100 ;
            int i_indexOffset = 0 ;
            NativeArray <EntityInstance> a_entitiesInstancesCopy = new NativeArray<EntityInstance> ( i_copyArraySize, Allocator.Temp ) ;
            
            for ( int y = -25; y < 24; y ++ )
            {
                for ( int z = -50; z < 49; z ++ )
                {

                    for ( int x = -50; x < 49; x ++ )
                    {
                        testEntity = entityManager.CreateEntity () ;
                        //testEntity = commandsBuffer.CreateEntity () ;
                        // test example entity instance
                        testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( x +0.5f, -y +0.5f, z +0.5f ) * 1 } ;
                        BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

                        i_indexOffset++ ;

                        // Debug.Log ( i_indexOffset ) ;
                        a_entitiesInstancesCopy [i_indexOffset] = testEntityInstance ;
                    }

                }
            } // for
                        
            a_entitiesInstancesCopy.Dispose () ;
            */

            /// small stucture
            /*
            // int i_copyArraySize = 5 * 11 * 21 ;
            int i_copyArraySize = 4 * 5 * 4 ;
            int i_indexOffset = 0 ;
            NativeArray <EntityInstance> a_entitiesInstancesCopy = new NativeArray<EntityInstance> ( i_copyArraySize, Allocator.Temp ) ;
            
            for ( int y = 0; y < 3; y ++ )
            {
                for ( int z = -2; z < 2; z ++ )
                {

                    for ( int x = -1; x < 2; x ++ )
                    {
                        testEntity = entityManager.CreateEntity () ;
                        //testEntity = commandsBuffer.CreateEntity () ;
                        // test example entity instance
                        testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( x +0.5f, -y +0.5f, z +0.5f ) * 1 } ;
                        BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

                        i_indexOffset++ ;

                        // Debug.Log ( i_indexOffset ) ;
                        a_entitiesInstancesCopy [i_indexOffset] = testEntityInstance ;
                    }

                }
            } // for
                        
            a_entitiesInstancesCopy.Dispose () ;
            */
            /*
            // int i_copyArraySize = 5 * 11 * 21 ;
            int i_copyArraySize = 12 * 15 * 7 ;
            int i_indexOffset = 0 ;
            NativeArray <EntityInstance> a_entitiesInstancesCopy = new NativeArray<EntityInstance> ( i_copyArraySize, Allocator.Temp ) ;
            
            for ( int y = 0; y < 11; y ++ )
            {
                for ( int z = -7; z < 8; z ++ )
                {

                    for ( int x = -3; x < 3; x ++ )
                    {
                        testEntity = entityManager.CreateEntity () ;
                        //testEntity = commandsBuffer.CreateEntity () ;
                        // test example entity instance
                        testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( x +0.5f, -y +0.5f, z +0.5f ) * 1 } ;
                        BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

                        i_indexOffset++ ;

                        // Debug.Log ( i_indexOffset ) ;
                        a_entitiesInstancesCopy [i_indexOffset] = testEntityInstance ;
                    }

                }
            } // for
            
            // a_entitiesInstancesCopy.Dispose () ;
            
            
            // clone
            float3 f3_posOffset = new float3 ( 0, 10 , 0 ) ;
            float3 f_scale = 1 ;
            float4 f4_octreNodeColor = new float4 ( 1,1,1,1 ) ;
            
            for ( int x = 1; x < 5; x ++ )
            {
            
                for ( int y = 1; y < 5; y ++ )
                {

                    for ( int z = 1; z < 5; z ++ )
                    {
                        
                        // duplicate and offset instances
                        for ( int i = 0; i < a_entitiesInstancesCopy.Length; i ++ )
                        {
                            EntityInstance testEntityInstance2 = a_entitiesInstancesCopy [i] ;

                            testEntityInstance2.f3_position += f3_posOffset  ;
                            testEntityInstance2.entity = entityManager.CreateEntity () ; // new entity
                            
                            // entityManager.AddComponent ( testEntityInstance2.entity, typeof (Unity.Rendering.MeshCulledComponent) ) ;
                            if ( y < 4 )
                            {
                                // testEntityInstance2.f3_position = new float3 ( 0,0,0 ) ;
                                // entityManager.AddComponent ( testEntityInstance2.entity, typeof (Unity.Rendering.MeshCulledComponent) ) ;
                            }
                            // testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = testEntityInstance2.f3_position + new float3 ( 0, y_offset, 0 ) * 1 } ;
                            // BoundingOctreeAddNodeSystem._AddNodeRequest ( 0, testEntityInstance ) ;

                            ECS.Test02.AddBlockSystem._AddBlockRequestWithEntity ( testEntityInstance2.entity, testEntityInstance2.f3_position, f_scale, new float3 (), new Entity (), f4_octreNodeColor ) ; // test
                            // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f_addAtPosition, new float3 (), new Entity (), f_octreNodeColor ) ; // test
                        }

                        f3_posOffset = new float3 ( x * 20, y * 10, z * 10 ) ;

                    }

                }

            }
                        
            a_entitiesInstancesCopy.Dispose () ;
            */

            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 1f, 0f ) * 4 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            */

            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 4f, 0f ) * 7 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 0f, 1f ) * 5 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;

            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 1f, 2f, 1f ) * 2 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            */

            /*
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 2f, 0f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            
            testEntity = entityManager.CreateEntity () ;
            testEntityInstance = new EntityInstance () { entity = testEntity, f3_position = new float3 ( 0f, 3f, 0f ) * 1 } ;
            _AddNodeRequest ( 0, testEntityInstance ) ;
            */

        }


    }

}