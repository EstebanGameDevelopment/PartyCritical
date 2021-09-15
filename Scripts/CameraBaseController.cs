#if ENABLE_GOOGLE_ARCORE
using GoogleARCore;
#endif
#if ENABLE_OCULUS
using OculusSampleFramework;
#endif
#if ENABLE_HTCVIVE
using WaveVR_Log;
using wvr;
#endif
#if ENABLE_PICONEO
using Pvr_UnitySDKAPI;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;
#if ENABLE_MULTIPLAYER_TIMELINE
using MultiplayerTimeline;
#endif

namespace PartyCritical
{
    public class CameraBaseController : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA = "EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA";
        public const string EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA = "EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA";
        public const string EVENT_CAMERACONTROLLER_DATA_SHOTGUN = "EVENT_CAMERACONTROLLER_DATA_SHOTGUN";
        public const string EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION = "EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION";
        public const string EVENT_CAMERACONTROLLER_OPEN_INVENTORY = "EVENT_CAMERACONTROLLER_OPEN_INVENTORY";
        public const string EVENT_CAMERACONTROLLER_START_MOVING = "EVENT_CAMERACONTROLLER_START_MOVING";
        public const string EVENT_CAMERACONTROLLER_STOP_MOVING = "EVENT_CAMERACONTROLLER_STOP_MOVING";
        public const string EVENT_CAMERACONTROLLER_FIX_DIRECTOR_CAMERA = "EVENT_CAMERACONTROLLER_FIX_DIRECTOR_CAMERA";
        public const string EVENT_CAMERACONTROLLER_ENABLE_LASER_POINTER = "EVENT_CAMERACONTROLLER_ENABLE_LASER_POINTER";
        public const string EVENT_CAMERACONTROLLER_ACTIVATE_SKYBOX = "EVENT_CAMERACONTROLLER_ACTIVATE_SKYBOX";
        public const string EVENT_CAMERACONTROLLER_APPLY_ROTATION_CAMERA = "EVENT_CAMERACONTROLLER_APPLY_ROTATION_CAMERA";

        public const string EVENT_CAMERACONTROLLER_GENERIC_ACTION_DOWN = "EVENT_CAMERACONTROLLER_GENERIC_ACTION_DOWN";
        public const string EVENT_CAMERACONTROLLER_GENERIC_ACTION_UP = "EVENT_CAMERACONTROLLER_GENERIC_ACTION_UP";

        public const string MARKER_NAME = "MARKER";

        // ----------------------------------------------
        // PUBLIC VARIABLES
        // ----------------------------------------------	
        public Transform CameraLocal;
        public GameObject OVRPlayer;
        public GameObject CenterEyeAnchor;
        public GameObject ShotgunContainer;
        public GameObject ShotgunRightContainer;
        public GameObject ShotgunLeftContainer;
        public GameObject SubContainerCamera;
        public GameObject[] HandTrackingObjects;

        public float ScaleMovementXZ = 4;
        public float ScaleMovementY = 2;

        public bool AreOculusHandsEnabled = false;

        // ----------------------------------------------
        // protected VARIABLES
        // ----------------------------------------------	
        protected enum RotationAxes { None = 0, MouseXAndY = 1, MouseX = 2, MouseY = 3, Controller = 4 }
        protected float m_sensitivityX = 7F;
        protected float m_sensitivityY = 7F;
        protected float m_minimumY = -60F;
        protected float m_maximumY = 60F;
        protected float m_rotationY = 0F;

        protected GameObject m_avatar;

        protected bool m_enableGyroscope = false;
        protected bool m_isTouchMode = false;
        protected bool m_enableVR = false;
        protected bool m_rotatedTo90 = false;
        protected float m_timeoutPressed = 0;
        protected float m_timeoutForPause = 0;
        protected float m_timeoutToMove = 0;
        protected float m_timeoutToTeleport = 0;
        protected bool m_hasBeenTeleported = false;
        protected bool m_actionTriggerDetected = false;
        protected bool m_shotTriggerDetected = false;

        protected bool m_enableFreeMovementCamera = false;

        protected bool m_activateMovement = false;

        protected bool m_enableShootAction = true;
        protected bool m_ignoreNextShootAction = false;

        protected bool m_enabledCameraInput = true;

#if (ONLY_REMOTE_CONNECTION || TELEPORT_INDIVIDUAL) && (ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO)
        protected bool m_teleportAvailable = false;
        protected bool m_teleportEnabled = false;
#else
        protected bool m_teleportAvailable = false;
        protected bool m_teleportEnabled = false;
#endif

        protected Vector3 m_shiftCameraFromOrigin = Vector3.zero;
        protected Vector3 m_fixedCameraPosition;
        protected Vector3 m_fixedCameraForward;

        protected float m_timeShotgun = 0;
#if ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
        protected GameObject m_armModel;
        protected GameObject m_laserPointer;
        protected Vector3 m_originLaser;
        protected Vector3 m_forwardLaser;
        protected Vector3 m_incrementJoystickTranslation = Vector3.zero;
#endif

        // ----------------------------------------------
        // GETTERS/SETTERS
        // ----------------------------------------------	
        public GameObject Avatar
        {
            get { return m_avatar; }
            set
            {
                m_avatar = value;
                this.transform.position = new Vector3(m_avatar.transform.position.x, m_avatar.transform.position.y + 1, m_avatar.transform.position.z);
            }
        }

        public bool EnableVR
        {
            get { return m_enableVR; }
        }
        public bool EnableGyroscope
        {
            get
            {
#if UNITY_EDITOR
                return m_enableGyroscope;
#else

                if (EnableARCore && !m_enableVR)
                {
                    return true;
                }
                else
                {
                    return m_enableGyroscope;
                }
#endif
            }
            set
            {
                m_enableGyroscope = value;
            }
        }
        public bool IsTouchMode
        {
            get { return m_isTouchMode; }
        }
        public virtual bool DirectorMode
        {
            get { return false; }
        }
        public virtual bool EnableARCore
        {
            get { return false; }
        }
        public virtual bool EnableBackgroundVR
        {
            get { return false; }
        }
        public virtual string EVENT_GAMECONTROLLER_MARKER_BALL
        {
            get { return ""; }
        }
        public virtual string EVENT_GAMESHOOT_NEW
        {
            get { return ""; }
        }
        public virtual string EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER
        {
            get { return ""; }
        }
        public virtual string EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER
        {
            get { return ""; }
        }
        public virtual string EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR
        {
            get { return ""; }
        }
        public virtual string EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR
        {
            get { return ""; }
        }
        public virtual float CAMERA_SHIFT_HEIGHT_ARCORE // NOT USED
        {
            get { return 1.8f; }
        }
        public virtual float CAMERA_SHIFT_HEIGHT_WORLDSENSE // YES, IT'S USED
        {
            get
            {
#if ENABLE_HTCVIVE && !UNITY_EDITOR
                return 1.7f;
#else
                return -1.7f;
#endif
            }
        }
        public virtual float CAMERA_SHIFT_HEIGHT_OCULUS  // NOT USED
        {
            get { return -1; }
        }
        public virtual List<GameObject> Players
        {
            get { return null; }
        }
        public virtual float PLAYER_SPEED
        {
            get { return -1; }
        }
        public virtual float PLAYER_DIRECTOR_SPEED
        {
            get { return 2; }
        }
        public virtual float TIMEOUT_TO_MOVE
        {
            get { return -1; }
        }
        public virtual float TIMEOUT_TO_MOVE_DIRECTOR
        {
            get { return 1; }
        }
        public virtual float TIMEOUT_TO_INVENTORY
        {
            get { return -1; }
        }
        public virtual float TIMEOUT_FOR_PAUSE
        {
            get { return -1; }
        }
        public virtual float TIME_UPDATE_SHOTGUN
        {
            get { return -1; }
        }
        public virtual float TIMEOUT_TO_TELEPORT
        {
            get { return 1; }
        }
        public virtual float ROTATE_LOCALCAMERA_VALUE
        {
            get { return 20; }
        }
        public Vector3 ShiftCameraFromOrigin
        {
            get { return m_shiftCameraFromOrigin; }
        }


