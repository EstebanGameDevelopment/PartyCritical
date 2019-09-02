﻿using System;
using System.Collections.Generic;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;

namespace PartyCritical
{
	public class TeleportController : MonoBehaviour
	{
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_TELEPORTCONTROLLER_ACTIVATION     = "EVENT_TELEPORTCONTROLLER_ACTIVATION";
        public const string EVENT_TELEPORTCONTROLLER_DEACTIVATION   = "EVENT_TELEPORTCONTROLLER_DEACTIVATION";
        public const string EVENT_TELEPORTCONTROLLER_TELEPORT       = "EVENT_TELEPORTCONTROLLER_TELEPORT";

        private const int PARABOLA_PRECISION = 450;

        // ----------------------------------------------
        // SINGLETON
        // ----------------------------------------------	
        private static TeleportController _instance;

        public static TeleportController Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = GameObject.FindObjectOfType(typeof(TeleportController)) as TeleportController;
                }
                return _instance;
            }
        }

        // ----------------------------------------------
        // PUBLIC MEMBERS
        // ----------------------------------------------	
        public GameObject CameraController;
        public GameObject MarkerDestination;
        public Material LineMaterial;
        public LayerMask AllowedLayers;
        public float MaxTeleportDistance = 22f;
        public float MatScale = 5;
        public Vector3 DestinationNormal;
        public float LineWidth = 0.05f;
        public Color GoodDestinationColor = new Color(0, 0.6f, 1f, 0.2f);
        public Color BadDestinationColor = new Color(0.8f, 0, 0, 0.2f);

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        private Vector3 m_FinalHitLocation;
        private Vector3 m_FinalHitNormal;        
        private GameObject m_FinalHitGameObject;
        private LineRenderer m_lineRenderer;

        private GameObject c_lineParent;
        private GameObject c_line1;
        private GameObject c_line2;

        private GameObject m_markerDestination;

        private bool m_activateTeleport = false;
        private bool m_calculateParabola = false;

        // ----------------------------------------------
        // GETTERS/SETTERS
        // ----------------------------------------------	
        public bool ActivateTeleport
        {
            get { return m_activateTeleport; }
        }

        // -------------------------------------------
        /* 
		 * Start
		 */
        public void Start()
        {
            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
            
            c_lineParent = new GameObject("Line");
            Utilities.AttachChild(this.gameObject.transform, c_lineParent);
            c_lineParent.transform.localScale = CameraController.transform.localScale;
            c_line1 = new GameObject("Line1");

            c_line1.transform.SetParent(c_lineParent.transform);
            m_lineRenderer = c_line1.AddComponent<LineRenderer>();
            c_line2 = new GameObject("Line2");
            c_line2.transform.SetParent(c_lineParent.transform);
            m_lineRenderer.startWidth = LineWidth * CameraController.transform.localScale.magnitude;
            m_lineRenderer.endWidth = LineWidth * CameraController.transform.localScale.magnitude;
            m_lineRenderer.material = LineMaterial;
            m_lineRenderer.SetPosition(0, Vector3.zero);
            m_lineRenderer.SetPosition(1, Vector3.zero);
        }

        // -------------------------------------------
        /* 
		 * OnDestroy
		 */
        public void OnDestroy()
        {
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
        }

        // -------------------------------------------
        /* 
        * DestroyMarkerTeleport
        */
        private void DestroyMarkerTeleport()
        {
            if (m_markerDestination != null)
            {
                GameObject.Destroy(m_markerDestination);
                m_markerDestination = null;
            }
            if (m_lineRenderer != null)
            {
                if (m_lineRenderer.gameObject != null)
                {
                    m_lineRenderer.gameObject.SetActive(false);
                }
            }            
        }

        // -------------------------------------------
        /* 
        * CheckReleasedKey
        */
        private void CheckReleasedKey()
        {            
                bool keyReleased = false;
#if ENABLE_WORLDSENSE && !UNITY_EDITOR
                if (KeysEventInputController.Instance.GetAppButtonDowDaydreamController(false))
                {
                    keyReleased = true;
                }
#endif
#if ENABLE_OCULUS && ENABLE_QUEST && !UNITY_EDITOR
                if (KeysEventInputController.Instance.GetThumbstickUpOculusController())
                {
                    keyReleased = true;
                }
#endif
#if UNITY_EDITOR
            if (Input.GetKeyUp(KeyCode.RightControl))
            {
                keyReleased = true;
            }
#endif
            if (keyReleased)
            {
                m_calculateParabola = false;
                Vector3 shiftToTarget = m_markerDestination.transform.position - transform.position;
                DestroyMarkerTeleport();
                
                NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_TELEPORTCONTROLLER_TELEPORT, 0.1f, Utilities.Vector3ToString(shiftToTarget));
                BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_TELEPORTCONTROLLER_DEACTIVATION, 0.2f);
            }
        }

        // -------------------------------------------
        /* 
        * ComputeParabola
        */
        internal void ComputeParabola()
        {
            if (m_markerDestination == null)
            {
                m_markerDestination = Instantiate(MarkerDestination);
                m_lineRenderer.gameObject.SetActive(true);
            }

            //	Line renderer position storage (two because line renderer texture will stretch if one is used)
            List<Vector3> positions1 = new List<Vector3>();

            //	first Vector3 positions array will be used for the curve and the second line renderer is used for the straight down after the curve
            float totalDistance1 = 0;

            //	Variables need for curve
            Quaternion currentRotation = transform.rotation;
            Vector3 currentPosition = transform.position;
            Vector3 lastPostion;
            positions1.Add(currentPosition);

            lastPostion = transform.position - transform.forward;
            Vector3 currentDirection = transform.forward;
            Vector3 downForward = new Vector3(transform.forward.x * 0.01f, -1, transform.forward.z * 0.01f);
            RaycastHit hit = new RaycastHit();
            m_FinalHitLocation = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int step = 0; step < PARABOLA_PRECISION; step++)
            {
                Quaternion downRotation = Quaternion.LookRotation(downForward);
                currentRotation = Quaternion.RotateTowards(currentRotation, downRotation, 0.2f);

                Ray newRay = new Ray(currentPosition, currentPosition - lastPostion);

                float length = (MaxTeleportDistance * 0.01f) * CameraController.transform.localScale.magnitude;
                if (currentRotation == downRotation)
                {
                    length = (MaxTeleportDistance * MatScale) * CameraController.transform.localScale.magnitude;
                    positions1.Add(currentPosition);
                }

                float raycastLength = length * 1.1f;

                //	Check if we hit something
                bool hitSomething = Physics.Raycast(newRay, out hit, raycastLength, AllowedLayers);

                // don't allow to teleport to negative normals (we don't want to be stuck under floors)
                if (hit.normal.y > 0)
                {
                    m_FinalHitLocation = hit.point;
                    m_FinalHitNormal = hit.normal;
                    m_FinalHitGameObject = hit.collider.gameObject;

                    totalDistance1 += (currentPosition - m_FinalHitLocation).magnitude;
                    positions1.Add(m_FinalHitLocation);

                    DestinationNormal = m_FinalHitNormal;

                    break;
                }

                //	Convert the rotation to a forward vector and apply to our current position
                currentDirection = currentRotation * Vector3.forward;
                lastPostion = currentPosition;
                currentPosition += currentDirection * length;

                totalDistance1 += length;
                positions1.Add(currentPosition);

                if (currentRotation == downRotation)
                    break;
            }

            m_lineRenderer.enabled = true;

            m_lineRenderer.startColor = GoodDestinationColor;
            m_lineRenderer.endColor = GoodDestinationColor;

            m_lineRenderer.positionCount = positions1.Count;
            m_lineRenderer.SetPositions(positions1.ToArray());

            m_markerDestination.transform.position = positions1[positions1.Count - 1];
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        private void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_TELEPORTCONTROLLER_ACTIVATION)
            {
                m_activateTeleport = true;
                m_calculateParabola = true;
            }
            if (_nameEvent == EVENT_TELEPORTCONTROLLER_DEACTIVATION)
            {
                m_activateTeleport = false;
                m_calculateParabola = false;
                DestroyMarkerTeleport();
            }
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        private void Update()
        {
            if (m_activateTeleport)
            {
                if (m_calculateParabola)
                {
                    ComputeParabola();
                    CheckReleasedKey();
                }
            }            
        }
    }
}