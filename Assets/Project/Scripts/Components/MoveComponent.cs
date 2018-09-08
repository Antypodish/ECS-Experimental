using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{   

    public struct MoveInstanceTag : IComponentData { } ;

    /// <summary>
    /// Momentary velocity 
    /// </summary>
    public struct VelocityPulseComponent : IComponentData 
    {
        public float3 f3 ;
    }

    public struct VelocityComponent : IComponentData 
    {
        public float3 f3 ;
    }

    /*
    /// <summary>
    /// Used for compensating Position delay, during execution of job systems and commandsBuffer Barrier
    /// </summary>
    public struct PastPosition : IComponentData
    {
        public float3 f3 ;
    }
    */

    public struct RotateInstanceTag : IComponentData { } ;

    public struct AngularVelocityPulseComponent : IComponentData 
    {
        public quaternion q ;
    }

    public struct AngularVelocityComponent : IComponentData 
    {
        public quaternion q ;
    }

}