        // -------------------------------------------
        /* 
         * InitialitzationLaserPointer
         */
        protected virtual void InitialitzationLaserPointer()
        {
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_CAMERACONTROLLER_ENABLE_LASER_POINTER, false);
        }


        // -------------------------------------------
        /* 
		 * InitialitzeHMDHeight
		 */
        protected virtual void InitialitzeHMDHeight()
        {
#if ENABLE_OCULUS
            this.gameObject.GetComponentInChildren<OVRCameraRig>().gameObject.transform.position = -new Vector3(0, 5*(CAMERA_SHIFT_HEIGHT_WORLDSENSE/10), 0);
#elif ENABLE_HTCVIVE
            if (OVRPlayer != null) OVRPlayer.transform.position = -new Vector3(0, 5 * (CAMERA_SHIFT_HEIGHT_WORLDSENSE / 10), 0);
#elif ENABLE_PICONEO
            if (OVRPlayer != null) OVRPlayer.transform.position = new Vector3(0, 5 * (CAMERA_SHIFT_HEIGHT_WORLDSENSE / 10), 0);
#endif
        }

        // -------------------------------------------
        /* 
		 * Initialize
		 */
        public virtual void Initialize()
        {
            InitialitzeHMDHeight();

            m_teleportAvailable = (GameObject.FindObjectOfType<TeleportController>() != null);

#if (ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO)
#if !DISABLE_ONLY_ONE_HAND
            if (KeysEventInputController.Instance.IsRightHanded())
            {
                if (ShotgunRightContainer != null)
                {
                    ShotgunContainer = ShotgunRightContainer;
                    if (ShotgunLeftContainer != null) ShotgunLeftContainer.SetActive(false);
                }
                else
                {
                    if (ShotgunLeftContainer != null)
                    {
                        ShotgunContainer = ShotgunLeftContainer;
                        if (ShotgunRightContainer != null) ShotgunRightContainer.SetActive(false);
                    }
                }
            }
            else
            {
                if (ShotgunLeftContainer != null)
                {
                    ShotgunContainer = ShotgunLeftContainer;
                    if (ShotgunRightContainer != null) ShotgunRightContainer.SetActive(false);
                }
                else
                {
                    if (ShotgunRightContainer != null)
                    {
                        ShotgunContainer = ShotgunRightContainer;
                        if (ShotgunLeftContainer != null) ShotgunLeftContainer.SetActive(false);
                    }
                }
            }
#endif
#else
            if (ShotgunLeftContainer != null) ShotgunLeftContainer.SetActive(false);
            if (ShotgunRightContainer != null)
            {
                ShotgunRightContainer.SetActive(true);
                ShotgunContainer = ShotgunRightContainer;
            }
#endif

#if ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
            if (ShotgunContainer != null) ShotgunContainer.SetActive(true);
#else
            if (ShotgunContainer != null) ShotgunContainer.SetActive(false);
#endif

#if ENABLE_OCULUS
            m_enableVR = true;
            AreOculusHandsEnabled = false;
            CameraLocal.gameObject.SetActive(false);
            if (OVRPlayer!=null) OVRPlayer.SetActive(true);
            this.GetComponent<Rigidbody>().useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            this.GetComponent<Collider>().isTrigger = true;

            if (ScreenOculusControlSelectionView.ControOculusWithHands())
            {
                if (ShotgunContainer != null) ShotgunContainer.SetActive(false);
            }
            else
            {
                if (GameObject.FindObjectsOfType<HandRayToolView>() != null)
                {
                    HandRayToolView[] handRays = GameObject.FindObjectsOfType<HandRayToolView>();
                    for (int j = 0; j < handRays.Length; j++)
                    {
                        if (handRays[j] != null)
                        {
                            handRays[j].gameObject.SetActive(false);
                        }
                    }
                }

                if (HandTrackingObjects != null)
                {
                    for (int k = 0; k < HandTrackingObjects.Length; k++)
                    {
                        if (HandTrackingObjects[k] != null)
                        {
                            HandTrackingObjects[k].gameObject.SetActive(false);
                        }
                    }
                }
            }
#elif ENABLE_HTCVIVE
            m_enableVR = true;
            if (CameraLocal != null) CameraLocal.gameObject.SetActive(false);
            if (OVRPlayer != null) OVRPlayer.SetActive(true);
            this.GetComponent<Rigidbody>().useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            this.GetComponent<Collider>().isTrigger = true;
#elif ENABLE_PICONEO
            m_enableVR = true;
            if (CameraLocal != null) CameraLocal.gameObject.SetActive(false);
            if (OVRPlayer != null) OVRPlayer.SetActive(true);
            this.GetComponent<Rigidbody>().useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            this.GetComponent<Collider>().isTrigger = true;
#else
            CameraLocal.gameObject.SetActive(true);
            if (OVRPlayer != null) OVRPlayer.SetActive(false);
#endif

            InitialitzationLaserPointer();

            if (DirectorMode)
            {
                if (ShotgunContainer != null) ShotgunContainer.SetActive(false);

                m_fixedCameraPosition = new Vector3(6.1f, 7.4f, -5.1f);
                m_fixedCameraForward = new Vector3(-0.6f, -0.7f, 0.4f);
            }

            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
            NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);

#if !UNITY_EDITOR && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
		if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
		{
			m_enableGyroscope = true;
		}
		else
		{
			m_enableVR = true;
        }
#endif

#if UNITY_EDITOR && !ENABLE_WORLDSENSE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
            if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
            {
                m_enableGyroscope = true;
            }
#endif

#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE && !ENABLE_HTCVIVE && !ENABLE_PICONEO
            CardboardLoaderVR.Instance.InitializeCardboard();
#endif

#if !ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                if (this.gameObject.GetComponentInChildren<Skybox>()!=null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif

            if (EnableARCore)
            {
                m_enableGyroscope = false;
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                if (EnableBackgroundVR)
                {
                    if (this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>() != null)
                    {
                        this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>().enabled = false;
                        if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
                    }
                }
                else
                {
                    if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = false;
                }
#else
                if (this.gameObject.GetComponentInChildren<Skybox>()!=null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif

            }
            else
            {
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                if (this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>() != null)
                {
                    this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>().enabled = false;
                }
#endif
            }

            if (DirectorMode)
            {
                if (!EnableARCore)
                {
                    m_enableGyroscope = true;
                }
                m_enableVR = false;
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
                if (this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>() != null) this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>().enabled = false;
#else
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif
            }

#if !ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
            if (CameraLocal != null)
            {
                if (CameraLocal.GetComponent<Camera>() != null)
                {
                    CameraLocal.GetComponent<Camera>().enabled = true;
                }
            }            
#endif

            BasicSystemEventController.Instance.DelayBasicSystemEvent(GameBaseController.EVENT_GAMECONTROLLER_REPORT_GYROSCOPE_MODE, 0.1f, m_enableGyroscope);
        }

        // -------------------------------------------
        /* 
		 * OnDestroy
		 */
        void OnDestroy()
        {
            m_avatar = null;

            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
            UIEventController.Instance.UIEvent -= OnUIEvent;
        }

        // -------------------------------------------
        /* 
		 * RotateCamera
		 */
        protected virtual void RotateCamera()
        {
            if (m_isTouchMode)
            {
                LogicTouchMode();
            }
            else
            {
                RotationAxes axes = RotationAxes.None;

                if ((Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0))
                {
                    axes = RotationAxes.MouseXAndY;
                }

                if ((axes != RotationAxes.Controller) && (axes != RotationAxes.None))
                {
                    if (axes == RotationAxes.MouseXAndY)
                    {
                        float rotationX = CameraLocal.localEulerAngles.y + Input.GetAxis("Mouse X") * m_sensitivityX;

                        m_rotationY += Input.GetAxis("Mouse Y") * m_sensitivityY;
                        m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);

                        CameraLocal.localEulerAngles = new Vector3(-m_rotationY, rotationX, 0);
                    }
                    else if (axes == RotationAxes.MouseX)
                    {
                        CameraLocal.Rotate(0, Input.GetAxis("Mouse X") * m_sensitivityX, 0);
                    }
                    else
                    {
                        m_rotationY += Input.GetAxis("Mouse Y") * m_sensitivityY;
                        m_rotationY = Mathf.Clamp(m_rotationY, m_minimumY, m_maximumY);

                        CameraLocal.localEulerAngles = new Vector3(-m_rotationY, transform.localEulerAngles.y, 0);
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
         * MoveCamera
         */
        protected virtual void MoveCamera()
        {
            bool ignoreMovement = true;
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE
            if (!DirectorMode)
            {
                ignoreMovement = false;
            }
            else
            {
                if (m_playerCameraActivated == null)
                {
                    if (m_enableFreeMovementCamera)
                    {
                        ignoreMovement = false;
                    }
                }
            }
#endif

            if (m_isTouchMode || ignoreMovement)
            {

            }
            else
            {
                if (!DirectorMode)
                {
                    this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                    this.gameObject.GetComponent<Rigidbody>().useGravity = true;
                    this.gameObject.GetComponent<Collider>().isTrigger = false;
                }

                float axisVertical = Input.GetAxis("Vertical");
                float axisHorizontal = Input.GetAxis("Horizontal");

                Vector3 forward = axisVertical * CameraLocal.forward * PLAYER_SPEED * Time.deltaTime;
                Vector3 lateral = axisHorizontal * CameraLocal.right * PLAYER_SPEED * Time.deltaTime;

                Vector3 increment = forward + lateral;
                if (!DirectorMode)
                {
                    increment.y = 0;
                }
                transform.GetComponent<Rigidbody>().MovePosition(transform.position + increment);
            }
        }

        // -------------------------------------------
        /* 
         * GetPositionLaser
         */
        public Vector3 GetPositionLaser()
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);            
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
            }
#endif

            return pos;
        }

        // -------------------------------------------
        /* 
         * GetForwardLaser
         */
        public Vector3 GetForwardLaser()
        {
#if ENABLE_YOURVRUI
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif

            return fwd;
        }


        // -------------------------------------------
        /* 
         * GetRightLaser
         */
        public Vector3 GetRightLaser()
        {
#if ENABLE_YOURVRUI
            Vector3 rightLaser = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.right);
#else
            Vector3 rightLaser = Utilities.Clone(CameraLocal.transform.right);
#endif
#if ENABLE_OCULUS
            rightLaser = m_laserPointer.transform.right;
#elif ENABLE_HTCVIVE
            rightLaser = m_laserPointer.transform.right;
#elif ENABLE_PICONEO
            rightLaser = m_laserPointer.transform.right;
#elif ENABLE_WORLDSENSE
            rightLaser = m_armModel.GetComponent<GvrArmModel>().ControllerRotationFromHead * Vector3.right;
#endif

            return rightLaser.normalized;
        }


        // -------------------------------------------
        /* 
         * GetForwardPoint
         */
        public Vector3 GetForwardPoint(float _distance)
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#endif

            return pos + fwd * _distance;
        }

        // -------------------------------------------
        /* 
         * CheckRaycastAgainst
         */
        public virtual GameObject CheckRaycastAgainst(params string[] _layers)
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif

            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRayWithMask(pos, fwd, ref raycastHit, _layers))
            {
                return raycastHit.collider.gameObject;
            }
            return null;
        }

        // -------------------------------------------
        /* 
         * CheckRaycastCollisionPoint
         */
        public virtual Vector3 CheckRaycastCollisionPoint(params string[] _layers)
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif

            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRayWithMask(pos, fwd, ref raycastHit, _layers))
            {
                return raycastHit.point;
            }
            return Vector3.zero;
        }

        // -------------------------------------------
        /* 
         * CheckRaycastCollisionPoint
         */
        public Vector3 CheckWorldsenseRaycastCollisionPoint(params string[] _layers)
        {
#if ENABLE_WORLDSENSE
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.LaserPointer.transform.position);
                Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.LaserPointer.transform.forward);
                RaycastHit raycastHit = new RaycastHit();
                if (Utilities.GetRaycastHitInfoByRayWithMask(pos, fwd, ref raycastHit, _layers))
                {
                    return raycastHit.point;
                }
            }
