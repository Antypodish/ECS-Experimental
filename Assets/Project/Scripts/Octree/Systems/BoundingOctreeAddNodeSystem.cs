using System.Collections.Generic;
using UnityEngine;

// A Dynamic, Loose Octree for storing any objects that can be described with AABB bounds
// See also: PointOctree, where objects are stored as single points and some code can be simplified
// Octree:	An octree is a tree data structure which divides 3D space into smaller partitions (nodes)
//			and places objects into the appropriate nodes. This allows fast access to objects
//			in an area of interest without having to check every object.
// Dynamic: The octree grows or shrinks as required when objects as added or removed
//			It also splits and merges nodes as appropriate. There is no maximum depth.
//			Nodes have a constant - numObjectsAllowed - which sets the amount of items allowed in a node before it splits.
// Loose:	The octree's nodes can be larger than 1/2 their parent's length and width, so they overlap to some extent.
//			This can alleviate the problem of even tiny objects ending up in large nodes if they're near boundaries.
//			A looseness value of 1.0 will make it a "normal" octree.
// T:		The content of the octree can be anything, since the bounds data is supplied separately.

// Octree based on Nition/UnityOctree C# OOP, March 2018 by Nitron
// Nition/UnityOctree
// https://github.com/Nition/UnityOctree
// And converted to ECS, By Dobromil K Duda August 2018




using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Mathematics.Experimental;
using Unity.Transforms;
using UnityEngine;
// using Samples.Common;

namespace ECS.Octree.Point
{    

    // [UpdateBefore(typeof(TransformInputBarrier))]
    // [UpdateBefore(typeof(WaitForFixedUpdate))]
    //[UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.FixedUpdate))]
    // [UpdateAfter ( typeof (BoundingOctreeSystem) )]
    // [UpdateBefore ( typeof ( UnityEngine.Experimental.PlayerLoop.Initialization ) )]
    public class BoundingOctreeAddNodeSystem : JobComponentSystem
    {
        
        [ReadOnly] static public EntityArchetype nodeArchetype ;

        // [ReadOnly] static public float f_initialBoundRegion = 3 ;
        // [ReadOnly] static public float f_minNodeSize = 1f ;
        
            
        [Inject] private Data data ;

        struct Data
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            [ReadOnly] public ComponentDataArray <AddNodeTag> a_nodeTags ;   
            [ReadOnly] public ComponentDataArray <BoundsOctreeNodeTag> a_boundTreeNodeTags ;                
        }


        [Inject] private Barrier addNodeBarrier ;

        static private EntityManager entityManager ;
        static private EntityCommandBuffer commandBuffer ;
        


        /// <summary>
        /// Octree nodes
        /// </summary>
        public struct Nodes
        {
            // The total amount of nodes currently in the tree
	        public int i_objectsCount ;

            /// <summary>
            /// The last index indicating latest inserted node
            /// This index always grows, when node is added.
            /// When any node is removed, this index don't change.
            /// This prevents entity collisions in array.
            /// </summary>
            public int i_lastNodeIndex ;

            //public int i_totalNodesCount ;
            // The total amount of objects currently in the tree
            //public int i_totalObjectsCount ;

            // If there are already numObjectsAllowed in a node, we split it into children
	        // A generally good number seems to be something around 8-15       
           // [ReadOnly] public int NUM_ENTITIES_ALLOWED ; // = 2 ; // efault 8, suitable 16. Do NOT excced 256     

             
            // [ReadOnly] public int NUM_OBJECTS_ALLOWED_DOUBLE ; // = NUM_OBJECTS_ALLOWED * 2 ;   
            // Allows for extra capacity of objects, if is bigger than allowed number of objects
            [ReadOnly] public int NUM_ENTITIES_ALLOWED ; // = NUM_OBJECTS_ALLOWED * 2 ;   


            // Root node of the octree
	        // public NodeComponent rootNode;
            public int i_rootNodeIndex ;

            // Size that the octree was on creation
	        public float f_initialSize ;

            // Minimum side length that a node can be. Essentially an alternative to having a max depth
            // Minimum size for a node in this octree
	        public float f_minNodeSize ; // defualt 1.0f

            /// <summary
            /// Looseness value for this node
            /// Range 1.0f to 2.0f. Shuold be clamped.
            /// </summary>
            
	        public float f_looseness ;



            // nodes properties


            // Centre of this node
	        // public NativeArray <half3> a_center ;

            public NativeArray <int> a_rootNodeIndex ; // 8 children

            /// <summary>
            /// Medium precision floating point value; generally 16 bits (range of –60000 to +60000, with about 3 decimal digits of precision).
            /// https://docs.unity3d.com/Manual/SL-DataTypesAndPrecision.html
            /// Length of this node if it has a looseness of 1.0
            /// </summary>	        
	        public NativeArray <float> a_baseLength ;

            // Actual length of sides, taking the looseness value into account
	        public NativeArray <float> a_actualLength ;

            
            /// <summary>
            /// Bounding box that represents this node
	        /// Public UnityEngine.Bounds bounds ; 
            /// Is blittable
            /// Also contains center of this node
            /// </summary>
            public NativeArray <Bounds> a_bounds ; // default
            
            /// <summary>
            /// Child nodes, if any
            /// State 0, or 8 elements 2x2x2 cube
            /// TODO: change to bool, when become blittable, to indicate only flag, wheterh inserts are presents
            /// </summary>
            public NativeArray <byte> a_childrenNodesCount ;

            // uses index multiplied by 8 as offset
            /// <summary>
            /// Indexes of children nodes indexes
            /// </summary>
            public NativeArray <int> a_childrenNodeIndex ; // 8 children per node

            // Child nodes, if any

            // Bounds of potential children to this node. These are actual size (with looseness taken into account), not base size
            //public NativeArray <Bounds> a_childrenNodeBounds ; // 8 children

            /// <summary>
            /// Objects entites count in this node                        
            /// TODO: change to bool, when become blitable, to indicate only flag, wheather inserts are presents, or not
            /// </summary>
            public NativeArray <byte> a_entitiesInNodeCount ;    

            /// <summary>
            /// Stores objects' entities list per node.
            /// When object is destroyed, method Remove from octree should be called.
            /// Has capcity of temporarly store overflow of object in a nonde
            /// Then node is split into smaller nodes, when required, or tree is expanded.
            /// Size as per NUM_OBJECTS_ALLOWED_PLUS_ONE (with overflow buffer)
            /// </summary>            
            public NativeArray <EntityInstance> a_entitiesInNodes ; 



	// If there are already numObjectsAllowed in a node, we split it into children
	// A generally good number seems to be something around 8-15
	// public const int numObjectsAllowed = 8;
            
            /*
            /// <summary>
            /// Child nodes, if any
            /// State 0, or 8 elements 2x2x2 cube
            /// TODO: change to bool, when become blittable, to indicate only flag, wheterh inserts are presents
            /// </summary>
            public  NativeArray <byte> a_childrenNodesCount ;
            */
        }


        //NativeArray <int> a_childrenNodeIds ;

        protected override void OnCreateManager (int capacity)
        {
            // commandsBuffer = addNodeBarrier.CreateCommandBuffer () ; // this may be not required

            if ( !nodeArchetype.Valid )
            {
                entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

                nodeArchetype = entityManager.CreateArchetype 
                (
                    typeof (AddNodeTag),
                    typeof (BoundsOctreeNodeTag)

                    // typeof (ECS.Test02.RayCastComponent)
                ) ;
            }

        }


        

        // [BurstCompile]
        // IJobParallelFor // Adding nodes should NOT be parallerised, as they are referenced between each other 
        struct AddNode : IJob 
        {
            //[ReadOnly] public bool isInitialized ;
            [ReadOnly] public Data data ;
            // [ReadOnly] public EntityArray a_entities;
            // public ComponentDataArray <AddNodeTag> a_nodeTags ;

