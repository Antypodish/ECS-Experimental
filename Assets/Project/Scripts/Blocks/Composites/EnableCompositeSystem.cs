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
    //[UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    class EnableCompositeSystem : JobComponentSystem
    {

        [Inject] private RequestPatternSetupData requestPatternSetupData ;  

        // request to assing pattern
        struct RequestPatternSetupData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositesInPattern ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_entityBuffer ;

            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            public ComponentDataArray <Blocks.Pattern.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;
            // [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }
        
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {          
            if ( requestPatternSetupData.Length > 0 )
            {
                World.Active.GetOrCreateManager<CompositeSystem>().Enabled = true ;
                World.Active.GetOrCreateManager<EnableCompositeSystem>().Enabled = false ;
            }
                
            return inputDeps ;            
        }

    }
}
