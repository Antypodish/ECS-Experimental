﻿using UnityEngine ;
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

    // public struct PatternIsNotAssignedTag : IComponentData {} ;
    public struct AssignComposites2PatternTag : IComponentData { }


    public struct PatternComponent : IComponentData
    {
        public Entity blockEntity ;
        public int i_patternIndex ; 
        /// <summary>
        /// Base scale, to which owned entity compnents, are scalled accordingly.
        /// </summary>
        public float f_baseScale ;
        public float3 f3_localPosition ;

        /// <summary>
        /// Describes depth level of Lod.
        /// 0 is the defualt depth level
        /// Negative value indicated greater depth level, with higher details (zoom in)
        /// Positive value indicates lower depth level, with simplified details (zoom out)
        /// </summary>
        public int i_lodDepth ;

        /// <summary>
        /// Used composite prefab ID, when lower level of detail is switched
        /// </summary>        
        public int i_prefabIndex ;
    }
        
    public struct MovePattern : IComponentData 
    {   
        public float3 f3_position ;
    }

    // public struct RequestPatternSetupTag : IComponentData { }
    
    // public struct AssignCompositePattern2EntityTag : IComponentData { }
    
    

}