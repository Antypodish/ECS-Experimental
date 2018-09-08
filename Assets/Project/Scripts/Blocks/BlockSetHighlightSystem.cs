using UnityEngine ;
using Unity.Entities ;
using Unity.Collections ;
using Unity.Rendering ;
using Unity.Jobs ;

namespace ECS.Test02
{

    public class BlockSetHighlightSystem : JobComponentSystem
    {

        struct Data
        {
            // [ReadOnly] public int Length ;
            [ReadOnly] public EntityArray a_entities;
            [ReadOnly] public ComponentDataArray <BlockSetHighlightTag> a_setBlockHighlight ;            
        }

        [Inject] private Data data ;               
        
        [Inject] private Barrier setBlockHiglightBarrier ;

        static private EntityManager entityManager ;

        static private Ray ray ;


        protected override void OnCreateManager ( int capacity )
        {
            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
        }
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        struct SetBlockHiglightJob : IJob
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            public bool isBool;
            
            [ReadOnly] public EntityArray a_entities;
            [ReadOnly] public ComponentDataArray <BlockSetHighlightTag> a_setBlockHighlight ;
            
            public EntityCommandBuffer commandsBuffer ;

            public void Execute ()
            {
                for (int i = 0; i < a_entities.Length; ++i )
                {
                    Entity entity = a_entities [i] ;
                      
                    // renderer
                    BlockResetHighlight.previousMeshInstanceRenderer = entityManager.GetSharedComponentData <MeshInstanceRenderer> ( entity ) ;
                    // assigne new renderrer
                    Unity.Rendering.MeshInstanceRenderer renderer = Bootstrap.highlightRenderer ;
                    // renderer.material.SetColor ( "_Color", Color.blue ) ;                        
                    commandsBuffer.SetSharedComponent <MeshInstanceRenderer> ( entity, renderer ) ; // replace renderer with material and mesh

                    // commandsBuffer.AddComponent ( entity, new IsBlockHighlightedTag () ) ;
                    commandsBuffer.RemoveComponent <BlockSetHighlightTag> ( entity ) ; 

                }
                               
            }           
            
        }


        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            
            return new SetBlockHiglightJob
            {
                isBool = true,
                a_entities = data.a_entities,
                a_setBlockHighlight = data.a_setBlockHighlight,

                commandsBuffer = setBlockHiglightBarrier.CreateCommandBuffer (),

            }.Schedule(inputDeps) ;

        }
    }

}