            public EntityCommandBuffer commandsBuffer ;

            public void Execute ()
            {
                for (int i = 0; i < data.Length; ++i )
                {                    
                    Entity entity2Remove = data.a_entities [i] ;

                    AddNodeTag addNodeTag = data.a_nodeTags [i] ;

                    Nodes octree = BoundingOctreeSystem.l_octrees [addNodeTag.i_octreeIndex] ;
                     
                    // Debug.Log ( "Add to root node #" + octree.i_rootNodeIndex ) ;   
                    _AddEntity ( addNodeTag.i_octreeIndex , addNodeTag.entityInstance ) ;

                    // Debug.Log ("Remove entity, once octree node has been created.") ;                    
                    commandsBuffer.DestroyEntity ( entity2Remove ) ; 
                  

                } // for

                EntityInstance entityInstanceOut ;
                bool isColliding = _IsColliding ( 0, new Bounds { f3_center = new float3 (2f, 2f, 4f), f_size = 1 }, out entityInstanceOut ) ;
                Debug.LogWarning ( isColliding ? "yes" : "No" ) ;
                               
            }  

        }
        /* 
        // Parallel For example
        public void Execute(int index)
        {
            var hash = GridHash.Hash(positions[index].Value, cellRadius);
            hashMap.Add(hash, index);
        }
        */
        
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {        
            commandBuffer = addNodeBarrier.CreateCommandBuffer () ;

            return new AddNode
            {
                data = data,
                
                commandsBuffer = commandBuffer

            }.Schedule (inputDeps) ;
            
        }


        /* ***************************************** 
        * Methods
        * ***************************************** 
        */


        /// <summary>
        /// Call it from whatever place, to add object instance to octree.
        /// Takes index of octree (not the node) as imput and object position to insert.
        /// </summary>
        static public void _AddNodeRequest ( int i_octreeIndex, EntityInstance entityInstance ) 
        {
            // Debug.Log ( "Add node request" ) ;
            // add new octrees if required
            while ( BoundingOctreeSystem.l_octrees.Count < i_octreeIndex + 1)
            {
                BoundingOctreeSystem.l_octrees.Add ( new Nodes () ) ;
            }

            // check if archetype has been created
            if ( !nodeArchetype.Valid )
            {
                entityManager = World.Active.GetOrCreateManager <EntityManager>() ;

                nodeArchetype = entityManager.CreateArchetype 
                (
                    typeof (AddNodeTag),
                    typeof (BoundsOctreeNodeTag)

                    // typeof (ECS.Test02.RayCastComponent)
                ) ;
                
            }

            // Create an entity 
            // Temporary entity, to trigger adding new octree nodes, via command buffer.
            Entity nodeEntity = entityManager.CreateEntity ( nodeArchetype ) ; 

            // entityManager.AddComponent( nodeEntity, typeof ( Unity.Rendering.MeshCulledComponent ) );

            AddNodeTag addNodeTag = new AddNodeTag () { i_octreeIndex = i_octreeIndex, entityInstance = entityInstance } ;
            entityManager.SetComponentData <AddNodeTag> ( nodeEntity, addNodeTag ) ;
                               
            
            
            // test debug     
            float3 f_scale = new float3 (1,1,1) ;

            // set group of nodes same random color
            // float4 f_octreNodeColor = new float4 ( 1, 1, 1, 1 ) * 1 ; // z axis is important // for octree semi transparent rendering
            float4 f_octreNodeColor = new float4 ( 1, 1, 1, 1 ) * 0 ; // z axis is important // for normal opque rendering
       
            // test debug
            ECS.Test02.AddBlockSystem._AddBlockRequestWithEntity ( entityInstance.entity, entityInstance.f3_position, f_scale, new float3 (), new Entity (), f_octreNodeColor ) ; // test
            // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f_addAtPosition, new float3 (), new Entity (), f_octreNodeColor ) ; // test
            // Debug.Log ( "Added new bock for octree node #" + i_octreeIndex + " at position: " + entityInstance.f3_position ) ;

        }
        
        static int count2 = 0 ; // test only

        /// <summary>
        /// Add node with object instance, as entity
        /// </summary>
        /// <param name="i_octreeIndex"></param>
        /// <param name="entityInstance"></param>
        /// <param name="objectBounds"></param>
        /// <param name="entity2Remove"></param>
        static private void _AddEntity ( int i_octreeIndex, EntityInstance entityInstance )
        {
                  
            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            int i_nodeIndex = octree.i_rootNodeIndex ;
           
            // Add object or expand the octree until it can be added
		    int count = 0; // Safety check against infinite/excessive growth
            // Debug.Log ( "**** _AddNode # " + count2 + " Add Node" ) ; 
            
            // Create principle bounds, which uses predefined by octree, size of bounding cube.
            Bounds entityBounds = _Unity2ECSBounds ( new UnityEngine.Bounds ( entityInstance.f3_position, new float3 ( 1, 1, 1 ) * octree.f_minNodeSize ) ) ;

		    while ( !_Add2RootNode ( i_octreeIndex, i_nodeIndex, entityInstance, entityBounds ) ) 
            {
                float3 f3_growthDirection = entityInstance.f3_position - octree.a_bounds [i_nodeIndex].f3_center ;
                // Debug.Log ( "-- While: " + count ) ;
                
                // grow the tree
			    octree = _Grow ( i_octreeIndex, i_nodeIndex, f3_growthDirection ) ;
                
                i_nodeIndex = octree.i_rootNodeIndex ;

                BoundingOctreeSystem.l_octrees [i_octreeIndex] = octree ; // save changes

                // Debug.Log ( "Grow: New root node #" + i_nodeIndex ) ;

			    if ( ++count > 20) {
				    Debug.LogError("Aborted Add operation as it seemed to be going on forever (" + (count - 1) + ") attempts at growing the octree.");
				    return;
			    }
		    }		    

            count2 ++ ; // test only

            //octree.i_objectsCount ++;
            
        }


        /// <summary>
	    /// Add an root entity.
	    /// </summary>
	    /// <param name="entityInstance">Entity instance to add.</param>
	    /// <param name="entityBounds">3D bounding box around the object.</param>
	    /// <returns>True if the object fits entirely within this node.</returns>
	    static private bool _Add2RootNode ( int i_octreeIndex, int i_nodeIndex, EntityInstance entityInstance, Bounds entityBounds ) 
        {            
            // Nodes rootNode = l_octrees [i_rootNodeIndex] ;
            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            Bounds outerBounds = octree.a_bounds [i_nodeIndex] ;

		    if ( !_Encapsulates ( outerBounds, entityBounds ) )
            {
			    return false;
		    }

            // add if objects is encapsulated in the boundaries
		    octree = _SubAdd ( octree, i_nodeIndex, entityInstance, entityBounds ) ;


            // save updated octree
            BoundingOctreeSystem.l_octrees [i_octreeIndex] = octree ;
                        
            // test debug     
            // float3 f3_scale = new float3 (1,1,1) * octree.a_bounds [i_nodeIndex].f_size ;
            //float3 f3_position = octree.a_bounds [i_nodeIndex].f3_center ;

            // set group of nodes same random color
            // float4 f_octreNodeColor = new float4 ( 1, 1, 1, 0.1f ) * 1; // z axis is important
       
            // test debug
            // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f3_position, f3_scale, new float3 (), new Entity (), f_octreNodeColor ) ; // test
            // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f_addAtPosition, new float3 (), new Entity (), f_octreNodeColor ) ; // test
            // Debug.Log ( "Added new bock for octree node #" + i_octreeIndex + " at position: " + entityInstance.f3_position + " Size: " + octree.a_bounds [i_nodeIndex].f_size ) ;

		    return true;
	    }

