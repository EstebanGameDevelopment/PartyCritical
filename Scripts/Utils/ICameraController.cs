using UnityEngine;

namespace PartyCritical
{
    /******************************************
    * 
    * ICameraController
    * 
    * @author Esteban Gallardo
    */
    public interface ICameraController
    {
        // FUNCTIONS
        Vector3 GetPositionLaser();
        Vector3 GetForwardLaser();
        Vector3 GetForwardPoint(float _distance);
        GameObject CheckRaycastAgainst(params string[] _layers);
        Vector3 GetCollisionPointOfLaser(params string[] _layerIgnore);
    }
}