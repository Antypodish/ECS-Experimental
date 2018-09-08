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
    
    
    public struct CompositeComponent : IComponentData
    {
        public Entity blockEntity ;
        public Entity patternEntity ;

        // used prefab ID
        public int i_prefabId ;
    }

    public struct AssignComposites2PatternTag : IComponentData { }


    public struct CompositePatternComponent : IComponentData
    {
        public Entity blockEntity ;
        public int i_componentsPatternIndex ; 
    }

    public struct MovePatternComonent : IComponentData 
    {   
        public float3 f3_position ;
    }

    public struct RequestPatternSetupTag : IComponentData { }

    public struct AssignCompositePattern2EntityTag : IComponentData { }
    
    

    public struct BlockCompositeBufferElement : IBufferElementData
    { 
        // entity owned by this block composite
        // public Entity entity ;
        // used prefab ID
        // public int i_prefabId ;
        public float3 f3_position ;
    }

}