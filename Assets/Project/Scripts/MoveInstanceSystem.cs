using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Test02
{
    // public class BarrierA : BarrierSystem {}

    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierB))]
    public class MoveInstanceSystem : JobComponentSystem
    {        
        struct Data
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities;
            //[ReadOnly] public ComponentDataArray<Health> Health;
            // [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;
            // [ReadOnly] public ComponentDataArray <PastPosition> a_pastPositions ;
            public ComponentDataArray <Position> a_positions ;
            [ReadOnly] public ComponentDataArray <VelocityComponent> a_velocity ;
            public ComponentDataArray <VelocityPulseComponent> a_velocityPulse ;          
            
            [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        [Inject] private Data data ;               
        
        // [Inject] private Barrier moveInstanceBarrier ;

        /// <summary>
        /// Execute Jobs
        /// </summary>
        [BurstCompile]
        struct MoveInstanceJob : IJobParallelFor
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            [ReadOnly] public EntityArray a_entities;
            
            public Data data ;

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {
                float3 f3_velocity = data.a_velocity [i].f3 ;    

                float3 f3_velocityPulse = data.a_velocityPulse [i].f3 * 0.1f ;      
                                        
                data.a_velocityPulse [i] = new VelocityPulseComponent { } ; // reset velocity pulse
                float3 f3_position = data.a_positions [i].Value + f3_velocity + f3_velocityPulse ; 
                data.a_positions [i] = new Position { Value = f3_position } ;
                                               
            }           
            
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            // return new MoveInstanceJob // for IJob
            var jobHandle = new MoveInstanceJob // for IJobParallelFor
            {
                a_entities = data.a_entities,
                data = data,

                // commandsBuffer = moveInstanceBarrier.CreateCommandBuffer (),
            //}.Schedule(inputDeps) ; // for IJob
            }.Schedule( data.Length, 64, inputDeps) ; // for IJobParallelFor

            return jobHandle ; // for IJobParallelFor

        }

    }
    
}

