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
    
    public struct CompositeInPatternPrefabComponent : IBufferElementData
    { 
        // entity owned by this block composite
        public int i_compositePrefabIndex ; // not used yet

        // used prefab ID
        // public int i_prefabId ;
        public float3 f3_position ;
    }

    public struct RequestAddPrefabBufferElement : IBufferElementData 
    {
        // entity owned by this block composite
        public int i_compositePrefabIndex ; // not used yet

        // used prefab ID
        // public int i_prefabId ;
        public float3 f3_position ;
    }

    public struct RequestAddPrefabTag : IComponentData { } ;

    public struct RequestPatternReleaseTag : IComponentData { }

}
