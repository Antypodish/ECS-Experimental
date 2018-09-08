using Unity.Entities ;
using Unity.Mathematics ;

namespace ECS.Test02
{   

    public struct IsBlockTag : IComponentData {}

    public struct IsBlockHighlightedTag : IComponentData {}  

    public struct IsBlockNotHighlightedTag : IComponentData {}  

    public struct BlockSetHighlightTag : IComponentData {}  

    public struct BlockResetHighlightTag : IComponentData {}  
    
    public struct ReetBlockHighlightTag : IComponentData {}  

    public struct AddBlockComponent : IComponentData 
    {    
        public float3 f3_position ;
        // public float4 f4_rotation ;   
        
        public float3 f3_scale ;

        /// <summary>
        /// Neigbour reference block, from which new block has been created.
        /// </summary>
        public Entity referenceNeighbourBlock ;    

        /// <summary>
        /// In which direction new block has beencreated, from the reference neighbour block
        /// </summary>
        public float3 f_directionFromReferenceNeighbourBlock ;  

        public float4 f4_color ;
    }

    public struct NeighbourBlocks : IComponentData 
    {    
        public Entity left ;
        public Entity right ;  

        public Entity front ;
        public Entity back ;

        public Entity up ;
        public Entity down ;
    }

    public struct RemoveBlockTag : IComponentData {}    

}