#endif
            return Vector3.zero;
        }

        // -------------------------------------------
        /* 
         * SetDirectorMarkerSignal
         */
        protected virtual void SetDirectorMarkerSignal(float _x, float _y)
        {
            Ray rayCamera = CameraLocal.GetComponent<Camera>().ScreenPointToRay(new Vector3(_x, _y, 0));

            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRay(rayCamera, ref raycastHit, ActorTimeline.LAYER_PLAYERS))
            {
                Vector3 pc = Utilities.Clone(raycastHit.point);
                NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_GAMECONTROLLER_MARKER_BALL, 0.1f, DirectorMode.ToString(), pc.x.ToString(), pc.y.ToString(), pc.z.ToString());
            }
        }

        // -------------------------------------------
        /* 
         * SetAMarkerSignal
         */
        protected virtual void SetAMarkerSignal()
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif

            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRay(pos, fwd, ref raycastHit, ActorTimeline.LAYER_PLAYERS))
            {
                Vector3 pc = Utilities.Clone(raycastHit.point);
                NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_GAMECONTROLLER_MARKER_BALL, 0.1f, DirectorMode.ToString(), pc.x.ToString(), pc.y.ToString(), pc.z.ToString());
            }
        }

        // -------------------------------------------
        /* 
         * GetCollisionPointOfLaser
         */
        public virtual Vector3 GetCollisionPointOfLaser(params string[] _layerIgnore)
        {
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#endif

            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRay(pos, fwd, ref raycastHit, _layerIgnore))
            {
                return Utilities.Clone(raycastHit.point);
            }
            return Vector3.zero;
        }

        // -------------------------------------------
        /* 
         * ActionShootPlayer
         */
        protected virtual void ActionShootPlayer(int _indexShoot = 0)
        {
            string position = "";
            string forward = "";
            bool shootDone = false;
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
			if ((m_armModel != null) && (m_laserPointer != null))
			{
				position = m_originLaser.x + "," + m_originLaser.y + "," + m_originLaser.z;
				forward = m_forwardLaser.x + "," + m_forwardLaser.y + "," + m_forwardLaser.z;
				shootDone = true;
			}
#else
#if ENABLE_YOURVRUI
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#else
            Vector3 pos = Utilities.Clone(CameraLocal.transform.position);
            Vector3 fwd = Utilities.Clone(CameraLocal.transform.forward.normalized);
#endif
            position = pos.x + "," + pos.y + "," + pos.z;
            forward = fwd.x + "," + fwd.y + "," + fwd.z;
            shootDone = true;
#endif

            if (shootDone)
            {
                // SixDOFConfiguration.PlayFxShoot();
                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMESHOOT_NEW, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), position, forward, _indexShoot.ToString());
            }
        }

        // -------------------------------------------
        /* 
         * OpenInventory
         */
        protected virtual void OpenInventory(bool _openedByTimer = true)
        {
#if !ENABLE_WORLDSENSE && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
            return;
#endif

            if (m_timeoutPressed > TIMEOUT_TO_INVENTORY)
            {
                m_timeoutPressed = 0;
#if (ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO) && !UNITY_EDITOR && ENABLE_MULTIPLAYER_TIMELINE
                if (GameObject.FindObjectOfType<ScreenInventoryView>() == null)
                {
                    UIEventController.Instance.DispatchUIEvent(GameLevelData.EVENT_GAMELEVELDATA_OPEN_INVENTORY);
                }                            
#elif UNITY_EDITOR && ENABLE_MULTIPLAYER_TIMELINE
                if (GameObject.FindObjectOfType<ScreenInventoryView>() == null)
                {
                    UIEventController.Instance.DispatchUIEvent(GameLevelData.EVENT_GAMELEVELDATA_OPEN_INVENTORY);
                }
#else
#if ENABLE_MULTIPLAYER_TIMELINE
                if (!EnableARCore)
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(GameLevelData.EVENT_GAMELEVELDATA_REQUEST_COLLISION_RAY, this);
                }
                else
                {

                    if (GameObject.FindObjectOfType<ScreenInventoryView>() == null)
                    {
                        UIEventController.Instance.DispatchUIEvent(GameLevelData.EVENT_GAMELEVELDATA_OPEN_INVENTORY);
                    }              
                }
#endif
#endif
            }
        }

        // -------------------------------------------
        /* 
         * OpenPause
         */
        protected virtual void OpenPause()
        {
            bool force = false;
#if ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
            force = true;
#endif
            if ((!m_enableGyroscope && !m_isTouchMode) || force)
            {
                m_timeoutForPause += Time.deltaTime;
                if (m_timeoutForPause > TIMEOUT_FOR_PAUSE)
                {
                    m_timeoutForPause = 0;
                    m_timeoutPressed = 0;

                    NetworkEventController.Instance.PriorityDelayNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_PAUSE_ACTION, 0.01f, true.ToString());
                }
            }
        }

        protected bool m_destroyScreensOnStartMovement = true;

        // -------------------------------------------
        /* 
        * RunOnMoveTimeoutPressed
        */
        protected virtual void RunMoveOnTimeoutPressed()
        {
#if (UNITY_EDITOR || (!ENABLE_OCULUS && !ENABLE_WORLDSENSE && !ENABLE_HTCVIVE && !ENABLE_PICONEO))
            if (m_timeoutToMove >= TIMEOUT_TO_MOVE)
            {
                bool allowMovement = !EnableARCore;
#if UNITY_EDITOR
                allowMovement = true;
#endif
                Vector3 normalForward = CameraLocal.forward.normalized;
                normalForward = new Vector3(normalForward.x, 0, normalForward.z);
                if (allowMovement)
                {
                    transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward * PLAYER_SPEED * Time.deltaTime);
                }
                else
                {
                    m_shiftCameraFromOrigin += normalForward * PLAYER_SPEED * Time.deltaTime;
                }
                if (m_destroyScreensOnStartMovement)
                {
                    m_destroyScreensOnStartMovement = false;
                    UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);
                }
            }
            else
            {
                m_destroyScreensOnStartMovement = true;
            }