        /// <summary>
	    /// Private counterpart to the public Add method.
	    /// </summary>
	    /// <param name="entityInstance">Entity instance to add.</param>
	    /// <param name="objBounds">3D bounding box around the object.</param>
	    static private Nodes _SubAdd ( Nodes octree, int i_nodeIndex, EntityInstance entityInstance, Bounds entityBounds ) 
        {

            Bounds boundsTest = octree.a_bounds [i_nodeIndex] ;
            // Debug.Log ( "SubAdd Position: " + boundsTest.f3_center + " size: " + boundsTest.f_size ) ;

		    // We know it fits at this level if we've got this far
		    // Just add if few objects are here, or children would be below min size            
		    if ( octree.a_entitiesInNodeCount [i_nodeIndex] < octree.NUM_ENTITIES_ALLOWED || ( octree.a_baseLength [i_nodeIndex] * 0.5f ) < octree.f_minNodeSize ) 
            {
                // Add entities here, if number do not exceed the permited count, or if new entity is smaller, than minimum permited node size
                
                EntityInstance emptyEntityStruct = new EntityInstance () ;
                
                int i_newNodeOffsetIndex = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;

                // find empty index and assign to it
                for ( int i = i_newNodeOffsetIndex; i < i_newNodeOffsetIndex + octree.NUM_ENTITIES_ALLOWED; i ++ )
                {
                    // Check if is object index empty (not used)?
                    if ( octree.a_entitiesInNodes [i].entity.Equals ( emptyEntityStruct.entity ) )
                    {
                        // assign new entity instance to empty index
                        octree.a_entitiesInNodes [i] = entityInstance ;
                        // increase entity counter of the parent node
                        octree.a_entitiesInNodeCount [i_nodeIndex] ++ ;
                        
                        break ;
                    }
                } // for

			    // Debug.Log("ADD " + obj.name + " to depth " + depth);

		    }
		    else 
            {
			    // Fits at this level, but we can go deeper. Would it fit there?
                
                int i_bestFirChild ;

			    // Create the 8 children
			    int i_bestFitChildNodeIndex ;
                float3 f3_nodeCenter = octree.a_bounds [i_nodeIndex].f3_center ;
                EntityInstance emptyEntityStruct = new EntityInstance () ;

                int i_bestFitChildIndex ;
                
			    if ( octree.a_childrenNodesCount [i_nodeIndex] == 0 ) // if has no children, then split octree
                {
                    int i_entitiesAllowedPlusOne = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;

				    octree = _Split ( octree, i_nodeIndex ) ;
                    int i_splitParentOffset = i_nodeIndex * 8 ;
                    
                    // Debug.Log ( "--- Tst Best Fit" ) ;

				    // Now that we have the new children, see if this node's existing objects would fit there
                    // for ( int i_childIndex = i_objectsAllowedDoubled + octree.NUM_OBJECTS_ALLOWED_DOUBLE - 1; i_childIndex >= i_objectsAllowedDoubled; i_childIndex--) 
				    // for (int i = objects.Count - 1; i >= 0; i--) 
                    // for ( int i_childIndex = i_nodeIndex * 8 + 8 - 1; i_childIndex >= i_objectsAllowedDoubled; i_childIndex--) 
                    // iterate through possible objects in the node, with overflow buffer
                    // Count down, so if removing object, is removed from top list of entities in this node. 
                    for ( int i_entityInNodeIndex = i_entitiesAllowedPlusOne + octree.NUM_ENTITIES_ALLOWED - 1; i_entityInNodeIndex >= i_entitiesAllowedPlusOne; i_entityInNodeIndex--) 
                    {
                        // Debug.Log ( "i_entityInNodeIndex #" + i_entityInNodeIndex ) ;

                        EntityInstance entityInstanceInChildNode = octree.a_entitiesInNodes [ i_entityInNodeIndex ] ;

                        // Check if entityStruct is not empty
                        // Use only assigned entities.
                        if ( !entityInstanceInChildNode.Equals ( emptyEntityStruct ) )
                        {
					    
                            // i_childIndex + Range (0 to 7)
                            i_bestFirChild = _BestFitChild ( f3_nodeCenter, entityInstanceInChildNode.f3_position ) ;
                            i_bestFitChildIndex = i_splitParentOffset + i_bestFirChild ;

                            // Debug.Log ( "i_bestFitChildIndex #" + i_bestFitChildIndex + " at pos: " + entityInstanceInChildNode.f3_position ) ;

					        // Find which child the object is closest to based on where the
					        // object's center is located in relation to the octree's center.
					        i_bestFitChildNodeIndex = octree.a_childrenNodeIndex [i_bestFitChildIndex] ; // get child node index, in child list of selected node
                            
                            // Debug.Log ( "i_bestFitChildNodeIndex #" + i_bestFitChildNodeIndex ) ;
                                                    
                            // create new minimal size bounds, for the node entity 
                            Bounds entityBoundsInNode = _Unity2ECSBounds ( new UnityEngine.Bounds ( entityInstanceInChildNode.f3_position, new float3 (1, 1, 1) * octree.f_minNodeSize ) ) ;

                            // Debug.Log ( "entityBoundsInNode #" + entityBoundsInNode.f3_center ) ;

					        // Does it fit?
                            // if bounds center of entity, is inside the base bounds,
                            // then move entity to the next deeper level.
					        if ( _Encapsulates ( octree.a_bounds [i_bestFitChildNodeIndex], entityBoundsInNode ) ) 
                            {						        
                                octree = _SubAdd ( octree, i_bestFitChildNodeIndex, entityInstanceInChildNode, entityBoundsInNode ) ; // Go a level deeper		                                

                                // get child node location offset
                                int i_entitiesCount = octree.a_entitiesInNodeCount [i_nodeIndex] ;
                                int i_childNodeOffset = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;
                                                                
                                octree.a_entitiesInNodes [i_childNodeOffset + i_entitiesCount - 1] = emptyEntityStruct ; // reset
                                octree.a_entitiesInNodeCount [i_nodeIndex] -- ; // reduce objects count
					        }
                        }

				    } // for
			    }

			    // Now handle the new object we're adding now
                i_bestFirChild = _BestFitChild ( f3_nodeCenter, entityBounds.f3_center ) ;
                int i_parentNodeOffset = i_nodeIndex * 8 ;
                i_bestFitChildIndex = i_parentNodeOffset + i_bestFirChild ;

                // Find which child the object is closest to based on where the
				// object's center is located in relation to the octree's center.
                i_bestFitChildNodeIndex = octree.a_childrenNodeIndex [i_bestFitChildIndex] ; // get child node index, in child list of selected node
				
			    if ( _Encapsulates ( octree.a_bounds [i_bestFitChildNodeIndex], entityBounds ) ) 
                {
				    octree = _SubAdd ( octree, i_bestFitChildNodeIndex, entityInstance, entityBounds) ;
			    }
			    else 
                {
                    int i_newNodeOffsetIndex = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;

                    // find empty index and assign to it
                    for ( int i = i_newNodeOffsetIndex; i < i_newNodeOffsetIndex + octree.NUM_ENTITIES_ALLOWED; i ++ )
                    {

                        // Check if is object index empty (not used)?
                        if ( octree.a_entitiesInNodes [i].entity.Equals ( emptyEntityStruct.entity ) )
                        {
                            octree.a_entitiesInNodes [i] = entityInstance ;
                            octree.a_entitiesInNodeCount [i] ++ ;
                        }

                    }

			    }

		    }

            return octree ;
	    }

