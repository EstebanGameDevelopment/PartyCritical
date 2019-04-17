#if ENABLE_GOOGLE_ARCORE
using GoogleARCore;
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
        public const string EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA    = "EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA";
        public const string EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA   = "EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA";
        public const string EVENT_CAMERACONTROLLER_DATA_SHOTGUN             = "EVENT_CAMERACONTROLLER_DATA_SHOTGUN";
        public const string EVENT_GAMECAMERA_REAL_PLAYER_FORWARD            = "EVENT_GAMECAMERA_REAL_PLAYER_FORWARD";
        
        public const string MARKER_NAME = "MARKER";

        // ----------------------------------------------
        // PUBLIC VARIABLES
        // ----------------------------------------------	
        public Transform CameraLocal;
        public GameObject OVRPlayer;
        public GameObject CenterEyeAnchor;
        public GameObject ShotgunContainer;

        public float ScaleMovementXZ = 4;
        public float ScaleMovementY = 2;

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
        protected bool m_enableVR = false;
        protected bool m_rotatedTo90 = false;
        protected float m_timeoutPressed = 0;
        protected float m_timeoutToMove = 0;

        protected bool m_twoFingersHasBeenPressedOnce = false;

        protected bool m_activateMovement = false;

        protected bool m_enableShootAction = true;
        protected bool m_ignoreNextShootAction = false;

#if ENABLE_WORLDSENSE || ENABLE_OCULUS
        protected GameObject m_armModel;
        protected GameObject m_laserPointer;
        protected float m_timeShotgun = 0;
        protected Vector3 m_originLaser;
        protected Vector3 m_forwardLaser;
#endif


