using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;

namespace ECS.Test02
{
    //[UpdateAfter(typeof(Time.TimeIntervalCustomSystem))]
    [UpdateAfter(typeof(UnityEngine.Experimental.PlayerLoop.Update))]
    public class PlayerInputSystem : ComponentSystem
    {
        struct PlayerData
        {
            readonly public int Length ;
                        
            //[ReadOnly] public EntityArray a_entities ;

            [ReadOnly] public ComponentDataArray <PlayerInputComponent> a_inputs ;

            // public ComponentDataArray <Position> a_positions ;
            [ReadOnly] public ComponentDataArray <VelocityComponent> a_velocity ;
            [ReadOnly] public ComponentDataArray <VelocityPulseComponent> a_velocityPulse ;

            [ReadOnly] public ComponentDataArray <AngularVelocityComponent> a_angularVelocity ;
            [ReadOnly] public ComponentDataArray <AngularVelocityPulseComponent> a_angularVelocityPulse ;
        }

        [Inject] [ReadOnly] private PlayerData playersData ;

        public struct InputPointerData
        {
            public InputPointerComponent inputPointer ;            
            public KeysActionComponent keysInputs ;

            public RayCastComponent rayCastData ;
            
        }

        static public InputPointerData inputPointerData ;

        protected override void OnCreateManager ( int capacity )
        {
            // base.OnCreateManager ( capacity );

            // Entity entity = EntityManager.CreateEntity ( ) ;

            //EntityManager.AddComponent ( entity, typeof ( PlayerInputComponent ) ) ;
            //EntityManager.AddComponent ( entity, typeof ( Render ) ) ;
        }

        protected override void OnUpdate ()
        {
            // Debug.Log ( "aa" ) ;
            // float dt = Time.deltaTime;

            // left click, or key pressed
            if ( Input.GetKeyUp ( KeyCode.Q ) || Input.GetMouseButtonUp ( 0 ) ) 
            {

                if ( inputPointerData.rayCastData.isHitpoint )
                {
                    
                    // Debug.Log ( inputPointerData.rayCastData.f3_hitpoint ) ;
                    // Debug.Log ( inputPointerData.rayCastData.f3_objectCenter ) ;

                    float3 f3_pointFromObjectCenter = inputPointerData.rayCastData.f3_hitpoint - inputPointerData.rayCastData.f3_objectCenter;

                    // Debug.Log ( f3_pointFromObjectCenter ) ;

                    float3 f3_pointFromObjectCenterAbs = new float3 ( Mathf.Abs ( f3_pointFromObjectCenter.x ),  Mathf.Abs ( f3_pointFromObjectCenter.y ),  Mathf.Abs ( f3_pointFromObjectCenter.z ) ) ;
                    
                    // Debug.Log ( f3_pointFromObjectCenterAbs ) ;

                    float3 f3_principleAxis ;

                    if ( f3_pointFromObjectCenterAbs.x > f3_pointFromObjectCenterAbs.y && f3_pointFromObjectCenterAbs.x > f3_pointFromObjectCenterAbs.z )
                    {
                        // x axis is the biggest

                        f3_principleAxis = new float3 ( f3_pointFromObjectCenter.x >= 0 ? 1 : -1, 0, 0 ) ;
                    }
                    else if ( f3_pointFromObjectCenterAbs.y > f3_pointFromObjectCenterAbs.z )
                    {
                        // y axis is the biggest
                        f3_principleAxis = new float3 ( 0, f3_pointFromObjectCenter.y >= 0 ? 1 : -1, 0 ) ;
                    }
                    else
                    {
                        // z axis is the biggest
                        f3_principleAxis = new float3 ( 0, 0, f3_pointFromObjectCenter.z >= 0 ? 1 : -1 ) ;
                    }
                    
                    // AddBlockSystem._AddBlockRequest ( new float3 (2,1,2) ) ;
                    AddBlockSystem._AddBlockRequest ( inputPointerData.rayCastData.f3_objectCenter + f3_principleAxis, new float3 (1,1,1), f3_principleAxis, inputPointerData.rayCastData.entityHit, new float4 ( 1, 1, 1,1 ) * 0 ) ;
                    //}

                    // Debug.Log ( "Add" ) ;


                    // Test 
                    //Debug.Log ( "Test debug: Add entity test" ) ;
                    // ECS.Octree.Point.AddOctreeNodeSystem._AddOctreeNodeRequest ( inputPointerData.rayCastData.f3_objectCenter + f3_principleAxis ) ;

                    //Debug.Log ( "Test debug: Add Octree node test" ) ;
                    //Debug.Log ( "From selected block position: " + inputPointerData.rayCastData.f3_objectCenter ) ;
                    //Debug.Log ( "To target block position: " + inputPointerData.rayCastData.f3_objectCenter + f3_principleAxis ) ;
                    //ECS.Octree.Point.AddPointOctreeNodeSystem2._AddNodeRequest ( 0, inputPointerData.rayCastData.f3_objectCenter + f3_principleAxis ) ;
                }

            }
            else if ( Input.GetMouseButtonUp ( 1 ) ) // right click
            {

                if ( inputPointerData.rayCastData.isHitpoint )
                {
                // Test 
                    Debug.Log ( "Test debug: Remove entity #" + inputPointerData.rayCastData.entityHit.Index ) ;
                    // ECS.Octree.Point.AddOctreeNodeSystem._AddOctreeNodeRequest ( inputPointerData.rayCastData.f3_objectCenter + f3_principleAxis ) ;

                    ECS.Test02.RemoveBlockSystem._RemoveBlockRequest ( inputPointerData.rayCastData.entityHit ) ;
                }

            }
            else
            {
                
                for (int i = 0; i < playersData.Length; ++i)
                {
                    _UpdatePlayerInput ( i ) ;                          
                }

            }
            
            InputPointerData ipd = new InputPointerData () ;

            inputPointerData.keysInputs.i3_mouseButtons = new int3 ( Input.GetMouseButtonUp ( 0 ) ? 1 : 0, Input.GetMouseButtonUp ( 1 ) ? 1 : 0, Input.GetMouseButtonUp ( 2 ) ? 1 : 0 ) ;
                        
            Vector3 V3_pointerPosition = Input.mousePosition ;
            Ray ray = Camera.main.ScreenPointToRay ( V3_pointerPosition ) ;
            
            inputPointerData.rayCastData.f3_origin = ray.origin ;
            inputPointerData.rayCastData.f3_direction = ray.direction ;
            
            inputPointerData.inputPointer = ipd.inputPointer ;
        }