        /// <summary>
	    /// Find which child node this object would be most likely to fit in.
	    /// </summary>
	    /// <param name="f3_objPos">The object's position.</param>
	    /// <returns>One of the eight child octants.</returns>
	    static private int _BestFitChild ( float3 f3_nodeCenter, float3 f3_objPos ) 
        {
		    return ( f3_objPos.x <= f3_nodeCenter.x ? 0 : 1) + (f3_objPos.y >= f3_nodeCenter.y ? 0 : 4) + (f3_objPos.z <= f3_nodeCenter.z ? 0 : 2);
	    }

        /// <summary>
	    /// Splits the octree into 8 children.
	    /// </summary>
	    static private Nodes _Split ( Nodes octree, int i_parentNodeIndex ) 
        {
            float f_parentBaseLength = octree.a_baseLength [i_parentNodeIndex] ;
		    float f_quarter = f_parentBaseLength * 0.25f ; // 1/4
		    float f_newLength = f_parentBaseLength * 0.5f ; // 1/2
            float3 f3_parentNodeCenter = octree.a_bounds [i_parentNodeIndex].f3_center ;
            
            int i_parentNodeIndexOffset = i_parentNodeIndex * 8 ;

            // assign new split node to child            
            for ( int i = 0; i < 8; i++ )
            {
                octree.i_lastNodeIndex ++ ;
                int i_newChildIndex = octree.i_lastNodeIndex ;

                // assign to new child
                float3 f3_center = new float3 () ;

                // assign center to new 8 children of the parent node
                switch ( i ) // 8 elements
                {
                    case 0 :                        
                        f3_center = f3_parentNodeCenter + new float3 (-f_quarter, f_quarter, -f_quarter) ;                        
                        break ;
                    case 1 :                        
                        f3_center = f3_parentNodeCenter + new float3 (f_quarter, f_quarter, -f_quarter) ;                        
                        break ;
                    case 2 :                        
                        f3_center = f3_parentNodeCenter + new float3 (-f_quarter, f_quarter, f_quarter) ;                        
                        break ;
                    case 3 :                        
                        f3_center = f3_parentNodeCenter + new float3 (f_quarter, f_quarter, f_quarter) ;                        
                        break ;
                    case 4 :                        
                        f3_center = f3_parentNodeCenter + new float3 (-f_quarter, -f_quarter, -f_quarter) ;                        
                        break ;
                    case 5 :                        
                        f3_center = f3_parentNodeCenter + new float3 (f_quarter, -f_quarter, -f_quarter) ;                        
                        break ;
                    case 6 :                        
                        f3_center = f3_parentNodeCenter + new float3 (-f_quarter, -f_quarter, f_quarter) ;                        
                        break ;
                    case 7 :                        
                        f3_center = f3_parentNodeCenter + new float3 (f_quarter, -f_quarter, f_quarter) ;                        
                        break ;

                }

                octree.a_childrenNodeIndex [i_parentNodeIndexOffset + i] = i_newChildIndex ;

                // assign original root node to new child node
                octree.a_rootNodeIndex [i_newChildIndex] = i_parentNodeIndex ;

                // set new node for new index
                octree = _SetValues ( octree, i_newChildIndex, f_newLength, f3_center, true ) ;

                octree.a_childrenNodesCount [i_newChildIndex] = 8 ;

                // debug test
                // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f3_center, new float3 (1,1,1) * f_newLength, new float3 (), new Entity (), f_octreNodeColor ) ; // test
                // Debug.Log ( "Octree Split: Added new block #" + i + " to node #" + octree.i_lastNodeIndex + "; at position: " + f3_center ) ;
            }

            // 0 or 8 (byte)
            // TODO, make it boolean when bit become blittable
            octree.a_childrenNodesCount [i_parentNodeIndex] = 8 ;

            return octree ;
	    }
            

            
	    /// <summary>
	    /// Checks if outerBounds encapsulates innerBounds.
	    /// </summary>
	    /// <param name="outerBounds">Outer bounds.</param>
	    /// <param name="innerBounds">Inner bounds.</param>
	    /// <returns>True if innerBounds is fully encapsulated by outerBounds.</returns>
	    static bool _Encapsulates ( Bounds outerBounds, Bounds innerBounds ) 
        {
            UnityEngine.Bounds uOuterBounds = _ECS2UnityBounds ( outerBounds ) ;
            UnityEngine.Bounds uInnerBounds = _ECS2UnityBounds ( innerBounds ) ;
                        
		    return uOuterBounds.Contains ( uInnerBounds.min ) && uOuterBounds.Contains ( uInnerBounds.max ) ;
	    }