#if ENABLE_OCULUS
        protected Vector3 m_forwardOculus;
        protected Vector3 m_positionOculus;
        protected bool m_initializedOculus = false;
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
                this.transform.position = m_avatar.transform.position;
            }
        }

        public bool EnableVR
        {
            get { return m_enableVR; }
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
            get {
#if ENABLE_OCULUS
                return 0;
#else
                return -1.5f;
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
        public virtual float TIMEOUT_TO_MOVE
        {
            get { return -1; }
        }
        public virtual float TIMEOUT_TO_INVENTORY
        {
            get { return -1; }
        }
        public virtual float TIME_UPDATE_SHOTGUN
        {
            get { return -1; }
        }

        // -------------------------------------------
        /* 
		 * InitialitzationLaserPointer
		 */
        protected virtual void InitialitzationLaserPointer()
        {
            if (YourVRUIScreenController.Instance.LaserPointer != null) YourVRUIScreenController.Instance.LaserPointer.SetActive(false);
        }

        // -------------------------------------------
        /* 
		 * Initialize
		 */
        public virtual void Initialize()
        {
#if ENABLE_OCULUS
            m_enableVR = true;
            CameraLocal.gameObject.SetActive(false);
            if (OVRPlayer!=null) OVRPlayer.SetActive(true);
            this.GetComponent<Rigidbody>().useGravity = false;
            this.GetComponent<Rigidbody>().isKinematic = true;
            this.GetComponent<Collider>().isTrigger = true;
#else
            CameraLocal.gameObject.SetActive(true);
            if (OVRPlayer != null) OVRPlayer.SetActive(false);
#endif

#if ENABLE_WORLDSENSE || ENABLE_OCULUS
            if (ShotgunContainer != null) ShotgunContainer.SetActive(true);
#else
            if (ShotgunContainer != null) ShotgunContainer.SetActive(false);
#endif

            InitialitzationLaserPointer();

            if (DirectorMode)
            {
                if (ShotgunContainer != null) ShotgunContainer.SetActive(false);
            }

            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
            NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);

#if !UNITY_EDITOR && !ENABLE_OCULUS
		if (!CardboardLoaderVR.LoadEnableCardboard())
		{
			m_enableGyroscope = true;
		}
		else
		{
			m_enableVR = true;
		}
#endif

#if !ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
            if (this.gameObject.GetComponentInChildren<Skybox>()!=null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif

            if (EnableARCore)
            {
                m_enableGyroscope = false;
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                if (EnableBackgroundVR)
                {
                    if (this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>() != null)
                    {
                        this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>().enabled = false;
                        this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
                    }
                }
                else
                {
                    this.gameObject.GetComponentInChildren<Skybox>().enabled = false;
                }
#else
                if (this.gameObject.GetComponentInChildren<Skybox>()!=null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif
            }
            else
            {
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
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
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
                if (this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>() != null) this.gameObject.GetComponentInChildren<ARCoreBackgroundRenderer>().enabled = false;
#else
                if (this.gameObject.GetComponentInChildren<Skybox>() != null) this.gameObject.GetComponentInChildren<Skybox>().enabled = true;
#endif
            }

#if !ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
            CameraLocal.GetComponent<Camera>().enabled = true;
#endif
        }

        // -------------------------------------------
        /* 
		 * OnDestroy
		 */
        void OnDestroy()
        {
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
            UIEventController.Instance.UIEvent -= OnUIEvent;
        }

        // -------------------------------------------
        /* 
		 * RotateCamera
		 */
        protected void RotateCamera()
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

        // -------------------------------------------
        /* 
         * Daydream logic
         */
        protected virtual void LogicDaydream6DOF()
        {
#if ENABLE_WORLDSENSE && !UNITY_EDITOR
            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            Vector3 posWorld = Utilities.Clone(CameraLocal.transform.localPosition);
            Vector3 centerLevel = new Vector3(0, transform.position.y, 0);
            transform.position = centerLevel + new Vector3(posWorld.x * ScaleMovementXZ,
												0,
                                                posWorld.z * ScaleMovementXZ);
            CameraLocal.transform.parent.localPosition = -new Vector3(CameraLocal.transform.localPosition.x, CAMERA_SHIFT_HEIGHT_WORLDSENSE - (posWorld.y * ScaleMovementY), CameraLocal.transform.localPosition.z);
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
         * MoveCamera
         */
        protected void MoveCamera()
        {
            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

            Vector3 forward = Input.GetAxis("Vertical") * CameraLocal.forward * PLAYER_SPEED * Time.deltaTime;
            Vector3 lateral = Input.GetAxis("Horizontal") * CameraLocal.right * PLAYER_SPEED * Time.deltaTime;

            Vector3 increment = forward + lateral;
            increment.y = 0;
            transform.GetComponent<Rigidbody>().MovePosition(transform.position + increment);
        }

        // -------------------------------------------
        /* 
         * GetPositionLaser
         */
        public Vector3 GetPositionLaser()
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
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
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#endif

            return fwd;
        }


        // -------------------------------------------
        /* 
         * GetForwardPoint
         */
        public Vector3 GetForwardPoint(float _distance)
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
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
        public GameObject CheckRaycastAgainst(params string[] _layers)
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
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
         * SetAMarkerSignal
         */
        protected virtual void SetAMarkerSignal()
        {
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
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
         * ActionShootPlayer
         */
        protected virtual void ActionShootPlayer()
        {
            string position = "";
            string forward = "";
            bool shootDone = false;
#if ENABLE_OCULUS && !UNITY_EDITOR
			if ((m_armModel != null) && (m_laserPointer != null))
			{
				position = m_originLaser.x + "," + m_originLaser.y + "," + m_originLaser.z;
				forward = m_forwardLaser.x + "," + m_forwardLaser.y + "," + m_forwardLaser.z;
				shootDone = true;
			}
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
			if ((m_armModel != null) && (m_laserPointer != null))
			{
				position = m_originLaser.x + "," + m_originLaser.y + "," + m_originLaser.z;
				forward = m_forwardLaser.x + "," + m_forwardLaser.y + "," + m_forwardLaser.z;
				shootDone = true;
			}
#else
            Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
            Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
            position = pos.x + "," + pos.y + "," + pos.z;
            forward = fwd.x + "," + fwd.y + "," + fwd.z;
            shootDone = true;
#endif

            if (shootDone)
            {
                // SixDOFConfiguration.PlayFxShoot();
                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMESHOOT_NEW, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), position, forward);
            }
        }

        // -------------------------------------------
        /* 
         * OpenInventory
         */
        protected virtual void OpenInventory()
        {
#if (ENABLE_WORLDSENSE || ENABLE_OCULUS) && !UNITY_EDITOR && ENABLE_MULTIPLAYER_TIMELINE
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

        // -------------------------------------------
        /* 
        * ProcessInputCustomer
        */
        protected void ProcessInputCustomer()
        {
            // ENABLE DEFAULT INPUTS WHEN THERE ARE SCREEN ACTIVATED
            if (YourVRUIScreenController.Instance.ScreensTemporal.Count > 0)
            {
#if ENABLE_OCULUS
                KeysEventInputController.Instance.EnableActionButton = false;
#else
                KeysEventInputController.Instance.EnableActionButton = true;
                return;
#endif
            }
            else
            {
                KeysEventInputController.Instance.EnableActionButton = false;
            }

            // INPUTS FOR THE IN-GAME, NOT THE SCREENS
            if (false
#if ENABLE_OCULUS && !UNITY_EDITOR
                || (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger))
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
                || (KeysEventInputController.Instance.GetActionDaydreamController(false))
#else
                || Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.Z) || Input.GetButtonUp("Fire1")
#endif
                )
            {
                ActionShootPlayer();

                m_timeoutPressed = 0;

                // Debug.LogError("KEY UP+++++++++++++++");
                UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_UP);
            }

#if ENABLE_OCULUS && !UNITY_EDITOR
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger))
            {
                m_timeoutPressed = 0;
                UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                SetAMarkerSignal();
            }
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
#else
            if (false
#if ENABLE_WORLDSENSE && !UNITY_EDITOR
                || (KeysEventInputController.Instance.GetActionDaydreamController(true))
#else
                || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Z) || Input.GetButtonDown("Fire1")
