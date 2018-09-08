// using UnityEngine ;
using Unity ;
using Unity.Entities ;
// using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Test02
{
    // public class BarrierB : BarrierSystem {}

    // [UpdateAfter ( typeof ( MoveInstanceSystem ) ) ]
    // check if bounding box is intersecting, and get closest hit point of AABB.
    // AABB ( Axis Alligned Bounding Box)
    //[UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(BarrierA))]
    [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]   
    public class GravitySystem : JobComponentSystem
    {
        /// <summary>
        /// Source of gravity pull
        /// </summary>
        struct SourceData
        {            
            [ReadOnly] public EntityArray a_entities ;
            //[ReadOnly] public ComponentDataArray<Health> Health;
            // [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;
            [ReadOnly] public ComponentDataArray <Position> a_positions ;
            //[ReadOnly] public ComponentDataArray <MassCompnent> a_mass ;
            //[ReadOnly] public ComponentDataArray <grav> a_gravitySource ;  
            [ReadOnly] public ComponentDataArray <GravitySourceTag> a_gravitySourceTag ;   


        }

        /// <summary>
        /// Target for gravity to apply
        /// </summary>
        struct TargetData
        {            
            [ReadOnly] public readonly int Length;

            [ReadOnly] public EntityArray a_entities ;
            //[ReadOnly] public ComponentDataArray<Health> Health;
            // [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;
            [ReadOnly] public ComponentDataArray <Position> a_positions ;
            //[ReadOnly] public ComponentDataArray <PastPosition> a_pastPositions ;
            public ComponentDataArray <VelocityComponent> a_velocity ;
            [ReadOnly] public ComponentDataArray <MassCompnent> a_mass ;
            //[ReadOnly] public ComponentDataArray <grav> a_gravitySource ;  
            [ReadOnly] public ComponentDataArray <GravityApplyTag> a_gravityApplyTag ;   
        }

        // static private RayCastComponent raycastData ;

        [Inject] private SourceData sourceData ; 
        [Inject] private TargetData targetData ;  

        // [Inject] private Barrier gravityBarrier ;
        
        // static private EntityManager en ;

            /*
        protected override void OnCreateManager ( int capacity )
        {
            base.OnCreateManager ( capacity );

            en = World.Active.GetOrCreateManager  <EntityManager> () ;
        }
        */
        /// <summary>
        /// Execute Jobs
        /// </summary>
        [BurstCompile]
        // struct GravityJob : IJob
        struct GravityJob : IJobParallelFor
        // struct CollisionJob : IJobParallelFor // example of job parallel for
        {
            // public bool isBool;
            // public bool isIntersecting ;

            // [ReadOnly] public InputPointerComponent inputPointerData ;
            // [ReadOnly] public PlayerInputSystem.InputPointerData inputPointerData ;

            public TargetData targetData ; 
            public SourceData sourceData ; 

            /*
            [ReadOnly] public EntityArray a_sourceEntities;
            [ReadOnly] public ComponentDataArray <Position> a_sourcePositions ;
            //[ReadOnly] public ComponentDataArray <MassCompnent> a_sourceMass ;

            [ReadOnly] public EntityArray a_targetEntities;
            [ReadOnly] public ComponentDataArray <Position> a_targetPositions ;
            //[ReadOnly] public ComponentDataArray <PastPosition> a_targetPastPositions ;
            public ComponentDataArray <VelocityComponent> a_targetVelocity ;
            [ReadOnly] public ComponentDataArray <MassCompnent> a_targetMass ;
            */

            // [ReadOnly] public ComponentDataArray <GravitySourceTag> a_gravitySourceTag ;  
            //[ReadOnly] public ComponentDataArray <Unity.Rendering> a_renderer ;

            // static public EntityManager en ;
            //static private float3 f3_test ;

            // public EntityCommandBuffer commandsBuffer ;
            

            // public void Execute ()  // for IJob
            public void Execute ( int i_target )  // for IJobParallelFor
            {
                
                //for ( int i_target = 0; i_target < targetData.a_positions.Length; ++i_target )
                //{
                           
                    // Entity targetEntity = a_targetEntities [i_target] ;
                           
                    float3 f3_targetPos = targetData.a_positions [i_target].Value ;

                    

                    // float3 f3_direction = new float3 () ;

                    // commandsBuffer.SetComponent ( targetEntity, new VelocityComponent { f3 = f3_test } ) ;

                    float3 f3_targetVelocity = targetData.a_velocity [i_target].f3 ;
                                        
                    for ( int i_soruce = 0; i_soruce < sourceData.a_entities.Length; ++i_soruce )
                    {
                        //Entity sourceEntity = a_sourceEntities [i_soruce] ;
                           
                        float3 sourcePos = sourceData.a_positions [i_soruce].Value ;

                        float3 f3_direction = ( f3_targetPos - sourcePos ) ;

                        // float3 f3_directionNorm = Vector3.Normalize ( f3_direction ) ;

                        f3_targetVelocity -= ( new float3 ( ( f3_direction.x < 0 ? -1 : 1 ) * f3_direction.x * f3_direction.x, ( f3_direction.y < 0 ? -1 : 1 ) * f3_direction.y * f3_direction.y, ( f3_direction.z < 0 ? -1 : 1 ) * f3_direction.z * f3_direction.z ) * 0.001f ) ;
                        // f3_velocity -=  1 / ( new float3 ( ( f3_direction.x < 0 ? -1 : 1 ) * f3_direction.x * f3_direction.x, ( f3_direction.y < 0 ? -1 : 1 ) * f3_direction.y * f3_direction.y, ( f3_direction.z < 0 ? -1 : 1 ) * f3_direction.z * f3_direction.z ) * 1000 ) ;
                        //f3_velocity +=  1 / ( new float3 ( f3_direction.x * f3_direction.x, f3_direction.y * f3_direction.y, f3_direction.z * f3_direction.z ) ) * 0.001f ;
                        //f3_direction += posDiff ;
                         
                    } // for
                    

                    targetData.a_velocity [i_target] = new VelocityComponent { f3 = f3_targetVelocity } ;
                    
                    //f3_direction = -Vector3.Normalize ( f3_direction ) ;
                    //f3_direction = - f3_direction ;

                    // float3 f3_velocity = a_targetVelocity [i_target].f3 + f3_direction * 0.01f ;
                    // f3_test = f3_velocity ;
                    // commandsBuffer.SetComponent ( targetEntity, new VelocityComponent { f3 = f3_velocity } ) ;
                    // commandsBuffer.SetComponent ( targetEntity, new Position { Value = targetPos + f3_velocity } ) ;
                    // commandsBuffer.Playback ( en ) ;
                //}
 
            }

        }

        /*
        protected override void OnCreateManager ( int capacity )
        {
           // en = World.GetOrCreateManager ( typeof ( GravitySystem ) ) ;

            base.OnCreateManager ( capacity );
        }
        */

        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {
            
            // return new GravityJob // for IJob
            var jobHandle = new GravityJob // for IJobParallelFor            
            {
                targetData = targetData,
                sourceData = sourceData,
                /*
                //isBool = true,
                a_sourceEntities = sourceData.a_entities,
                a_sourcePositions = sourceData.a_positions,
                // a_sourceMass = sourceData.a_mass,

                a_targetEntities = targetData.a_entities,
                a_targetPositions = targetData.a_positions,
                //a_targetPastPositions = targetData.a_pastPositions,
                a_targetVelocity = targetData.a_velocity,
                a_targetMass = targetData.a_mass,

                commandsBuffer = gravityBarrier.CreateCommandBuffer (),
                */
                
            // }.Schedule(inputDeps) ; // for IJob
            }.Schedule( targetData.Length, 64, inputDeps) ; // for IJobParallelFor

            return jobHandle ; // for IJobParallelFor

        }

    }
    
}

