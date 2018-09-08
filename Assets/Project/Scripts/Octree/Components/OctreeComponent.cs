// Octree based on Nition/UnityOctree C# OOP, March 2018 by Nitron
// Nition/UnityOctree
// https://github.com/Nition/UnityOctree
// And converted to ECS, By Dobromil K Duda August 2018


using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Octree
{   

    /// <summary>
    /// For new octree system
    /// </summary>
    public struct AddOctreeTag : IComponentData {}

    public struct OctreeTag : IComponentData 
    {
        public int i_octreeID ;
    } ;

    /// <summary>
    /// For new octree system
    /// </summary>
    public struct OctreeComponent : IComponentData 
    {
        // The total amount of objects currently in the tree
	    public int i_count ;

        // Root node of the octree
	    // public NodeComponent rootNode;
        public Entity rootNode ;

        // Size that the octree was on creation
	    public float f_initialSize ;

        // Minimum side length that a node can be - essentially an alternative to having a max depth
	    public float f_minNodeSize ;
    }


    
    // public struct NodeTag : IComponentData {}
    public struct AddPointNodeTag : IComponentData 
    {
        public int i_octreeIndex ;
        public float3 f_addAtPosition ;
        public EntityInstance objectInstance ;
        public Bounds objectBounds ; // used by Bounds Octree
    }

    // public struct NodeTag : IComponentData {}
    public struct AddNodeTag : IComponentData 
    {
        public int i_octreeIndex ;
        public float3 f3_addAtPosition ; // not used by Bound Octree
        public EntityInstance entityInstance ;
        // public Bounds entityBounds ; // Not used by Bounds Octree
    }
    public struct BoundsOctreeNodeTag : IComponentData  {}
    public struct PointOctreeNodeTag : IComponentData  {}

    public struct OctreeTestNodeComponent : IComponentData  
    {
        public float nodeIndex ;
        public float rootIndex ;

        public float f_size ;
        public float position ;        
        public float f_min ;
        public float f_max ;      
        
        public float entitiesCount ;
    }

    public struct RemoveNodeTag : IComponentData {}
    public struct GrowNodeTag : IComponentData {}
    public struct ShrinkNodeTag : IComponentData {}

    
    public struct SettingsComponent : IComponentData 
    {        
	    // If there are already numObjectsAllowed in a node, we split it into children
	    // A generally good number seems to be something around 8-15
	    // public const int NUM_OBJECTS_ALLOWED = 8 ;
    }

    /// <summary>
    /// 
    /// </summary>
    public struct NodeComponent : IComponentData 
    {
        /// <summary>
        /// component of entity that belongs to
        /// TODO: try to remove it, if only used by OctreeComponent
        /// </summary>
        public Entity entitySelf ; // self

        // [ReadOnly] public int NUM_OBJECTS_ALLOWED = 8 ;
        // public int i_objects_allowed ; // should be 8

        // entity of this node
        // public Entity entity ;

        // Centre of this node
        public float3 f3_center ;

        // Length of the sides of this node
	    public float f_sideLength ;

	    // Minimum size for a node in this octree
	    public float f_minSize;

	    // Bounding box that represents this node
	    //public UnityEngine.Bounds bounds ; // is also blittable
        public Bounds bounds ;
        
	    // Objects in this node
	    // readonly List<OctreeObject> objects = new List<OctreeObject>();
        // Entities in this node
        // See constance NUM_OBJECTS_ALLOWED, for limiet. For safe keeping, set fixed array to double size of the allowed count
        //public NativeArray <ObjectInstance> a_objectInstance ;
        //public ComponentDataArray <ObjectInstance> a_objectInstance ;
        public int i_objectInstanceInserts ;        

        /// <summary>
        /// Child nodes, if any
        /// 0 to 8 elements 2x2x2 cube
        /// </summary>
        // NodeComponent <T>[] children ; // default null? 0 to 8 elements 2x2x2
        // public NativeArray <NodeComponent> a_childrenNodes ;
        // public NativeArray <Entity> a_childrenNodes ;
        //public ComponentDataArray <EntityComponent> a_childrenNodes ;
        public int i_childrenNodesInserts ;
        
	    // bounds of potential children to this node. These are actual size (with looseness taken into account), not base size
	    // public Bounds [] childBounds ;
        // size 8
        //public FixedArrayArray <Bounds> fa_childBounds ;
        //public NativeArray <Bounds> a_childBounds ;
        // public ComponentDataArray <Bounds> a_childBounds ;
        // public fixed  [] a_childBounds3 ;

	    // For reverting the bounds size after temporary changes
	    public float3 f3_actualBoundsSize;
        
    }
    
    /// <summary>
    /// Bounds minimalistic version of UnityEngine Bounds
    /// Passing value into UnityEngineBounds can calculate vertices and expansion
    /// This Struct only holds size and center, for storing i.e. in large arrays
    /// Size is single float, as bounds are cubical. There fore no ned storing f3.
    /// </summary>
    public struct Bounds
    {
        //
        // Summary:
        //     The extents of the Bounding Box. This is always half of the size of the Bounds.
        //public float3 extents ;
        //
        // Summary:
        //     The total size of the box. This is always twice as large as the extents.
        public float f_size ;
        //
        // Summary:
        //     The center of the bounding box.
        public float3 f3_center ;
        //
        // Summary:
        //     The minimal point of the box. This is always equal to center-extents.
        // public float3 min ;
        //
        // Summary:
        //     The maximal point of the box. This is always equal to center+extents.
        // public float3 max ;

    }

    // An instance in the octree
    public struct EntityInstance // : IComponentData 
    {
        public Entity entity ;
        /// <summary>
        /// Octree object instance position.
        /// Not to be confused, with Transform.Position
        /// </summary>
        public float3 f3_position ;
    }
}
