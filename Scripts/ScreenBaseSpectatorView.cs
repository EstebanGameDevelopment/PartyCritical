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
	 * ScreenBaseSpectatorView
	 * 
	 * Control panel of director to select the cameras of the players
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenBaseSpectatorView : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_SPECTATOR_SET_UP_PLAYERS          = "EVENT_SPECTATOR_SET_UP_PLAYERS";

        public const string EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR = "EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR";
        public const string EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER  = "EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        private GameObject m_root;
		private Transform m_container;

        private Text m_textCamera;
        private GameObject m_iconPlayer;
        private GameObject m_iconDirector;

        private int m_playerIndexSelected = -1;

        private List<GameObject> m_players;

        protected bool m_cameraFixedEnabled = true;
        protected GameObject m_stopFixCamera;

        protected bool CameraFixedEnabled
        {
            get { return m_cameraFixedEnabled; }
            set
            {
                m_cameraFixedEnabled = value;
                if (m_stopFixCamera != null)
                {
                    m_stopFixCamera.SetActive(m_cameraFixedEnabled);
                }
            }
        }

        // -------------------------------------------
        /* 
		* Constructor
		*/
        public virtual void Start()
        {
			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

			GameObject changeToPlayerCameraGame = m_container.Find("Change").gameObject;
            m_iconPlayer = changeToPlayerCameraGame.transform.transform.Find("IconPlayer").gameObject;
            m_iconDirector = changeToPlayerCameraGame.transform.transform.Find("IconDirector").gameObject;
            m_textCamera = changeToPlayerCameraGame.transform.Find("Text").GetComponent<Text>();
            m_iconPlayer.SetActive(false);
            m_iconDirector.SetActive(true);
            m_textCamera.text = "SPECTATOR";
            changeToPlayerCameraGame.GetComponent<Button>().onClick.AddListener(ChangeToPlayerCamera);

			GameObject hideThisScreen = m_container.Find("Hide").gameObject;
			hideThisScreen.GetComponent<Button>().onClick.AddListener(HidePanel);

			UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);
            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);

            if (m_container.Find("FixCamera") != null)
            {
                GameObject fixCameraButton = m_container.Find("FixCamera").gameObject;
                fixCameraButton.GetComponent<Button>().onClick.AddListener(FixCameraChanged);
                m_stopFixCamera = m_container.Find("FixCamera/Stop").gameObject;
                CameraFixedEnabled = true;
            }

            Invoke("LoadRightCamera", 2);
        }

        // -------------------------------------------
        /* 
		* LoadRightCamera
		*/
        public virtual void LoadRightCamera()
        {
            ChangeToPlayerCamera();
            HidePanel();
        }

        // -------------------------------------------
        /* 
		* OnDestroy
		*/
        void OnDestroy()
		{
            Destroy();
        }

        // -------------------------------------------
        /* 
		 * Destroy
		 */
        public virtual void Destroy()
        {
            UIEventController.Instance.UIEvent -= OnMenuEvent;
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
        }

        // -------------------------------------------
        /* 
		* ChangeToPlayerCamera
		*/
        protected virtual void ChangeToPlayerCamera()
		{
            if (m_players == null) return;

            m_playerIndexSelected++;
            if (m_playerIndexSelected >= m_players.Count)
            {
                m_playerIndexSelected = -1;
                m_iconPlayer.SetActive(false);
                m_iconDirector.SetActive(true);
                m_textCamera.text = "DIRECTOR";
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR);
            }
            else
            {
                m_iconPlayer.SetActive(true);
                m_iconDirector.SetActive(false);
                m_textCamera.text = "PLAYER-" + m_playerIndexSelected;
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_SPECTATOR_CHANGE_CAMERA_TO_PLAYER, m_playerIndexSelected);
            }
        }

        // -------------------------------------------
        /* 
		* FixCameraChanged
		*/
        private void FixCameraChanged()
        {
            CameraFixedEnabled = !CameraFixedEnabled;
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_FIX_DIRECTOR_CAMERA, !m_cameraFixedEnabled);
        }

        // -------------------------------------------
        /* 
		* PlayWithGyroscopePressed
		*/
        protected virtual void HidePanel()
		{
            m_container.gameObject.SetActive(false);
        }

        // -------------------------------------------
        /* 
		* OnGameEvent
		*/
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_SPECTATOR_SET_UP_PLAYERS)
            {
                m_players = (List<GameObject>)_list[0];
            }
            if (_nameEvent == EVENT_SPECTATOR_RESET_CAMERA_TO_DIRECTOR)
            {
                m_playerIndexSelected = -1;
                m_iconPlayer.SetActive(false);
                m_iconDirector.SetActive(true);
                m_textCamera.text = "SPECTATOR";
            }
        }

        // -------------------------------------------
        /* 
		* OnMenuEvent
		*/
        protected virtual void OnMenuEvent(string _nameEvent, params object[] _list)
		{
            if (_nameEvent == KeysEventInputController.ACTION_BUTTON_DOWN)
            {
                if (!m_container.gameObject.activeSelf)
                {
                    m_container.gameObject.SetActive(true);
                }
            }
		}
	}
}