        /// <summary>
	    /// Grow the octree to fit in all objects.
	    /// </summary>
	    /// <param name="direction">Direction to grow.</param>
	    static private Nodes _Grow ( int i_octreeIndex, int i_rootNodeIndex, Vector3 f3_direction ) 
        {
            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            // Debug.Log ( "Grow octree node #" + i_rootNodeIndex ) ;

		    int xDirection = f3_direction.x >= 0 ? 1 : -1;
		    int yDirection = f3_direction.y >= 0 ? 1 : -1;
		    int zDirection = f3_direction.z >= 0 ? 1 : -1;

            int i_rootPos = _GetRootPosIndex ( xDirection, yDirection, zDirection ) ; // range 0 to 7

            // Debug.Log ( "Grow: From Root: " + octree.a_bounds  [i_rootNodeIndex].f3_center ) ;
            // Debug.Log ( "Grow: From Root: " + octree.a_baseLength  [i_rootNodeIndex] ) ;
                
            float f_rootBaseLength = octree.a_baseLength [i_rootNodeIndex] ;
		    // BoundsOctreeNode<T> oldRoot = rootNode;
            float f_half = f_rootBaseLength * 0.5f ;
		    float f_newLength = f_rootBaseLength * 2;
		    float3 f3_newCenter = octree.a_bounds  [i_rootNodeIndex].f3_center + new float3 ( xDirection, yDirection, zDirection ) * f_half ;

            int i_oldRootNodeIndex = i_rootNodeIndex ;

		    // Create a new, bigger octree root node
            octree.i_lastNodeIndex ++ ;

            int i_newRootIndex = octree.i_lastNodeIndex ;  
            
            // Create 7 new octree children to go with the old root as children of the new root
            octree = _SetValues ( octree, i_newRootIndex, f_newLength, f3_newCenter, true ) ;
            octree.a_childrenNodesCount [i_newRootIndex] = 8 ;

            // updated base length
            f_rootBaseLength = octree.a_baseLength [i_newRootIndex] ;            
            octree.i_rootNodeIndex = i_newRootIndex ;

            // Debug.Log ( "Grow: To: " + octree.a_bounds  [i_newRootIndex].f3_center ) ;
            // Debug.Log ( "Grow: To: " + octree.a_baseLength  [i_newRootIndex] ) ;

            bool b_hasEntities = _HasAnyObjects ( octree, i_oldRootNodeIndex ) ;
            // Debug.Log ( "Entities Count : " + octree.a_entitiesInNodeCount [i_oldRootNodeIndex] ) ;
            // Debug.Log ( "Has any child and me entities : " + ( b_hasEntities ? "true" : "false" ) ) ;

            // does entity has any objects inside
		    if ( b_hasEntities ) 
            {

            	// it has children now
                octree.a_childrenNodesCount [i_newRootIndex] = 8 ;		
                        
                int i_nodeIndexOffsetMult = i_newRootIndex * 8 ;
                float f_rootLength = octree.a_baseLength [i_oldRootNodeIndex] ;

                // 8 new bounds nodes
			    for (int i = 0; i < 8; i++) 
                {
                    int i_nodeIndexOffset = octree.a_childrenNodeIndex [i_nodeIndexOffsetMult + i] ;
                    
				    if ( i == i_rootPos ) 
                    {
                        // copy old root data to current child match
                        // Debug.LogWarning ( "Root Pos Match: " + i_rootPos + " for node index #" + i_nodeIndexOffset ) ;
                        int i_testOffset = octree.a_childrenNodeIndex [i_nodeIndexOffsetMult + i] ;
                                                                        
                        octree.a_childrenNodeIndex [i_nodeIndexOffsetMult + i] = i_oldRootNodeIndex ; // alternative access route. Just for consistency
                                            
                        i_testOffset = octree.a_childrenNodeIndex [i_nodeIndexOffsetMult + i] ;
                        // Debug.Log ( "for node index #" + i_nodeIndexOffset + " for sub child #" + i ) ;
                                            
                        // Debug.Log ( "Grow: Child: #" + i + "; index offset: " + i_nodeIndexOffset + "; " + octree.a_bounds [i_nodeIndexOffset].f3_center + "; " + octree.a_baseLength [i_nodeIndexOffset] ) ;                        
                        // Debug.Log ( "Grow: Child: #" + i + "; index offset: " + i_oldRootNodeIndex + "; " + octree.a_bounds [i_oldRootNodeIndex].f3_center + "; " + octree.a_baseLength [i_oldRootNodeIndex] ) ;

				    }
				    else 
                    {
					    xDirection = i % 2 == 0 ? -1 : 1;
					    yDirection = i > 3 ? -1 : 1;
					    zDirection = (i < 2 || (i > 3 && i < 6)) ? -1 : 1;

					    // children[i] = new BoundsOctreeNode<T>(rootNode.BaseLength, minSize, looseness, newCenter + new Vector3(xDirection * half, yDirection * half, zDirection * half));

                        float3 f3_newChildCenter = f3_newCenter + new float3 ( xDirection, yDirection, zDirection ) * f_half ;
                        // get index of child node
                       
                        int i_subChildIndex = i_nodeIndexOffset * 8 ;
                        // Debug.Log ( "Set length " + f_rootBaseLength + " for node index #" + i_nodeIndexOffset + " for sub child #" + i_subChildIndex ) ; 


                        // Debug.Log ( "should be f_rootBaseLength but f_newLength works instead?" ) ;
                        octree = _SetValues ( octree, i_nodeIndexOffset, f_newLength, f3_newChildCenter, b_hasEntities ) ; 
                        
                        octree.a_rootNodeIndex [i_nodeIndexOffset] = octree.a_rootNodeIndex [i_oldRootNodeIndex] ;
                        
				    }
                
                    // Debug.Log ( "Grow: Child: #" + i + "; For child #" + ( i_nodeIndexOffsetMult + i ) + "; index offset: " + i_nodeIndexOffset + "; " + octree.a_bounds [i_nodeIndexOffset].f3_center + "; " + octree.a_baseLength [i_nodeIndexOffset] ) ;
                    // Debug.Log ( "Child Bounds: #" + i + "; For child #" + ( i_nodeIndexOffsetMult + i ) + "; " + octree.a_bounds[i_nodeIndexOffset].f3_center + "; " + octree.a_bounds[i_nodeIndexOffset].f_size ) ;
                    
			    } // for

			    // Attach the new children to the new root node
			    // rootNode.SetChildren(children);
                //octree.a_childrenNodeIndex [i_rootNodeIndex] = children ; ??
                //octree.a_childrenNodeBounds [i_rootNodeIndex] = children ; ??
                //octree.a_childrenNodesCount [i_rootNodeIndex] = 8 ;

		    }

            return octree ;
	    }

        /// <summary>
	    /// Used when growing the octree. Works out where the old root node would fit inside a new, larger root node.
	    /// </summary>
	    /// <param name="xDir">X direction of growth. 1 or -1.</param>
	    /// <param name="yDir">Y direction of growth. 1 or -1.</param>
	    /// <param name="zDir">Z direction of growth. 1 or -1.</param>
	    /// <returns>Octant where the root node should be.</returns>
	    static private int _GetRootPosIndex ( int xDir, int yDir, int zDir ) 
        {
		    int result = xDir > 0 ? 1 : 0 ;
		    if ( yDir < 0 ) result += 4 ;
		    if ( zDir > 0 ) result += 2 ;

		    return result;
	    }

	    /// <summary>
	    /// Shrink the octree if possible, else leave it the same.
	    /// </summary>
	    static private Nodes _Shrink ( Nodes octree, int i_octreeIndex, int i_nodeIndex ) 
        {
            // Nodes octree = l_octrees [i_octreeIndex] ;
            int i_rootNodeIndex = octree.a_rootNodeIndex [i_nodeIndex] ;

		    i_rootNodeIndex = _ShrinkIfPossible ( i_octreeIndex, i_rootNodeIndex, octree.f_initialSize ) ;

            octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            octree.a_rootNodeIndex [i_nodeIndex] = i_rootNodeIndex ;

            return octree ;
	    }


