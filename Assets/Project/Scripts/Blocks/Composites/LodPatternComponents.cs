using Unity.Entities ;

namespace ECS.Blocks.Pattern.Components
{
    public struct LodComponent : IComponentData 
    {
        // arrays storing data, for each LOD level, up to 4 levels
        public Unity.Mathematics.float4 f4_switch2NextLodDistance ;
        public Unity.Mathematics.float4 f4_switch2PreviousLodDistance ;
                
        public int i_triggerID ;
        
    }

    /// <summary>
    /// Reference to which position is entity measure distance, to switch LOD
    /// </summary>
    public struct LodTargetTag : IComponentData { } ;
    public struct IsLodSwitchedTag : IComponentData { } ;

    public struct Lod010Tag : IComponentData { } ;
    public struct Lod020Tag : IComponentData { } ;
    public struct Lod030Tag : IComponentData { } ;
    public struct Lod040Tag : IComponentData { } ;
    public struct Lod050Tag : IComponentData { } ;
    public struct Lod060Tag : IComponentData { } ;
    public struct Lod070Tag : IComponentData { } ;
    public struct Lod080Tag : IComponentData { } ;
    public struct Lod090Tag : IComponentData { } ;
    public struct Lod100Tag : IComponentData { } ;

    public struct IsLodActiveTag : IComponentData { } ;
}
