using UnityEngine;


namespace TestCollision01
{

    class _TestAABB_Collision : MonoBehaviour
    {

        Vector3 V3_boundingBoxMin = Vector3.one * -0.5f ;
        Vector3 V3_boundingBoxMax = Vector3.one * 0.5f ;

        private Vector3 V3_rayMin = Vector3.one * -0.2f ;
        private Vector3 V3_rayMax = Vector3.one * 1f ;

        public Vector3 V3_Hit = Vector3.zero ;

        public Transform tr ;

        private void Start ( )
        {
           
        }

        private void FixedUpdate ( )
        {
            Ray ray = Camera.main.ScreenPointToRay ( Input.mousePosition ) ;
            V3_rayMin = ray.origin ;
            V3_rayMax = ray.direction * 10000 ;

            Vector3 V3_boundingBoxOrigin = this.transform.position ;
            V3_boundingBoxMin = V3_boundingBoxOrigin + Vector3.one * -0.5f ;
            V3_boundingBoxMax = V3_boundingBoxOrigin + Vector3.one * 0.5f ;

            float f_closestHitPointSqrDistance = 0 ;
            bool isColliding = _AABBIntersectionTest ( V3_boundingBoxOrigin, V3_boundingBoxMin, V3_boundingBoxMax, V3_rayMin, V3_rayMax, ref V3_Hit, ref f_closestHitPointSqrDistance ) ;

           // Debug.Log ( V3_Hit.ToString ( "F4" ) ) ;
            //Debug.Log ( isColliding ? "Y: " + V3_Hit.ToString ( "F4" ) : "N") ;

            if ( isColliding )
            { 
                tr.position = V3_Hit ;
            }
            else
            {
                tr.position = this.transform.position ;
            }

            Debug.DrawLine ( V3_boundingBoxMin, V3_boundingBoxMax, Color.green ) ;
            Debug.DrawLine ( V3_rayMin, V3_rayMax, Color.blue ) ;
        }


        static public bool _AABBIntersectionTest ( Vector3 V3_boundingBoxOrigin, Vector3 V3_bbMin, Vector3 V3_bbMax, Vector3 V3_rayMin, Vector3 V3_rayMax, ref Vector3 V3_lastNearestHitPoint, ref float f_closestHitPointSqrDistance )
        {
            if (V3_rayMax.x < V3_bbMin.x && V3_rayMin.x < V3_bbMin.x) return false;
            if (V3_rayMax.x > V3_bbMax.x && V3_rayMin.x > V3_bbMax.x) return false;
            if (V3_rayMax.y < V3_bbMin.y && V3_rayMin.y < V3_bbMin.y) return false;
            if (V3_rayMax.y > V3_bbMax.y && V3_rayMin.y > V3_bbMax.y) return false;
            if (V3_rayMax.z < V3_bbMin.z && V3_rayMin.z < V3_bbMin.z) return false;
            if (V3_rayMax.z > V3_bbMax.z && V3_rayMin.z > V3_bbMax.z) return false;
            if (V3_rayMin.x > V3_bbMin.x && V3_rayMin.x < V3_bbMax.x &&
                V3_rayMin.y > V3_bbMin.y && V3_rayMin.y < V3_bbMax.y &&
                V3_rayMin.z > V3_bbMin.z && V3_rayMin.z < V3_bbMax.z)
            {
                // V3_hitPoint = V3_rayMin;
                V3_lastNearestHitPoint = V3_rayMin ;
                return true;
            }

            Vector3 V3_rayMaxSubMin = V3_rayMax - V3_rayMin ;
            Vector3 V3_rayMinSubBBMin = V3_rayMin - V3_bbMin ;
            Vector3 V3_rayMinSubBBMax = V3_rayMin - V3_bbMax ;
            Vector3 V3_rayMaxSubBBMin = V3_rayMax - V3_bbMin ;
            Vector3 V3_rayMaxSubBBMax = V3_rayMax - V3_bbMax ;

            V3_lastNearestHitPoint = V3_rayMin - V3_boundingBoxOrigin ;
            f_closestHitPointSqrDistance = 9999999 ;

            Vector3 V3_hitPoint = Vector3.zero ;

            bool isHit = false ;

            if (_GetIntersection ( V3_rayMinSubBBMin.x, V3_rayMaxSubBBMin.x, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 1) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }
            
            if (_GetIntersection ( V3_rayMinSubBBMin.y, V3_rayMaxSubBBMin.y, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 2) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }

            if (_GetIntersection ( V3_rayMinSubBBMin.z, V3_rayMaxSubBBMin.z, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 3) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }

            if (_GetIntersection ( V3_rayMinSubBBMax.x, V3_rayMaxSubBBMax.x, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 1) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }

            if (_GetIntersection ( V3_rayMinSubBBMax.y, V3_rayMaxSubBBMax.y, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 2) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }

            if (_GetIntersection ( V3_rayMinSubBBMax.z, V3_rayMaxSubBBMax.z, V3_rayMin, V3_rayMaxSubMin, ref V3_hitPoint ) )
            { 
                if ( _InBox (V3_hitPoint, V3_bbMin, V3_bbMax, 3) ) // return true; 
                {
                    _NearestHitpoint ( V3_hitPoint, V3_rayMin, ref f_closestHitPointSqrDistance, ref V3_lastNearestHitPoint ) ;

                    isHit |= true ;
                }
            }
              //)

            V3_lastNearestHitPoint += V3_rayMin ;

            /*
            if ((_GetIntersection (V3_rayMin.x - V3_bbMin.x, V3_rayMax.x - V3_bbMin.x, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 1))
              || (_GetIntersection (V3_rayMin.y - V3_bbMin.y, V3_rayMax.y - V3_bbMin.y, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 2))
              || (_GetIntersection (V3_rayMin.z - V3_bbMin.z, V3_rayMax.z - V3_bbMin.z, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 3))
              || (_GetIntersection (V3_rayMin.x - V3_bbMax.x, V3_rayMax.x - V3_bbMax.x, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 1))
              || (_GetIntersection (V3_rayMin.y - V3_bbMax.y, V3_rayMax.y - V3_bbMax.y, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 2))
              || (_GetIntersection (V3_rayMin.z - V3_bbMax.z, V3_rayMax.z - V3_bbMax.z, V3_rayMin, V3_rayMax, ref V3_HitPoint) && _InBox (V3_HitPoint, V3_bbMin, V3_bbMax, 3)))
              */
            return isHit;

            // return false;
        }

