using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{   
    /*
    /// <summary>
    /// Gravity pull (acceleration) [m/s^2]
    /// </summary>
    public struct GravitySourceComponent : IComponentData 
    {
        public float3 f3_pullAcceleration ;
    }
    */

    public struct GravityApplyTag : IComponentData { } ;

    public struct GravitySourceTag : IComponentData { } ;

    public struct AddGravitySourceTag : IComponentData 
    {
        public float3 f3_position ;    
    } ;

}