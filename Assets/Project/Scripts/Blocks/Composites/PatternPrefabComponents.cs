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
    
    public struct CompositeInPatternPrefabComponent : IBufferElementData
    { 
        // entity owned by this block composite
        public int i_compositePrefabIndex ; // not used yet

        // used prefab ID
        // public int i_prefabId ;
        public float3 f3_position ;

        /// <summary>
        /// When scale of any axis is 0 no mesh is generated
        /// For each axis with scale greater than 1, there is offset applied, 
        /// assuming pivot of mesh, is at the position of this composite.
        /// </summary>
        public float3 f3_scale ;
    }

    /*
    public struct RequestAddPrefabBufferElement : IBufferElementData 
    {
        // entity owned by this block composite
        public int i_compositePrefabIndex ; // not used yet

        // used prefab ID
        // public int i_prefabId ;
        public float3 f3_position ;
    }
    */

    public struct RequestAddPrefabTag : IComponentData { } ;

    public struct RequestPatternReleaseTag : IComponentData { } ;

    public struct RequestPatternSetupTag : IComponentData { } ;

}