#endif
        }

        // -------------------------------------------
        /* 
        * UpdateTransformShotgun
        */
        protected virtual void UpdateTransformShotgun()
        {

#if ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel == null) && (m_laserPointer == null))
            {
#if ENABLE_WORLDSENSE
                WorldsenseHandController deviceController = GameObject.FindObjectOfType<WorldsenseHandController>();
                if (deviceController == null)
                {
                    if (GameObject.FindObjectOfType<GvrArmModel>() != null) m_armModel = GameObject.FindObjectOfType<GvrArmModel>().gameObject;
                    if (GameObject.FindObjectOfType<GvrControllerVisual>() != null) m_laserPointer = GameObject.FindObjectOfType<GvrControllerVisual>().gameObject;
                }
                else
                {
                    m_armModel = deviceController.ControlledObject.gameObject;
                    m_laserPointer = deviceController.ControlledObject.gameObject;
                }
#elif ENABLE_HTCVIVE
#if DISABLE_ONLY_ONE_HAND
                m_armModel = YourVRUIScreenController.Instance.ContainerLaser;
                m_laserPointer = YourVRUIScreenController.Instance.ContainerLaser;
#else
                HTCHandController deviceController = GameObject.FindObjectOfType<HTCHandController>();
                if (deviceController != null)
                {
#if UNITY_EDITOR
                    m_armModel = YourVRUIScreenController.Instance.GameCamera.gameObject;
                    m_laserPointer = YourVRUIScreenController.Instance.GameCamera.gameObject;
#else
                    m_armModel = deviceController.ControlledObject.gameObject;
                    m_laserPointer = deviceController.ControlledObject.gameObject;
#endif
                }
#endif
#elif ENABLE_PICONEO
#if DISABLE_ONLY_ONE_HAND
                m_armModel = YourVRUIScreenController.Instance.ContainerLaser;
                m_laserPointer = YourVRUIScreenController.Instance.ContainerLaser;
#else
                PicoNeoHandController deviceController = GameObject.FindObjectOfType<PicoNeoHandController>();
                if (deviceController != null)
                {
#if UNITY_EDITOR
                    m_armModel = YourVRUIScreenController.Instance.GameCamera.gameObject;
                    m_laserPointer = YourVRUIScreenController.Instance.GameCamera.gameObject;
#else
                    m_armModel = deviceController.ControlledObject.gameObject;
                    m_laserPointer = deviceController.ControlledObject.gameObject;
#endif
                }
#endif
#elif ENABLE_OCULUS
                bool lookForLaser = true;
                if (ScreenOculusControlSelectionView.ControOculusWithHands() &&
                    (GameObject.FindObjectsOfType<HandRayToolView>() != null))
                {
                    HandRayToolView[] handRays = GameObject.FindObjectsOfType<HandRayToolView>();
                    for (int j = 0; j < handRays.Length; j++)
                    {
                        if (handRays[j].EnableState)
                        {
                            m_armModel = new GameObject();
                            m_laserPointer = handRays[j].gameObject;
                            lookForLaser = false;
                            AreOculusHandsEnabled = true;
                        }
                    }
                }
#if DISABLE_ONLY_ONE_HAND
                if (lookForLaser)
                {
                    m_armModel = new GameObject();
                    m_laserPointer = YourVRUIScreenController.Instance.ContainerLaser;
                    if (m_laserPointer != null)
                    {
                        if (GameObject.FindObjectOfType<InteractableOculusHandsCreator>() != null)
                        {
                            GameObject.FindObjectOfType<InteractableOculusHandsCreator>().gameObject.SetActive(false);
                        }
                    }
                }
                else
                {
                    YourVRUIScreenController.Instance.ContainerLaser.SetActive(false);
                }
#else
                if (GameObject.FindObjectOfType<OVRControllerHelper>() != null)
                {
                    if (lookForLaser)
                    {
                        m_armModel = new GameObject();
                        m_laserPointer = GameObject.FindObjectOfType<OVRControllerHelper>().gameObject;
                        if (m_laserPointer != null)
                        {
                            if (GameObject.FindObjectOfType<InteractableOculusHandsCreator>() != null)
                            {
                                GameObject.FindObjectOfType<InteractableOculusHandsCreator>().gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        GameObject.FindObjectOfType<OVRControllerHelper>().gameObject.SetActive(false);
                    }
                }
#endif
#endif
            }
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                m_originLaser = m_laserPointer.transform.position;
#if ENABLE_WORLDSENSE
                WorldsenseHandController deviceController = GameObject.FindObjectOfType<WorldsenseHandController>();
                if (deviceController == null)
                {
                    m_forwardLaser = m_armModel.GetComponent<GvrArmModel>().ControllerRotationFromHead * Vector3.forward;
                }
                else
                {
                    m_forwardLaser = m_laserPointer.transform.forward;
                }
#elif ENABLE_OCULUS
                m_forwardLaser = m_laserPointer.transform.forward;
#elif ENABLE_HTCVIVE
                m_forwardLaser = m_laserPointer.transform.forward;
#elif ENABLE_PICONEO
                m_forwardLaser = m_laserPointer.transform.forward;
#endif
                m_forwardLaser.Normalize();
            }
            m_timeShotgun += Time.deltaTime;
            if (m_timeShotgun > TIME_UPDATE_SHOTGUN)
            {
                m_timeShotgun = 0;
                SendDataShotgun();
            }
#endif
        }

        // -------------------------------------------
        /* 
        * IsThereBlockingScreen
        */
        protected virtual bool IsThereBlockingScreen()
        {
#if !ENABLE_YOURVRUI
            return false;
#else
            // ENABLE DEFAULT INPUTS WHEN THERE ARE SCREEN ACTIVATED
            if (YourVRUIScreenController.Instance.ScreensTemporal.Count > 0)
            {
#if ENABLE_OCULUS
                KeysEventInputController.Instance.EnableActionButton = false;
#elif ENABLE_HTCVIVE
                KeysEventInputController.Instance.EnableActionButton = false;
#elif ENABLE_PICONEO
                KeysEventInputController.Instance.EnableActionButton = false;
#elif ENABLE_WORLDSENSE
                KeysEventInputController.Instance.EnableActionButton = true;
                return true;
#else
                KeysEventInputController.Instance.EnableActionButton = true;
                bool allowBlocking = EnableARCore;
#if UNITY_EDITOR
                allowBlocking = false;
#endif
                if (allowBlocking || (m_timeoutToMove < 1))
                {
                    return true;
                }
#endif
            }
            else
            {
                KeysEventInputController.Instance.EnableActionButton = false;
            }
            return false;
#endif
        }

        // -------------------------------------------
        /* 
         * CheckKeyInventoryTriggered
         */
        protected virtual bool CheckKeyInventoryTriggered(bool _keyEventUpToActivateInventory)
        {
            return _keyEventUpToActivateInventory
#if ENABLE_OCULUS
                || KeysEventInputController.Instance.GetMenuButtonDownOculusController();
#elif ENABLE_HTCVIVE
                || KeysEventInputController.Instance.GetMenuDownHTCViveController();
#elif ENABLE_PICONEO
                || KeysEventInputController.Instance.GetMenuDownPicoNeoController();
#elif ENABLE_WORLDSENSE
                || KeysEventInputController.Instance.GetActionCurrentStateDaydreamController();
#else
                || KeysEventInputController.Instance.GetActionCurrentStateDefaultController();
#endif
        }

        // -------------------------------------------
        /* 
         * CheckKeyPauseTriggered
         */
        protected virtual bool CheckKeyPauseTriggered()
        {
            return
#if ENABLE_OCULUS
                    KeysEventInputController.Instance.GetAppButtonDownOculusController(null, false) || KeysEventInputController.Instance.GetAppButtonOculusController();
#elif ENABLE_HTCVIVE
                    KeysEventInputController.Instance.GetAppDownHTCViveController(null, false) || KeysEventInputController.Instance.GetAppHTCViveController();
#elif ENABLE_PICONEO
                    KeysEventInputController.Instance.GetAppDownPicoNeoController(null, false) || KeysEventInputController.Instance.GetAppPicoNeoController();
#elif ENABLE_WORLDSENSE
                    KeysEventInputController.Instance.GetAppButtonDowDaydreamController(false, false);
#else
                    KeysEventInputController.Instance.GetActionCurrentStateDefaultController();

#endif
        }

        // -------------------------------------------
        /* 
         * CheckToOpenInventory
         */
        protected virtual void CheckToOpenInventory()
        {
            // APP BUTTON PRESSED
#if ENABLE_QUEST && ENABLE_OCULUS
            if (KeysEventInputController.Instance.GetMenuButtonDownOculusController())
            {
                m_timeoutPressed = TIMEOUT_TO_INVENTORY + 1;
            }
#endif
#if ENABLE_HTCVIVE
            if (KeysEventInputController.Instance.GetMenuDownHTCViveController())
            {
                m_timeoutPressed = TIMEOUT_TO_INVENTORY + 1;
            }
#endif
#if ENABLE_PICONEO
            if (KeysEventInputController.Instance.GetMenuDownPicoNeoController())
            {
                m_timeoutPressed = TIMEOUT_TO_INVENTORY + 1;
            }
#endif
        }

        // -------------------------------------------
        /* 
         * UpdateLogicTeleportInventory
         */
        protected virtual void UpdateLogicTeleportInventory(bool _openInventory = true)
        {
            // TELEPORT INPUT ACTIVATION
            if (m_teleportAvailable)
            {
                if (m_teleportEnabled)
                {
                    if (m_timeoutToTeleport > 0)
                    {
                        m_timeoutToTeleport += Time.deltaTime;
                        if (m_timeoutToTeleport > TIMEOUT_TO_TELEPORT)
                        {
                            m_timeoutToTeleport = 0;
                            m_hasBeenTeleported = true;
                            BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_ACTIVATION);
                        }
                    }
                }

#if ENABLE_WORLDSENSE
                if (m_teleportEnabled)
                {
                    if (KeysEventInputController.Instance.GetAppButtonDowDaydreamController(true, false))
                    {
                        m_timeoutToTeleport += Time.deltaTime;
                    }
                    if (!KeysEventInputController.Instance.GetAppButtonDowDaydreamController(true, false))
                    {
                        m_timeoutToTeleport = 0;
                    }
                }
#endif
#if ENABLE_OCULUS
                if (m_teleportEnabled)
                {
                    if (KeysEventInputController.Instance.GetHandTriggerOculusController())
                    {
#if ENABLE_QUEST
                        m_timeoutToTeleport = TIMEOUT_TO_TELEPORT + 1;
#else
                        m_timeoutToTeleport += Time.deltaTime;
#endif
                    }
                    if (!KeysEventInputController.Instance.GetHandTriggerOculusController())
                    {
                        m_timeoutToTeleport = 0;
                    }
                }
#endif
#if ENABLE_HTCVIVE
                if (m_teleportEnabled)
                {
                    if (KeysEventInputController.Instance.GetMenuHTCViveController())
                    {
                        m_timeoutToTeleport += Time.deltaTime;
                    }
                    if (!KeysEventInputController.Instance.GetMenuHTCViveController())
                    {
                        m_timeoutToTeleport = 0;
                    }
                }
#endif
#if ENABLE_PICONEO
                if (m_teleportEnabled)
                {
                    if (KeysEventInputController.Instance.GetMenuPicoNeoController())
                    {
                        m_timeoutToTeleport += Time.deltaTime;
                    }
                    if (!KeysEventInputController.Instance.GetMenuPicoNeoController())
                    {
                        m_timeoutToTeleport = 0;
                    }
                }
#endif
#if UNITY_EDITOR
                if (Input.GetKeyDown(KeyCode.RightControl))
                {
                    m_timeoutToTeleport = 0.01f;
                }
#endif
            }

            // OPEN INVENTORY
            CheckToOpenInventory();

            bool activateInventory = true;


            bool keyEventUpToActivateInventory = false;
#if UNITY_EDITOR
            if (Input.GetKeyUp(KeyCode.RightControl))
            {
                if (m_teleportAvailable && m_teleportEnabled)
                {
                    activateInventory = (m_timeoutToTeleport < TIMEOUT_TO_TELEPORT);
                }
                if (activateInventory)
                {
                    m_timeoutToTeleport = 0;
                    m_timeoutPressed = TIMEOUT_TO_INVENTORY;
                    keyEventUpToActivateInventory = true;
                }
            }
#endif

            if (CheckKeyInventoryTriggered(keyEventUpToActivateInventory))
            {
                m_timeoutPressed += Time.deltaTime;

                if (_openInventory)
                {
                    OpenInventory(true);

#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                    m_timeoutToMove += Time.deltaTime;
#endif
                }
            }
            RunMoveOnTimeoutPressed();

            if (CheckKeyPauseTriggered())
            {
                if (_openInventory)
                {
                    OpenPause();
                }
            }
            else
            {
                m_timeoutForPause = 0;
            }

            CheckReleasedKeyForTeleport();
        }

        // -------------------------------------------
        /* 
        * CheckReleasedKeyForTeleport
        */
        protected virtual void CheckReleasedKeyForTeleport()
        {
            if (m_hasBeenTeleported)
            {
#if ENABLE_WORLDSENSE
                if (!KeysEventInputController.Instance.GetAppButtonDowDaydreamController(true, false))
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_KEY_RELEASED);
                }
#endif
#if ENABLE_OCULUS
                if (!KeysEventInputController.Instance.GetHandTriggerOculusController())
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_KEY_RELEASED);
                }
#endif
#if ENABLE_HTCVIVE
                if (!KeysEventInputController.Instance.GetMenuHTCViveController())
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_KEY_RELEASED);
                }
#endif
#if ENABLE_PICONEO
                if (!KeysEventInputController.Instance.GetMenuPicoNeoController())
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_KEY_RELEASED);
                }
#endif
#if UNITY_EDITOR
                if (Input.GetKeyUp(KeyCode.RightControl))
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_KEY_RELEASED);
                }
#endif
            }
        }

        // -------------------------------------------
        /* 
        * LogicTeleportQuest
        */
        protected virtual void LogicTeleportQuest()
        {
#if ENABLE_OCULUS
            Vector2 axisValueRight = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            if (axisValueRight.sqrMagnitude > 0.2f)
            {
                Vector3 forward = axisValueRight.y * CenterEyeAnchor.transform.forward * PLAYER_SPEED * Time.deltaTime;
                Vector3 lateral = axisValueRight.x * CenterEyeAnchor.transform.right * PLAYER_SPEED * Time.deltaTime;

                m_incrementJoystickTranslation = forward + lateral;
                m_incrementJoystickTranslation.y = 0;
            }
#endif
        }

        // -------------------------------------------
        /* 
        * LogicTeleportHTCVive
        */
        protected virtual void LogicTeleportHTCVive()
        {
#if ENABLE_HTCVIVE
            /*
            Vector2 axisValueRight = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            if (axisValueRight.sqrMagnitude > 0.2f)
            {
                Vector3 forward = axisValueRight.y * CenterEyeAnchor.transform.forward * PLAYER_SPEED * Time.deltaTime;
                Vector3 lateral = axisValueRight.x * CenterEyeAnchor.transform.right * PLAYER_SPEED * Time.deltaTime;

                m_incrementJoystickTranslation = forward + lateral;
                m_incrementJoystickTranslation.y = 0;
            }
            */
#endif
        }

        // -------------------------------------------
        /* 
        * LogicTeleportPicoNeo
        */
        protected virtual void LogicTeleportPicoNeo()
        {
#if ENABLE_PICONEO
            /*
            Vector2 axisValueRight = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            if (axisValueRight.sqrMagnitude > 0.2f)
            {
                Vector3 forward = axisValueRight.y * CenterEyeAnchor.transform.forward * PLAYER_SPEED * Time.deltaTime;
                Vector3 lateral = axisValueRight.x * CenterEyeAnchor.transform.right * PLAYER_SPEED * Time.deltaTime;

                m_incrementJoystickTranslation = forward + lateral;
                m_incrementJoystickTranslation.y = 0;
            }
            */
#endif
        }

        // -------------------------------------------
        /* 
        * ProcessOculusCustomerInput
        */
        protected virtual void ProcessOculusCustomerInput()
        {
#if ENABLE_OCULUS
            if (KeysEventInputController.Instance.GetActionOculusController(true))
            {
                m_timeoutPressed = 0;
                if (KeysEventInputController.Instance.EnableActionOnMouseDown)
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_SET_ANCHOR_POSITION);
                }
                if (YourVRUIScreenController.Instance.ScreensTemporal.Count == 0)
                {
                    SetAMarkerSignal();
                }
            }

