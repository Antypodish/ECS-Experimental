using UnityEngine ;
using Unity.Entities ;
using Unity.Collections ;
using Unity.Rendering ;
using Unity.Jobs ;

namespace ECS.Test02
{

    public class BlockResetHighlight : JobComponentSystem
    {

        struct Data
        {
            [ReadOnly] public EntityArray a_entities;
            [ReadOnly] public ComponentDataArray <BlockResetHighlightTag> a_resetBlockHighlight ;            
        }

        [Inject] private Data data ;               
        
        [Inject] private Barrier resetBlockHiglightBarrier ;

        static private Ray ray ;

        static public Unity.Rendering.MeshInstanceRenderer previousMeshInstanceRenderer ;

        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct ResetBlockHiglightJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            [ReadOnly] public EntityArray a_entities;
            [ReadOnly] public ComponentDataArray <BlockResetHighlightTag> a_setBlockHighlight ;
            
            public EntityCommandBuffer commandsBuffer ;

            public void Execute ()
            {
                for (int i = 0; i < a_entities.Length; ++i )
                {
                    Entity entity = a_entities [i] ;
                      
                    // renderer
                    Unity.Rendering.MeshInstanceRenderer renderer = previousMeshInstanceRenderer ; // Bootstrap.playerRenderer ;
                    // renderer.material.SetColor ( "_Color", Color.blue ) ;                        
                    commandsBuffer.SetSharedComponent <MeshInstanceRenderer> ( entity,  renderer ) ; // replace renderer with material and mesh

                    // commandsBuffer.AddComponent ( entity, new IsBlockHighlightedTag () ) ;
                    commandsBuffer.RemoveComponent <BlockResetHighlightTag> ( entity ) ; 

                }
                               
            }           
            
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {

            return new ResetBlockHiglightJob
            {
                a_entities = data.a_entities,
                a_setBlockHighlight = data.a_resetBlockHighlight,

                commandsBuffer = resetBlockHiglightBarrier.CreateCommandBuffer (),

            }.Schedule(inputDeps) ;

        }
    }

}
