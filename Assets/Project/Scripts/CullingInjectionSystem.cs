// none functional

using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;
 
namespace ECS.Systems {
 
    [UpdateAfter(typeof(CullingInjectionSystem))]
    public class CullingBarrier : BarrierSystem {
    }
 
    public class CullingInjectionSystem : JobComponentSystem {
 
        struct Group {
            public readonly int Length;
            public EntityArray a_entities;
            public ComponentDataArray<MeshCullingComponent> culling;

            [ReadOnly] public Disabled disabled ;
        }
 
        [Inject] Group group;
 
        struct CullingInjectionJob : IJob {
            public EntityCommandBuffer commandBuffer;
 
            [ReadOnly] public EntityArray entities;
 
            public void Execute() {
                for (int i = 0; i < entities.Length; ++i) {
                    //commandBuffer.RemoveComponent<MeshCullingComponent>(entities[i]);
                    commandBuffer.AddComponent(entities[i], new MeshCulledComponent());
                }
            }
        }
 
        [Inject] CullingBarrier Barrier;
        static EntityManager entityManager ;

        protected override void OnCreateManager(int capacity) 
        {      
            
        }
 
        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var cullStatusUpdateJob = new CullingInjectionJob {
                commandBuffer = Barrier.CreateCommandBuffer(),
                entities = group.a_entities,
            };
            return cullStatusUpdateJob.Schedule();
        }
    }
}