﻿using System;
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
#if !UNITY_EDITOR
using PartaGames.Android;
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
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_SPLASHBASE_REPORT_LOGIN_INFO = "EVENT_SPLASHBASE_REPORT_LOGIN_INFO";

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

        protected const string CTE_ENABLE_SOCKET                = "#ENABLE_SOCKET_";
        protected const string CTE_LEVEL_                       = "#LEVEL_";
        protected const string CTE_PLAYER_                      = "#PLAYER_";
        protected const string CTE_LOGIN_                       = "#LOGIN_";

        protected const string CTE_ENABLE_AUGMENTED             = "#ENABLE_AUGMENTED";

        private const string WRITE_EXTERNAL_STORAGE = "android.permission.WRITE_EXTERNAL_STORAGE";

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
        protected string m_serverIP = "";
        protected int m_serverPort = 0;

        protected bool m_enableAugmented = false;

        protected bool m_hasBeenCalledInitialitzation = false;
        protected bool m_hasPermissionsBeenGranted = false;

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
		 * Ask permissions
		 */
        public void Awake()
        {
#if !UNITY_EDITOR && !ENABLE_OCULUS
            if (!PermissionGranterUnity.IsPermissionGranted(WRITE_EXTERNAL_STORAGE))
            {
                PermissionGranterUnity.GrantPermission(WRITE_EXTERNAL_STORAGE, PermissionGrantedCallback);
            }
            else
            {
                m_hasPermissionsBeenGranted = true;
            }
#else
            m_hasPermissionsBeenGranted = true;
#endif
        }

        // -------------------------------------------
        /* 
		 * Ask permissions
		 */
        public void PermissionGrantedCallback(string permission, bool isGranted)
        {
            m_hasPermissionsBeenGranted = true;
        }

#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE
        // -------------------------------------------
        /* 
		 * OnApplicationFocus
		 */
        void OnApplicationFocus(bool hasFocus)
        {

            m_hasPermissionsBeenGranted = true;
            Invoke("ForceInitialitzation", 1);
        }

        // -------------------------------------------
        /* 
         * ForceInitialitzation
         */
        public void ForceInitialitzation()
        {
            Initialize();
        }
