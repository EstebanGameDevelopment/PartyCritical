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
	 * ScreenDirectorView
	 * 
	 * Control panel of director to select the cameras of the players
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenBaseDirectorView : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_DIRECTOR_SET_UP_PLAYERS          = "EVENT_DIRECTOR_SET_UP_PLAYERS";

        public const string EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR = "EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR";
        public const string EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER  = "EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER";

        public const string EVENT_DIRECTOR_TELEPORT_ENABLE  = "EVENT_DIRECTOR_TELEPORT_ENABLE";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected GameObject m_root;
        protected Transform m_container;

        protected Text m_textCamera;
        protected GameObject m_iconPlayer;
        protected GameObject m_iconDirector;

        protected int m_playerIndexSelected = -1;

        protected List<GameObject> m_players;

        protected bool m_teleportEnabled = false;
        protected GameObject m_stopTeleport;


        protected bool TeleportEnabled
        {
            get { return m_teleportEnabled; }
            set {
                m_teleportEnabled = value;
                if (m_stopTeleport != null)
                {
                    m_stopTeleport.SetActive(!m_teleportEnabled);
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
            m_textCamera.text = "DIRECTOR";
            changeToPlayerCameraGame.GetComponent<Button>().onClick.AddListener(ChangeToPlayerCamera);

			GameObject hideThisScreen = m_container.Find("Hide").gameObject;
			hideThisScreen.GetComponent<Button>().onClick.AddListener(HidePanel);

            if (m_container.Find("KillParty") != null)
            {
                GameObject killPartyButton = m_container.Find("KillParty").gameObject;
                killPartyButton.GetComponent<Button>().onClick.AddListener(KillThisParty);
            }

            if (m_container.Find("Teleport") != null)
            {
                GameObject teleportButton = m_container.Find("Teleport").gameObject;
                teleportButton.GetComponent<Button>().onClick.AddListener(TeleportChanged);
                m_stopTeleport = m_container.Find("Teleport/Stop").gameObject;
            }

            UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);
            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);

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
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR);
            }
            else
            {
                m_iconPlayer.SetActive(true);
                m_iconDirector.SetActive(false);
                m_textCamera.text = "PLAYER-" + m_playerIndexSelected;
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_DIRECTOR_CHANGE_CAMERA_TO_PLAYER, m_playerIndexSelected);
            }
        }

        // -------------------------------------------
        /* 
		* HidePanel
		*/
        protected virtual void HidePanel()
		{
            m_container.gameObject.SetActive(false);
        }

        // -------------------------------------------
        /* 
		* KillThisParty
		*/
        protected virtual void KillThisParty()
        {
            NetworkEventController.Instance.PriorityDelayNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_PARTY_OVER, 0.1f);
        }

        // -------------------------------------------
        /* 
		* OnGameEvent
		*/
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_DIRECTOR_SET_UP_PLAYERS)
            {
                m_players = (List<GameObject>)_list[0];
            }
            if (_nameEvent == EVENT_DIRECTOR_RESET_CAMERA_TO_DIRECTOR)
            {
                m_playerIndexSelected = -1;
                m_iconPlayer.SetActive(false);
                m_iconDirector.SetActive(true);
                m_textCamera.text = "DIRECTOR";
            }
        }

        // -------------------------------------------
        /* 
		* TeleportChanged
		*/
        protected void TeleportChanged()
        {
            TeleportEnabled = !TeleportEnabled;
            NetworkEventController.Instance.DispatchNetworkEvent(EVENT_DIRECTOR_TELEPORT_ENABLE, TeleportEnabled.ToString());            
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