        /// <summary>
	    /// We can shrink the octree if:
	    /// - This node is >= double minLength in length
	    /// - All objects in the root node are within one octant
	    /// - This node doesn't have children, or does but 7/8 children are empty
	    /// We can also shrink it if there are no objects left at all!
	    /// </summary>
	    /// <param name="f_minLength">Minimum dimensions of a node in this octree.</param>
	    /// <returns>The new root, or the existing one if we didn't shrink.</returns>
	    // static public BoundsOctreeNode<T> _ShrinkIfPossible ( int i_octreeIndex, int i_nodeIndex, float f_minLength ) 
        static public int _ShrinkIfPossible ( int i_octreeIndex, int i_nodeIndex, float f_minLength ) 
        {
            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            int i_objectsCount = octree.a_entitiesInNodeCount [i_nodeIndex] ;
            int i_childrenCount = octree.a_childrenNodesCount [i_nodeIndex] ;

		    if ( octree.a_baseLength [i_nodeIndex] < (2 * f_minLength) ) {
			    return i_nodeIndex ; // index
		    }
		    if ( i_objectsCount == 0 && i_childrenCount == 0 ) {
			    return i_nodeIndex ; // index
		    }

		    // Check objects in root
		    int i_bestFit = -1 ;

            EntityInstance emptyEntityStruct = new EntityInstance () ;

            int i_childOffsetIndex = i_nodeIndex * 8 ;
            
            int i_bestFitChildIndex ;

		    for (int i = 0; i < octree.NUM_ENTITIES_ALLOWED; i++) 
            {
                
                int i_newNodeOffsetIndex = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED + i ;

                EntityInstance childEntity = octree.a_entitiesInNodes [ i_newNodeOffsetIndex ] ;

                // find empty index and assign to it
                // Check if is object index not empty (used)?
                if ( !childEntity.entity.Equals ( emptyEntityStruct.entity ) )
                {
                    
			        int newBestFit = _BestFitChild ( octree.a_bounds [i_nodeIndex].f3_center, childEntity.f3_position ) ;

			        if (i == 0 || newBestFit == i_bestFit ) 
                    {
                        i_bestFitChildIndex = octree.a_childrenNodeIndex [i_childOffsetIndex + newBestFit] ;
                                                
                        Bounds enityBounds = new Bounds () { f3_center = childEntity.f3_position, f_size = octree.f_minNodeSize } ;

				        // In same octant as the other(s). Does it fit completely inside that octant?
				        if ( _Encapsulates ( octree.a_bounds [ i_bestFitChildIndex ], enityBounds ) )
                        {
					        if (i_bestFit < 0) {
						        i_bestFit = newBestFit;
					        }
				        }
				        else {
					        // Nope, so we can't reduce. Otherwise we continue
					        return i_nodeIndex;
				        }
			        }
			        else {
				        return i_nodeIndex; // Can't reduce - objects fit in different octants
			        }

                }
                else
                {
                    return i_nodeIndex; // Can't reduce - objects fit in different octants
                }
		    } // for


		    // Check objects in children if there are any
		    if ( i_childrenCount > 0 ) 
            {

			    bool isChildHadContent = false;
                
			    for ( int i = 0; i < 8; i++ )
                {

                    int i_childIndex = octree.a_childrenNodeIndex [i_childOffsetIndex + i] ;
                    
                    // octree.a_childrenNodeIndex []

                    // has any children
				    if ( octree.a_entitiesInNodeCount [i_childIndex] > 0 ) 
                    {
                        
                        EntityInstance childEntityInstance = octree.a_entitiesInNodes [i_childIndex] ;

					    if (isChildHadContent) {

						    return i_nodeIndex; // Can't shrink - another child had content already
					    }

					    if ( i_bestFit >= 0 && i_bestFit != i ) 
                        {
						    return i_nodeIndex; // Can't reduce - objects in root are in a different octant to objects in child
					    }

					    isChildHadContent = true ;

					    i_bestFit = i;
				    }
			    }
		    }

		    // Can reduce
		    if ( i_childrenCount == 0 ) 
            {
			    // We don't have any children, so just shrink this node to the new size
			    // We already know that everything will still fit in it

                i_bestFitChildIndex = octree.a_childrenNodeIndex [i_childOffsetIndex + i_bestFit] ;

                Bounds childBounds = octree.a_bounds [i_bestFitChildIndex] ;

                Debug.Log ( "!!!!! _SetValues3 is to be changed " ) ;
			    octree = _SetValues3 ( octree, i_nodeIndex, octree.a_baseLength [i_nodeIndex] * 0.5f, childBounds.f3_center ) ;

                BoundingOctreeSystem.l_octrees [i_octreeIndex] = octree ;

			    return i_nodeIndex;
		    }

		    // No objects in entire octree
		    if ( i_bestFit == -1) 
            {
			    return i_nodeIndex;
		    }

            i_bestFitChildIndex = octree.a_childrenNodeIndex [i_childOffsetIndex + i_bestFit] ;

		    // We have children. Use the appropriate child as the new root node
            return i_bestFitChildIndex ;
	    }


            
        /// <summary>
	    /// Set values for this node. 
	    /// </summary>
	    /// <param name="f_baseLength">Length of this node, not taking looseness into account.</param>
	    /// <param name="f_minSize">Minimum size of nodes in this octree.</param>
	    /// <param name="f3_parentNodeCenter">Centre position of this node.</param>
	    static private Nodes _SetValues ( Nodes octree, int i_parentNodeIndex, float f_baseLength, float3 f3_parentNodeCenter, bool b_addChildrenBounds ) 
        {
            
            float adjLength = octree.f_looseness * f_baseLength ;
            octree.a_actualLength [i_parentNodeIndex] = adjLength ;
            octree.a_baseLength [i_parentNodeIndex] = f_baseLength ;

		    // Create the bounding box.		    
            float3 f_size = new float3 ( 1, 1, 1 ) * adjLength ;		    
            octree.a_bounds [i_parentNodeIndex] = new Bounds () { f3_center = f3_parentNodeCenter, f_size = f_size.x } ;
            
            if ( b_addChildrenBounds )
            {
                // Debug.Log ( "_SetValues has children") ;

		        float f_quarter = f_baseLength * 0.25f;
		        float f_childActualLength = f_baseLength * 0.5f * octree.f_looseness ;


                int i_parentNodeIndexOffset = i_parentNodeIndex * 8 ;

                // assign new node to child                
                for ( int i = 0; i < 8; i++ )
                {
                    
                    octree.i_lastNodeIndex ++ ;
                    int newChildIndex = octree.i_lastNodeIndex ;
             
                    float3 f3_newCenter = new float3 () ;

                    // assign center to new 8 children of the parent node
                    switch ( i ) // 8 elements
                    {
                        case 0 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (-f_quarter, f_quarter, -f_quarter) ;                        
                            break ;
                        case 1 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (f_quarter, f_quarter, -f_quarter) ;                        
                            break ;
                        case 2 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (-f_quarter, f_quarter, f_quarter) ;                        
                            break ;
                        case 3 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (f_quarter, f_quarter, f_quarter) ;                        
                            break ;
                        case 4 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (-f_quarter, -f_quarter, -f_quarter) ;                        
                            break ;
                        case 5 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (f_quarter, -f_quarter, -f_quarter) ;                        
                            break ;
                        case 6 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (-f_quarter, -f_quarter, f_quarter) ;                        
                            break ;
                        case 7 :                        
                            f3_newCenter = f3_parentNodeCenter + new float3 (f_quarter, -f_quarter, f_quarter) ;                        
                            break ;

                    }

                    // finally assign new boundaries to new child node
                    octree.a_childrenNodeIndex [i_parentNodeIndexOffset + i] = newChildIndex ;
                    
                    // assign original root node to new child node
                    octree.a_rootNodeIndex [newChildIndex] = i_parentNodeIndex ;

                    // set new node for new index                    
                    octree = _SetChildValues ( octree, newChildIndex, f_childActualLength, f3_newCenter ) ;

                                        
                    // debug test _SetValues
                    // float4 f_octreNodeColor = new float4 (1,1,1,1) ;
                    // ECS.Test02.AddBlockSystem._AddBlockRequestViaBuffer ( f3_newCenter, new float3 (1,1,1) * f_childActualLength, new float3 (), new Entity (), f_octreNodeColor ) ; // test
                    // Debug.Log ( "Octree Set: Added new block to node #" + newChildIndex + "; for loop #" + i + "; at position: " + f3_newCenter + "; base length: " + f_childActualLength + "; bounds " + octree.a_bounds [newChildIndex].f_size ) ;

                }

            }

            return octree ;
	    }