#endif

        // -------------------------------------------
        /* 
		 * Ask permissions
		 */
        public void DoNotRun()
        {
#if !UNITY_EDITOR
            PermissionGranterUnity.IsPermissionGranted(WRITE_EXTERNAL_STORAGE);
            PermissionGranterUnity.GrantPermission(WRITE_EXTERNAL_STORAGE, PermissionGrantedCallback);
#endif
        }

        // -------------------------------------------
        /* 
		 * Constructor
		 */
        public override void Initialize(params object[] _list)
        {
            if (!m_hasPermissionsBeenGranted) return;
            if (m_hasBeenCalledInitialitzation) return;
            m_hasBeenCalledInitialitzation = true;

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

            UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);
            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);

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
            InitializeWithShortcut("");
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
            if (m_enableSocket)
            {
                int startIndex = _configData.IndexOf(CTE_ENABLE_SOCKET) + CTE_ENABLE_SOCKET.Length;
                string subConfigSocket = _configData.Substring(startIndex, _configData.Length - startIndex);
                int indexFinalIP = subConfigSocket.IndexOf("_");
                m_serverIP = subConfigSocket.Substring(0, indexFinalIP);
                subConfigSocket = subConfigSocket.Substring(indexFinalIP + 1, subConfigSocket.Length - (indexFinalIP + 1));
                int indexFinalPort = subConfigSocket.IndexOf("_");
                m_serverPort = int.Parse(subConfigSocket.Substring(0, indexFinalPort));
                MenuScreenController.Instance.ServerIPAdress = m_serverIP;
                MenuScreenController.Instance.ServerPortNumber = m_serverPort;
            }

            m_enableAugmented = (_configData.IndexOf(CTE_ENABLE_AUGMENTED) != -1);
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
		 * ParseLogin
		 */
        protected void ParseLogin(string _configData)
        {
            if (_configData.IndexOf(CTE_LOGIN_) != -1)
            {
                int indexLoginData = _configData.IndexOf(CTE_LOGIN_) + CTE_LOGIN_.Length;
                string[] loginData = (_configData.Substring(indexLoginData, _configData.Length - indexLoginData)).Split('_');
                if (loginData.Length == 2)
                {
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_SPLASHBASE_REPORT_LOGIN_INFO, 1, loginData[0], loginData[1]);
                }
            }
        }

        // -------------------------------------------
        /* 
         * IsShortCutEnabled
         */
        protected bool IsShortCutEnabled()
        {
            bool isShortcutEnabled = false;
#if ENABLE_PLAYER_WORLDSENSE
            isShortcutEnabled = true;
#elif ENABLE_PLAYER_ARCORE
            isShortcutEnabled = true;
#elif ENABLE_PLAYER_GYRO
            isShortcutEnabled = true;
#elif ENABLE_PLAYER_NOARCORE
            isShortcutEnabled = true;
#elif ENABLE_DIRECTOR_JOIN
            isShortcutEnabled = true;
#elif ENABLE_SPECTATOR
            isShortcutEnabled = true;
#endif

            return isShortcutEnabled;
        }

        // -------------------------------------------
        /* 
         * InitializeWithShortcut
         */
        protected void InitializeWithShortcut(string _configData)
        {
            string localConfigData = _configData;

            MenuScreenController.Instance.MaxPlayers = TotalNumberOfPlayers + 1;

            float delayShortcutSplash = 1;
#if UNITY_EDITOR
            delayShortcutSplash = 0.1f;
#else
            delayShortcutSplash = 1;
#endif

            // 
#if ENABLE_PLAYER_WORLDSENSE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_WORLDSENSE");
            localConfigData = "#ENABLE_PLAYER_WORLDSENSE#LEVEL_00#PLAYER_00";
            // localConfigData = "#ENABLE_PLAYER_WORLDSENSE#LEVEL_01#PLAYER_00";
#elif ENABLE_PLAYER_ARCORE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_ARCORE");
            localConfigData = "#ENABLE_PLAYER_ARCORE#LEVEL_00#PLAYER_00";
#elif ENABLE_PLAYER_GYRO
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_GYRO");
            localConfigData = "#ENABLE_PLAYER_GYRO#LEVEL_00#PLAYER_01";
#elif ENABLE_PLAYER_NOARCORE
            Debug.LogError("++++USING CONFIG::ENABLE_PLAYER_NOARCORE");
            localConfigData = "#ENABLE_PLAYER_NOARCORE#LEVEL_00#PLAYER_00";
#elif ENABLE_DIRECTOR_JOIN
            Debug.LogError("++++USING CONFIG::ENABLE_DIRECTOR_JOIN");
            localConfigData = "#ENABLE_DIRECTOR_JOIN#LEVEL_00#PLAYER_00";
#elif ENABLE_SPECTATOR
            Debug.LogError("++++USING CONFIG::ENABLE_SPECTATOR");
            localConfigData = "#ENABLE_SPECTATOR#LEVEL_00#PLAYER_00";
#endif

#if ENABLE_SOCKET
            localConfigData += "#ENABLE_SOCKET_localhost_7890_";
#endif

#if ENABLE_LOGIN
            localConfigData += "#LOGIN_esteban@yourvrexperience.com_mierda";
#endif

            m_configData = localConfigData;
            ParseConfigData(m_configData);

            ParseLogin(m_configData);

            InitializeSecondPhase(delayShortcutSplash);

#if ENABLE_OCULUS || ENABLE_WORLDSENSE
            KeysEventInputController.Instance.EnableActionOnMouseDown = false;
            UIEventController.Instance.DispatchUIEvent(EventSystemController.EVENT_EVENTSYSTEMCONTROLLER_RAYCASTING_SYSTEM, false);
#endif
        }

        // -------------------------------------------
        /* 
        * InitializeSecondPhase
        */
        protected virtual void InitializeSecondPhase(float _delayShortcutSplash)
        {
            if (m_enablePlayerWorldsense)
            {
                m_runUpdate = false;
                Invoke("Direct2PlayerGame", _delayShortcutSplash);
            }
            else
            if (m_enablePlayerARCore)
            {
                m_runUpdate = false;
                Invoke("JoinAsOtherPlayerGame_ARCore", _delayShortcutSplash);
            }
            else
            if (m_enablePlayerGyro)
            {
                m_runUpdate = false;
                Invoke("JoinAsOtherPlayerGame_Gyro", _delayShortcutSplash);
            }
            else
            if (m_enablePlayerNoARCore)
            {
                m_runUpdate = false;
                Invoke("JoinAsOtherPlayerGame_NoARCore", _delayShortcutSplash);
            }
            else
            if (m_enableDirectorJoin || m_enableSpectator)
            {
                m_runUpdate = false;
                Invoke("JoinAsDirectorGame", _delayShortcutSplash);
            }
            else
            {
                if (!m_isThereButtons)
                {
                    m_runUpdate = true;
                    StartCoroutine(ShowSplashDelay());
                }
            }
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
            ActionsPostConfirmation();
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
            MultiplayerConfiguration.SaveEnableBackground(!m_enableAugmented);
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
            ActionsPostConfirmation();
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
            MultiplayerConfiguration.SaveEnableBackground(!m_enableAugmented);
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
            ActionsPostConfirmation();
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
            ActionsPostConfirmation();
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
            ActionsPostConfirmation();
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenLoadingView.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
        }

        // -------------------------------------------
        /* 
		 * ActionsPostConfirmation
		 */
        protected virtual void ActionsPostConfirmation()
        {
            if (!m_enablePlayerGyro)
            {
                PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.NONE);
            }
            else
            {
                PlayerPrefs.SetInt(YourVRUI.YourVRUIScreenController.DEFAULT_YOURVUI_CONFIGURATION, (int)YourVRUI.CONFIGURATIONS_YOURVRUI.CONFIGURATION_COMPUTER_RAYCAST_NORMAL_SCREENS);
            }
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
        public IEnumerator ShowSplashDelay()
        {
            yield return new WaitForSeconds(
#if UNITY_EDITOR
                0.2f
#else
                2
#endif
                );
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
		 * CustomInitializeWithShortCut
		 */
        protected virtual void CustomInitializeWithShortCut(string _data)
        {
            m_runUpdate = true;
            InitializeWithShortcut(_data);
        }

        // -------------------------------------------
        /* 
		 * OnBasicSystemEvent
		 */
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
#if ENABLE_VALIDATION
            if (_nameEvent == VRPartyValidationController.EVENT_VRPARTYVALIDATION_RESPONSE_ACCESS)
            {
                if ((bool)_list[0])
                {
                    if ((long)_list[1] > 0)
                    {
                        CustomInitializeWithShortCut((string)_list[2]);
                    }
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
        * CustomUpdate
        */
        protected virtual void CustomUpdate()
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

        // -------------------------------------------
        /* 
        * Update
        */
        public void Update()
        {
            CustomUpdate();
        }
    }
}