using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;
#if ENABLE_VALIDATION
using VRPartyValidation;
#endif
#if ENABLE_BITCOIN
using YourBitcoinController;
#endif

namespace PartyCritical
{
    /******************************************
	 * 
	 * ScreenSplash6DOFBaseView
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenSplash6DOFBaseView : ScreenBaseView, IBasicView
    {
        public const string SCREEN_NAME = "SCREEN_SPLASH";

        // ----------------------------------------------
        // CONSTANTS
        // ----------------------------------------------	
        public const int SHORTCUT_CREATE_GAME_AS_WORLDSENSE_PLAYER  = 0;
        public const int SHORTCUT_JOIN_GAME_AS_ARCORE_PLAYER        = 1;
        public const int SHORTCUT_JOIN_GAME_AS_GYRO_PLAYER          = 2;
        public const int SHORTCUT_JOIN_GAME_AS_NO_ARCORE_PLAYER     = 3;
        public const int SHORTCUT_JOIN_GAME_AS_DIRECTOR             = 4;

        protected const string CTE_ENABLE_PLAYER_WORLDSENSE     = "#ENABLE_PLAYER_WORLDSENSE";
        protected const string CTE_ENABLE_PLAYER_ARCORE         = "#ENABLE_PLAYER_ARCORE";
        protected const string CTE_ENABLE_PLAYER_GYRO           = "#ENABLE_PLAYER_GYRO";
        protected const string CTE_ENABLE_PLAYER_NOARCORE       = "#ENABLE_PLAYER_NOARCORE";
        protected const string CTE_ENABLE_DIRECTOR_JOIN         = "#ENABLE_DIRECTOR_JOIN";
        protected const string CTE_ENABLE_SPECTATOR             = "#ENABLE_SPECTATOR";

        protected const string CTE_ENABLE_SOCKET                = "#ENABLE_SOCKET";
        protected const string CTE_LEVEL_                       = "#LEVEL_";
        protected const string CTE_PLAYER_                      = "#PLAYER_";

        // ----------------------------------------------
        // PROTECTED MEMBERS
        // ----------------------------------------------	
        protected Transform m_container;
        protected Transform m_poweredBy;

        protected float m_timerToVRMenus = 5.9f;

        protected bool m_runUpdate = false;
        protected bool m_isThereButtons = false;
        protected int m_shortcut;
		
		protected string m_debugRoomName = "m_debugRoomName";

        protected bool m_isCreatingGame = true;

        protected string m_configData = "";

        protected bool m_enablePlayerWorldsense = false;
        protected bool m_enablePlayerARCore = false;
        protected bool m_enablePlayerGyro = false;
        protected bool m_enablePlayerNoARCore = false;
        protected bool m_enableDirectorJoin = false;
        protected bool m_enableSpectator = false;

        protected bool m_enableSocket = false;

        // ----------------------------------------------
        // SETTERS/GETTERS
        // ----------------------------------------------	
        protected virtual int TotalNumberOfPlayers
        {
            get { return 300; }
        }
        protected virtual string BASE_URL_PARTY_VALIDATION
        {
            get { return ""; }
        }
        protected virtual string ACCESS_SENTENCE
        {
            get { return ""; }
        }
        protected virtual string ENCRYPTION_KEY
        {
            get { return ""; }
        }

        // -------------------------------------------
        /* 
		 * Constructor
		 */
        public override void Initialize(params object[] _list)
        {
            base.Initialize(_list);

            m_isThereButtons = false;

            m_container = this.gameObject.transform.Find("Content");

            if (m_container.Find("Text") != null)
            {
                m_container.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.presentation.text");
            }

            m_poweredBy = m_container.Find("PoweredBy");
            if (m_poweredBy != null)
            {
                m_poweredBy.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.powered.by");
            }
#if DISABLE_POWERED_BY
            if (m_poweredBy != null)
            {
                m_poweredBy.gameObject.SetActive(false);
            }
#endif

            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
            UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);

#if ENABLE_VALIDATION
            if (m_container.Find("Text") != null)
            {
                m_container.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.connecting.multiplayer.server");
            }
            string networkBitcoinName = "Fake Net";
#if ENABLE_BITCOIN
            networkBitcoinName = BitCoinController.OPTION_NETWORK_MAIN;
#endif
            VRPartyValidationController.Instance.Initialitzation(OPERATION_VRPARTY_MODE.MODE_GAME, BASE_URL_PARTY_VALIDATION, networkBitcoinName, ACCESS_SENTENCE, ENCRYPTION_KEY);
#else
            InitializeWithShortcut();
#endif
        }

        // -------------------------------------------
        /* 
		 * ParseConfigData
		 */
        protected virtual void ParseConfigData(string _configData)
        {
            m_enablePlayerWorldsense = (_configData.IndexOf(CTE_ENABLE_PLAYER_WORLDSENSE) != -1);
            m_enablePlayerARCore = (_configData.IndexOf(CTE_ENABLE_PLAYER_ARCORE) != -1);
            m_enablePlayerGyro = (_configData.IndexOf(CTE_ENABLE_PLAYER_GYRO) != -1);
            m_enablePlayerNoARCore = (_configData.IndexOf(CTE_ENABLE_PLAYER_NOARCORE) != -1);
            m_enableDirectorJoin = (_configData.IndexOf(CTE_ENABLE_DIRECTOR_JOIN) != -1);
            m_enableSpectator = (_configData.IndexOf(CTE_ENABLE_SPECTATOR) != -1);

            m_enableSocket = (_configData.IndexOf(CTE_ENABLE_SOCKET) != -1);
        }

        // -------------------------------------------
        /* 
		 * ParseConfigLevel
		 */
        protected int ParseConfigLevel(string _configData)
        {
            if (_configData.IndexOf(CTE_LEVEL_) != -1)
            {
                string levelSelected = _configData.Substring(_configData.IndexOf(CTE_LEVEL_) + CTE_LEVEL_.Length, 2);
                if (levelSelected.IndexOf("0") == 0)
                {
                    levelSelected = levelSelected.Substring(1,1);
                }
                return int.Parse(levelSelected);
            }
            else
            {
                return 0;
            }
        }

        // -------------------------------------------
        /* 
		 * ParseConfigPlayer
		 */
        protected int ParseConfigPlayer(string _configData)
        {
            if (_configData.IndexOf(CTE_PLAYER_) != -1)
            {
                string playerSelected = _configData.Substring(_configData.IndexOf(CTE_PLAYER_) + CTE_PLAYER_.Length, 2);
                if (playerSelected.IndexOf("0") == 0)
                {
                    playerSelected = playerSelected.Substring(1,1);
                }
                return int.Parse(playerSelected);
            }
            else
            {
                return 0;
            }
        }

        // -------------------------------------------
        /* 
         * InitializeWithShortcut
         */
        protected void InitializeWithShortcut(string _configData)
        {
            string localConfigData = _configData;

            MenuScreenController.Instance.MaxPlayers = TotalNumberOfPlayers + 1;

            float delayShortcutSplash = 4;
#if UNITY_EDITOR
            delayShortcutSplash = 0.1f;
#else
            delayShortcutSplash = 4;
#endif

#if ENABLE_PLAYER_WORLDSENSE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_WORLDSENSE");
#elif ENABLE_PLAYER_ARCORE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_ARCORE");
#elif ENABLE_PLAYER_GYRO
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_GYRO");
            localConfigData = "#ENABLE_PLAYER_WORLDSENSE#LEVEL_01#PLAYER_00#ENABLE_SOCKET";
#elif ENABLE_PLAYER_NOARCORE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_NOARCORE");
#elif ENABLE_DIRECTOR_JOIN
            Debug.LogError("++++USING CONFIG::ENABLE_DIRECTOR_JOIN");
#elif ENABLE_SPECTATOR
            Debug.LogError("++++USING CONFIG::ENABLE_SPECTATOR");
#endif

            m_configData = localConfigData;
            ParseConfigData(m_configData);

#if ENABLE_PLAYER_WORLDSENSE
            m_enablePlayerWorldsense = true;
#elif ENABLE_PLAYER_ARCORE
            m_enablePlayerARCore = true;
#elif ENABLE_PLAYER_GYRO
            m_enablePlayerGyro = true;
#elif ENABLE_PLAYER_NOARCORE
            m_enablePlayerNoARCore = true;
#elif ENABLE_DIRECTOR_JOIN
            m_enableDirectorJoin = true;
#elif ENABLE_SPECTATOR
            m_enableSpectator = true;
#endif

#if ENABLE_SOCKET
            m_enableSocket = true;
#endif

            if (m_enablePlayerWorldsense)
            {
                Invoke("Direct2PlayerGame", delayShortcutSplash);
            }
            else
            if (m_enablePlayerARCore)
            {
                Invoke("JoinAsOtherPlayerGame_ARCore", delayShortcutSplash);
            }
            else
            if (m_enablePlayerGyro)
            {
                Invoke("JoinAsOtherPlayerGame_Gyro", delayShortcutSplash);
            }
            else
            if (m_enablePlayerNoARCore)
            {
                Invoke("JoinAsOtherPlayerGame_NoARCore", delayShortcutSplash);
            }
            else
            if (m_enableDirectorJoin || m_enableSpectator)
            {
                Invoke("JoinAsDirectorGame", delayShortcutSplash);
            }
            else
            {
                m_runUpdate = true;
                if (!m_isThereButtons)
                {
                    StartCoroutine(ShowSplashDelay());
                }
            }

#if ENABLE_OCULUS || ENABLE_WORLDSENSE
            KeysEventInputController.Instance.EnableActionOnMouseDown = false;
            UIEventController.Instance.DispatchUIEvent(EventSystemController.EVENT_EVENTSYSTEMCONTROLLER_RAYCASTING_SYSTEM, false);
#endif
        }

        // -------------------------------------------
        /* 
        * Direct2PlayerGame
        */
        public void Direct2PlayerGame()
        {
            PlayerPrefs.DeleteAll();
            if (m_enableSocket)
            {
                MultiplayerConfiguration.SaveIPAddressServer(MenuScreenController.Instance.ServerIPAdress);
                MultiplayerConfiguration.SavePortServer(MenuScreenController.Instance.ServerPortNumber);

                m_shortcut = SHORTCUT_CREATE_GAME_AS_WORLDSENSE_PLAYER;
                YourNetworkTools.SetLocalGame(false);
                NetworkEventController.Instance.MenuController_SetLobbyMode(true);
                NetworkEventController.Instance.MenuController_InitialitzationSocket(-1, 0);
            }
            else
            {
                YourNetworkTools.SetLocalGame(true);
                NetworkEventController.Instance.MenuController_SetLobbyMode(false);
                NetworkEventController.Instance.MenuController_SetNameRoomLobby("");
                Direct2PlayerGameConfirmation();
            }
        }

        // -------------------------------------------
        /* 
		 * Direct2PlayerGameConfirmation
		 */
        protected virtual void Direct2PlayerGameConfirmation()
        {
            MultiplayerConfiguration.SaveEnableBackground(true);
            CardboardLoaderVR.SaveEnableCardboard(true);
            if (m_isCreatingGame)
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, TotalNumberOfPlayers, null, null, false);
            }
            else
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, MultiplayerConfiguration.VALUE_FOR_JOINING, null, null, false);
            }
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_DISABLED);
            MultiplayerConfiguration.SaveDirectorMode(MultiplayerConfiguration.DIRECTOR_MODE_DISABLED);
            MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_DISABLED);
            PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.HIDE_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_ARCore
		 */
        public void JoinAsOtherPlayerGame_ARCore()
        {
            PlayerPrefs.DeleteAll();
            if (m_enableSocket)
            {
                MultiplayerConfiguration.SaveIPAddressServer(MenuScreenController.Instance.ServerIPAdress);
                MultiplayerConfiguration.SavePortServer(MenuScreenController.Instance.ServerPortNumber);

                m_shortcut = SHORTCUT_JOIN_GAME_AS_ARCORE_PLAYER;
                YourNetworkTools.SetLocalGame(false);
                NetworkEventController.Instance.MenuController_SetLobbyMode(true);
                NetworkEventController.Instance.MenuController_InitialitzationSocket(-1, 0);
            }
            else
            {
                YourNetworkTools.SetLocalGame(true);
                NetworkEventController.Instance.MenuController_SetLobbyMode(false);
                NetworkEventController.Instance.MenuController_SetNameRoomLobby("");
                JoinAsOtherPlayerGame_ARCoreConfirmation();
            }
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_ARCoreConfirmation
		 */
        protected virtual void JoinAsOtherPlayerGame_ARCoreConfirmation()
        {
            MultiplayerConfiguration.SaveEnableBackground(true);
            CardboardLoaderVR.SaveEnableCardboard(true);
            if (m_isCreatingGame)
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, TotalNumberOfPlayers, null, null, false);
            }
            else
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, MultiplayerConfiguration.VALUE_FOR_JOINING, null, null, false);
            }
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_ENABLED);
            MultiplayerConfiguration.SaveDirectorMode(MultiplayerConfiguration.DIRECTOR_MODE_DISABLED);
            MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_DISABLED);
            PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_Gyro
		 */
        public void JoinAsOtherPlayerGame_Gyro()
        {
            PlayerPrefs.DeleteAll();
            if (m_enableSocket)
            {
                MultiplayerConfiguration.SaveIPAddressServer(MenuScreenController.Instance.ServerIPAdress);
                MultiplayerConfiguration.SavePortServer(MenuScreenController.Instance.ServerPortNumber);

                m_shortcut = SHORTCUT_JOIN_GAME_AS_GYRO_PLAYER;
                YourNetworkTools.SetLocalGame(false);
                NetworkEventController.Instance.MenuController_SetLobbyMode(true);
                NetworkEventController.Instance.MenuController_InitialitzationSocket(-1, 0);
            }
            else
            {
                YourNetworkTools.SetLocalGame(true);
                NetworkEventController.Instance.MenuController_SetLobbyMode(false);
                NetworkEventController.Instance.MenuController_SetNameRoomLobby("");
                JoinAsOtherPlayerGame_GyroConfirmation();
            }
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_ARCoreConfirmation
		 */
        protected virtual void JoinAsOtherPlayerGame_GyroConfirmation()
        {
            MultiplayerConfiguration.SaveEnableBackground(true);
            CardboardLoaderVR.SaveEnableCardboard(false);
            if (m_isCreatingGame)
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, TotalNumberOfPlayers, null, null, false);
            }
            else
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, MultiplayerConfiguration.VALUE_FOR_JOINING, null, null, false);
            }
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_ENABLED);
            MultiplayerConfiguration.SaveDirectorMode(MultiplayerConfiguration.DIRECTOR_MODE_DISABLED);
            MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_DISABLED);
            PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_NoARCore
		 */
        public void JoinAsOtherPlayerGame_NoARCore()
        {
            PlayerPrefs.DeleteAll();
            if (m_enableSocket)
            {
                MultiplayerConfiguration.SaveIPAddressServer(MenuScreenController.Instance.ServerIPAdress);
                MultiplayerConfiguration.SavePortServer(MenuScreenController.Instance.ServerPortNumber);

                m_shortcut = SHORTCUT_JOIN_GAME_AS_NO_ARCORE_PLAYER;
                YourNetworkTools.SetLocalGame(false);
                NetworkEventController.Instance.MenuController_SetLobbyMode(true);
                NetworkEventController.Instance.MenuController_InitialitzationSocket(-1, 0);
            }
            else
            {
                YourNetworkTools.SetLocalGame(true);
                NetworkEventController.Instance.MenuController_SetLobbyMode(false);
                NetworkEventController.Instance.MenuController_SetNameRoomLobby("");
                JoinAsOtherPlayerGame_NoARCoreConfirmation();
            }
        }

        // -------------------------------------------
        /* 
		 * JoinAsOtherPlayerGame_ARCoreConfirmation
		 */
        protected virtual void JoinAsOtherPlayerGame_NoARCoreConfirmation()
        {
            MultiplayerConfiguration.SaveEnableBackground(true);
            CardboardLoaderVR.SaveEnableCardboard(true);
            if (m_isCreatingGame)
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, TotalNumberOfPlayers, null, null, false);
            }
            else
            {
                MenuScreenController.Instance.LoadCustomGameScreenOrCreateGame(false, MultiplayerConfiguration.VALUE_FOR_JOINING, null, null, false);
            }
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_DISABLED);
            MultiplayerConfiguration.SaveDirectorMode(MultiplayerConfiguration.DIRECTOR_MODE_DISABLED);
            MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_DISABLED);
            PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
         * JoinAsDirectorGame
         */
        public void JoinAsDirectorGame()
        {
            PlayerPrefs.DeleteAll();
            if (m_enableSocket)
            {
                MultiplayerConfiguration.SaveIPAddressServer(MenuScreenController.Instance.ServerIPAdress);
                MultiplayerConfiguration.SavePortServer(MenuScreenController.Instance.ServerPortNumber);

                m_shortcut = SHORTCUT_JOIN_GAME_AS_DIRECTOR;
                YourNetworkTools.SetLocalGame(false);
                NetworkEventController.Instance.MenuController_SetLobbyMode(true);
                NetworkEventController.Instance.MenuController_InitialitzationSocket(-1, 0);
            }
            else
            {
                YourNetworkTools.SetLocalGame(true);
                NetworkEventController.Instance.MenuController_SetLobbyMode(false);
                NetworkEventController.Instance.MenuController_SetNameRoomLobby("");
                JoinAsDirectorGameConfirmation();
            }
        }

        // -------------------------------------------
        /* 
         * JoinAsDirectorGameConfirmation
         */
        protected virtual void JoinAsDirectorGameConfirmation()
        {
            MultiplayerConfiguration.SaveEnableBackground(true);
            CardboardLoaderVR.SaveEnableCardboard(false);
            NetworkEventController.Instance.MenuController_SaveNumberOfPlayers(MultiplayerConfiguration.VALUE_FOR_JOINING);
#if ENABLE_GOOGLE_ARCORE
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_ENABLED);
#else
            MultiplayerConfiguration.SaveGoogleARCore(MultiplayerConfiguration.GOOGLE_ARCORE_DISABLED);
