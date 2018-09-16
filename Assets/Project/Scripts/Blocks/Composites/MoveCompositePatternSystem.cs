using UnityEngine ;
using Unity.Entities ;
using Unity.Rendering ;
using Unity.Transforms ;
using Unity.Mathematics ;
using Unity.Collections ;
using Unity.Jobs ;
using Unity.Burst ;

namespace ECS.Blocks.Pattern
{
    // Creates prefab composites groups, to be utilised later by blocks
    // Each composite group holds number of components, creating pattern.
    
    public class MoveCompositeBarrier : BarrierSystem {} // required for conflicts avoidance (race condition)

    // [UpdateAfter ( typeof ( UnityEngine.Experimental.PlayerLoop.FixedUpdate ) ) ]
    // [UpdateAfter ( typeof ( GravitySystem ) ) ]    
    // [UpdateAfter(typeof(Barrier))]
    // [UpdateAfter(typeof(ReleaseCompositeBarrier))]
    public class MovePatternCompositesSystem : JobComponentSystem
    {     
       
        [Inject] private MovePatternData movePatternData ;  

        // request to assing pattern
        struct MovePatternData
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;
            
            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod010Tag> a_lod01Tag ;
            // public SubtractiveComponent <Common.Components.Lod05Tag> a_lod05Tag ; // test
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }

        [Inject] private MovePatternData2 movePatternData2 ;  

        // request to assing pattern
        struct MovePatternData2
        {
            public readonly int Length ;

            public EntityArray a_entities ;

            public ComponentDataArray <Blocks.PatternComponent> a_compositePatternComponent ;
            
            public ComponentDataArray <Blocks.MovePattern> a_movePatterns ;
            public BufferArray <Common.BufferElements.EntityBuffer> a_compositeEntities ;

            // Excludes entities that contain a MeshCollider from the group
            public SubtractiveComponent <Blocks.Pattern.RequestPatternReleaseTag> a_requestPatternReleaseTag ;
            public SubtractiveComponent <Blocks.Pattern.RequestPatternSetupTag> a_notSetupTag ;
            public SubtractiveComponent <Common.Components.IsNotAssignedTag> a_isNotAssignedTag ;

            public SubtractiveComponent <Blocks.Pattern.Components.IsLodSwitchedTag> a_isLodSwitchedTag ;
            public ComponentDataArray <Blocks.Pattern.Components.Lod020Tag> a_lod02Tag ; // test temp
            
            /// <summary>
            /// Tag requires composte pattern commponent to be set 
            /// </summary>            
            //public ComponentDataArray <Blocks.RequestPatternSetupTag> a_requestPatternSetupTag ;  
            // public ComponentDataArray <Common.Components.DisableSystemTag> a_disbaledSystemTag ;
        }
        
        [Inject] private ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
        [Inject] private MoveCompositeBarrier compositeBarrier ;
        static private float3 f3_moveAbout ;
                
        protected override void OnCreateManager ( int capacity )
        {
            // commandBuffer = compositeBarrier.CreateCommandBuffer () ; // new EntityCommandBuffer () ;
            // entityManager = World.Active.GetOrCreateManager <EntityManager>() ;            
                        
            base.OnCreateManager ( capacity );
        }

        protected override void OnDestroyManager ( )
        {
            // a_compositesPatternPrefabs.Dispose () ;
            base.OnDestroyManager ( );
        }