#endif
                 )
            {
                m_timeoutPressed = 0;
                UIEventController.Instance.DispatchUIEvent(KeysEventInputController.ACTION_BUTTON_DOWN);
                SetAMarkerSignal();
            }
#endif


#if ENABLE_WORLDSENSE && !UNITY_EDITOR
            if (KeysEventInputController.Instance.GetAppButtonDowDaydreamController())
            {
                m_timeoutPressed = TIMEOUT_TO_INVENTORY;
            }
#endif

                if (false
#if ENABLE_OCULUS && !UNITY_EDITOR
                || (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger))
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
                || (KeysEventInputController.Instance.GetAppButtonDowDaydreamController())
#else
                || Input.GetMouseButton(0) || Input.GetKey(KeyCode.Z) || Input.GetButton("Fire1")
#endif
                )
            {
                m_timeoutPressed += Time.deltaTime;
                if (m_timeoutPressed > TIMEOUT_TO_INVENTORY)
                {
                    m_timeoutPressed = 0;
                    OpenInventory();
                }

#if !UNITY_EDITOR && !ENABLE_OCULUS && !ENABLE_WORLDSENSE
                if ((m_timeoutPressed >= TIMEOUT_TO_MOVE) || (m_timeoutToMove >= TIMEOUT_TO_MOVE))
				{
                    if (!EnableARCore)
					{
						Vector3 normalForward = CameraLocal.forward.normalized;
                        normalForward = new Vector3(normalForward.x, 0, normalForward.z);
                        transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward * PLAYER_SPEED * Time.deltaTime);
					}
                }
#endif
            }


