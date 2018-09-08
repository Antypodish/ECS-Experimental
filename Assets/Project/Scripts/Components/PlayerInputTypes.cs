using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{

    public struct PlayerInputComponent : IComponentData
    {
        /// <summary>
        /// Mouse pointer
        /// </summary>
        // public float2 pointerOnViewport ;

        public float3 f3_move ;

        public float f_roll ;
        public float f_ptich ;
        public float f_yaw ;
        //public float2 Shoot;
        //public float FireCooldown;

        // public bool Fire => FireCooldown <= 0.0 && math.length(Shoot) > 0.5f;
    }

    public struct InputPointerComponent : IComponentData
    {
        /// <summary>
        /// Mouse pointer
        /// </summary>
        //public float2 pointerOnViewport ;
        
        //public float3 f3_rayOrigin ;
        //public float3 f3_rayDirection ;
    }

    public struct KeysActionComponent : IComponentData
    {
        /// <summary>
        /// Mouse pointer
        /// </summary>
        public int3 i3_mouseButtons ;
    }
}
