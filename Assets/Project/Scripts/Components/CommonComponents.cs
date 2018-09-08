using Unity.Entities ;

namespace ECS.Common.SharedComponents
{
    

    public struct IntSharedComponent : ISharedComponentData
    { 
        public int i ;
    }
    
    public struct Int3SharedComponent : ISharedComponentData
    { 
        public Unity.Mathematics.int3 i3 ;
    }
    
    public struct Half3SharedComponent : ISharedComponentData
    { 
        public Unity.Mathematics.half3 h3 ;
    }
    
    public struct FloatSharedComponent : ISharedComponentData
    { 
        public float f ;
    }
}

namespace ECS.Common.BufferElements
{
    
    public struct EntityBuffer : IBufferElementData
    { 
        public Entity entity ;
    }

    public struct IntBuffer : IBufferElementData
    { 
        public int i ;
    }
    
    // forum thread
    // 05.09.2018
    // https://forum.unity.com/threads/how-can-i-improve-or-jobify-this-system-building-a-list.547324/#post-3614746
    public struct FloatBufferElement : IBufferElementData
    { 
        public float f ;
    }
}

namespace ECS.Common.Components
{

    public struct EntityComponent : IComponentData
    {
        public Entity entity ;
    }


    // Used for requesting initialization
    // Component yypically should be removed after is executed
    public struct InitializeTag : IComponentData {}
 
    public struct HalfComponent : IComponentData
    { 
        public Unity.Mathematics.half h ;
    }

    public struct IntComponent : IComponentData
    { 
        public int i ;
    }
       

    public struct Half3Component : IComponentData
    { 
        public Unity.Mathematics.half3 h3 ;
    }


    public struct Float3Component : IComponentData
    { 
        public Unity.Mathematics.float3 f3 ;
    }

    public struct LodComponent : IComponentData 
    {
        // arrays storing data, for each LOD level, up to 4 levels
        public Unity.Mathematics.float4 f4_switch2NextLodDistance ;
        public Unity.Mathematics.float4 f4_switch2PreviousLodDistance ;
        public Unity.Mathematics.int4 i4_meshID ;
        
        public int i_triggerID ;
        /*
        public ComponentDataArray <HalfComponent> a_switch2NextLodDistance ;
        public ComponentDataArray <HalfComponent> a_switch2PreviousLodDistance ;
        public ComponentDataArray <IntComponent> a_meshID ;

        public BufferArray <IntBufferntComponent> a_int ; // experimental // new as per preview 11
        */
        // id to the position, from where LOD is calculated, based on distance
        
    }

    public struct LodTargetTag : IComponentData { } ;

    public struct Lod01Tag : IComponentData { } ;
    public struct Lod02Tag : IComponentData { } ;
    public struct Lod03Tag : IComponentData { } ;
    public struct Lod04Tag : IComponentData { } ;
    public struct Lod05Tag : IComponentData { } ;
    public struct Lod06Tag : IComponentData { } ;
    public struct Lod07Tag : IComponentData { } ;

    public struct IsLodActiveTag : IComponentData { } ;


    public struct AddNewTag : IComponentData { } ;

    
    /// <summary>
    /// Use this tag only on systems Data, when need to filter them off
    /// For example disabling for testing
    /// Do not use this tag on components
    /// </summary>
    public struct DisableSystemTag : IComponentData {} ;
    public struct IsNotAssignedTag : IComponentData {} ;


    


} 
