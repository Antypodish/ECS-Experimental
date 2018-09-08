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
    public class RotateInstanceSystem : JobComponentSystem
    {        
        struct Data
        {
            public readonly int Length;

            [ReadOnly] public EntityArray a_entities;

            //public ComponentDataArray <RotateInstanceTag> a_rotateInstanceTag ;
            public ComponentDataArray <Rotation> a_rotation ;
            public ComponentDataArray <AngularVelocityComponent> a_angularVelocity ;
            public ComponentDataArray <AngularVelocityPulseComponent> a_angularVelocityPulse ;
            
            [ReadOnly] public ComponentDataArray <Disabled> a_disabled ;
        }

        [Inject] private Data data ;               
        
        // [Inject] private Barrier moveInstanceBarrier ;

        /// <summary>
        /// Execute Jobs
        /// </summary>
        [BurstCompile]
        struct RotateInstanceJob : IJobParallelFor
        {
            // [ReadOnly] public EntityArray a_entities;
            
            public Data data ;            

            // [ReadOnly] public EntityCommandBuffer commandsBuffer ;

            // public void Execute ()  // for IJob
            public void Execute ( int i )  // for IJobParallelFor
            {
                // commandsBuffer.RemoveComponent <RotateInstanceTag> ( a_entities [i] ) ;
                // rotation
                //float3 f3_angularVelocityPulse = data.a_angularVelocityPulse [i].f3 * 10f ;  
                //data.a_angularVelocityPulse [i] = new AngularVelocityPulseComponent { } ; // reset angular velocity pulse
                
                // AngularVelocityPulseComponent a_angularVelocityPulse = data.a_angularVelocityPulse [i] ;
                // Quaternion q = data.a_rotation [i].Value ; // * data.a_angularVelocityPulse [i].q ; // * data.a_angularVelocity [i].q ;
                Quaternion q = math.mul ( data.a_rotation [i].Value, data.a_angularVelocityPulse [i].q ) ;
                q = math.mul ( q, data.a_angularVelocity [i].q ) ;
                //Quaternion q = data.a_rotation [i].Value * Quaternion.Euler ( new float3 ( 1, 2, 3) ) * Quaternion.Euler ( new float3 ( 4,5,6) ) ;
                // Quaternion q = data.a_rotation [i].Value * Quaternion.Euler ( f3_angularVelocityPulse ) * Quaternion.Euler ( data.a_angularVelocity [i].f3 ) ;
                data.a_rotation [i] = new Rotation { Value = q } ; 

                
            }           
            
        }

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            // return new MoveInstanceJob // for IJob
            var jobHandle = new RotateInstanceJob // for IJobParallelFor
            {
                // commandsBuffer = new EntityCommandBuffer (),

                // isBool = true,
                // a_entities = data.a_entities,
                // a_pastPositions = data.a_pastPositions,
                data = data,

            //}.Schedule(inputDeps) ; // for IJob
            }.Schedule( data.Length, 128, inputDeps) ; // for IJobParallelFor

            return jobHandle ; // for IJobParallelFor

        }

    }
    
}