        /// <summary>
	    /// Set values for this node. 
	    /// </summary>
	    /// <param name="f_baseLength">Length of this node, not taking looseness into account.</param>
	    /// <param name="f_minSize">Minimum size of nodes in this octree.</param>
	    /// <param name="f3_parentNodeCenter">Centre position of this node.</param>
	    static private Nodes _SetValues3 ( Nodes octree, int i_nodeIndex, float f_baseLength, float3 f3_center ) 
        {

            float adjLength = octree.f_looseness * f_baseLength ;
            octree.a_actualLength [i_nodeIndex] = adjLength ;

		    // Create the bounding box.		    
            float3 f_size = new float3 ( 1, 1, 1 ) * adjLength ;
		    octree.a_bounds [i_nodeIndex] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center, f_size ) ) ;
    
		    float f_quarter = f_baseLength * 0.25f;
		    float f_childActualLength = f_baseLength * 0.5f * octree.f_looseness ;
            
		    float3 f3_childActualSize = new float3 ( 1, 1, 1) * f_childActualLength ;
           
            int nodeIndexOffset = i_nodeIndex * 8 ;
            int childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset ] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (-f_quarter, f_quarter, -f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 1] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (f_quarter, f_quarter, -f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 2] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (-f_quarter, f_quarter, f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 3] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (f_quarter, f_quarter, f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 4] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (-f_quarter, -f_quarter, -f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 5] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (f_quarter, -f_quarter, -f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 6] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (-f_quarter, -f_quarter, f_quarter), f3_childActualSize ) ) ;
            childIndexOffset = octree.a_childrenNodeIndex [ nodeIndexOffset + 7] ;
		    octree.a_bounds [childIndexOffset] = _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center + new float3 (f_quarter, -f_quarter, f_quarter), f3_childActualSize ) ) ;
                
            octree.a_childrenNodesCount [i_nodeIndex] = 8 ;

            return octree ;
	    }

        static private Nodes _SetChildValues ( Nodes octree, int i_nodeIndex, float f_baseLength, float3 f3_center ) 
        {
            // Nodes octree = l_octrees [i_octreeIndex] ;

            octree.a_baseLength [i_nodeIndex] = f_baseLength ;

            // float adjLength = octree.f_looseness * f_baseLength ;
            octree.a_actualLength [i_nodeIndex] = f_baseLength ;

		    // Create the bounding box.		    
            // float3 f_size = new float3 ( 1, 1, 1 ) * f_baseLength ;
		    // octree.a_bounds [i_nodeIndex] = // _Unity2ECSBounds ( new UnityEngine.Bounds ( f3_center, f_size ) ) ;
            octree.a_bounds [i_nodeIndex] = new Bounds () { f3_center = f3_center, f_size = f_baseLength } ;
            
            return octree ;
	    }


        static private UnityEngine.Bounds _ECS2UnityBounds ( Bounds bounds )
        {
            // Bounds bounds = new Bounds { center = uBounds.center, size = uBounds.size, min = uBounds.min, max = uBounds.max  } ;
            UnityEngine.Bounds uBounds = new UnityEngine.Bounds { center = bounds.f3_center, size = new float3 (1, 1, 1) * bounds.f_size } ;

            return uBounds ;
        }

        static private Bounds _Unity2ECSBounds ( UnityEngine.Bounds uBounds )
        {
            // Bounds bounds = new Bounds { center = uBounds.center, size = uBounds.size, min = uBounds.min, max = uBounds.max  } ;
            Bounds bounds = new Bounds { f3_center = uBounds.center, f_size = uBounds.size.x } ; // is always cubical, therefore takes only one axis

            return bounds ;
        }






        /* *************************
         * Interaction
         ************************** */

        /// <summary>
	    /// Check if the specified bounds intersect with anything in the tree. See also: GetColliding.
	    /// </summary>
	    /// <param name="checkBounds">bounds to check.</param>
	    /// <returns>True if there was a collision.</returns>
	    static public bool _IsColliding ( int i_octreeIndex, Bounds checkBounds, out EntityInstance entityInstanceOut ) 
        {		    
            // assign defualt empty entity
            entityInstanceOut = new EntityInstance () ;
            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;
            
            Debug.Log ("_IsColliding: Bounds ****************** *********** " ) ;

            UnityEngine.Bounds uCheckBounds = _ECS2UnityBounds ( checkBounds ) ;

		    return _IsCollidingChecks ( octree, octree.i_rootNodeIndex, uCheckBounds, out entityInstanceOut );
	    }


        /// <summary>
	    /// Check if the specified bounds intersect with anything in the tree. See also: GetColliding.
	    /// </summary>
	    /// <param name="checkBounds">bounds to check.</param>
	    /// <returns>True if there was a collision.</returns>
	    static public bool _IsRayColliding ( int i_octreeIndex, Ray ray, float f_maxDistance, out float f_intersectDistance, out EntityInstance entityInstanceOut ) 
        {		    
            // assign defualt empty entity
            entityInstanceOut = new EntityInstance () ;

            Nodes octree = BoundingOctreeSystem.l_octrees [i_octreeIndex] ;

            // Debug.Log ("_IsColliding: Ray ****************** *********** " ) ;
            // defualt no intersection distance is found, otherwise is positive
            
		    return _IsRayCollidingChecks ( octree, octree.i_rootNodeIndex, ray, f_maxDistance, out f_intersectDistance, out entityInstanceOut ) ;
            
            // return false ;

	    }


        /// <summary>
	    /// Check if the specified bounds intersect with anything in the tree. See also: GetColliding.
	    /// </summary>
	    /// <param name="CheckBounds">Bounds to check.</param>
	    /// <returns>True if there was a collision.</returns>
	    static private bool _IsCollidingChecks ( Nodes octree, int i_nodeIndex, UnityEngine.Bounds uCheckBounds, out EntityInstance entityInstanceOut ) 
        {            
            // assign defualt empty entity
            entityInstanceOut = new EntityInstance () ;

            Bounds bounds = octree.a_bounds [i_nodeIndex] ;

            UnityEngine.Bounds uBounds = _ECS2UnityBounds ( bounds ) ;

            Debug.Log (">> Next ****************** *********** Node index #" + i_nodeIndex ) ;
            Debug.Log ("Bounds: " + uBounds.center + "; " + uBounds.size ) ;
            Debug.Log ("Check Againts Bounds: " + uCheckBounds.center + "; " + uCheckBounds.size ) ;

            // Are the input bounds at least partially in this node?
		    if ( i_nodeIndex == 94 ) { // 4.5, 7.5, -4.5

                //Debug.Log ( "Check index #" + i_nodeIndex ) ;
		    }

		    // Are the input bounds at least partially in this node?
		    if ( !uBounds.Intersects (uCheckBounds) ) {

               // Debug.Log ( "Not Intersect: return false " ) ;
			    return false;
		    }
            
            // bool isHavingEntities = octree.a_entitiesInNodeCount [octree.i_rootNodeIndex] > 0 ? true : false ;
            bool isHavingEntities = octree.a_entitiesInNodeCount [i_nodeIndex] > 0 ? true : false ;


            // does have nay entity objects
            if ( isHavingEntities )
            {
                // used for comparison with none assigned entities instances
                EntityInstance emptyInstance = new EntityInstance () ; 

                //int i_offsetIndex = octree.i_rootNodeIndex * octree.NUM_ENTITIES_ALLOWED_PLUS_ONE ;
                int i_offsetIndex = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;

		        // Check against any objects in this node
		        for ( int i = i_offsetIndex; i < i_offsetIndex + octree.NUM_ENTITIES_ALLOWED; i++ ) 
                {
                    entityInstanceOut = octree.a_entitiesInNodes [i] ;
                    
                    // check if is not empty
                    if ( !entityInstanceOut.entity.Equals ( emptyInstance.entity ) )
                    {
                        Bounds entityBounds = new Bounds () { f3_center = entityInstanceOut.f3_position, f_size = octree.f_minNodeSize } ;
                        
                        UnityEngine.Bounds uEntityBounds = _ECS2UnityBounds ( entityBounds ) ;

                        //Debug.Log ("Object Bounds: " + uEntityBounds.center + "; " + uEntityBounds.size ) ;

			            if ( uEntityBounds.Intersects ( uCheckBounds ) ) 
                        {
                            //Debug.Log ( "#" + i + " Intersects: return true" ) ;
                                                       
                            // has at least one intersection
				            return true;
			            }
                    }
		        }
            }

            
            // When no entity is detected, this searches children, for contained entities
            bool isHavingChildren = octree.a_childrenNodesCount [i_nodeIndex] > 0 ? true : false ;

		    // Check children
		    if ( isHavingChildren ) 
            {
                
                //int i_offsetIndex = octree.i_rootNodeIndex * 8 ;
                int i_offsetIndex = i_nodeIndex * 8 ;

			    for ( int i = i_offsetIndex; i < i_offsetIndex + 8; i++ ) 
                {   
                    // get index of the node, from the children store
                    int i_childNodeIndex = octree.a_childrenNodeIndex [i] ;

                    //Debug.Log ( "Check child node index #" + i_childNodeIndex + " from child store #" + i ) ;

				    if ( _IsCollidingChecks ( octree, i_childNodeIndex, uCheckBounds, out entityInstanceOut ) ) 
                    {
                        //Debug.Log ( "Is child node index #" + i_childNodeIndex + " from child store #" + i + " colliding." ) ;
					    return true;
				    }
			    }
		    }

		    return false;
	    }


        /// <summary>
	    /// Check if the specified ray intersects with anything in the tree. See also: GetColliding.
	    /// </summary>
	    /// <param name="CheckBounds">Bounds to check.</param>
	    /// <returns>True if there was a collision.</returns>
	    static private bool _IsRayCollidingChecks ( Nodes octree, int i_nodeIndex, Ray checkRay, float f_maxDistance, out float f_closestIntersectDistance, out EntityInstance entityInstanceOut ) 
        {            
            // used for comparison with none assigned entities instances
            EntityInstance emptyInstance = new EntityInstance () ;

            // assign defualt empty entity
            entityInstanceOut = emptyInstance ;

            // Is the input ray at least partially in this node?
            f_closestIntersectDistance = f_maxDistance ;

            Bounds bounds = octree.a_bounds [i_nodeIndex] ;

            UnityEngine.Bounds uBounds = _ECS2UnityBounds ( bounds ) ;



            //Debug.Log (">> Next ****************** *********** Node index #" + i_nodeIndex ) ;
            //Debug.Log ("Entity Bounds: " + uBounds.center + "; " + uBounds.size ) ;
            // Debug.Log ("Check Againts Bounds: " + uCheckBounds.center + "; " + uCheckBounds.size ) ;

            /*
            // Are the input bounds at least partially in this node?
		    if ( i_nodeIndex == 149 || i_nodeIndex == 75 || i_nodeIndex == 76 ) { // 4.5, 7.5, -4.5

                Debug.Log ( "Check index #" + i_nodeIndex ) ;
		    }
            */

            float f_boundIntersectionDistance = -1 ;
		    // Are the input bounds at least partially in this node?
		    if ( !uBounds.IntersectRay ( checkRay, out f_boundIntersectionDistance) || f_boundIntersectionDistance > f_maxDistance ) {

                //Debug.Log ( "Not Intersect: return false " ) ;
			    return false;
		    }
            
            int i_entitiesCount = octree.a_entitiesInNodeCount [i_nodeIndex] ;
            // Debug.Log ( "Entities count: " + i_entitiesCount ) ;
            bool isHavingEntities = i_entitiesCount > 0 ? true : false ;
                        
            // does have nay entity objects
            // find closes entities to tray origin
            if ( isHavingEntities )
            {
                int i_offsetIndex = i_nodeIndex * octree.NUM_ENTITIES_ALLOWED ;

                int i_entitiesIteration = 0 ;

                // Define new empty instance
                // If closest entity is found, this variable will store it for duration of iteration
                // and will pass it to return entity instance.
                EntityInstance closestEntityInstance = emptyInstance ; 

                // arbitrary limit, of furthest distance. Such ray distance should never happen.
                // float f_closestEntitySoFar = f_maxDistance ; 

		        // Check against any objects in this node
		        for ( int i = i_offsetIndex; i < i_offsetIndex + octree.NUM_ENTITIES_ALLOWED; i++ ) 
                {
                    EntityInstance entityInNode = octree.a_entitiesInNodes [i] ;

                    // check if is not empty
                    if ( !entityInNode.entity.Equals ( emptyInstance.entity ) )
                    {
                        Bounds entityBounds = new Bounds () { f3_center = entityInNode.f3_position, f_size = octree.f_minNodeSize } ;
                        
                        UnityEngine.Bounds uEntityBounds = _ECS2UnityBounds ( entityBounds ) ;

                        //Debug.Log ("Object Bounds: " + uEntityBounds.center + "; " + uEntityBounds.size ) ;
                        i_entitiesIteration ++ ;

                        
                        float f_intersectDistance = -1 ; // no intersection
			            if ( uEntityBounds.IntersectRay ( checkRay, out f_intersectDistance ) && f_intersectDistance <= f_maxDistance ) 
                        {
                            // Debug.Log ( "#" + i + " Intersects: return true at distance: " + f_distance ) ;

                            // check if this entity is closer to ray origin
                            if ( f_intersectDistance < f_closestIntersectDistance && !entityInNode.Equals ( emptyInstance ) )
                            {
                                f_closestIntersectDistance = f_intersectDistance ;
                                // closestEntityInstance = entityInNode ;
                                entityInstanceOut = entityInNode ;
                            }

                            if ( i_entitiesIteration >= i_entitiesCount )
                            {
                                // null is never to be expected here
                                // entityInstanceOut = closestEntityInstance ;
                                // f_intersectDistance = f_closestIntersectDistance ;
                                // has at least one intersection
                                // return true;
                                break ;
                            }
			            }
                    }
		        }
            }

            // return false ;

            
            // When no entity is detected, this searches children, for contained entities
            bool isHavingChildren = octree.a_childrenNodesCount [i_nodeIndex] > 0 ? true : false ;

		    // Check children
		    if ( isHavingChildren ) 
            {
                
                // arbitrary limit, of furthest distance. Such ray distance should never happen.
                //float f_closestEntitySoFar = f_maxDistance ; 

                // Define new empty instance
                // If closest entity is found, this variable will store it for duration of iteration
                // and will pass it to return entity instance.
                // EntityInstance closestEntityInstance = emptyInstance ; 
                EntityInstance childEntityInstanceOut = emptyInstance ;

                
                //int i_offsetIndex = octree.i_rootNodeIndex * 8 ;
                int i_offsetIndex = i_nodeIndex * 8 ;

			    for ( int i = i_offsetIndex; i < i_offsetIndex + 8; i++ ) 
                {   
                    // get index of the node, from the children store
                    int i_childNodeIndex = octree.a_childrenNodeIndex [i] ;

                    //Debug.Log ( "Check child node index #" + i_childNodeIndex + " from child store #" + i ) ;
                                        
                    float f_intersectDistance = -1 ; // no intersection
				    if ( _IsRayCollidingChecks ( octree, i_childNodeIndex, checkRay, f_maxDistance, out f_intersectDistance, out childEntityInstanceOut ) ) 
                    {

                        //Debug.Log ( "Is child node index #" + i_childNodeIndex + " from child store #" + i + " colliding." ) ;
                        // Debug.Log ( "#" + i + " Intersects: return true at distance: " + f_intersectDistance ) ;

                        // check if this entity is closer to ray origin
                        if ( f_intersectDistance > -1 && f_intersectDistance < f_closestIntersectDistance  )
                        {
                            f_closestIntersectDistance = f_intersectDistance ;
                            entityInstanceOut = childEntityInstanceOut ;
                        }
                                                
					    //return true;
				    }
			    }

                if ( f_closestIntersectDistance > -1 && f_closestIntersectDistance < f_maxDistance ) return true ;
		    }

            if ( f_closestIntersectDistance < 0 || f_closestIntersectDistance >= f_maxDistance )
            {
		        return false ;
            }
            
            return true ;
            
	    }


        static public bool _HasAnyObjects ( Nodes octree, int i_nodeIndex ) 
        {
		    // if ( objects.Count > 0) return true;
            if ( octree.a_entitiesInNodeCount [i_nodeIndex] > 0) return true;

		    if ( octree.a_childrenNodesCount [i_nodeIndex] > 0 ) 
            {
		        int i_childIndexOfsset = i_nodeIndex * 8 ;

                // iterate through children
                for (int i = i_childIndexOfsset; i < i_childIndexOfsset + 8; i++)
                {
                    int i_childIndex = octree.a_childrenNodeIndex [i] ;

				    if ( _HasAnyObjects ( octree, i_childIndex) ) return true;
			    } // for

		    }

		    return false;
	    }

    }

}