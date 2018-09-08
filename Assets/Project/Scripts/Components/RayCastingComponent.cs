using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{   

    public struct AllowRayCastingTag : IComponentData {}

    public struct RayCastComponent : IComponentData 
    {
        public Entity entityHit ;

        public float3 f3_origin ;
        public float3 f3_direction ;

        /// <summary>
        /// If detected, otherwise 0
        /// </summary>
        public float3 f3_hitpoint ;
        public float3 f3_objectCenter ;

        /// <summary>
        /// If hitpoint is detected
        /// </summary>
        public bool isHitpoint ;

    }
}

