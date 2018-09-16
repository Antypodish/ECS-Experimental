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
        public int i_inPrefabIndex ;
    }

    public struct AssignComposites2PatternTag : IComponentData { }


    public struct PatternComponent : IComponentData
    {
        public Entity blockEntity ;
        public int i_patternIndex ; 
        /// <summary>
        /// Base scale, to which owned entity compnents, are scalled accordingly.
        /// </summary>
        public float f_baseScale ;
        public float3 f_localPosition ;
    }
        
    public struct MovePattern : IComponentData 
    {   
        public float3 f3_position ;
    }

    // public struct RequestPatternSetupTag : IComponentData { }
    
    // public struct AssignCompositePattern2EntityTag : IComponentData { }
    
    

}