        // Forum topic discussing, why using IJob, rather IJObPrallelFor for BufferArray
        // https://forum.unity.com/threads/how-can-i-improve-or-jobify-this-system-building-a-list.547324/#post-3614746
        // Prevents potential race condition, of writting into same entities, form differnet prallel jobs
        // August 2018

            

             
        protected override JobHandle OnUpdate ( JobHandle inputDeps )
        {            
                      
            EntityCommandBuffer commandBuffer = compositeBarrier.CreateCommandBuffer () ;

            Unity.Mathematics.Random random = Pattern.AddPatternPrefabSystem._Random ( 1 ) ;
            f3_moveAbout += new float3 ( random.NextFloat ( -0.15f, 0.15f ) ,0,0 ) ;

            var movePatternDataJobHandle = new MovePatternCompositesDataJob // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = commandBuffer,
                movePatternData = movePatternData,
                // random = Pattern.AddPatternPrefabSystem._Random ( 1 ),
                // f3_moveAbout = f3_moveAbout,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( inputDeps ) ;

            var movePatternDataJobHandle2 = new MovePatternCompositesDataJob2 // for IJobParallelFor
            {    
                // options = GetBufferArrayFromEntity <Common.BufferElements.IntBuffer> (false), // not ReadOnly
                a_compositeComponents = a_compositeComponents,

                // entityManager = World.Active.GetOrCreateManager <EntityManager>(), // unable to use following
                commandBuffer = commandBuffer,
                movePatternData = movePatternData2,
                // random = Pattern.PatternPrefabSystem._Random ( 1 ),
                // f3_moveAbout = f3_moveAbout,
                //requestPatternSetupData = requestPatternSetupData,
                //spareCompositeData = spareCompositeData,
                
                
            } ; // .Schedule (inputDeps) ; // .Schedule( lod01Data.Length, 64, inputDeps) ; // IJobParallelFor

            // JobHandle mergeJobHandle = movePatternDataJobHandle.Schedule ( movePatternData.Length, 64, inputDeps ) ;
            mergeJobHandle = movePatternDataJobHandle2.Schedule ( mergeJobHandle ) ;  // test only 
          
            return mergeJobHandle ;
        
        }

            
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct MovePatternCompositesDataJob : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityManager entityManager ;
            // public EntityArray a_entities;     
            
            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public MovePatternData movePatternData ;

            // public Unity.Mathematics.Random random ;

            public ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
            // public float3 f3_moveAbout ;