        private void _UpdatePlayerInput ( int i )
        {
            PlayerInputComponent pi ;
            // KeysActionComponent keysInputs ;
                        
            VelocityComponent velocity ;
            VelocityPulseComponent velocityPulse ;
                        
            AngularVelocityPulseComponent f3_angularVelocityPulse ;

            // f3_velocityPulse = m_Players.a_velocityPulse [i].f3 ;


            pi.f3_move.x = Input.GetKey ( KeyCode.A ) ? 1: Input.GetKey ( KeyCode.D ) ? -1 : 0 ;
            pi.f3_move.y = Input.GetKey ( KeyCode.W ) ? 1: Input.GetKey ( KeyCode.S ) ? -1 : 0 ;

            pi.f_roll = Input.GetKey ( KeyCode.Z ) ? 1: Input.GetKey ( KeyCode.C ) ? -1 : 0 ;
            pi.f_ptich = Input.GetKey ( KeyCode.UpArrow ) ? 1: Input.GetKey ( KeyCode.DownArrow ) ? -1 : 0 ;
            pi.f_yaw = Input.GetKey ( KeyCode.LeftArrow ) ? 1: Input.GetKey ( KeyCode.RightArrow ) ? -1 : 0 ;

            //pi.Move.y = Input.GetAxis("Vertical");
            //pi.Shoot.x = Input.GetAxis("ShootX");
            //pi.Shoot.y = Input.GetAxis("ShootY");
            
            // pi.FireCooldown = Mathf.Max(0.0f, m_Players.Input[i].FireCooldown - dt);
            // pos.Value = m_Players.a_positions [i].Value + new float3 ( pi.Move.x * 0.01f, 0, 0 ) ;
            // m_Players.a_positions [i] = pos ;

            
            velocityPulse.f3 = new float3 ( pi.f3_move.x, 0, 0 ) ;
            playersData.a_velocityPulse [i] = velocityPulse ;

            velocity.f3 = playersData.a_velocity [i].f3 + new float3 ( 0, pi.f3_move.y * 0.01f, 0 ) ;
            playersData.a_velocity [i] = velocity ;
            // Debug.Log ( pi.Move.x ) ;

            // rotation
            Quaternion q_angularVelocityPulse = Quaternion.Euler ( new float3 ( pi.f_ptich, pi.f_yaw, pi.f_roll ) ) ;
            playersData.a_angularVelocityPulse [i] = new AngularVelocityPulseComponent { q = q_angularVelocityPulse } ;
                        
        }

    }
}
