using UnityEngine ;
using Unity.Entities ;
// using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Rendering ;

namespace ECS.Test02
{    
    public class AddBlockSystem : ComponentSystem
    {
        //static public EntityArchetype objectArchetype;

        struct BlockData
        {
            [ReadOnly] public readonly int Length;

            [ReadOnly] public EntityArray a_entities; // check this isntead entities Data Array
            public ComponentDataArray <AddBlockComponent> a_blockTags ;
            // public ComponentDataArray <EntityComponent> a_entities ;

            //public ComponentDataArray <TransformMatrix> a_transformMatrix ;
            //public ComponentDataArray <Position> a_position ;
            //public ComponentDataArray <PlayerInputComponent> a_playerInputs ;
            //public ComponentDataArray <IsBlockTag> a_isBlock ;
        }


        [Inject] private BlockData blockData ;

        [Inject] private Barrier addBlockBarrier ;

        static EntityManager entityManager ;

        protected override void OnCreateManager ( int capacity )
        {            
            Debug.Log ( "Add block system requires add Job Parallel For" ) ;

            commandsBuffer = addBlockBarrier.CreateCommandBuffer () ;

            entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
                        
            // entityManager = World.Active.GetOrCreateManager <EntityManager>() ;
            MeshInstanceRenderer renderer = Bootstrap.octreeCenter01 ;
            commandsBuffer.CreateEntity () ;
            // commandsBuffer.AddComponent ( new MeshCullingComponent { } ) ;     
            commandsBuffer.AddSharedComponent ( renderer ) ;
            
        }

        static private EntityCommandBuffer commandsBuffer ;

        protected override void OnUpdate ()
        {
            commandsBuffer = addBlockBarrier.CreateCommandBuffer () ;
            
            // Debug.Log ( "aa" ) ;
            // float dt = Time.deltaTime;

            for (int i = 0; i < blockData.Length; ++i)
            {
                // commandsBuffer.DestroyEntity ( blockData.a_entities [i].entity ) ;

                // entityManager.DestroyEntity ( blockData ) ;
                _AddBlock ( i );
                
            }
            
            
            /*
            NativeArray <Entity> a_entities = entityManager.get () ;

            Debug.Log ( a_entities.Length ) ;

            // destroy entities with AddBlock tags
            for (int i = 0; i < a_entities.Length; ++i)
            {
                Entity entity = a_entities [i] ;
                entityManager.DestroyEntity ( entity ) ;
            }

            a_entities.Dispose () ;
            */
            
        }