#if ENABLE_WORLDSENSE || ENABLE_OCULUS
            if ((m_armModel == null) && (m_laserPointer == null))
            {
#if ENABLE_WORLDSENSE
                if (GameObject.FindObjectOfType<GvrArmModel>() != null) m_armModel = GameObject.FindObjectOfType<GvrArmModel>().gameObject;
                if (GameObject.FindObjectOfType<GvrControllerVisual>() != null) m_laserPointer = GameObject.FindObjectOfType<GvrControllerVisual>().gameObject;
#elif ENABLE_OCULUS
                m_armModel = new GameObject();
                if (GameObject.FindObjectOfType<OVRTrackedRemote>() != null) m_laserPointer = GameObject.FindObjectOfType<OVRTrackedRemote>().gameObject;
#endif
            }
            if ((m_armModel != null) && (m_laserPointer != null))
            {
                m_originLaser = m_laserPointer.transform.position;
#if ENABLE_WORLDSENSE
                m_forwardLaser = m_armModel.GetComponent<GvrArmModel>().ControllerRotationFromHead * Vector3.forward;
#elif ENABLE_OCULUS
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
            if (!m_twoFingersHasBeenPressedOnce)
            {
                if (!EnableARCore)
                {
                    this.gameObject.transform.position = new Vector3(6.1f, 7.4f, -5.1f);
                    CameraLocal.forward = new Vector3(-0.6f, -0.7f, 0.4f);
                }
                else
                {
                    m_twoFingersHasBeenPressedOnce = true;
                }
#if UNITY_EDITOR
                m_twoFingersHasBeenPressedOnce = true;
#endif
            }
        }

        // -------------------------------------------
        /* 
         * ProcessInputDirector
         */
        protected virtual void ProcessInputDirector()
        {
            Vector3 normalForward = CameraLocal.forward.normalized * PLAYER_SPEED * 4 * Time.deltaTime;

            transform.GetComponent<Rigidbody>().useGravity = false;
            transform.GetComponent<Rigidbody>().isKinematic = true;

            bool twoFingersInScreen = false;
#if !UNITY_EDITOR
            // PINCH ZOOM
            if (Input.touchCount == 2)
            {
                twoFingersInScreen = true;
                m_twoFingersHasBeenPressedOnce = true;
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
                transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward * 3);
            }
            else
            {
                if (Input.GetAxis("Mouse ScrollWheel") < 0)
                {
                    transform.GetComponent<Rigidbody>().MovePosition(transform.position - normalForward * 3);
                }
            }