        static private bool _GetIntersection( float fDst1, float fDst2, Vector3 P1, Vector3 V3_rayMaxSubMin, ref Vector3 V3_hit )
        {
            if ((fDst1 * fDst2) >= 0.0f) return false;
            if (fDst1 == fDst2) return false;
            V3_hit = P1 + V3_rayMaxSubMin * (-fDst1 / (fDst2 - fDst1));
            return true;
        }

        /*
        bool _GetIntersection(float fDst1, float fDst2, Vector3 P1, Vector3 P2, ref Vector3 Hit)
        {
            if ((fDst1 * fDst2) >= 0.0f) return false;
            if (fDst1 == fDst2) return false;
            Hit = P1 + (P2 - P1) * (-fDst1 / (fDst2 - fDst1));
            return true;
        }
        */

        static private bool _InBox (Vector3 Hit, Vector3 B1, Vector3 B2, int Axis)
        {
            if (Axis == 1 && Hit.z > B1.z && Hit.z < B2.z && Hit.y > B1.y && Hit.y < B2.y) return true;
            if (Axis == 2 && Hit.z > B1.z && Hit.z < B2.z && Hit.x > B1.x && Hit.x < B2.x) return true;
            if (Axis == 3 && Hit.x > B1.x && Hit.x < B2.x && Hit.y > B1.y && Hit.y < B2.y) return true;
            return false;
        }

        /*
        static private bool _InAndONBox (Vector3 Hit, Vector3 B1, Vector3 B2, int Axis)
        {
            if (Axis == 1 && Hit.z >= B1.z && Hit.z <= B2.z && Hit.y >= B1.y && Hit.y <= B2.y) return true;
            if (Axis == 2 && Hit.z >= B1.z && Hit.z <= B2.z && Hit.x >= B1.x && Hit.x <= B2.x) return true;
            if (Axis == 3 && Hit.x >= B1.x && Hit.x <= B2.x && Hit.y >= B1.y && Hit.y <= B2.y) return true;
            return false;
        }
        */

        static private void _NearestHitpoint ( Vector3 V3_hitPoint, Vector3 V3_rayMin, ref float f_lastNearestHitPointSqrDistance, ref Vector3 V3_lastNearestHitPoint )
        {
            Vector3 V3_nearestHitPoint = V3_hitPoint - V3_rayMin ;
            float f_nearestHitPointSqrDistance = V3_nearestHitPoint.sqrMagnitude ;

            if ( f_nearestHitPointSqrDistance < f_lastNearestHitPointSqrDistance )
            {
                f_lastNearestHitPointSqrDistance = f_nearestHitPointSqrDistance ;
                V3_lastNearestHitPoint = V3_nearestHitPoint ;
                // return V3_nearestHitPoint ;
            }

        }
    }
}