        private void _AddBlock ( int i )
        {

            //var player = Object.Instantiate(Settings.PlayerPrefab);
            //player.GetComponent<Position2D>().Value = new float2(0, 0);
            //player.GetComponent<Heading2D>().Value = new float2(0, 1);

            // Access the ECS entity manager
            // var entityManager = World.Active.GetOrCreateManager <EntityManager> ();

            // Create an entity based on the archetype. It will get default-constructed
            // defaults for all the component types we listed.
            //Entity entity = entityManager.CreateEntity ( objectArchetype ) ;
            // commandsBuffer.CreateEntity ( objectArchetype ) ;

            // We can tweak a few components to make more sense like this.
            //entityManager.SetComponentData(entity, new Position2D {Value = new float2(0.0f, 0.0f)});
            //entityManager.SetComponentData(entity, new Heading2D  {Value = new float2(0.0f, 1.0f)});
            //entityManager.SetComponentData(entity, new Health { Value = Settings.playerInitialHealth });
            
            
            // TransformMatrix tm = new TransformMatrix () ;
            // float3 scale = new float3 (1,1,1) * 2 ;
            // TransformMatrix tm = new TransformMatrix { Value = new float4x4(scale.x, 0, 0, 0, 0, scale.y, 0, 0, 0, 0, scale.z, 0, 0, 0, 0, 1) };

            // entityManager.AddComponentData ( temObjectEntity, tm ) ;
            //var scale = Scale.Scale[i].Value;
            // Scale.TransformMatrix[i] = new TransformMatrix { Value = math.mul(Scale.TransformMatrix[i].Value, new float4x4(scale.x, 0, 0, 0, 0, scale.y, 0, 0, 0, 0, scale.z, 0, 0, 0, 0, 1)) };
            // EntityStruct entityInstance = new EntityStruct () { entity = temObjectEntity, f3_position = f_addAtPosition } ;
            
            AddBlockComponent blockTagsData = blockData.a_blockTags [i] ;
                        
            float3 position = blockTagsData.f3_position ;
            float3 scale = blockTagsData.f3_scale ;
            
            
            // Entity entity = blockData.a_entities [i].entity ;
            Entity entity = blockData.a_entities [i] ;
            // float4x4 f4x4 = math.mul ( float4x4.identity, new float4x4(scale.x, 0, 0, 0, 0, scale.y, 0, 0, 0, 0, scale.z, 0, 0, 0, 0, 1) ) ; // set default position/rotation/scale matrix
            float4x4 f4x4 = new float4x4(scale.x, 0, 0, 0, 0, scale.y, 0, 0, 0, 0, scale.z, 0, position.x, position.y, position.z, 1) ; // set default position/rotation/scale matrix
            // commandsBuffer.AddComponent ( entity, new TransformMatrix { Value = f4x4 } ) ;
            commandsBuffer.AddComponent ( entity, new Position { Value = position } ) ;
            commandsBuffer.AddComponent ( entity, new Rotation { Value = new quaternion () } ) ;
            commandsBuffer.AddComponent ( entity, new Scale { Value  = scale } ) ;
            //commandsBuffer.AddComponent ( entity, new MeshCulledComponent { } ) ;
            // commandsBuffer.AddComponent ( entity, new MeshCullingComponent { } ) ;
            // commandsBuffer.AddComponent ( entity, new MeshInstanceRenderer { } ) ;
            /*commandsBuffer.AddComponent ( entity, new MeshCullingComponent { 
                BoundingSphereCenter = new float3 (0, 0, 0), 
                BoundingSphereRadius = 50, 
                CullStatus = 0 
            } ) ;*/
            
            //commandsBuffer.AddComponent ( entity, new MeshRenderBounds { Center = new float3 (1,1,1) * -50, Radius = 1 } ) ;   
            //commandsBuffer.AddComponent ( entity, new WorldMeshRenderBounds { Center = new float3 ( 1,1,1) * -7.5f, Radius = 3.5f } ) ;  
            //commandsBuffer.AddComponent ( entity, new MeshCulledComponent { } ) ;   

            // commandsBuffer.AddComponent ( entity, new PastPosition { f3 = blockData.a_blockTags [i].f3_position } ) ;   
            // commandsBuffer.AddComponent ( entity, new Position { Value = blockTagsData.f3_position } ) ;    
            commandsBuffer.AddComponent ( entity, new VelocityComponent { f3 = new float3 (0,0,0) } ) ;   
            commandsBuffer.AddComponent ( entity, new VelocityPulseComponent { f3 = new float3 (0,0,0) } ) ;  
            
            // commandsBuffer.AddComponent ( entity, new Rotation { Value = Quaternion.Euler ( 0, 0, 0 ) } ) ;    
            commandsBuffer.AddComponent ( entity, new AngularVelocityComponent { q = Quaternion.Euler ( new float3 (0,0,0) ) } ) ;   
            commandsBuffer.AddComponent ( entity, new AngularVelocityPulseComponent { q = Quaternion.Euler ( new float3 (0,0,0) ) } ) ; 
            
//            commandsBuffer.AddComponent ( entity, new PlayerInputComponent { } ) ;    

            commandsBuffer.AddComponent ( entity, new MassCompnent { } ) ;    
            commandsBuffer.AddComponent ( entity, new GravityApplyTag { } ) ;    
            
            // commandsBuffer.AddComponent ( entity, typeof ( SubtractiveComponent < IsBlockNotHighlightedTag > ) ) ; // dont work

            // assign neigbour block, from which this block has been created.
            // if creation has not been made from other block, then neighbour direction values are not set
            float3 f_directionFromReferenceNeighbourBlock = blockTagsData.f_directionFromReferenceNeighbourBlock ;
            if ( f_directionFromReferenceNeighbourBlock.x < 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { left = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            else if ( f_directionFromReferenceNeighbourBlock.x > 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { right = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            else if ( f_directionFromReferenceNeighbourBlock.y < 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { down = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            else if ( f_directionFromReferenceNeighbourBlock.y > 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { up = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            else if ( f_directionFromReferenceNeighbourBlock.z < 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { back = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            else if ( f_directionFromReferenceNeighbourBlock.z > 0 )
            {
                commandsBuffer.AddComponent ( entity, new NeighbourBlocks { front = blockTagsData.referenceNeighbourBlock } ) ; 
            }
            

            // tags
            commandsBuffer.AddComponent ( entity, new IsBlockTag { } ) ;     
            // commandsBuffer.AddComponent ( entity, new AllowRayCastingTag { } ) ;  
            
            // renderer
            MeshInstanceRenderer renderer ;
                        
            //blockTagsData.f4_color
            if ( blockTagsData.f4_color.x == 1 )
            {
                // apply random renderer (color/mesh), from the prefabs

                int i_textureIndex ;
                

                if ( Mathf.RoundToInt ( blockTagsData.f4_color.z ) == 0 || Mathf.RoundToInt ( blockTagsData.f4_color.z ) == 1 )
                {
                    
                    i_textureIndex = blockTagsData.f4_color.z == 1 ? Mathf.RoundToInt ( UnityEngine.Random.Range ( 1, 7) ) : Mathf.RoundToInt ( blockTagsData.f4_color.y ) ;

                    switch ( i_textureIndex )
                    {
                        case 1:
                            renderer = Bootstrap.octreeCenter01 ;
                            break ;
                        case 2:
                            renderer = Bootstrap.octreeCenter02 ;
                            break ;
                        case 3:
                            renderer = Bootstrap.octreeCenter03 ;
                            break ;
                        case 4:
                            renderer = Bootstrap.octreeCenter04 ;
                            break ;
                        case 5:
                            renderer = Bootstrap.octreeCenter05 ;
                            break ;
                        case 6:
                            renderer = Bootstrap.octreeCenter06 ;
                            break ;
                        case 7:
                            renderer = Bootstrap.octreeCenter07 ;
                            break ;
                        default:
                            renderer = Bootstrap.octreeCenter01 ;
                            break ;
                    }

                }
                else
                {
                    renderer = Bootstrap.octreeNode ;
                }
                
            }
            else
            {
                renderer = Bootstrap.playerRenderer ;
            }

            // renderer.material.SetColor ( "_Color", Color.blue ) ;
            commandsBuffer.AddSharedComponent ( entity, renderer ) ;
            // commandsBuffer.AddComponent ( entity, new MeshInstanceRenderer { } ) ;
            //commandsBuffer.AddComponent ( entity, new MeshCulledComponent { } ) ;
            //commandsBuffer.AddComponent ( entity, new MeshCullingComponent { BoundingSphereRadius = 100, CullStatus = 1 } ) ;
            /*
             * commandsBuffer.AddComponent ( entity, new MeshCullingComponent { 
                BoundingSphereCenter = new float3 (0, 0, 0), 
                BoundingSphereRadius = 50, 
                CullStatus = 0 
            } ) ;
            */
            //commandsBuffer.SetComponent <> () ;
            commandsBuffer.RemoveComponent <AddBlockComponent> ( entity ) ; // block added. Remove tag
                  



            
            // entityManager.AddComponent ( entity, typeof ( TransformMatrix ) ) ;
            // TransformMatrix tm = entityManager.GetComponentData <TransformMatrix> ( entity ) ;
            
            

            /*
             * // see an example of system
             * // https://gist.github.com/JoeCoo7/f497af9b1ba2ab5babae3060635a9c6a
             * // from forum
             * // https://forum.unity.com/threads/transformmatrixcomponent-and-scaling.524054/#post-3572125
             * // 08.2018
             * // also example
             * // Scale.TransformMatrix[i] = new TransformMatrix { Value = math.mul(Scale.TransformMatrix[i].Value, new float4x4(scale.x, 0, 0, 0, 0, scale.y, 0, 0, 0, 0, scale.z, 0, 0, 0, 0, 1)) };
             * float3 position = Positions[_index].Value;
            quaternion quat = Rotations[_index].Value;
            float3 scale = Scales[_index].Value;

            float4x4 matrix = math.mul(math.scale(scale), math.rottrans(quat, MathExt.Float3Zero()));
            matrix = math.mul(math.translate(position), matrix);
            Matrices[_index]= new ModelMatrix {Value = matrix };
            */

            /*
            TransformMatrix tm = new TransformMatrix () ;
            float4 matrixRow0 = new float4 ( 5, 0, 0, 0) ;
            float4 matrixRow1 = new float4 ( 0, 5, 0, 0) ;
            float4 matrixRow2 = new float4 ( 0, 0, 2, 0) ;
            float4 matrixRow3 = new float4 ( -3, -3, -3, 1) ;

            float4x4 matrix = new float4x4 ( matrixRow0, matrixRow1, matrixRow2, matrixRow3 ) ;
            tm.Value = matrix ;
            commandsBuffer.SetComponent <TransformMatrix> ( entity, tm ) ;
            */
            

            /*
            // set default position/rotation/scale matrix
            float4x4 f4x4 = float4x4.identity ;
            // blockData.a_transformMatrix [i].Value = f4x4 ;
            TransformMatrix transformMatrix = blockData.a_transformMatrix [i] ;
            transformMatrix.Value = f4x4 ;

            Position position = blockData.a_position [i] ;
            position.Value = new float3 (0,0,0) ;
            */
            //commandsBuffer.SetComponent () ;
            //entityManager.SetComponentData ( entity, new TransformMatrix { Value = f4x4 } ) ;
            //entityManager.SetComponentData ( entity, new Position { Value = new float3 (0,0,0) } ) ;

            // Finally we add a shared component which dictates the rendered mesh
            //entityManager.AddSharedComponentData ( entity, Bootstrap.playerRenderer ) ;



            //PlayerInputComponent pi ;
            //Position pos ;

            //pi.Move.x = Input.GetKey ( KeyCode.A ) ? 1: Input.GetKey ( KeyCode.D ) ? -1 : 0 ;
           // pi.Move.y = Input.GetKey ( KeyCode.W ) ? 1: Input.GetKey ( KeyCode.S ) ? -1 : 0 ;
            //pi.Move.y = Input.GetAxis("Vertical");
            //pi.Shoot.x = Input.GetAxis("ShootX");
            //pi.Shoot.y = Input.GetAxis("ShootY");
            
            // pi.FireCooldown = Mathf.Max(0.0f, m_Players.Input[i].FireCooldown - dt);
           // pos.Value = m_Players.a_positions [i].Value + new float3 ( pi.Move.x, 0, 0 ) ;
          //  m_Players.a_positions [i] = pos ;

          //  m_Players.a_inputs[i] = pi ;

            // Debug.Log ( "Add Block" ) ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public Entity _AddBlockRequest ( float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            // Create an entity based on the archetype. It will get default-constructed
            // defaults for all the component types we listed.
            // Entity entity = entityManager.CreateEntity ( objectArchetype ) ;
            Entity newEntity = entityManager.CreateEntity ( ) ;

            Debug.Log ( "Requested add new Block #" + newEntity.Index + " from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;

            // Entity entity = entityManager.CreateEntity ( typeof ( AddBlockTag ), typeof ( EntityComponent ) ) ;
            // entityManager.AddComponentData ( entity, new EntityStruct { entity = entity } ) ;
            entityManager.AddComponentData ( newEntity, new AddBlockComponent { referenceNeighbourBlock = entitySrc, f3_position = f3_position, f3_scale = f3_scale, f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, f4_color = f4_color } ) ; // tag it as new block. This tag will be removed after block added

            
            // Debug.Log ( "Adding Entity #" + entity.Index ) ;
            /*             
            commandsBuffer = m_RemoveDeadBarrier.CreateCommandBuffer () ;

            commandsBuffer.CreateEntity ( objectArchetype ) ;
            // commandsBuffer.AddComponent ( new EntityComponent () ) ;
            commandsBuffer.AddComponent ( new AddBlockTag () ) ;
            commandsBuffer.AddSharedComponent ( Bootstrap.playerRenderer ) ;
            */

            return newEntity ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public Entity _AddBlockRequestWithEntity ( Entity blockEntity, float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block #" + blockEntity.Index + " from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;

            if ( entityManager == null )
            {
                Debug.Log ( "Create new entity manager and commandsBuffer because method was called before OnCreateManager ()" ) ;
                entityManager = new EntityManager () ;
                commandsBuffer = new EntityCommandBuffer () ;
            }
            entityManager.AddComponentData ( blockEntity, new AddBlockComponent { referenceNeighbourBlock = entitySrc, f3_position = f3_position, f3_scale = f3_scale, f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, f4_color = f4_color } ) ; // tag it as new block. This tag will be removed after block added
            
            return blockEntity ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public EntityCommandBuffer _AddBlockRequestViaBuffer ( float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;

            commandsBuffer.CreateEntity ( ) ;            
            commandsBuffer.AddComponent ( new AddBlockComponent { 
                referenceNeighbourBlock = entitySrc, 
                f3_position = f3_position, 
                f3_scale = f3_scale, 
                f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, 
                f4_color = f4_color } ) ; // tag it as new block. This tag will be removed after block added            

            return commandsBuffer ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public EntityCommandBuffer _AddBlockRequestViaBufferWithEntity ( Entity entity, float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block #" + blockEntity.Index + " from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;
                                   
            commandsBuffer.AddComponent ( entity, new AddBlockComponent { 
                referenceNeighbourBlock = entitySrc, 
                f3_position = f3_position, 
                f3_scale = f3_scale, 
                f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, 
                f4_color = f4_color 
            } ) ; // tag it as new block. This tag will be removed after block added            

            return commandsBuffer ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public EntityCommandBuffer _AddBlockRequestViaCustomBuffer ( EntityCommandBuffer commandsBuffer, float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;

            commandsBuffer.CreateEntity ( ) ;            
            commandsBuffer.AddComponent ( new AddBlockComponent { 
                referenceNeighbourBlock = entitySrc, 
                f3_position = f3_position, f3_scale = f3_scale, 
                f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, 
                f4_color = f4_color 
            } ) ; // tag it as new block. This tag will be removed after block added            

            return commandsBuffer ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Requires precreated entity with command buffer.
        /// Call it from whatever place
        /// </summary>
        static public EntityCommandBuffer _AddBlockRequestViaCustomBufferNoCreateEntity ( EntityCommandBuffer commandsBuffer, float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;
         
            commandsBuffer.AddComponent ( new AddBlockComponent { 
                referenceNeighbourBlock = entitySrc, 
                f3_position = f3_position, f3_scale = f3_scale, 
                f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, 
                f4_color = f4_color 
            } ) ; // tag it as new block. This tag will be removed after block added            

            return commandsBuffer ;
        }

        /// <summary>
        /// Requests to adds new entity with block component.
        /// Call it from whatever place
        /// </summary>
        static public EntityCommandBuffer _AddBlockRequestViaCustomBufferWithEntity ( EntityCommandBuffer commandsBuffer, Entity entity, float3 f3_position, float3 f3_scale, float3 f3_directionAxisOfCreation, Entity entitySrc, float4 f4_color )
        {
            //Debug.Log ( "Requested add new Block #" + blockEntity.Index + " from entity # " + entitySrc.Index + "; at postion " + f3_position ) ;
                                   
            commandsBuffer.AddComponent ( entity, new AddBlockComponent { 
                referenceNeighbourBlock = entitySrc, 
                f3_position = f3_position, 
                f3_scale = f3_scale, 
                f_directionFromReferenceNeighbourBlock = f3_directionAxisOfCreation, 
                f4_color = f4_color 
            } ) ; // tag it as new block. This tag will be removed after block added            

            return commandsBuffer ;
        }

    }
}
