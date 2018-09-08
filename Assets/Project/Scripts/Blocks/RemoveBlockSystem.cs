using UnityEngine ;
using Unity.Entities ;
using Unity.Collections ;

namespace ECS.Test02
{    
    public class RemoveBlockSystem : ComponentSystem
    {
        //static public EntityArchetype objectArchetype;

        struct BlockData
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            [ReadOnly] public ComponentDataArray <RemoveBlockTag> a_blockTags ;

        }


        [Inject] private BlockData blockData ;

        [Inject] private Barrier removeBlockBarrier ;

        static EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {
            commandsBuffer = removeBlockBarrier.CreateCommandBuffer () ;

            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            
        }

        static private EntityCommandBuffer commandsBuffer ;

        protected override void OnUpdate ()
        {
            commandsBuffer = removeBlockBarrier.CreateCommandBuffer () ;

            for (int i = 0; i < blockData.Length; ++i)
            {
                _RemoveBlock ( i );                
            }
                        
        }


        private void _RemoveBlock ( int i )
        {

            // Entity entity = blockData.a_entities [i].entity ;
            Entity entity = blockData.a_entities [i] ;
            
            commandsBuffer.DestroyEntity ( entity ) ;
        }

        /// <summary>
        /// Requests to remove entity block with.
        /// Call it from whatever place
        /// </summary>
        static public void _RemoveBlockRequest ( Entity entity )
        {
            commandsBuffer.AddComponent ( entity, new RemoveBlockTag () ) ; // tag it as block to remove.

            Debug.Log ( "Requested to remove Block #" + entity.Index ) ;
        }

    }
}