#if ENABLE_QUEST
#if TELEPORT_INDIVIDUAL || ONLY_REMOTE_CONNECTION
            LogicTeleportQuest();
#endif
#else
            if (OVRInput.GetDown(OVRInput.Button.PrimaryTouchpad))
            {
                m_timeoutToMove = TIMEOUT_TO_MOVE;
            }
            if (OVRInput.GetUp(OVRInput.Button.PrimaryTouchpad))
            {
                m_timeoutToMove = 0;
            }
            if (OVRInput.Get(OVRInput.Button.PrimaryTouchpad))
            {
                m_timeoutToMove += Time.deltaTime;
                if (m_timeoutToMove >= TIMEOUT_TO_MOVE)
                {
                    Vector3 normalForward = CameraLocal.forward.normalized;
                    normalForward = CenterEyeAnchor.transform.forward.normalized;
                    normalForward = new Vector3(normalForward.x, 0, normalForward.z);
                    transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward * PLAYER_SPEED * Time.deltaTime);
                }
            }
#endif
#endif
        }

        // -------------------------------------------
        /* 
        * ProcessHTCViveCustomerInput
        */
        protected virtual void ProcessHTCViveCustomerInput()
        {
#if ENABLE_HTCVIVE
            if (KeysEventInputController.Instance.GetActionHTCViveController(true))
            {
                m_timeoutPressed = 0;
                if (KeysEventInputController.Instance.EnableActionOnMouseDown)
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_SET_ANCHOR_POSITION);
                }
                if (YourVRUIScreenController.Instance.ScreensTemporal.Count == 0)
                {
                    SetAMarkerSignal();
                }
            }
#if TELEPORT_INDIVIDUAL || ONLY_REMOTE_CONNECTION
            LogicTeleportHTCVive();
#endif
#endif
        }

        // -------------------------------------------
        /* 
        * ProcessPicoNeoCustomerInput
        */
        protected virtual void ProcessPicoNeoCustomerInput()
        {
#if ENABLE_PICONEO
            if (KeysEventInputController.Instance.GetActionPicoNeoController(true))
            {
                m_timeoutPressed = 0;
                if (KeysEventInputController.Instance.EnableActionOnMouseDown)
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_SET_ANCHOR_POSITION);
                }
                if (YourVRUIScreenController.Instance.ScreensTemporal.Count == 0)
                {
                    SetAMarkerSignal();
                }
            }
#if TELEPORT_INDIVIDUAL || ONLY_REMOTE_CONNECTION
            LogicTeleportPicoNeo();
#endif
#endif
        }

        // -------------------------------------------
        /* 
        * ProcessActionInputCustomer
        */
        protected virtual void ProcessActionInputCustomer()
        {
            // INPUTS FOR THE IN-GAME, NOT THE SCREENS
            if (false
#if ENABLE_OCULUS
                || KeysEventInputController.Instance.GetActionOculusController(false)
#elif ENABLE_WORLDSENSE
                || KeysEventInputController.Instance.GetActionDaydreamController(false)
#elif ENABLE_HTCVIVE
                || KeysEventInputController.Instance.GetActionHTCViveController(false)
#elif ENABLE_PICONEO
                || KeysEventInputController.Instance.GetActionPicoNeoController(false)
#else
                || KeysEventInputController.Instance.GetActionDefaultController(false)
                || m_shotTriggerDetected
#endif
                )
            {
                m_shotTriggerDetected = false;
                ActionShootPlayer();

                m_timeoutPressed = 0;
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                m_timeoutToMove = 0;
#endif

#if ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
                if (KeysEventInputController.Instance.EnableActionOnMouseDown)
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_UP);
                }
                else
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                }
#else
                UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_UP);
#endif
            }
        }

        // -------------------------------------------
        /* 
        * ProcessInputCustomer
        */
        protected virtual void ProcessInputCustomer()
        {
            if (IsThereBlockingScreen())
            {
                UpdateLogicTeleportInventory(false);
                return;
            }

            ProcessActionInputCustomer();

#if ENABLE_OCULUS
            ProcessOculusCustomerInput();
#elif ENABLE_HTCVIVE
            ProcessHTCViveCustomerInput();
#elif ENABLE_PICONEO
            ProcessPicoNeoCustomerInput();
#else
            if (false
#if ENABLE_WORLDSENSE
                || (KeysEventInputController.Instance.GetActionDaydreamController(true))
#else
                || KeysEventInputController.Instance.GetActionDefaultController(true)
                || m_actionTriggerDetected
#endif
                 )
            {
                m_actionTriggerDetected = false;
                m_timeoutPressed = 0;
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                m_timeoutToMove = 0;
#endif
                if (m_enabledCameraInput)
                {
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                    SetAMarkerSignal();
                }
            }
#endif

            UpdateLogicTeleportInventory();

            // SHOTGUN
            UpdateTransformShotgun();
        }

        // -------------------------------------------
        /* 
         * SendDataShotgun
         */
        protected virtual void SendDataShotgun()
        {
            /*
            if (Avatar != null)
            {
                if (Avatar.GetComponent<GamePlayer>() != null)
                {
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_CAMERACONTROLLER_DATA_SHOTGUN, Avatar.GetComponent<GamePlayer>().NetworkID.NetID.ToString(), Avatar.GetComponent<GamePlayer>().NetworkID.UID.ToString(), Utilities.Vector3ToString(m_originLaser), Utilities.Vector3ToString(m_forwardLaser));
                }
            }
            */
        }

        // -------------------------------------------
        /* 
         * InitialPositionCameraDirector
         */
        protected virtual void InitialPositionCameraDirector()
        {
            if (!m_enableFreeMovementCamera)
            {
                if (!EnableARCore)
                {
                    this.gameObject.transform.position = m_fixedCameraPosition;
                    CameraLocal.forward = m_fixedCameraForward;
                }
            }
        }

        // -------------------------------------------
        /* 
         * ProcessInputDirector
         */
        protected virtual void ProcessInputDirector()
        {
            Vector3 normalForward = CameraLocal.forward.normalized * PLAYER_DIRECTOR_SPEED * 4 * Time.deltaTime;

            transform.GetComponent<Rigidbody>().useGravity = false;
            transform.GetComponent<Rigidbody>().isKinematic = true;

            InitialPositionCameraDirector();

            bool twoFingersInScreen = false;
            bool shouldConsiderMovement = true;
            if (m_enableFreeMovementCamera)
            {
#if !UNITY_EDITOR && !UNITY_WEBGL && !UNITY_STANDALONE
                // PINCH ZOOM
                if (Input.touchCount == 2)
                {
                    twoFingersInScreen = true;
                    Touch touchZero = Input.GetTouch(0);
                    Touch touchOne = Input.GetTouch(1);
                    Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
                    Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;
                    float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
                    float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

                    float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

                    // If the camera is orthographic...
                    if (!EnableARCore)
                    {
                        if (deltaMagnitudeDiff > 0)
                        {
                            transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward);
                        }
                        else
                        {
                            if (deltaMagnitudeDiff < 0)
                            {
                                transform.GetComponent<Rigidbody>().MovePosition(transform.position - normalForward);
                            }
                        }
                    }
                }
#else

                if (Input.GetAxis("Mouse ScrollWheel") > 0)
                {
                    transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward * 30);
                    shouldConsiderMovement = false;
                }
                else
                {
                    if (Input.GetAxis("Mouse ScrollWheel") < 0)
                    {
                        transform.GetComponent<Rigidbody>().MovePosition(transform.position - normalForward * 30);
                        shouldConsiderMovement = false;
                    }
                }
#endif
                // USE ARROW KEYS TO MOVE
                if (!twoFingersInScreen)
                {
                    if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.LeftControl) || Input.GetMouseButton(0))
                    {
                        m_timeoutPressed += Time.deltaTime;
                        if (!EnableARCore)
                        {
                            if (m_timeoutPressed > TIMEOUT_TO_MOVE_DIRECTOR)
                            {
                                transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward);
                            }
                        }
                    }
                }

#if UNITY_WEBGL || UNITY_STANDALONE
                if (shouldConsiderMovement) MoveCamera();