#endif
            MultiplayerConfiguration.SaveDirectorMode(MultiplayerConfiguration.DIRECTOR_MODE_ENABLED);
            if (m_enableSpectator)
            {
                MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_ENABLED);
            }
            else
            {
                MultiplayerConfiguration.SaveSpectatorMode(MultiplayerConfiguration.SPECTATOR_MODE_DISABLED);
            }
            PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
		 * Destroy
		 */
        public override bool Destroy()
        {
            if (base.Destroy()) return true;

#if ENABLE_OCULUS || ENABLE_WORLDSENSE
            KeysEventInputController.Instance.EnableActionOnMouseDown = false;
#endif

            UIEventController.Instance.UIEvent -= OnMenuEvent;
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_SCREEN, this.gameObject);
            if (m_runUpdate)
            {
                if (m_timerToVRMenus > 0)
                {
                    UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenMenuMainView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false);
                }
            }
            return false;
        }

        // -------------------------------------------
        /* 
		 * Constructor
		 */
        IEnumerator ShowSplashDelay()
        {
            yield return new WaitForSeconds(5);
            Destroy();
        }

        // -------------------------------------------
        /* 
		 * PlayGame
		 */
        protected void PlayGame()
        {
            Destroy();
        }

        // -------------------------------------------
        /* 
		 * GoToStore
		 */
        protected virtual void GoToStore()
        {
            Application.OpenURL("https://assetstore.unity.com/publishers/28662");
        }

        // -------------------------------------------
        /* 
		 * GetGameObject
		 */
        public GameObject GetGameObject()
        {
            return this.gameObject;
        }

        // -------------------------------------------
        /* 
		 * Global manager of events
		 */
        protected override void OnMenuEvent(string _nameEvent, params object[] _list)
        {
            base.OnMenuEvent(_nameEvent, _list);

#if ENABLE_VALIDATION
            if (_nameEvent == VRPartyValidationController.EVENT_VRPARTYVALIDATION_TEXT_NO_ACCESS_STORAGE)
            {
                if (m_container.Find("Text") != null)
                {
                    m_container.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.no.access.to.storage");
                }
            }
            if (_nameEvent == VRPartyValidationController.EVENT_VRPARTYVALIDATION_TEXT_SERVER_AVAILABLE)
            {
                if (m_container.Find("Text") != null)
                {
                    m_container.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.no.connection.multiplayer.server");
                }
            }
#endif

            /*
            if (_nameEvent == ClientTCPEventsController.EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID)
            {
                Debug.LogError("EVENT_CLIENT_TCP_ESTABLISH_NETWORK_ID!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                switch (m_shortcut)
                {
                    case SHORTCUT_CREATE_GAME_AS_WORLDSENSE_PLAYER:
                        Direct2PlayerGameConfirmation();
                        break;

                    case SHORTCUT_JOIN_GAME_AS_WORLDSENSE_PLAYER:
                    case SHORTCUT_JOIN_GAME_AS_ARCORE_PLAYER:
                    case SHORTCUT_JOIN_GAME_AS_NO_ARCORE_PLAYER:
                    case SHORTCUT_JOIN_GAME_AS_DIRECTOR:
                        break;
                }
            }
            */
            if (_nameEvent == ClientTCPEventsController.EVENT_CLIENT_TCP_LIST_OF_GAME_ROOMS)
            {
                if (ClientTCPEventsController.Instance.RoomsLobby.Count == 0)
                {
                    NetworkEventController.Instance.MenuController_SetNameRoomLobby(m_debugRoomName);

                    m_isCreatingGame = true;
                }
                else
                {
                    ItemMultiTextEntry room = ClientTCPEventsController.Instance.RoomsLobby[ClientTCPEventsController.Instance.RoomsLobby.Count - 1];
                    int roomNumber = int.Parse(room.Items[1]);
                    string extraData = room.Items[3];
                    Debug.LogError("roomNumber[" + roomNumber + "]::extraData[" + extraData + "]******************************************************************************");

                    NetworkEventController.Instance.MenuController_SaveRoomNumberInServer(roomNumber);
                    MultiplayerConfiguration.SaveExtraData(extraData);

                    m_isCreatingGame = false;
                }

                switch (m_shortcut)
                {
                    case SHORTCUT_CREATE_GAME_AS_WORLDSENSE_PLAYER:
                        Invoke("Direct2PlayerGameConfirmation", 1);
                        break;

                    case SHORTCUT_JOIN_GAME_AS_ARCORE_PLAYER:
                        Invoke("JoinAsOtherPlayerGame_ARCoreConfirmation", 1);
                        break;

                    case SHORTCUT_JOIN_GAME_AS_GYRO_PLAYER:
                        Invoke("JoinAsOtherPlayerGame_GyroConfirmation", 1);
                        break;

                    case SHORTCUT_JOIN_GAME_AS_NO_ARCORE_PLAYER:
                        Invoke("JoinAsOtherPlayerGame_NoARCoreConfirmation", 1);
                        break;

                    case SHORTCUT_JOIN_GAME_AS_DIRECTOR:
                        Invoke("JoinAsDirectorGameConfirmation", 1);
                        break;
                }
            }
        }

        // -------------------------------------------
        /* 
		 * OnBasicSystemEvent
		 */
        protected void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