                      // Blocks.MovePatternComonent
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                
                // Iterate through patterns groups, to move its composites
                for ( int i = 0; i < movePatternData.Length; i++ )
                {                    

                    PatternComponent patternGroup = movePatternData.a_compositePatternComponent [i] ;
                    // int i_componentsPatternIndex = patternGroup.i_patternIndex ; 

                    patternGroup.f_baseScale = 1f ; // test temp

                    MovePattern movePattern = movePatternData.a_movePatterns [i] ;
                    
                    int i_entityBufferCount = movePatternData.a_compositeEntities [i].Length ;

                    // position offset test
                    //movePattern.f3_position = f3_moveAbout + patternGroup.f_localPosition * patternGroup.f_baseScale * 3; // * 0.001f;
                    // movePattern.f3_position = f3_moveAbout + patternGroup.f_localPosition * patternGroup.f_baseScale + new float3 (5,0,0) ; // * 0.001f;
                    movePattern.f3_position = f3_moveAbout + patternGroup.f_localPosition * 3 + new float3 (5,0,0) ; // * 0.001f;

                    movePatternData.a_movePatterns [i] = movePattern ; // update
                            
                    float f_distance = math.lengthSquared ( movePattern.f3_position ) ;

                   // if ( f_distance < 8 )
                  //  {

                    // iterate through pattern's group composites
                    for ( int i_bufferIndex = 0; i_bufferIndex < i_entityBufferCount; i_bufferIndex ++)
                    {
                        
                        Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_compositeEntities [i][i_bufferIndex] ;
                        
                        Entity compositeEntity = entityBuffer.entity ;

                        if ( compositeEntity.Index != 0 )
                        {   
                            // get composite data
                            Blocks.CompositeComponent compositeComponent = a_compositeComponents [compositeEntity] ;                            
                            Blocks.Pattern.CompositeInPatternPrefabComponent blockCompositeBufferElement = Pattern.AddPatternPrefabSystem._GetCompositeFromPatternPrefab ( compositeComponent.i_inPrefabIndex ) ;
                        
                            // move composite
                            Position position = new Position () ;                            
                            position.Value = blockCompositeBufferElement.f3_position * patternGroup.f_baseScale + movePattern.f3_position ;
                            //position.Value = blockCompositeBufferElement.f3_position * 3 + movePattern.f3_position ;
                            // movePattern.f3_position = f3_moveAbout + patternGroup.f_localPosition * patternGroup.f_baseScale + new float3 (5,0,0) ; // * 0.001f;
                            commandBuffer.SetComponent ( compositeEntity, position ) ;

                            Scale scale = new Scale () { Value = blockCompositeBufferElement.f3_scale * patternGroup.f_baseScale } ;                             
                            commandBuffer.SetComponent ( compositeEntity, scale ) ;
                        }
                    } // for

                   // }
                  //  else
                  //  {
                        /*
                        Entity paternEntity = movePatternData.a_entities [i] ;
                        commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                        // Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;                        
                        commandBuffer.AddComponent ( paternEntity, new Common.Components.Lod05Tag () ) ; // test only 
                        commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                        */

                        //patternGroup.f_baseScale = 3 ;
                        //movePatternData.a_compositePatternComponent [i] = patternGroup ;
                  //  }

                    
                    
                } // for                
                
            }                       
        }
        
       
        /// <summary>
        /// Execute Jobs
        /// </summary>
        // [BurstCompile]
        // struct CompositeJob : IJobParallelFor
        struct MovePatternCompositesDataJob2 : IJob
        {
            public EntityCommandBuffer commandBuffer ; // concurrent is required for parallel job
            // public EntityManager entityManager ;
            // public EntityArray a_entities;     
            

            //public SpareCompositeData spareCompositeData ;            
            // public RequestPatternSetupData requestPatternSetupData ;
            public MovePatternData2 movePatternData ;

            //public Unity.Mathematics.Random random ;

            public ComponentDataFromEntity <Blocks.CompositeComponent> a_compositeComponents ;
            // public float3 f3_moveAbout ;

                      // Blocks.MovePatternComonent
            public void Execute ()  // for IJob
            // public void Execute ( int i )  // for IJobParallelFor
            {             
                
                // f3_moveAbout += new float3 ( random.NextFloat ( -0.01f, 0.01f ) ,0,0 ) ;

                // iterate through patterns group, to move its composites
                for ( int i = 0; i < movePatternData.Length; i++ )
                {                    

                    PatternComponent patternGroup = movePatternData.a_compositePatternComponent [i] ;
                    //int i_ComponentsPatternIndex = compositePatternComponent.i_patternIndex ; 

                    patternGroup.f_baseScale = 3 ; // test temp
                    movePatternData.a_compositePatternComponent [i] = patternGroup ;

                    MovePattern movePattern = movePatternData.a_movePatterns [i] ;
                    
                    //int i_entityBufferCount = movePatternData.a_compositeEntities [i].Length ;

                    //movePattern.f3_position = f3_moveAbout + new float3 (1,0,0) * i * patternGroup.f_baseScale + new float3 (5,0,0) ; // * 0.001f;
                    movePattern.f3_position = f3_moveAbout + patternGroup.f_localPosition * patternGroup.f_baseScale + new float3 (5,0,0) ; // * 0.001f;
                    movePatternData.a_movePatterns [i] = movePattern ; // update
                            
                    float f_distance = math.lengthSquared ( movePattern.f3_position ) ;

                    
                    Entity paternEntity = movePatternData.a_entities [i] ;
                        
                    // move composite
                    Position position = new Position () { Value = movePattern.f3_position } ;
                    //position.Value = blockCompositeBufferElement.f3_position + movePattern.f3_position ;
                    // commandBuffer.SetComponent ( paternEntity, position ) ;

                    // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternSetupTag () ) ;
                     
                    int i_entityBufferCount = movePatternData.a_compositeEntities [i].Length ;
                    
                    // iterate through pattern's group composites
                    for ( int i_bufferIndex = 0; i_bufferIndex < i_entityBufferCount; i_bufferIndex ++)
                    {
                        
                        Common.BufferElements.EntityBuffer entityBuffer = movePatternData.a_compositeEntities [i][i_bufferIndex] ;
                        
                        Entity compositeEntity = entityBuffer.entity ;

                        // get composite data
                        Blocks.CompositeComponent compositeComponent = a_compositeComponents [compositeEntity] ;                            
                        Blocks.Pattern.CompositeInPatternPrefabComponent blockCompositeBufferElement = Pattern.AddPatternPrefabSystem._GetCompositeFromPatternPrefab ( compositeComponent.i_inPrefabIndex ) ;
                        
                        // move composite
                        // Position position = new Position () ;
                        position.Value = blockCompositeBufferElement.f3_position * patternGroup.f_baseScale + movePattern.f3_position ;
                        commandBuffer.SetComponent ( compositeEntity, position ) ;

                        Scale scale = new Scale () { Value = blockCompositeBufferElement.f3_scale * patternGroup.f_baseScale } ;                             
                        commandBuffer.SetComponent ( compositeEntity, scale ) ;

                        if ( f_distance > 10 )
                        {

                        

                        }
                        else
                        {
                            // Entity paternEntity = movePatternData.a_entities [i] ;
                            // commandBuffer.AddComponent ( paternEntity, new Blocks.Pattern.RequestPatternReleaseTag () ) ;

                            //Test02.AddBlockSystem._AddBlockRequestViaCustomBufferWithEntity ( commandBuffer, paternEntity, movePattern.f3_position, new float3 (1,1,1), float3.zero, new Entity (), float4.zero ) ;
                        }

                    }
                    
                    
                } // for                
                
            }                       
        }

        
    }

    
}