#endif
            }

            if (!twoFingersInScreen)
            {
                if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(0))
                {
                    m_timeoutPressed = 0;
                    SetDirectorMarkerSignal(Input.mousePosition.x, Input.mousePosition.y);
                    UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                }
                if (Input.GetButtonUp("Fire1") || Input.GetKeyUp(KeyCode.LeftControl) || Input.GetMouseButtonUp(0))
                {
                    if (m_timeoutPressed < TIMEOUT_TO_MOVE)
                    {
                        m_timeoutPressed = 0;

                        ActionDownForDirector();
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		 * ActionDownForDirector
		 */
        protected virtual void ActionDownForDirector()
        {
            /*
			if (!SpectatorMode)
			{
				// RAYCAST TO FLOOR TO CREATE A NEW ENEMY
				RaycastHit raycastHit = new RaycastHit();
				if (Utilities.GetCollidedInfoByRay(YourVRUIScreenController.Instance.GameCamera.transform.position,
													YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized,
													ref raycastHit))
				{
					if (raycastHit.collider.gameObject.tag == "FLOOR")
					{
						Vector3 positionNewEnemy = Utilities.Clone(raycastHit.point);
						positionNewEnemy.y += 5;
						string x = positionNewEnemy.x.ToString();
						string y = positionNewEnemy.y.ToString();
						string z = positionNewEnemy.z.ToString();
						NetworkEventController.Instance.DispatchNetworkEvent(GameController.EVENT_GAMECONTROLLER_CREATE_NEW_ENEMY, x, y, z);
					}
				}
			}
            */
        }

        // -------------------------------------------
        /* 
         * CreateNewElementInRaycast
         */
        public void CreateNewElementInRaycastLayer(string _eventElement, float _height, string _layer)
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif

            // RAYCAST TO FLOOR TO CREATE A NEW ENEMY
            RaycastHit raycastHit = new RaycastHit();
            if (Utilities.GetRaycastHitInfoByRayWithMask(pos, fwd, ref raycastHit, _layer))
            {
                Vector3 positionNewElement = Utilities.Clone(raycastHit.point);
                positionNewElement.y += _height;
                string x = positionNewElement.x.ToString();
                string y = positionNewElement.y.ToString();
                string z = positionNewElement.z.ToString();
                NetworkEventController.Instance.DispatchNetworkEvent(_eventElement, x, y, z);
            }
        }

        // -------------------------------------------
        /* 
		 * OnGUI
		 */
        public virtual void OnGUI()
        {
            /*
			if (m_avatar != null)
			{
				GUI.Label(new Rect(Screen.width - 100, 0, 100, 20), "LIFE [" + m_avatar.GetComponent<GamePlayer>().Life + "]");
			}
            */
        }

        // -------------------------------------------
        /* 
		 * OnCollisionEnter
		 */
        public virtual void OnCollisionEnter(Collision collision)
        {
            /*
			GameEnemy enemy = collision.gameObject.GetComponent<GameEnemy>();
			if (enemy != null)
			{
				NetworkEventController.Instance.DispatchNetworkEvent(GameEnemy.EVENT_GAMEENEMY_DESTROY, enemy.NetworkID.GetID());
				NetworkEventController.Instance.DispatchNetworkEvent(GameShoot.EVENT_GAMESHOOT_DAMAGE, m_avatar.GetComponent<GamePlayer>().NetworkID.GetID(), GameEnemy.DAMAGE.ToString());
			}
            */
        }

        // -------------------------------------------
        /* 
		 * OnTriggerEnter
		 */
        public virtual void OnTriggerEnter(Collider collision)
        {
            /*
            GameEnemy enemy = collision.gameObject.GetComponent<GameEnemy>();
            if (enemy != null)
            {
                NetworkEventController.Instance.DispatchNetworkEvent(GameEnemy.EVENT_GAMEENEMY_DESTROY, enemy.NetworkID.GetID());
                NetworkEventController.Instance.DispatchNetworkEvent(GameShoot.EVENT_GAMESHOOT_DAMAGE, m_avatar.GetComponent<GamePlayer>().NetworkID.GetID(), GameEnemy.DAMAGE.ToString());
            }
            */
        }


        // -------------------------------------------
        /* 
		 * LogicTouchMode
		 */
        protected virtual void LogicTouchMode()
        {

        }

        // -------------------------------------------
        /* 
		 * We rotate with the gyroscope
		 */
        protected void GyroModifyCamera()
        {
            if (m_enableGyroscope)
            {
                if (m_isTouchMode)
                {
                    LogicTouchMode();
                }
                else
                {
                    if (!m_rotatedTo90)
                    {
                        m_rotatedTo90 = true;
                        if (DirectorMode || (SubContainerCamera == null))
                        {
                            transform.Rotate(Vector3.right, 90);
                        }
                        else
                        {
                            if (SubContainerCamera != null) SubContainerCamera.transform.Rotate(Vector3.right, 90);
                        }
                    }

                    Quaternion rotFix = new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, -Input.gyro.attitude.z, -Input.gyro.attitude.w);
                    CameraLocal.localRotation = rotFix;
                }
            }
        }

        // -------------------------------------------
        /* 
		 * SetUpCloudAnchorPosition
		 */
        protected virtual void SetUpCloudAnchorPosition(Vector3 _position)
        {
            transform.position = m_shiftCameraFromOrigin + new Vector3(_position.x, transform.position.y, _position.z);
            CameraLocal.transform.parent.localPosition = -new Vector3(CameraLocal.transform.localPosition.x, CAMERA_SHIFT_HEIGHT_WORLDSENSE - _position.y, CameraLocal.transform.localPosition.z);
        }

        // -------------------------------------------
        /* 
		 * RequestSelectorData
		 */
        protected virtual void RequestSelectorData(object[] _list)
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                pos = Utilities.Clone(m_originLaser);
                fwd = Utilities.Clone(m_forwardLaser);
            }
#endif
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA, _list[0], pos, fwd);
        }

        // -------------------------------------------
        /* 
		 * ActionAfterLevelLoad
		 */
        protected virtual void ActionAfterLevelLoad()
        {
            if (this.gameObject.GetComponent<Rigidbody>() != null)
            {
                this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            }
            if (this.gameObject.GetComponent<Collider>() != null)
            {
                this.gameObject.GetComponent<Collider>().isTrigger = false;
            }
        }

        // -------------------------------------------
        /* 
		 * OnBasicSystemEvent
		 */
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
#if ENABLE_GOOGLE_ARCORE
            if (_nameEvent == CloudGameAnchorController.EVENT_CLOUDGAMEANCHOR_SETUP_ANCHOR)
            {
                if ((bool)_list[0])
                {
                    CameraLocal.GetComponent<Camera>().enabled = true;
                }
                else
                {
                    Debug.LogError("FAILED TO SET UP CLOUD ANCHOR");
                }
            }
            if (_nameEvent == CloudGameAnchorController.EVENT_CLOUDGAMEANCHOR_UPDATE_CAMERA)
            {
                bool ignoreUpdateARCoreInCamera = false;
                if ((DirectorMode) && (m_playerCameraActivated != null))
                {
                    ignoreUpdateARCoreInCamera = true;
                }
                if (!ignoreUpdateARCoreInCamera)
                {
                    Vector3 forward = (Vector3)_list[0];
                    Vector3 position = (Vector3)_list[1];
                    CameraLocal.forward = forward;
                    CloudGameAnchorController.Instance.ScaleVRWorldXZ = ScaleMovementXZ;
                    CloudGameAnchorController.Instance.ScaleVRWorldY = ScaleMovementY;
                    SetUpCloudAnchorPosition(position);
                }
            }
#endif
            if (_nameEvent == EVENT_CAMERACONTROLLER_ACTIVATE_SKYBOX)
            {
#if !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO
                CameraLocal.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
#else
                CenterEyeAnchor.GetComponent<Camera>().clearFlags = CameraClearFlags.Skybox;
#endif
            }
            if (_nameEvent == ActorTimeline.EVENT_GAMEPLAYER_SETUP_AVATAR)
            {
                if (!DirectorMode)
                {
                    Avatar = (GameObject)_list[0];
                }
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA)
            {
                RequestSelectorData(_list);
            }
            if (DirectorMode)
            {
                if ((_nameEvent == EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER) ||
                    (_nameEvent == EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER))
                {
                    if (m_playerCameraActivated == null)
                    {
                        if (YourVRUIScreenController.Instance != null)
                        {
                            m_backupCameraPosition = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
                            m_backupCameraForward = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward);
                        }
                        else
                        {
                            m_backupCameraPosition = Utilities.Clone(transform.position);
                            m_backupCameraForward = Utilities.Clone(transform.forward);
                        }
                    }
                    else
                    {
                        m_playerCameraActivated.GetComponent<Actor>().GetModel().gameObject.SetActive(true);
                    }
                    m_playerCameraActivated = Players[(int)_list[0]];
                    if (m_playerCameraActivated != null)
                    {
                        if (m_playerCameraActivated.GetComponent<Actor>() != null)
                        {
                            if (m_playerCameraActivated.GetComponent<Actor>().GetModel() != null)
                            {
                                m_playerCameraActivated.GetComponent<Actor>().GetModel().gameObject.SetActive(false);
                            }
                        }
                    }
                }
                if ((_nameEvent == EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR) ||
                    (_nameEvent == EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR))
                {
                    if (m_playerCameraActivated != null)
                    {
                        m_playerCameraActivated.GetComponent<Actor>().GetModel().gameObject.SetActive(true);
                    }
                    m_playerCameraActivated = null;
                    transform.position = m_backupCameraPosition;
                    CameraLocal.forward = m_backupCameraForward;
                    if (YourVRUIScreenController.Instance != null)
                    {
                        YourVRUIScreenController.Instance.GameCamera.transform.position = m_backupCameraPosition;
                        YourVRUIScreenController.Instance.GameCamera.transform.forward = m_backupCameraForward;
                    }
                    else
                    {
                        CameraLocal.position = m_backupCameraPosition;
                    }
                }
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED)
            {
                ActionAfterLevelLoad();
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION)
            {
                m_enabledCameraInput = (bool)_list[0];
            }
            if (_nameEvent == GrabNetworkObject.EVENT_GRABOBJECT_REQUEST_RAYCASTING)
            {
                if (!DirectorMode)
                {
                    GameObject collidedObjectCasting = CheckRaycastAgainst();
                    GameObject targetToReportIfFound = (GameObject)_list[0];

                    if ((collidedObjectCasting != null) && (collidedObjectCasting == targetToReportIfFound))
                    {
                        GameObject targetToFollow = YourVRUIScreenController.Instance.GameCamera.gameObject;

#if ENABLE_OCULUS || ENABLE_WORLDSENSE || ENABLE_HTCVIVE || ENABLE_PICONEO
                        if ((m_armModel != null) && (m_laserPointer != null))
                        {
                           targetToFollow = m_laserPointer;
                        }
#endif
                        BasicSystemEventController.Instance.DispatchBasicSystemEvent(GrabNetworkObject.EVENT_GRABOBJECT_RESPONSE_RAYCASTING, collidedObjectCasting, targetToFollow);
                    }
                }
            }
            if (_nameEvent == TeleportController.EVENT_TELEPORTCONTROLLER_TELEPORT)
            {
                NetworkEventController.Instance.PriorityDelayNetworkEvent(TeleportController.EVENT_TELEPORTCONTROLLER_TELEPORT, 0.1f, (string)_list[0]);
            }
            if (_nameEvent == KeysEventInputController.EVENT_REQUEST_TELEPORT_AVAILABLE)
            {
                m_teleportAvailable = (GameObject.FindObjectOfType<TeleportController>() != null);
                if (m_teleportAvailable)
                {
                    TeleportController.Instance.ForwardDirection = (Transform)_list[0];
                }
                // UIEventController.Instance.DelayUIEvent(ScreenDebugLogView.EVENT_SCREEN_DEBUGLOG_NEW_TEXT, 0.1f, false, "IS TELEPORT FOUND["+ m_teleportAvailable + "]["+ m_teleportEnabled + "]::::::::::");
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_FIX_DIRECTOR_CAMERA)
            {
                if (!(bool)_list[0])
                {
                    m_fixedCameraPosition = this.gameObject.transform.position;
                    m_fixedCameraForward = CameraLocal.forward;
                }
                m_enableFreeMovementCamera = (bool)_list[0];
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_APPLY_ROTATION_CAMERA)
            {
                if ((bool)_list[0])
                {
                    this.transform.Rotate(new Vector3(0, ROTATE_LOCALCAMERA_VALUE, 0));
                }
                else
                {
                    this.transform.Rotate(new Vector3(0, -ROTATE_LOCALCAMERA_VALUE, 0));
                }
            }

