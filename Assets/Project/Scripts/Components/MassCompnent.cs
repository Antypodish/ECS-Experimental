using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{   

    /// <summary>
    /// Mass [kg]
    /// </summary>
    public struct MassCompnent : IComponentData 
    {
        public float3 f ;
    }
    
}
