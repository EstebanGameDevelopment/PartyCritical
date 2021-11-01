using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;

namespace PartyCritical
{

    /******************************************
	 * 
	 * ScreenBasePlayerView
	 * 
	 * Default player HUD on Gyroscope mode
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenBasePlayerView : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_SCREENPLAYER_OPEN_INVENTORY = "EVENT_SCREENPLAYER_OPEN_INVENTORY";
        public const string EVENT_SCREENPLAYER_IGNORE_ONE_DESTRUCTION = "EVENT_SCREENPLAYER_IGNORE_ONE_DESTRUCTION";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected GameObject m_root;
        protected Transform m_container;

        protected GameObject m_buttonMove;
        protected GameObject m_buttonRotateLeft;
        protected GameObject m_buttonRotateRight;

        protected bool m_ignoreOneDestruction = false;

        // -------------------------------------------
        /* 
		* Constructor
		*/
        public virtual void Start()
        {
			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

            if (m_container.Find("Inventory") != null)
            {
                GameObject openInventoryScreen = m_container.Find("Inventory").gameObject;
                openInventoryScreen.GetComponent<Button>().onClick.AddListener(OpenInventory);
#if !ENABLE_MULTIPLAYER_TIMELINE
                openInventoryScreen.SetActive(false);
#endif
            }

            if (m_container.Find("Button_Move") != null)
            {
                m_buttonMove = m_container.Find("Button_Move").gameObject;
            }

            if (m_container.Find("Button_Action") != null)
            {
                m_container.Find("Button_Action").gameObject.GetComponent<Button>().onClick.AddListener(OnActionButton);
            }

            if (m_container.Find("RotateLeft") != null)
            {
                m_buttonRotateLeft = m_container.Find("RotateLeft").gameObject;
                m_buttonRotateLeft.GetComponent<Button>().onClick.AddListener(OnRotateLeft);
#if !ENABLE_ROTATE_LOCALCAMERA || UNITY_WEBGL || UNITY_STANDALONE
                m_buttonRotateLeft.SetActive(false);
#endif
            }
            if (m_container.Find("RotateRight") != null)
            {
                m_buttonRotateRight = m_container.Find("RotateRight").gameObject;
                m_buttonRotateRight.GetComponent<Button>().onClick.AddListener(OnRotateRight);
#if !ENABLE_ROTATE_LOCALCAMERA || UNITY_WEBGL || UNITY_STANDALONE
                m_buttonRotateRight.SetActive(false);
#endif
            }


#if UNITY_WEBGL || UNITY_STANDALONE
            if (m_buttonMove != null) m_buttonMove.SetActive(false);
#endif

            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);
        }


        // -------------------------------------------
        /* 
		 * Remove all the references
		 */
        void OnDestroy()
        {
            UIEventController.Instance.UIEvent -= OnUIEvent;
        }

        // -------------------------------------------
        /* 
		* OpenInventory
		*/
        protected virtual void OpenInventory()
		{
            UIEventController.Instance.DispatchUIEvent(EVENT_SCREENPLAYER_OPEN_INVENTORY);
        }

        // -------------------------------------------
        /* 
		 * OnActionButton
		 */
        protected void OnActionButton()
        {
            UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_GENERIC_ACTION_DOWN);
        }

        // -------------------------------------------
        /* 
         * OnRotateRight
         */
        protected void OnRotateRight()
        {
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_APPLY_ROTATION_CAMERA, true);
        }

        // -------------------------------------------
        /* 
         * OnRotateLeft
         */
        protected void OnRotateLeft()
        {
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_APPLY_ROTATION_CAMERA, false);
        }


        // -------------------------------------------
        /* 
         * OnUIEvent
         */
        protected void OnUIEvent(string _nameEvent, params object[] _list)
        {
            if (_nameEvent == EVENT_SCREENPLAYER_IGNORE_ONE_DESTRUCTION)
            {
                m_ignoreOneDestruction = true;
            }
            if ((_nameEvent == UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN)
                || (_nameEvent == UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN)
                || (_nameEvent == UIEventController.EVENT_SCREENMANAGER_VR_OPEN_GENERIC_SCREEN)
                || (_nameEvent == UIEventController.EVENT_SCREENMANAGER_VR_OPEN_INFORMATION_SCREEN))

            {
                UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_ENABLE_CONTROL_CAMERA, false, false);
                UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_STOP_MOVING);
                m_container.gameObject.SetActive(false);
            }
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_DESTROY_SCREEN)
            {
                UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_ENABLE_CONTROL_CAMERA, true, true);
                if (m_ignoreOneDestruction)
                {
                    m_ignoreOneDestruction = false;
                }
                else
                {
                    m_container.gameObject.SetActive(true);
                }                
            }
            if (_nameEvent == CustomButton.BUTTON_PRESSED_DOWN)
            {
                GameObject selectedButton = (GameObject)_list[0];
                if (selectedButton == m_buttonMove)
                {
                    UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_START_MOVING);
                }
            }
            if (_nameEvent == CustomButton.BUTTON_RELEASE_UP)
            {
                GameObject selectedButton = (GameObject)_list[0];
                if (selectedButton == m_buttonMove)
                {
                    UIEventController.Instance.DispatchUIEvent(CameraBaseController.EVENT_CAMERACONTROLLER_STOP_MOVING);
                }
            }
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        void Update()
        {
#if UNITY_EDITOR || UNITY_WEBGL || UNITY_STANDALONE
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                OnActionButton();
            }
#endif
        }
    }
}