#if ENABLE_MULTIPLAYER_TIMELINE
            if (_nameEvent == GameLevelData.EVENT_GAMELEVELDATA_REQUEST_COLLISION_RAY)
            {
                if (_list.Length > 1)
                {
                    RaycastHit raycastHit = new RaycastHit();
                    GameObject originRay = (GameObject)_list[1];
                    Vector3 forwardView = Utilities.Clone(originRay.transform.forward.normalized);
                    if (Utilities.GetCollidedInfoByRay(originRay.transform.position, forwardView, ref raycastHit))
                    {
                        BasicSystemEventController.Instance.DispatchBasicSystemEvent(GameLevelData.EVENT_GAMELEVELDATA_RESPONSE_COLLISION_RAY, _list[0], raycastHit.collider.gameObject, raycastHit);
                    }
                }
                else
                {
                    RaycastHit raycastHit = new RaycastHit();
                    if (Utilities.GetCollidedInfoByRay(transform.position, CameraLocal.forward.normalized, ref raycastHit))
                    {
                        BasicSystemEventController.Instance.DispatchBasicSystemEvent(GameLevelData.EVENT_GAMELEVELDATA_RESPONSE_COLLISION_RAY, _list[0], raycastHit.collider.gameObject, raycastHit);
                    }
                }
            }
            if (_nameEvent == GameLevelData.EVENT_GAMELEVELDATA_RESPONSE_COLLISION_RAY)
            {
                if (_list[0] is GameObject)
                {
                    if (this.gameObject == (GameObject)_list[0])
                    {
                        RaycastHit infoHit = (RaycastHit)_list[2];
                        float distanceHit = Vector3.Distance(this.gameObject.transform.position, infoHit.point);
                        if ((EnableARCore) && (distanceHit <= 1.2f))
                        {
                            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenItemInventoryView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
                        }
                    }
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
		 * OnNetworkEvent
		 */
        protected virtual void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, object[] _list)
        {
            if (_nameEvent == CloudGameAnchorController.EVENT_6DOF_UPDATE_SCALE_MOVEMENT_XZ)
            {
                ScaleMovementXZ = float.Parse((string)_list[0]);
                // Debug.LogError("NEW GAME CAMERA SCALE[" + ScaleMovementXZ + "]");
            }
            if (_nameEvent == CloudGameAnchorController.EVENT_6DOF_REQUEST_SCALE_MOVEMENT_XZ)
            {
                NetworkEventController.Instance.DispatchLocalEvent(CloudGameAnchorController.EVENT_6DOF_UPDATE_SCALE_MOVEMENT_XZ, ScaleMovementXZ.ToString());
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD)
            {
                this.gameObject.GetComponent<Rigidbody>().isKinematic = true;
                this.gameObject.GetComponent<Rigidbody>().useGravity = false;
                this.gameObject.GetComponent<Collider>().isTrigger = true;
            }
            if (_nameEvent == ScreenBaseDirectorView.EVENT_DIRECTOR_TELEPORT_ENABLE)
            {
                if (!DirectorMode)
                {
                    m_teleportEnabled = bool.Parse((string)_list[0]);
                    m_timeoutToTeleport = 0;
                }
            }
            if (_nameEvent == TeleportController.EVENT_TELEPORTCONTROLLER_TELEPORT)
            {
                ApplyTeleport(_networkOriginID, (string)_list[0]);
            }
        }

        // -------------------------------------------
        /* 
		 * ApplyTeleport
		 */
        protected virtual void ApplyTeleport(int _networkOriginID, string _shift)
        {
            bool applyTeleport = true;
#if TELEPORT_INDIVIDUAL || ONLY_REMOTE_CONNECTION
            if (YourNetworkTools.Instance.GetUniversalNetworkID() != _networkOriginID)
            {
                applyTeleport = false;
            }
            else
            {
                transform.position = new Vector3(0, transform.position.y, 0);
            }
#endif
            if (applyTeleport)
            {
                Vector3 shiftTeleport = Utilities.StringToVector3(_shift);
#if ENABLE_WORLDSENSE
                Vector3 posWorld = Utilities.Clone(CameraLocal.transform.localPosition);
#else
                Vector3 posWorld = Utilities.Clone(CenterEyeAnchor.transform.localPosition);
#endif
                m_shiftCameraFromOrigin += new Vector3(shiftTeleport.x, 0, shiftTeleport.z) + new Vector3(posWorld.x * ScaleMovementXZ, 0, posWorld.z * ScaleMovementXZ);
#if ENABLE_WORLDSENSE
                CameraLocal.transform.localPosition = new Vector3(0, CameraLocal.transform.localPosition.y, 0);
#else
                CenterEyeAnchor.transform.localPosition = new Vector3(0, CenterEyeAnchor.transform.localPosition.y, 0);
#endif
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(TeleportController.EVENT_TELEPORTCONTROLLER_COMPLETED);
                // Debug.LogError("CameraBaseController::EVENT_TELEPORTCONTROLLER_TELEPORT::m_shiftCameraFromOrigin=" + m_shiftCameraFromOrigin.ToString());
            }
        }

        // -------------------------------------------
        /* 
		 * EnableLaserLogic
		 */
        protected virtual void EnableLaserLogic(string _nameEvent, object[] _list)
        {
            if (_nameEvent == ScreenInformationView.EVENT_SCREEN_INFORMATION_DISPLAYED)
            {
                m_enableShootAction = false;
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_CAMERACONTROLLER_ENABLE_LASER_POINTER, true);
            }
            if (_nameEvent == ScreenInformationView.EVENT_SCREEN_INFORMATION_CLOSED)
            {
                m_enableShootAction = true;
                m_ignoreNextShootAction = true;
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_CAMERACONTROLLER_ENABLE_LASER_POINTER, false);
            }
        }

        // -------------------------------------------
        /* 
		 * OnUIEvent
		 */
        protected virtual void OnUIEvent(string _nameEvent, object[] _list)
        {
            EnableLaserLogic(_nameEvent, _list);

            if (_nameEvent == EVENT_CAMERACONTROLLER_OPEN_INVENTORY)
            {
#if ENABLE_MULTIPLAYER_TIMELINE
                if (GameObject.FindObjectOfType<ScreenInventoryView>() == null)
                {
                    UIEventController.Instance.DispatchUIEvent(GameLevelData.EVENT_GAMELEVELDATA_OPEN_INVENTORY);
                }
#else
                m_timeoutPressed = TIMEOUT_TO_INVENTORY + 1;
                OpenInventory(false);
#endif

            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_START_MOVING)
            {
                m_timeoutToMove = TIMEOUT_TO_MOVE + 1;
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_GENERIC_ACTION_DOWN)
            {
                m_actionTriggerDetected = true;
                m_shotTriggerDetected = true;
                UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_GENERIC_ACTION_UP)
            {
                SetAMarkerSignal();
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_STOP_MOVING)
            {
                m_timeoutToMove = 0;
            }
            if (_nameEvent == ScreenBasePlayerView.EVENT_SCREENPLAYER_OPEN_INVENTORY)
            {
#if ENABLE_MULTIPLAYER_TIMELINE
                m_timeoutPressed = 0;
                if (GameObject.FindObjectOfType<ScreenInventoryView>() == null)
                {
                    UIEventController.Instance.DispatchUIEvent(GameLevelData.EVENT_GAMELEVELDATA_OPEN_INVENTORY);
                }
#endif
            }

            if (_nameEvent == YourVRUIScreenController.EVENT_SCREENMANAGER_ASSIGNED_LASER)
            {
#if (ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO) && DISABLE_ONLY_ONE_HAND
                m_armModel = null;
                m_laserPointer = null;
#endif
            }
        }

        protected GameObject m_playerCameraActivated = null;
        protected Vector3 m_backupCameraPosition = new Vector3();
        protected Vector3 m_backupCameraForward = new Vector3();

        // -------------------------------------------
        /* 
         * IsCameraEnabled
         */
        public bool IsCameraEnabled()
        {
            return CameraLocal.GetComponent<Camera>().enabled;
        }

        // -------------------------------------------
        /* 
         * SetCameraPlayerToDirector
         */
        protected virtual bool SetCameraPlayerToDirector()
        {
            if (m_playerCameraActivated != null)
            {
                if (YourVRUIScreenController.Instance != null)
                {
                    YourVRUIScreenController.Instance.GameCamera.transform.position = m_playerCameraActivated.GetComponent<ActorNetwork>().GetCameraPosition() - (Vector3.up * CAMERA_SHIFT_HEIGHT_WORLDSENSE);
                    YourVRUIScreenController.Instance.GameCamera.transform.forward = m_playerCameraActivated.GetComponent<ActorNetwork>().GetCameraForward();
                }
                else
                {
                    CameraLocal.transform.position = m_playerCameraActivated.GetComponent<ActorNetwork>().GetCameraPosition() - (Vector3.up * CAMERA_SHIFT_HEIGHT_WORLDSENSE);
                    CameraLocal.transform.forward = m_playerCameraActivated.GetComponent<ActorNetwork>().GetCameraForward();
                }
                return true;
            }
            return false;
        }

        // -------------------------------------------
        /* 
         * Daydream logic
         */
        protected virtual void LogicDaydream6DOF()
        {
#if ENABLE_WORLDSENSE
            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            Vector3 posWorld = Utilities.Clone(CameraLocal.transform.localPosition);
            Vector3 centerLevel = new Vector3(0, transform.position.y, 0);
            Vector3 posRotatedWorld = Utilities.RotatePoint(new Vector2(posWorld.x * ScaleMovementXZ, posWorld.z * ScaleMovementXZ), Vector2.zero, -m_currentLocalCamRotation);
            transform.position = centerLevel 
                                    + new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y)
                                    + m_shiftCameraFromOrigin;
            Vector3 shiftToRecenter = -new Vector3(CameraLocal.transform.localPosition.x, 0.3f + CAMERA_SHIFT_HEIGHT_WORLDSENSE - (posWorld.y * ScaleMovementY), CameraLocal.transform.localPosition.z);
            CameraLocal.transform.parent.localPosition = shiftToRecenter;