#endif

            InitialPositionCameraDirector();

            // USE ARROW KEYS TO MOVE
            if (!twoFingersInScreen)
            {
                if (Input.GetButton("Fire1") || Input.GetKey(KeyCode.LeftControl) || Input.GetMouseButton(0))
                {
                    m_timeoutPressed += Time.deltaTime;
                    if (!EnableARCore)
                    {
                        if (m_timeoutPressed > TIMEOUT_TO_MOVE)
                        {
                            transform.GetComponent<Rigidbody>().MovePosition(transform.position + normalForward);
                        }
                    }
                }

                if (Input.GetButtonDown("Fire1") || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetMouseButtonDown(0))
                {
                    m_timeoutPressed = 0;
                    SetAMarkerSignal();
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
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
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
		 * We rotate with the gyroscope
		 */
        protected void GyroModifyCamera()
        {
            if (m_enableGyroscope)
            {
                if (!m_rotatedTo90)
                {
                    m_rotatedTo90 = true;
                    transform.Rotate(Vector3.right, 90);
                }

                Quaternion rotFix = new Quaternion(Input.gyro.attitude.x, Input.gyro.attitude.y, -Input.gyro.attitude.z, -Input.gyro.attitude.w);
                CameraLocal.localRotation = rotFix;
            }
        }

        // -------------------------------------------
        /* 
		 * SetUpCloudAnchorPosition
		 */
        protected virtual void SetUpCloudAnchorPosition(Vector3 _position)
        {
            transform.position = new Vector3(_position.x, transform.position.y, _position.z);
            CameraLocal.transform.parent.localPosition = -new Vector3(CameraLocal.transform.localPosition.x, CAMERA_SHIFT_HEIGHT_WORLDSENSE - _position.y, CameraLocal.transform.localPosition.z);
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
            if (_nameEvent == ActorTimeline.EVENT_GAMEPLAYER_SETUP_AVATAR)
            {
                if (!DirectorMode)
                {
                    Avatar = (GameObject)_list[0];
                }
            }
            if (_nameEvent == EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA)
            {
                Vector3 pos = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
                Vector3 fwd = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward.normalized);
#if ENABLE_OCULUS && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#elif ENABLE_WORLDSENSE && !UNITY_EDITOR
                if ((m_armModel != null) && (m_laserPointer != null))
                {
                    pos = Utilities.Clone(m_originLaser);
                    fwd = Utilities.Clone(m_forwardLaser);
                }
#endif
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA, _list[0], pos, fwd);                
            }
            if (DirectorMode)
            {
                if ((_nameEvent == EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER) ||
                    (_nameEvent == EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER))
                {
                    if (m_playerCameraActivated == null)
                    {
                        m_backupCameraPosition = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.position);
                        m_backupCameraForward = Utilities.Clone(YourVRUIScreenController.Instance.GameCamera.transform.forward);
                    }
                    else
                    {
                        m_playerCameraActivated.GetComponent<Actor>().GetModel().gameObject.SetActive(true);
                    }
                    m_playerCameraActivated = Players[(int)_list[0]];
                    m_playerCameraActivated.GetComponent<Actor>().GetModel().gameObject.SetActive(false);
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
                    YourVRUIScreenController.Instance.GameCamera.transform.position = m_backupCameraPosition;
                    YourVRUIScreenController.Instance.GameCamera.transform.forward = m_backupCameraForward;
                }
            }

            if (_nameEvent == ActorTimeline.EVENT_GAMEPLAYER_DATA_POSITION_PLAYER)
            {
                int netID = (int)_list[0];
                int uid = (int)_list[1];
                Vector3 positionPlayer = (Vector3)_list[2];
                Vector3 forwardPlayer = (Vector3)_list[3];
                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECAMERA_REAL_PLAYER_FORWARD, netID.ToString(), uid.ToString(),
                    positionPlayer.x.ToString(), positionPlayer.y.ToString(), positionPlayer.z.ToString(),
                    forwardPlayer.x.ToString(), forwardPlayer.y.ToString(), forwardPlayer.z.ToString());
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED)
            {
                this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
                this.gameObject.GetComponent<Rigidbody>().useGravity = true;
                this.gameObject.GetComponent<Collider>().isTrigger = false;
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
        }

        // -------------------------------------------
        /* 
		 * OnUIEvent
		 */
        protected virtual void OnUIEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == ScreenInformationView.EVENT_SCREEN_INFORMATION_DISPLAYED)
            {
                m_enableShootAction = false;
#if ENABLE_WORLDSENSE || ENABLE_OCULUS
                if (YourVRUIScreenController.Instance.LaserPointer!=null) YourVRUIScreenController.Instance.LaserPointer.SetActive(true);
#endif
            }
            if (_nameEvent == ScreenInformationView.EVENT_SCREEN_INFORMATION_CLOSED)
            {
                m_enableShootAction = true;
                m_ignoreNextShootAction = true;
#if ENABLE_WORLDSENSE || ENABLE_OCULUS
                if (YourVRUIScreenController.Instance.LaserPointer!=null) YourVRUIScreenController.Instance.LaserPointer.SetActive(false);
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
                YourVRUIScreenController.Instance.GameCamera.transform.position = m_playerCameraActivated.GetComponent<Actor>().RealPosition;
                YourVRUIScreenController.Instance.GameCamera.transform.forward = m_playerCameraActivated.GetComponent<Actor>().RealForward;
                return true;
            }
            return false;
        }

        // -------------------------------------------
        /* 
         * UpdateDefaultLogic
         */
        protected virtual void UpdateDefaultLogic()
        {
#if ENABLE_OCULUS
			OVRInput.Update();

            this.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            this.gameObject.GetComponent<Rigidbody>().useGravity = true;
            this.gameObject.GetComponent<Collider>().isTrigger = false;

			if (m_avatar != null)
			{
				m_avatar.transform.position = new Vector3(transform.position.x, -CAMERA_SHIFT_HEIGHT_WORLDSENSE + transform.position.y, transform.position.z);
				m_avatar.transform.forward = new Vector3(CenterEyeAnchor.transform.forward.x, 0, CenterEyeAnchor.transform.forward.z);
				m_avatar.GetComponent<Actor>().ForwardPlayer = CenterEyeAnchor.transform.forward;
                m_avatar.GetComponent<Actor>().PositionPlayer = CenterEyeAnchor.transform.position;
			}
#else
            if (m_avatar != null)
            {
                m_avatar.transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                m_avatar.transform.forward = new Vector3(CameraLocal.forward.x, 0, CameraLocal.forward.z);
                m_avatar.GetComponent<Actor>().ForwardPlayer = CameraLocal.forward;
                m_avatar.GetComponent<Actor>().PositionPlayer = CameraLocal.transform.position;
            }

            LogicDaydream6DOF();
#endif
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
        void Update()
        {
            if (IsGameFakeRunning()
#if ENABLE_WORLDSENSE || ENABLE_GOOGLE_ARCORE
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

#if UNITY_EDITOR
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