#if ENABLE_VALIDATION
            if (_nameEvent == VRPartyValidationController.EVENT_VRPARTYVALIDATION_RESPONSE_ACCESS)
            {
                if ((bool)_list[0])
                {
                    if ((long)_list[1] > 0)
                    {
                        InitializeWithShortcut((string)_list[2]);
                    }
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
        * Update
        */
        public void Update()
        {
#if ENABLE_WORLDSENSE || ENABLE_OCULUS
            if (m_runUpdate)
            {
                Destroy();
                if (MenuScreenController.Instance.MainCamera2D != null) MenuScreenController.Instance.MainCamera2D.SetActive(false);
                if (MenuScreenController.Instance.VRComponents != null) MenuScreenController.Instance.VRComponents.SetActive(true);
                UIEventController.Instance.DelayUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, 0.2f, ScreenMenuMainView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false);
            }
#else
            if (m_runUpdate)
            {
                if (MenuScreenController.Instance.MainCamera2D != null)
                {
                    m_timerToVRMenus -= Time.deltaTime;
                    if (m_timerToVRMenus > 0)
                    {
                        if (m_container.Find("Button_Play/Text") != null)
                        {
                            m_container.Find("Button_Play/Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.splash.timer.2d.game", (int)m_timerToVRMenus);
                        }
                    }
                    else
                    {
                        if (MenuScreenController.Instance.VRComponents != null)
                        {
                            Destroy();
                            MenuScreenController.Instance.MainCamera2D.SetActive(false);
                            MenuScreenController.Instance.VRComponents.SetActive(true);
                            UIEventController.Instance.DelayUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, 0.2f, ScreenMenuMainView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false);
                        }
                    }
                }
            }
#endif
        }
    }
}