#else
            if (!DirectorMode)
            {
                this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                this.gameObject.GetComponent<Rigidbody>().useGravity = true;
                this.gameObject.GetComponent<Collider>().isTrigger = false;
            }
#endif
        }

        // -------------------------------------------
        /* 
         * UpdateLogicOculus
         */
        protected virtual bool UpdateLogicOculus()
        {
#if ENABLE_OCULUS
            OVRInput.Update();

            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            if (m_avatar != null)
            {
                // m_avatar.transform.position = new Vector3(transform.position.x, -CAMERA_SHIFT_HEIGHT_WORLDSENSE + transform.position.y, transform.position.z);
                m_avatar.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                m_avatar.transform.forward = new Vector3(CenterEyeAnchor.transform.forward.x, CenterEyeAnchor.transform.forward.y, CenterEyeAnchor.transform.forward.z);
            }

            Vector3 posWorld = Utilities.Clone(CenterEyeAnchor.transform.localPosition);
            Vector3 centerLevel = new Vector3(0, transform.position.y, 0);
            m_shiftCameraFromOrigin += m_incrementJoystickTranslation;
            m_incrementJoystickTranslation = Vector3.zero;
            Vector3 posRotatedWorld = Utilities.RotatePoint(new Vector2(posWorld.x * ScaleMovementXZ, posWorld.z * ScaleMovementXZ), Vector2.zero, -m_currentLocalCamRotation);
            transform.position = centerLevel
                                    + new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y)
                                    + m_shiftCameraFromOrigin;
            Vector3 shiftToRecenter = -new Vector3(CenterEyeAnchor.transform.localPosition.x, CAMERA_SHIFT_HEIGHT_WORLDSENSE - (posWorld.y * ScaleMovementY), CenterEyeAnchor.transform.localPosition.z);
            CenterEyeAnchor.transform.parent.localPosition = shiftToRecenter;

            return true;
#else
            return false;
#endif
        }

        // -------------------------------------------
        /* 
         * UpdateLogicHTC
         */
        protected virtual bool UpdateLogicHTC()
        {
#if ENABLE_HTCVIVE
            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            if (m_avatar != null)
            {
                // m_avatar.transform.position = new Vector3(transform.position.x, -CAMERA_SHIFT_HEIGHT_WORLDSENSE + transform.position.y, transform.position.z);
                m_avatar.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                m_avatar.transform.forward = new Vector3(CenterEyeAnchor.transform.forward.x, CenterEyeAnchor.transform.forward.y, CenterEyeAnchor.transform.forward.z);
            }

            Vector3 posWorld = Utilities.Clone(CenterEyeAnchor.transform.localPosition);
            Vector3 centerLevel = new Vector3(0, transform.position.y, 0);
            m_shiftCameraFromOrigin += m_incrementJoystickTranslation;
            m_incrementJoystickTranslation = Vector3.zero;
            Vector3 posRotatedWorld = Utilities.RotatePoint(new Vector2(posWorld.x * ScaleMovementXZ, posWorld.z * ScaleMovementXZ), Vector2.zero, -m_currentLocalCamRotation);
            transform.position = centerLevel 
                                    + new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y)
                                    + m_shiftCameraFromOrigin;
            Vector3 shiftToRecenter = -new Vector3(CenterEyeAnchor.transform.localPosition.x, 0.25f + CAMERA_SHIFT_HEIGHT_WORLDSENSE - (posWorld.y * ScaleMovementY), CenterEyeAnchor.transform.localPosition.z);
            CenterEyeAnchor.transform.parent.localPosition = shiftToRecenter;

            return true;
#else
            return false;
#endif
        }


        // -------------------------------------------
        /* 
         * UpdateLogicPicoNeo
         */
        protected virtual bool UpdateLogicPicoNeo()
        {
#if ENABLE_PICONEO
            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            Vector3 positionHead = Pvr_UnitySDKSensor.Instance.HeadPose.Position;

            if (m_avatar != null)
            {
                // m_avatar.transform.position = new Vector3(transform.position.x, -CAMERA_SHIFT_HEIGHT_WORLDSENSE + transform.position.y, transform.position.z);
                m_avatar.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                m_avatar.transform.rotation = Pvr_UnitySDKSensor.Instance.HeadPose.Orientation;
            }

            Vector3 posWorld = Utilities.Clone(positionHead);
            Vector3 centerLevel = new Vector3(0, transform.position.y, 0);
            m_shiftCameraFromOrigin += m_incrementJoystickTranslation;
            m_incrementJoystickTranslation = Vector3.zero;
            Vector3 posRotatedWorld = Utilities.RotatePoint(new Vector2(posWorld.x * ScaleMovementXZ, posWorld.z * ScaleMovementXZ), Vector2.zero, -m_currentLocalCamRotation);
            transform.position = centerLevel 
                                    + new Vector3(posRotatedWorld.x, 0, posRotatedWorld.y)
                                    + m_shiftCameraFromOrigin;
            Vector3 shiftToRecenter = -new Vector3(posWorld.x, CAMERA_SHIFT_HEIGHT_WORLDSENSE - (posWorld.y * ScaleMovementY), posWorld.z);
            CenterEyeAnchor.transform.parent.localPosition = shiftToRecenter;

            return true;
#else
            return false;
#endif
        }


        // -------------------------------------------
        /* 
         * UpdateDefaultLogic
         */
        protected virtual void UpdateDefaultLogic()
        {
            if (UpdateLogicOculus())
            {
                // return;
            }
            else
            {
                if (UpdateLogicHTC())
                {
                    // return;
                }
                else
                {
                    if (UpdateLogicPicoNeo())
                    {
                        // return;
                    }
                    else
                    {
                        if (m_avatar != null)
                        {
                            m_avatar.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                            m_avatar.transform.forward = new Vector3(CameraLocal.forward.x, CameraLocal.forward.y, CameraLocal.forward.z);
                        }
                        LogicDaydream6DOF();
                    }
                }
            }

#if ENABLE_ROTATE_LOCALCAMERA
            UpdateRotateCamera();
#endif
        }

        protected bool m_hasBeenRotated = false;
        protected bool m_touchPadHTCDetected = false;
        protected float m_currentLocalCamRotation = 0;

        // -------------------------------------------
        /* 
		 * UpdateRotateCamera
		 */
        protected virtual void UpdateRotateCamera()
        {
#if ENABLE_WORLDSENSE || ENABLE_OCULUS || ENABLE_HTCVIVE || ENABLE_PICONEO
            bool considerPressedThumbstick = false;
            Vector2 pressedVector = Vector2.zero;
            float detectionDistance = 0.6f;
#if ENABLE_HTCVIVE
            m_touchPadHTCDetected = false;
            pressedVector = KeysEventInputController.Instance.GetVectorTouchpadHTC(true);
            if (pressedVector.magnitude == 0)
            {
                pressedVector = KeysEventInputController.Instance.GetVectorThumbstick(considerPressedThumbstick);
            }
            else
            {
                m_touchPadHTCDetected = true;
            }
#else
            pressedVector = KeysEventInputController.Instance.GetVectorThumbstick(considerPressedThumbstick);
#endif
            if (!m_hasBeenRotated)
            {
                if (Mathf.Abs(pressedVector.x) > detectionDistance)
                {
                    m_hasBeenRotated = true;
                    if (m_touchPadHTCDetected)
                    {
                        m_touchPadHTCDetected = false;
                        Invoke("ResetTouchDetected", 0.3f);
                    }
                    if (pressedVector.x > 0)
                    {
                        m_currentLocalCamRotation += ROTATE_LOCALCAMERA_VALUE;
                        this.transform.Rotate(new Vector3(0, ROTATE_LOCALCAMERA_VALUE, 0));
                    }
                    else
                    {
                        m_currentLocalCamRotation -= ROTATE_LOCALCAMERA_VALUE;
                        this.transform.Rotate(new Vector3(0, -ROTATE_LOCALCAMERA_VALUE, 0));
                    }                    
                }
            }
            else
            {
                if (Mathf.Abs(pressedVector.x) < 0.2f)
                {
                    m_hasBeenRotated = false;
                }
            }
#endif
        }

        private void ResetTouchDetected()
        {
            m_hasBeenRotated = false;
        }

        // -------------------------------------------
        /* 
		 * IsGameFakeRunning
		 */
        protected virtual bool IsGameFakeRunning()
        {
            return false;
        }

        // -------------------------------------------
        /* 
		 * IsGameLoading
		 */
        protected virtual bool IsGameLoading()
        {
            return false;
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        void LateUpdate()
        {
#if ENABLE_WORLDSENSE
            if (ShotgunContainer != null)
            {
                ShotgunContainer.transform.rotation = Quaternion.identity;
            }
#endif
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        public virtual void Update()
        {
            if (IsGameFakeRunning()
#if ENABLE_WORLDSENSE || ENABLE_QUEST || ENABLE_GOOGLE_ARCORE || ENABLE_HTCVIVE || ENABLE_PICONEO
                || IsGameLoading()
#endif
                )
            {
                if (YourNetworkTools.Instance.GetUniversalNetworkID() != -1)
                {
                    if (SetCameraPlayerToDirector())
                    {
                        return;
                    }

#if (UNITY_EDITOR && !ENABLE_OCULUS && !ENABLE_HTCVIVE && !ENABLE_PICONEO) || UNITY_WEBGL || UNITY_STANDALONE
                    MoveCamera();
                    RotateCamera();
#else
				     GyroModifyCamera();
#endif

                    if (DirectorMode)
                    {
                        ProcessInputDirector();
                    }
                    else
                    {
                        if (!IsGameLoading())
                        {
                            ProcessInputCustomer();
                        }
                    }

                    UpdateDefaultLogic();
                }
            }
        }
    }
}