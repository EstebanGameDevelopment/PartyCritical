using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;
#if ENABLE_VALIDATION
using VRPartyValidation;
#endif

namespace PartyCritical
{
    /******************************************
     * 
     * GameBaseController
     * 
     * @author Esteban Gallardo
     */
    public class GameBaseController : ScreenController
	{
        public const bool DEBUG = false;

        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_GAMECONTROLLER_CHANGE_STATE               = "EVENT_GAMECONTROLLER_CHANGE_STATE";
        public const string EVENT_GAMECONTROLLER_PLAYER_IS_READY            = "EVENT_GAMECONTROLLER_PLAYER_IS_READY";
        public const string EVENT_GAMECONTROLLER_ENEMIES_DISABLE_AUTO_SPAWN = "EVENT_GAMECONTROLLER_ENEMIES_DISABLE_AUTO_SPAWN";
        public const string EVENT_GAMECONTROLLER_CREATE_NEW_ENEMY           = "EVENT_GAMECONTROLLER_CREATE_NEW_ENEMY";
        public const string EVENT_GAMECONTROLLER_SELECTED_LEVEL             = "EVENT_GAMECONTROLLER_SELECTED_LEVEL";
        public const string EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL   = "EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL";
        public const string EVENT_GAMECONTROLLER_CONFIRMATION_NEXT_LEVEL    = "EVENT_GAMECONTROLLER_CONFIRMATION_NEXT_LEVEL";
        public const string EVENT_GAMECONTROLLER_CONFIRMATION_RELOAD_LEVEL  = "EVENT_GAMECONTROLLER_CONFIRMATION_RELOAD_LEVEL";        
        public const string EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD       = "EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD";
        public const string EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED       = "EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED";
        public const string EVENT_GAMECONTROLLER_MARKER_BALL                = "EVENT_GAMECONTROLLER_MARKER_BALL";
        public const string EVENT_GAMECONTROLLER_REQUEST_IS_GAME_RUNNING    = "EVENT_GAMECONTROLLER_REQUEST_IS_GAME_RUNNING";
        public const string EVENT_GAMECONTROLLER_RESPONSE_IS_GAME_RUNNING   = "EVENT_GAMECONTROLLER_RESPONSE_IS_GAME_RUNNING";
        public const string EVENT_GAMECONTROLLER_SELECT_SKYBOX              = "EVENT_GAMECONTROLLER_SELECT_SKYBOX";
        public const string EVENT_GAMECONTROLLER_CREATE_FX                  = "EVENT_GAMECONTROLLER_CREATE_FX";
        public const string EVENT_GAMECONTROLLER_LAYOUT_TOTALLY_LOADED      = "EVENT_GAMECONTROLLER_LAYOUT_TOTALLY_LOADED";
        public const string EVENT_GAMECONTROLLER_REFRESH_STATES_SWITCHES    = "EVENT_GAMECONTROLLER_REFRESH_STATES_SWITCHES";
        public const string EVENT_GAMECONTROLLER_RECALCULATE_COLLISIONS     = "EVENT_GAMECONTROLLER_RECALCULATE_COLLISIONS";
        public const string EVENT_GAMECONTROLLER_PARTY_OVER                 = "EVENT_GAMECONTROLLER_PARTY_OVER";
        public const string EVENT_GAMECONTROLLER_DIRECTOR_CONNECTED         = "EVENT_GAMECONTROLLER_DIRECTOR_CONNECTED";

        public const string SUBEVENT_CONFIRMATION_GO_TO_NEXT_LEVEL = "SUBEVENT_CONFIRMATION_GO_TO_NEXT_LEVEL";

        // ----------------------------------------------
        // CONSTANTS
        // ----------------------------------------------	
        public const int STATE_CONNECTING = 0;
        public const int STATE_LEVEL_LOAD = 1;
        public const int STATE_LOADING = 2;
        public const int STATE_REPOSITION = 3;
        public const int STATE_RUNNING = 4;
        public const int STATE_LEVEL_END = 5;
        public const int STATE_NULL = -1000;

        // ----------------------------------------------
        // PUBLIC MEMBERS
        // ----------------------------------------------	
        public GameObject CloudAnchorReference;
        public GameObject LevelContainer;
        public GameObject LevelReference;
        public string[] LevelsAssetsNames;
        public GameObject[] LevelsPrefab;
        public GameObject[] PlayerPrefab;
        public string[] NameModelPrefab;
        public GameObject[] ModelPrefab;
        public GameObject[] EnemyPrefab;
        public string[] EnemyModelPrefab;
        public GameObject[] EnemyAnimationPrefab;
        public GameObject[] NPCPrefab;
        public string[] NPCModelPrefab;
        public GameObject[] FXPrefab;
        public GameObject[] ShootPrefab;
        public GameObject Floor;
        public GameObject[] Shotgun;
        public GameObject RepositionBall;
        public GameObject DirectorScreen;
        public GameObject SpectatorScreen;
        public GameObject PlayerScreen;
        public GameObject LaserPointer;
        public GameObject MarkerPlayer;
        public GameObject MarkerDirector;
        public TextAsset PathfindingData;
        public Material[] SkyboxesLevels;
        public string[] SoundsLevels;
        public GameObject[] GenericObjects;

        // ----------------------------------------------
        // protected MEMBERS
        // ----------------------------------------------	
        protected string m_namePlayer = "";
        protected string m_className = "";
        protected StateManager m_stateManager;
        protected List<GameObject> m_players = new List<GameObject>();
        protected List<GameObject> m_enemies = new List<GameObject>();
        protected List<GameObject> m_npcs = new List<GameObject>();
        protected List<int> m_playersReady = new List<int>();
        protected List<int> m_repositionedPlayers = new List<int>();
        protected List<int> m_endLevelConfirmedPlayers = new List<int>();
        protected int m_totalNumberPlayers = -1;
        protected int m_totalNumberOfLevels = -1;

        protected List<Vector3> m_positionsSpawn = new List<Vector3>();

        protected GameObject m_level;
        protected int m_currentLevel = -1;
        protected bool m_positionReferenceInited = false;
        protected Vector3 m_positionReference;
        protected int m_characterSelected;

        protected bool m_isInitialConnectionEstablished = false;
        protected bool m_onNetworkRemoteConnection = false;

        protected float m_timeGenerationEnemies = 0;
        protected float m_timoutTotalToGenerateEnemy = -1;
        protected bool m_isCreatorGame = false;
        protected bool m_enableARCore = false;
        protected bool m_directorMode = false;
        protected bool m_spectatorMode = false;
        protected bool m_enableEnemyAutoGeneration = false;
        protected bool m_enableBackgroundVR = true;

        protected bool m_isFirstTimeRun = true;

        protected string m_currentMelody = "";

        // ----------------------------------------------
        // GETTERS/SETTERS
        // ----------------------------------------------	
        public string NamePlayer
        {
            get { return m_namePlayer; }
        }
        public string ClassName
        {
            get { return m_className; }
        }
        public int TotalNumberPlayers
        {
            get { return m_totalNumberPlayers; }
            set { m_totalNumberPlayers = value; }
        }
        public List<GameObject> Players
        {
            get { return m_players; }
        }
        public bool IsCreatorGame
        {
            get { return m_isCreatorGame; }
        }
        public bool DirectorMode
        {
            get { return m_directorMode; }
        }
        public bool SpectatorMode
        {
            get { return m_spectatorMode; }
        }
        public bool EnableARCore
        {
            get { return m_enableARCore; }
        }
        public bool EnableBackgroundVR
        {
            get { return m_enableBackgroundVR; }
        }
        public bool EnableEnemyAutoGeneration
        {
            get { return m_enableEnemyAutoGeneration; }
        }
        public virtual GameObject CurrentGameCamera
        {
            get { return null; }
        }
        public virtual bool EnableVR
        {
            get { return false; }
        }
        public bool IsRealDirectorMode
        {
            get
            {
                if (m_spectatorMode)
                {
                    return false;
                }
                else
                {
                    return m_directorMode;
                }
            }
        }


        // -------------------------------------------
        /* 
		* Awake
		*/
        public override void Awake()
		{
#if !ENABLE_OCULUS
			Screen.orientation = ScreenOrientation.LandscapeLeft;
			Screen.autorotateToLandscapeRight = false;
			Screen.autorotateToLandscapeLeft = false;
			Screen.autorotateToPortrait = false;
#else
            ScreenOculusControlSelectionView.ShouldCheckTheHandControl = true;
#endif
        }

		// -------------------------------------------
		/* 
		* Initialitzation
		*/
		public override void Start()
		{
            base.Start();

            // ADD STATE MANAGER
            m_stateManager = this.gameObject.GetComponent<StateManager>();
            if (m_stateManager == null)
            {
                m_stateManager = this.gameObject.AddComponent<StateManager>();
            }

            // INIT STATE
            SetState(STATE_CONNECTING);

			NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);

            m_totalNumberPlayers = MultiplayerConfiguration.LoadNumberOfPlayers();
            m_totalNumberOfLevels = MultiplayerConfiguration.LoadTotalNumberOfLevels();
            m_directorMode = (MultiplayerConfiguration.LoadDirectorMode(-1) == MultiplayerConfiguration.DIRECTOR_MODE_ENABLED);
            m_spectatorMode = (MultiplayerConfiguration.LoadSpectatorMode(-1) == MultiplayerConfiguration.SPECTATOR_MODE_ENABLED);
            m_isCreatorGame = (NetworkEventController.Instance.MenuController_LoadNumberOfPlayers() != MultiplayerConfiguration.VALUE_FOR_JOINING);

#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
            CloudAnchorReference.SetActive(true);
            m_enableARCore = (MultiplayerConfiguration.LoadGoogleARCore(-1) == MultiplayerConfiguration.GOOGLE_ARCORE_ENABLED);
            m_enableBackgroundVR = MultiplayerConfiguration.LoadEnableBackground();
            if (!m_enableARCore)
            {
                m_enableBackgroundVR = true;
            }
#else
            if (CloudAnchorReference != null) CloudAnchorReference.SetActive(false);
#endif

            // ARCORE ENABLE
            KeysEventInputController.Instance.Initialization();
			LanguageController.Instance.Initialize();
			SoundsController.Instance.Initialize();

            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);

            if (m_directorMode)
            {
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                if (!m_enableARCore)
                {
                    CloudGameAnchorController.Instance.DisableARCore(false);
                }
                else
                {
                    CloudGameAnchorController.Instance.Initialize();
                    CloudGameAnchorController.Instance.DisableARCore(true);
                }
#endif
            }
            else
            {
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                // Debug.LogError("GOOGLE_ARCORE_ENABLED[" + m_enableARCore + "]::MultiplayerConfiguration.LoadGoogleARCore(-1)=" + MultiplayerConfiguration.LoadGoogleARCore(-1));
                if (m_enableARCore)
                {                    
                    CloudGameAnchorController.Instance.Initialize();
                    CloudGameAnchorController.Instance.DisableARCore(true);

                    if (!m_enableBackgroundVR)
                    {
                        Floor.GetComponent<Renderer>().enabled = false;
                    }
                }
                else
                {
                    CloudGameAnchorController.Instance.DisableARCore(false);
                }
#endif
            }
            CreateLoadingScreen();
		}

        // -------------------------------------------
        /* 
         * OnApplicationFocus
         */
        void OnApplicationFocus(bool hasFocus)
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EVENT_APP_LOST_FOCUS, hasFocus);
        }

        // -------------------------------------------
        /* 
         * OnApplicationPause
         */
        void OnApplicationPause(bool pauseStatus)
        {
            UIEventController.Instance.DispatchUIEvent(ScreenController.EVENT_APP_PAUSED, pauseStatus);
        }

        // -------------------------------------------
        /* 
		 * Remove references
		 */
        public override void Destroy()
		{
            base.Destroy();

			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
			BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
            UIEventController.Instance.UIEvent -= OnUIEvent;
        }

		// -------------------------------------------
		/* 
		 * Destroy
		 */
		void OnDestroy()
		{
			Destroy();
		}

		// -------------------------------------------
		/* 
		 * Create the loading screen
		 */
		protected void CreateLoadingScreen(bool _considerForce = true)
		{
            bool displayScreen = true;
            if (_considerForce)
            {
#if FORCE_GAME
                displayScreen = false;
#endif
            }
            EnableLaserVR(false);
            if (displayScreen)
            {
                UIEventController.Instance.DispatchUIEvent(MenuScreenController.EVENT_FORCE_DESTRUCTION_POPUP);
                UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN, ScreenInformationView.SCREEN_WAIT, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, LanguageController.Instance.GetText("message.loading"), LanguageController.Instance.GetText("message.please.wait"), null, "");
            }
#if ENABLE_WORLDSENSE
            KeysEventInputController.Instance.EnableActionOnMouseDown = true;
#endif
        }

        // -------------------------------------------
        /* 
		 * Create the fit scan image screen
		 */
        protected void CreateFitScanImageScreen()
		{
#if ENABLE_GOOGLE_ARCORE
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);
            if (!EnableVR)
            {
                YourVRUIScreenController.Instance.GameCamera = CloudGameAnchorController.Instance.FirstPersonCamera;
            }
                
			List<PageInformation> pages = new List<PageInformation>();
			pages.Add(new PageInformation(LanguageController.Instance.GetText("message.loading"), LanguageController.Instance.GetText("arcore.message.to.synchronize"), null, null));
            if (!EnableVR)
            {
                YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(ScreenInformationView.SCREEN_FIT_SCAN, pages, 1.5f, -1, true, 1f);
                YourVRUIScreenController.Instance.GameCamera = CloudGameAnchorController.Instance.GameCamera;
            }
#endif
        }

        // -------------------------------------------
        /* 
		 * Create the welcome screen
		 */
        protected void CreateWelcomeScreen()
		{
            UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN, ScreenInformationView.SCREEN_INFORMATION, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.welcome"), null, "");
        }

        // -------------------------------------------
        /* 
		 * Create the welcome screen
		 */
        protected virtual void UpdateSkyboxWithNewLevel()
        {
            if (SkyboxesLevels != null)
            {
                if (m_currentLevel < SkyboxesLevels.Length)
                {
                    if (SkyboxesLevels[m_currentLevel] != null)
                    {
                        RenderSettings.skybox = SkyboxesLevels[m_currentLevel];
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		 * Create the welcome screen
		 */
        protected virtual void PlayerSoundWithNewLevel()
        {
            if (SoundsLevels != null)
            {
                if (m_currentLevel < SoundsLevels.Length)
                {
                    if (SoundsLevels[m_currentLevel] != null)
                    {
                        string nextMelody = SoundsLevels[m_currentLevel];
                        if (nextMelody != m_currentMelody)
                        {
                            SoundsController.Instance.StopAllSounds();
                            m_currentMelody = nextMelody;
                            SoundsController.Instance.PlaySoundLoop(AssetbundleController.Instance.CreateAudioclip(m_currentMelody));
                        }
                    }
                }
                else
                {
                    if (SoundsLevels.Length == 1)
                    {
                        string nextMelody = SoundsLevels[0];
                        if (nextMelody != m_currentMelody)
                        {
                            SoundsController.Instance.StopAllSounds();
                            m_currentMelody = nextMelody;
                            SoundsController.Instance.PlaySoundLoop(AssetbundleController.Instance.CreateAudioclip(m_currentMelody));
                        }                        
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		 * Load level assets and data
		 */
        protected virtual void LoadCurrentGameLevel(int _level = -1, int _nextState = -1)
        {
            SetState(STATE_LEVEL_LOAD);

            DestroyCurrentGameLevel();

            if (_level != -1)
            {
                m_currentLevel = _level;
            }
            if (m_level != null)
            {
                GameObject.Destroy(m_level);
                m_level = null;
            }

            if (m_currentLevel != -1)
            {
                if (m_level == null)
                {
#if ENABLE_ASSET_BUNDLE
                    bool loadLevelFromAssetBundle = true;
#if UNITY_EDITOR

                    if ((m_currentLevel < LevelsPrefab.Length) && (LevelsPrefab[m_currentLevel] != null))
                    {
                        m_currentLevel = m_currentLevel % LevelsPrefab.Length;
                        m_level = Utilities.AddChild(LevelContainer.transform, LevelsPrefab[m_currentLevel]);
                        loadLevelFromAssetBundle = false;
                    }
#endif
                    if (loadLevelFromAssetBundle)
                    {
                        m_currentLevel = m_currentLevel % LevelsAssetsNames.Length;
                        m_level = AssetbundleController.Instance.CreateGameObject(LevelsAssetsNames[m_currentLevel]);
                    }
#else
                    m_currentLevel = m_currentLevel % LevelsPrefab.Length;
                    m_level = Utilities.AddChild(LevelContainer.transform, LevelsPrefab[m_currentLevel]);
#endif

                    m_level.transform.SetParent(LevelContainer.transform, false);
                    if (!m_positionReferenceInited)
                    {
                        m_positionReferenceInited = true;
                        m_positionReference = Utilities.Clone(LevelReference.transform.position);
                        GameObject.Destroy(LevelReference);
                    }
                    m_level.transform.position = m_positionReference;

#if ENABLE_RELOCATE_LEVEL && !UNITY_EDITOR
                    if (EnableARCore)
                    {
                        m_level.transform.position = new Vector3(10000, 10000, 10000);
                    }
#endif

                    // Debug.LogError("NEW LEVEL LOADED[" + m_level.name + "]!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    // WE SEND A MESSAGE THAT THE LEVEL HAS BEEN LOADED WHEN EVERYTHING HAS BEEN INITIALIZED WITH "START"
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED, 0.2f, m_currentLevel, _nextState);
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(CloudGameAnchorController.EVENT_6DOF_CHANGED_LEVEL_COMPLETED, 0.2f);
                }
            }
        }

        // -------------------------------------------
        /* 
        * DestroyCurrentGameLevel
        */
        protected virtual void DestroyCurrentGameLevel()
        {
            /*
            GameEnemy[] enemies = GameObject.FindObjectsOfType<GameEnemy>();
            for (int i = 0; i < enemies.Length; i++)
            {
                ((GameEnemy)enemies[i]).Destroy();
            }

            // RUN SHOOTS FOR ALL
            GameShoot[] shoots = GameObject.FindObjectsOfType<GameShoot>();
            for (int i = 0; i<shoots.Length; i++)
            {
                GameObject.Destroy(shoots[i]);
            } 
            */
        }


        // -------------------------------------------
        /* 
		 * EnableLaserVR
		 */
        protected virtual void EnableLaserVR(bool _enable)
        {
#if ENABLE_WORLDSENSE || ENABLE_OCULUS
            if (LaserPointer!=null) LaserPointer.SetActive(_enable);
#else
            if (LaserPointer!=null) LaserPointer.SetActive(false);
#endif
        }

        // -------------------------------------------
        /* 
		 * PostDestroyScreenAction
		 */
        protected virtual void PostDestroyScreenAction()
        {
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE
            if (!m_directorMode)
            {
                if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
                {
                    KeysEventInputController.Instance.EnableInteractions = true;
                    KeysEventInputController.Instance.EnableActionButton = true;
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION, true);
                    UIEventController.Instance.DispatchUIEvent(InteractionController.EVENT_INTERACTIONCONTROLLER_ENABLE_INTERACTION, true);
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
		 * ProcessCustomUIScreenEvent
		 */
        protected virtual void ProcessCustomUIScreenEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN)
            {
                EnableLaserVR(true);

                if (_list.Length > 2)
                {
                    if ((bool)_list[2])
                    {
                        YourVRUIScreenController.Instance.DestroyScreens();
                    }
                    else
                    {
                        YourVRUIScreenController.Instance.EnableScreens = true;
                    }
                }
                object pages = null;
                if (_list.Length > 3)
                {
                    pages = _list[3];
                }
                float scaleScreen = -1;
                if (_list.Length > 4)
                {
                    if (_list[4] is float)
                    {
                        scaleScreen = (float)_list[4];
                    }
                }
                bool isTemporalScreen = true;
                if (_list.Length > 5)
                {
                    if (_list[5] is bool)
                    {
                        isTemporalScreen = (bool)_list[5];
                    }
                }
                GameObject prefabScreen = GetScreenPrefabByName((string)_list[0]);
                YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(prefabScreen, pages, 1.5f, -1, false, scaleScreen, (UIScreenTypePreviousAction)_list[1], isTemporalScreen);
                // AUTO-DESTROY THE POP UP WHEN YOU ARE NOT INTERESTED TO OFFER INTERACTION
                // UIEventController.Instance.DelayUIEvent(ScreenController.EVENT_FORCE_TRIGGER_OK_BUTTON, 5);
                if ((string)_list[0] == ScreenCreateRoomView.SCREEN_NAME)
                {
                    UIEventController.Instance.DispatchUIEvent(ScreenCreateRoomView.EVENT_SCREENCREATEROOM_CREATE_RANDOM_NAME);
                }
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE
                if (!m_directorMode)
                {
                    if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
                    {
                        KeysEventInputController.Instance.EnableInteractions = false;
                        KeysEventInputController.Instance.EnableActionButton = false;
                        BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION, false);
                        UIEventController.Instance.DispatchUIEvent(InteractionController.EVENT_INTERACTIONCONTROLLER_ENABLE_INTERACTION, false);
                    }
                }
#endif
            }
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN)
            {
                EnableLaserVR(true);

                string nameScreen = (string)_list[0];
                UIScreenTypePreviousAction previousAction = (UIScreenTypePreviousAction)_list[1];
                string title = (string)_list[2];
                string description = (string)_list[3];
                Sprite image = (Sprite)_list[4];
                string eventData = (string)_list[5];
                float scaleScreen = -1;
                if (_list.Length > 6)
                {
                    if (_list[6] is float)
                    {
                        scaleScreen = (float)_list[6];
                    }
                }
                List<PageInformation> pages = new List<PageInformation>();
                pages.Add(new PageInformation(title, description, image, eventData, "", ""));
                YourVRUIScreenController.Instance.CreateScreenLinkedToCamera(GetScreenPrefabByName((string)_list[0]), pages, 1.5f, -1, false, scaleScreen, previousAction, 0);
                // AUTO-DESTROY THE POP UP WHEN YOU ARE NOT INTERESTED TO OFFER INTERACTION
                // UIEventController.Instance.DelayUIEvent(ScreenController.EVENT_FORCE_TRIGGER_OK_BUTTON, 5);
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE
                if (!m_directorMode)
                {
                    if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
                    {
                        KeysEventInputController.Instance.EnableInteractions = false;
                        KeysEventInputController.Instance.EnableActionButton = false;
                        BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_ENABLE_INPUT_INTERACTION, false);
                        UIEventController.Instance.DispatchUIEvent(InteractionController.EVENT_INTERACTIONCONTROLLER_ENABLE_INTERACTION, false);
                    }
                }
#endif
            }
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_LOAD_NEW_SCENE)
            {
                if (YourVRUIScreenController.Instance != null)
                {
                    YourVRUIScreenController.Instance.Destroy();
                }
            }
            if ((_nameEvent == UIEventController.EVENT_SCREENMANAGER_DESTROY_SCREEN) || (_nameEvent == UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN))
            {
                PostDestroyScreenAction();
            }
        }

        // -------------------------------------------
        /* 
        * OnNetworkEventEnemy
        */
        protected virtual void OnNetworkEventEnemy(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
        {
            if (_nameEvent == EVENT_GAMECONTROLLER_ENEMIES_DISABLE_AUTO_SPAWN)
            {
                m_enableEnemyAutoGeneration = bool.Parse((string)_list[0]);
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_CREATE_NEW_ENEMY)
            {
                if (YourNetworkTools.Instance.IsServer)
                {
                    float x = float.Parse((string)_list[0]);
                    float y = float.Parse((string)_list[1]);
                    float z = float.Parse((string)_list[2]);
                    CreateNewEnemy(new Vector3(x, y, z));
                }
            }
        }

        // -------------------------------------------
        /* 
        * FinallyLoadLevel
        */
        protected void FinallyLoadLevel()
        {
            if (m_onNetworkRemoteConnection && m_isInitialConnectionEstablished)
            {
                if (!m_isCreatorGame)
                {
                    Debug.LogError("--RECEIVED--::EVENT_GAMECONTROLLER_SELECTED_LEVEL::_level=" + m_currentLevel);
                    if (m_stateManager.State == STATE_CONNECTING)
                    {
                        LoadCurrentGameLevel(m_currentLevel);
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
        * OnNetworkEventInitialConnection
        */
        protected virtual void OnNetworkEventInitialConnection()
        {
            m_isInitialConnectionEstablished = true;
            m_isCreatorGame = YourNetworkTools.Instance.IsServer;
            if (m_isCreatorGame)
            {
#if ENABLE_VALIDATION
                VRPartyValidationController.Instance.ResetConnectedMac();
#endif
                LoadCurrentGameLevel();
            }
            else
            {
                FinallyLoadLevel();
            }            
        }

        // -------------------------------------------
        /* 
        * OnNetworkEventRemoteConnection
        */
        protected virtual void OnNetworkEventRemoteConnection()
        {
            if (m_isInitialConnectionEstablished)
            {
                if (m_isCreatorGame)
                {
                    Debug.LogError("++SENDING++::EVENT_GAMECONTROLLER_SELECTED_LEVEL::m_currentLevel=" + m_currentLevel);
                    NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_GAMECONTROLLER_SELECTED_LEVEL, 0.1f, m_currentLevel.ToString());
                }
            }
        }

        // -------------------------------------------
        /* 
        * LoadSelectedLevel
        */
        protected virtual void LoadSelectedLevel(int _level)
        {
            m_currentLevel = _level;
#if ENABLE_ASSET_BUNDLE
            m_currentLevel = m_currentLevel % LevelsAssetsNames.Length;
#else
            m_currentLevel = m_currentLevel % LevelsPrefab.Length;
#endif
            m_onNetworkRemoteConnection = true;
            if (m_isInitialConnectionEstablished)
            {
                FinallyLoadLevel();
            }
        }

        // -------------------------------------------
        /* 
        * PlayerReadyConfirmation
        */
        protected virtual void PlayerReadyConfirmation(params object[] _list)
        {
#if !FORCE_GAME
            if (YourNetworkTools.Instance.IsServer)
            {
                int networkID = int.Parse((string)_list[0]);
                bool isDirector = bool.Parse((string)_list[1]);
                if (m_playersReady.IndexOf(networkID) == -1) m_playersReady.Add(networkID);
                // Debug.LogError("EVENT_SYSTEM_INITIALITZATION_REMOTE_COMPLETED::CONNECTED CLIENT[" + networkID + "] OF TOTAL["+ m_playersReady.Count + "] of EXPECTED[" + m_totalNumberPlayers +"]");
#if SINGLE_PLAYER
                m_totalNumberPlayers = 1;
#endif
                if ((isDirector) || ((m_totalNumberPlayers <= m_playersReady.Count) && (m_totalNumberPlayers != MultiplayerConfiguration.VALUE_FOR_JOINING)))
                {
                    m_totalNumberPlayers = m_playersReady.Count;
#if ENABLE_CONFUSION
                        NetworkEventController.Instance.DelayLocalEvent(ClientTCPEventsController.EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM, 0.2f);
#else
                    NetworkEventController.Instance.PriorityDelayNetworkEvent(ClientTCPEventsController.EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM, 0.2f);
#endif

                    StartRunningGame();
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
        * StartRunningGame
        */
        public virtual void StartRunningGame()
        {
            // Debug.LogError("EVENT_SYSTEM_INITIALITZATION_REMOTE_COMPLETED::START RUNNING***********************************");
#if FORCE_REPOSITION
                        ChangeState(STATE_REPOSITION);
#else
            ChangeState(STATE_RUNNING);
#endif
        }

        // -------------------------------------------
        /* 
        * AddNetworkObjectComponent
        */
        public void AddNetworkObjectComponent(GameObject _go, string _name, string _visualName, int _networkID, string _mode)
        {
            // ADDED NETWORKED FUNCTIONALITY TO KEEP THE SYNCRONIZATION
            if (_go.GetComponent<NetworkedObject>() == null)
            {
                this.transform.gameObject.AddComponent<NetworkedObject>();
                this.transform.GetComponent<NetworkedObject>().Name = _name;
                this.transform.GetComponent<NetworkedObject>().VisualsName = _visualName;
                this.transform.GetComponent<NetworkedObject>().NetIDOwner = _networkID;
                this.transform.GetComponent<NetworkedObject>().Params = _mode;
                Debug.LogError("AddNetworkObjectComponent::NAME[" + _name + "]");
            }
        }

        // -------------------------------------------
        /* 
        * InitializeNetworkedObject
        */
        protected void InitializeNetworkedObject()
        {
            NetworkedObject[] networkedObjects = GameObject.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < networkedObjects.Length; i++)
            {
                networkedObjects[i].Initialize();
            }
        }

        // -------------------------------------------
        /* 
        * FindNetworkedObject
        */
        protected NetworkedObject FindNetworkedObject(string _name)
        {
            NetworkedObject[] networkedObjects = GameObject.FindObjectsOfType<NetworkedObject>();
            for (int i = 0; i < networkedObjects.Length; i++)
            {
                if (networkedObjects[i].Name == _name)
                {
                    return networkedObjects[i];
                }
            }
            return null;
        }

        // -------------------------------------------
        /* 
        * ActionsWhenRoomClosed
        */
        protected virtual void ActionsWhenRoomClosed()
        {
            UIEventController.Instance.DispatchUIEvent(MenuScreenController.EVENT_FORCE_DESTRUCTION_POPUP);
        }
        
        // -------------------------------------------
        /* 
        * Manager of global events
        */
        protected virtual void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
            if (_nameEvent == NetworkedObject.EVENT_NETWORKED_REQUEST_EXISTANCE)
            {
                if ((_networkOriginID != YourNetworkTools.Instance.GetUniversalNetworkID()) || YourNetworkTools.Instance.IsServer)
                {
                    string recvName = (string)_list[0];
                    NetworkedObject networkedObject = FindNetworkedObject(recvName);
                   
                    if (networkedObject == null)
                    {
                        NetworkEventController.Instance.DispatchNetworkEvent(NetworkedObject.EVENT_NETWORKED_RESPONSE_EXISTANCE, recvName, false.ToString());
                    }
                    else
                    {
                        if (networkedObject.NetIDOwner == -1)
                        {
                            NetworkEventController.Instance.DispatchNetworkEvent(NetworkedObject.EVENT_NETWORKED_RESPONSE_EXISTANCE, recvName, false.ToString());
                        }
                        else
                        {
                            NetworkEventController.Instance.DispatchNetworkEvent(NetworkedObject.EVENT_NETWORKED_RESPONSE_EXISTANCE, recvName, true.ToString(), networkedObject.NetIDOwner.ToString());
                        }
                    }
                }
            }
            if (_nameEvent == NetworkedObject.EVENT_NETWORKED_OBJECT_UPDATE)
            {
                string recvName = (string)_list[0];
                if (_networkOriginID != YourNetworkTools.Instance.GetUniversalNetworkID())
                {
                    NetworkedObject objectInScene = FindNetworkedObject((string)_list[0]);
                    if (objectInScene == null)
                    {
                        // 1 - CREATE PREFAB
                        // 2 - AddNetworkObjectComponent();
                    }
                    else
                    {
                        objectInScene.NetIDOwner = _networkOriginID;
                    }
                }
            }
            if (_nameEvent == ClientTCPEventsController.EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM)
            {
                ActionsWhenRoomClosed();
            }
            if (_nameEvent == CloudGameAnchorController.EVENT_6DOF_REQUEST_LEVEL_NUMBER)
            {
                NetworkEventController.Instance.PriorityDelayNetworkEvent(CloudGameAnchorController.EVENT_6DOF_RESPONSE_LEVEL_NUMBER, 0.1f, m_currentLevel.ToString(), m_totalNumberOfLevels.ToString());
            }
            if (_nameEvent == CloudGameAnchorController.EVENT_6DOF_CHANGE_LEVEL)
            {
                int nextLevelToLoad = int.Parse((string)_list[0]);
                if (m_currentLevel != nextLevelToLoad)
                {
                    CreateLoadingScreen();
                    if (YourNetworkTools.Instance.IsServer)
                    {
                        m_currentLevel = nextLevelToLoad;
                        NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD, 0.5f, m_currentLevel.ToString());
                    }
                }
                else
                {
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(CloudGameAnchorController.EVENT_6DOF_CHANGED_LEVEL_COMPLETED, 0.2f);
                }
            }
            if (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_LOCAL_COMPLETED)
			{
                OnNetworkEventInitialConnection();
            }
            if (_nameEvent == NetworkEventController.EVENT_SYSTEM_INITIALITZATION_REMOTE_COMPLETED)
            {
                OnNetworkEventRemoteConnection();
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_CONFIRMATION_RELOAD_LEVEL)
            {
                if (YourNetworkTools.Instance.IsServer)
                {
                    m_repositionedPlayers.Clear();
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD, m_currentLevel.ToString());
                }
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_CONFIRMATION_NEXT_LEVEL)
            {
                if (YourNetworkTools.Instance.IsServer)
                {
                    /*
                    int networkID = int.Parse((string)_list[0]);
                    if (m_endLevelConfirmedPlayers.IndexOf(networkID) == -1) m_endLevelConfirmedPlayers.Add(networkID);
                    if (m_endLevelConfirmedPlayers.Count >= m_players.Count)
                    {
                        m_currentLevel++;
                        NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD, m_currentLevel.ToString());
                    }
                    */
                    m_currentLevel++;
                    // m_currentLevel = 2;
                    m_repositionedPlayers.Clear();
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD, m_currentLevel.ToString());
                }
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD)
            {
                m_currentLevel = int.Parse((string)_list[0]);

                int nextStateAfterCreateInstanceLevel = STATE_RUNNING;
#if FORCE_REPOSITION
                    nextStateAfterCreateInstanceLevel = STATE_REPOSITION;
#else
                    nextStateAfterCreateInstanceLevel = STATE_RUNNING;
#endif

                if (m_currentLevel >= LevelsAssetsNames.Length)
                {
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED, 0.2f, m_currentLevel, nextStateAfterCreateInstanceLevel);
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(CloudGameAnchorController.EVENT_6DOF_CHANGED_LEVEL_COMPLETED, 0.2f);
                    UIEventController.Instance.DelayUIEvent(EventSystemController.EVENT_ACTIVATION_INPUT_STANDALONE, 1f, true);
                }
                else
                {
                    LoadCurrentGameLevel(-1, nextStateAfterCreateInstanceLevel);
                    UIEventController.Instance.DelayUIEvent(EventSystemController.EVENT_ACTIVATION_INPUT_STANDALONE, 1f, true);
                }
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_SELECTED_LEVEL)
            {
                LoadSelectedLevel(int.Parse((string)_list[0]));
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_PLAYER_IS_READY)
            {
                PlayerReadyConfirmation(_list);
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL)
            {
                int networkID = int.Parse((string)_list[0]);
                if (m_repositionedPlayers.IndexOf(networkID) == -1)
                {
                    m_repositionedPlayers.Add(networkID);
                    if (m_repositionedPlayers.Count == m_playersReady.Count)
                    {
                        ChangeState(STATE_RUNNING);
                    }
                }
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_CHANGE_STATE)
            {
                SetState(int.Parse((string)_list[0]));
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_MARKER_BALL)
            {
                bool isDirector = bool.Parse((string)_list[0]);
                Vector3 posMarker = new Vector3(float.Parse((string)_list[1]), float.Parse((string)_list[2]), float.Parse((string)_list[3]));
                GameObject markerBall = Instantiate(isDirector ? MarkerDirector : MarkerPlayer);
                markerBall.transform.position = posMarker;
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_SELECT_SKYBOX)
            {
                SelectedSkybox(int.Parse((string)_list[0]));
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_CREATE_FX)
            {
                int fxIndex = int.Parse((string)_list[0]);
                float timeDestroy = float.Parse((string)_list[1]);
                Vector3 posFX =  Utilities.StringToVector3((string)_list[2]);
                Vector3 scaleFX = Utilities.StringToVector3((string)_list[3]);
                CreateNewFX(fxIndex, timeDestroy, posFX, scaleFX);
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_PARTY_OVER)
            {
                SoundsController.Instance.StopAllSounds();
                YourVRUIScreenController.Instance.DestroyScreens();
                KeysEventInputController.Instance.EnableInteractions = false;
                UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_GENERIC_SCREEN, ScreenPartyOver.SCREEN_NAME, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, false, null);
                Invoke("KillGameParty", 10);
            }

            OnNetworkEventEnemy(_nameEvent, _isLocalEvent, _networkOriginID, _networkTargetID, _list);
        }
        
        // -------------------------------------------
        /* 
        * KillGameParty
        */
        public void KillGameParty()
        {
            Debug.LogError("KILLING APP+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Application.Quit();
        }

        // -------------------------------------------
        /* 
        * GameHasLoadedLevel
        */
        protected virtual void GameHasLoadedLevel(object[] _list)
        {
            int nextState = (int)_list[1];
            FindSpawnPositions();
            if (nextState != STATE_NULL)
            {
                if (nextState == -1)
                {
                    SetState(STATE_LOADING);
                }
                else
                {
                    SetState(nextState);
                }
            }
            NetworkEventController.Instance.PriorityDelayNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_REFRESH_STATES_SWITCHES, 0.1f);
        }

        // -------------------------------------------
        /* 
        * OnBasicSystemEvent
        */
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
            if (_nameEvent == CloudGameAnchorController.EVENT_CLOUDGAMEANCHOR_SETUP_ANCHOR)
            {
                if ((bool)_list[0])
                {
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_PLAYER_IS_READY, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), IsRealDirectorMode.ToString());
                }
#if !UNITY_EDITOR
                if (EnableARCore)
                {
                    m_level.transform.position = m_positionReference;
                }
#endif

#if FORCE_GAME
                SetState(STATE_RUNNING);
#elif !UNITY_EDITOR
                CreateLoadingScreen();
#endif
            }
#endif
            if (_nameEvent == CardboardLoaderVR.EVENT_VRLOADER_LOADED_DEVICE_NAME)
            {
                
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL)
            {
                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL, YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED)
            {
                GameHasLoadedLevel(_list);
                InitializeNetworkedObject();
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_REQUEST_IS_GAME_RUNNING)
            {
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_GAMECONTROLLER_RESPONSE_IS_GAME_RUNNING, IsGameFakeRunning());
            }
            if (_nameEvent == EVENT_GAMECONTROLLER_SELECT_SKYBOX)
            {
                SelectedSkybox((int)_list[0]);
            }
        }

        // -------------------------------------------
        /* 
         * ManageTypeScreen
         */
        protected virtual void ManageTypeScreen(string _nameEvent, object[] _list)
        {
            bool isDirector = DirectorMode;
#if UNITY_EDITOR
            isDirector = false;
#endif
            if ((YourVRUIScreenController.Instance == null) || (isDirector))
            {
                ProcessScreenEvents(_nameEvent, _list);
            }
            else
            {
                ProcessCustomUIScreenEvent(_nameEvent, _list);
            }
        }

        // -------------------------------------------
        /* 
         * OnUIEvent
         */
        protected override void OnUIEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == KeysEventInputController.ACTION_BACK_BUTTON)
            {
                Application.Quit();
            }
            if (_nameEvent == ScreenController.EVENT_APP_LOST_FOCUS)
            {
#if ENABLE_WORLDSENSE
                if ((bool)_list[0])
                {
                    Application.Quit();
                }
#endif
            }
            ManageTypeScreen(_nameEvent, _list);

            if (_nameEvent == ScreenController.EVENT_CONFIRMATION_POPUP)
            {
                string subEvent = (string)_list[2];
                if (subEvent == SUBEVENT_CONFIRMATION_GO_TO_NEXT_LEVEL)
                {
                    CreateLoadingScreen(false);
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_CONFIRMATION_NEXT_LEVEL, YourNetworkTools.Instance.GetUniversalNetworkID().ToString());
                }
            }
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_DESTROY_SCREEN)
            {
                EnableLaserVR(false);
            }
        }

        // -------------------------------------------
        /* 
		* SelectedSkybox
		*/
        public void SelectedSkybox(int _skybox)
        {
            int selectedSkybox = _skybox;
            if (SkyboxesLevels != null)
            {
                if (selectedSkybox < SkyboxesLevels.Length)
                {
                    if (SkyboxesLevels[selectedSkybox] != null)
                    {
                        RenderSettings.skybox = SkyboxesLevels[selectedSkybox];
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		* GetClosestPlayer
		*/
        public virtual GameObject GetClosestPlayer(Vector3 _position)
		{
			GameObject target = m_players[0];
			float minDistance = 100000000f;
			for (int i = 0; i < m_players.Count; i++)
			{
				float distanceTarget = Vector3.Distance(m_players[i].gameObject.transform.position, _position);
				if (distanceTarget < minDistance)
				{
					minDistance = distanceTarget;
					target = m_players[i];
				}
			}
			return target;
		}

        // -------------------------------------------
        /* 
		* GetPlayerByNetworkID
		*/
        public GameObject GetPlayerByNetworkID(int _networkID)
        {
            for (int i = 0; i < m_players.Count; i++)
            {
                if (m_players[i].GetComponent<ActorTimeline>().NetworkID.NetID == _networkID)
                {
                    return m_players[i].gameObject;
                }
            }
            return null;
        }

        // -------------------------------------------
        /* 
		* GetEnemyByNetworkID
		*/
        public GameObject GetEnemyByNetworkID(int _uid)
        {
            for (int i = 0; i < m_enemies.Count; i++)
            {
                if (m_enemies[i].GetComponent<ActorTimeline>().NetworkID.UID == _uid)
                {
                    return m_enemies[i].gameObject;
                }
            }
            return null;
        }

        // -------------------------------------------
        /* 
		* FindSpawnPositions
		*/
        protected void FindSpawnPositions()
        {
            ClassFinder[] objectsSpawn = GameObject.FindObjectsOfType<ClassFinder>();
            m_positionsSpawn = new List<Vector3>();
            if (objectsSpawn.Length > 0)
            {
                for (int k = 0; k < objectsSpawn.Length; k++)
                {
                    if (objectsSpawn[k].Name == "SPAWN")
                    {
                        Vector3 spawnPos = Utilities.Clone(objectsSpawn[k].gameObject.transform.position);
#if ENABLE_RELOCATE_LEVEL && !UNITY_EDITOR
                    if (EnableARCore)
                    {
                        spawnPos = spawnPos - new Vector3(10000, 10000, 10000);
                    }
#endif
                        m_positionsSpawn.Add(spawnPos);
                    }
                }
                for (int k = 0; k < objectsSpawn.Length; k++)
                {
                    if (objectsSpawn[k].Name == "SPAWN")
                    {
                        GameObject.Destroy(objectsSpawn[k].gameObject);
                    }                        
                }
                // Debug.LogError("FindSpawnPositions::TOTAL FOUND[" + m_positionsSpawn.Count + "]::TOTAL LEFT["+ GameObject.FindObjectsOfType<ClassFinder>().Length + "]+++++++++++++++++++++++++++++++");
            }
        }


        // -------------------------------------------
        /* 
		 * ChangeState
		 */
        public void ChangeState(int _newState)
        {
            if (YourNetworkTools.Instance.IsServer)
            {
                /*
                if (_newState == STATE_RUNNING)
                {
                    Debug.LogError("CHANGING TO STATE_RUNNING**********************************************");
                    return;
                }
                */
                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_CHANGE_STATE, _newState.ToString());
            }
        }

        // -------------------------------------------
        /* 
		 * SetUpStateConnecting
		 */
        protected virtual void SetUpStateConnecting()
        {
        }

        // -------------------------------------------
        /* 
		 * SetUpInitialGamePlayerData
		 */
        protected virtual string SetUpInitialGamePlayerData(string _initialData)
        {
            string output = "";
            if (m_characterSelected >= NameModelPrefab.Length)
            {
                output = m_namePlayer + "," + "NO_ASSET_BUNDLE" + "," + _initialData;
            }
            else
            {
                output = m_namePlayer + "," + NameModelPrefab[m_characterSelected] + "," + _initialData;
            }
            return output;
        }

        // -------------------------------------------
        /* 
         * SetUpStateLoading
         */
        protected virtual void SetUpStateLoading()
        {
            m_playersReady.Clear();

            int timelineID = YourNetworkTools.Instance.GetUniversalNetworkID();
            if (YourNetworkTools.Instance.IsLocalGame)
            {
#if !ENABLE_CONFUSION
                timelineID = (timelineID - (int)(CommunicationsController.Instance.NetworkID / 2));
#else
                timelineID = (timelineID - (int)(1 / 2));
#endif
            }

            if (m_directorMode)
            {
                SetUpInitialGamePlayerData("");

                m_namePlayer = MultiplayerConfiguration.DIRECTOR_NAME + timelineID;
                if (!m_enableARCore)
                {
                    NetworkEventController.Instance.DelayNetworkEvent(EVENT_GAMECONTROLLER_PLAYER_IS_READY, 0.2f, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), IsRealDirectorMode.ToString());
                    YourNetworkTools.Instance.ActivateTransformUpdate = true;
                }
                else
                {
#if ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                    CloudGameAnchorController.Instance.EnableARCore();
                    if (CardboardLoaderVR.Instance.LoadEnableCardboard())
                    {
                        CreateFitScanImageScreen();
                    }
#endif
                }
#if FORCE_GAME
                        SetState(STATE_RUNNING);
#endif
            }
            else
            {
                m_namePlayer = MultiplayerConfiguration.HUMAN_NAME + timelineID;

                // Debug.LogError("+++++++++++++++++m_positionsSpawn=" + m_positionsSpawn.Count);
                if (m_positionsSpawn.Count == 0)
                {
                    m_positionsSpawn.Add(new Vector3(0, 5, 0));
                }

                Vector3 initialPosition = m_positionsSpawn[YourNetworkTools.Instance.GetUniversalNetworkID() % m_positionsSpawn.Count];
                string initialData = initialPosition.x + "," + initialPosition.y + "," + initialPosition.z;
#if (ENABLE_WORLDSENSE || ENABLE_QUEST) && !UNITY_EDITOR
                        initialData = 0 + "," + initialPosition.y + "," + 0;
#elif ENABLE_GOOGLE_ARCORE && !UNITY_EDITOR
                        if (m_enableARCore)
                        {
                            initialData = 0 + "," + initialPosition.y + "," + 0;
                        }
#endif
                if (m_characterSelected >= PlayerPrefab.Length)
                {
                    m_characterSelected = 0;
                }

                initialData = SetUpInitialGamePlayerData(initialData);


                // TO FORCE REPOSITION ON EDITOR
                // initialData = m_namePlayer + "," + NameModelPrefab[m_characterSelected] + "," + 0 + "," + initialPosition.y + "," + 0;
#if !ENABLE_CONFUSION
                YourNetworkTools.Instance.CreateLocalNetworkObject(PlayerPrefab[m_characterSelected].name, initialData, false);
                YourNetworkTools.Instance.ActivateTransformUpdate = true;
#else
                GameObject myOwnPlayer = Instantiate(PlayerPrefab[m_characterSelected]);
#endif

                YourVRUIScreenController.Instance.DestroyScreens();
                if (!m_enableARCore)
                {
                    CreateLoadingScreen();
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_PLAYER_IS_READY, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), IsRealDirectorMode.ToString());
#if FORCE_GAME
                            SetState(STATE_RUNNING);
#endif

                }
                else
                {
#if UNITY_EDITOR
                    CreateLoadingScreen();
                    NetworkEventController.Instance.DispatchNetworkEvent(EVENT_GAMECONTROLLER_PLAYER_IS_READY, YourNetworkTools.Instance.GetUniversalNetworkID().ToString(), IsRealDirectorMode.ToString());

#elif ENABLE_GOOGLE_ARCORE && !ENABLE_OCULUS
                            CloudGameAnchorController.Instance.EnableARCore();
                            if (CardboardLoaderVR.Instance.LoadEnableCardboard())
                            {
                                CreateFitScanImageScreen();
                            }
#endif
                }
            }
        }

        // -------------------------------------------
        /* 
		 * SetUpStateReposition
		 */
        protected virtual void SetUpStateReposition()
        {
            if (m_directorMode)
            {
                BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL, 0.2f);
            }
            else
            {
                bool createRepositionBall = false;
#if ENABLE_WORLDSENSE
                        createRepositionBall = true;
#else
                // TO FORCE REPOSITION ON EDITOR (COMMENT AND createRepositionBall = true;)
                // createRepositionBall = true;
                if (!m_enableARCore)
                {
                    BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMECONTROLLER_COLLIDED_REPOSITION_BALL, 0.2f);
                }
                else
                {
                    createRepositionBall = true;
                }
#endif

                if (createRepositionBall)
                {
                    Vector3 positionReposition = m_positionsSpawn[YourNetworkTools.Instance.GetUniversalNetworkID() % m_positionsSpawn.Count];
                    GameObject repositionBall = Instantiate(RepositionBall, positionReposition, Quaternion.identity);
                    repositionBall.GetComponent<CollisionTriggerEvent>().TargetObject = CurrentGameCamera;
                    repositionBall.GetComponent<Collider>().enabled = true;
                    UIEventController.Instance.DispatchUIEvent(MenuScreenController.EVENT_FORCE_DESTRUCTION_POPUP);
                    UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN, ScreenInformationView.SCREEN_INFORMATION, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.please.reposition"), null, "");
                }
            }
        }

        // -------------------------------------------
        /* 
		 * InitializeScreenPlayer
		 */
        protected virtual void InitializeScreenPlayer()
        {
#if !ENABLE_OCULUS && !ENABLE_WORLDSENSE
            if (PlayerScreen != null)
            {
                if (!CardboardLoaderVR.Instance.LoadEnableCardboard())
                {
                    Instantiate(PlayerScreen);
                    UIEventController.Instance.DelayUIEvent(EventSystemController.EVENT_ACTIVATION_INPUT_STANDALONE, 1f, true);
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
		 * SetUpStateRunning
		 */
        protected virtual bool SetUpStateRunning()
        {
            UpdateSkyboxWithNewLevel();
            UIEventController.Instance.DispatchUIEvent(MenuScreenController.EVENT_FORCE_DESTRUCTION_POPUP);
            m_endLevelConfirmedPlayers.Clear();
            if (m_directorMode)
            {
                if (m_spectatorMode)
                {
                    if (GameObject.FindObjectOfType<ScreenBaseSpectatorView>() == null)
                    {
                        Instantiate(SpectatorScreen);
                        BasicSystemEventController.Instance.DelayBasicSystemEvent(ScreenBaseSpectatorView.EVENT_SPECTATOR_SET_UP_PLAYERS, 1f, m_players);
                        UIEventController.Instance.DelayUIEvent(EventSystemController.EVENT_ACTIVATION_INPUT_STANDALONE, 1f, true);
                        NetworkEventController.Instance.DispatchNetworkEvent(ActorTimeline.EVENT_GAMEPLAYER_HUMAN_SPECTATOR_NAME, m_namePlayer);
                    }
                }
                else
                {
                    if (GameObject.FindObjectOfType<ScreenBaseDirectorView>() == null)
                    {
                        Instantiate(DirectorScreen);
                        BasicSystemEventController.Instance.DelayBasicSystemEvent(ScreenBaseDirectorView.EVENT_DIRECTOR_SET_UP_PLAYERS, 1f, m_players);
                        UIEventController.Instance.DelayUIEvent(EventSystemController.EVENT_ACTIVATION_INPUT_STANDALONE, 1f, true);
                        NetworkEventController.Instance.DispatchNetworkEvent(ActorTimeline.EVENT_GAMEPLAYER_HUMAN_DIRECTOR_NAME, m_namePlayer);
                    }
                    NetworkEventController.Instance.PriorityDelayNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_DIRECTOR_CONNECTED, 1);                    
                }
                UIEventController.Instance.DelayUIEvent(InteractionController.EVENT_INTERACTIONCONTROLLER_ENABLE_INTERACTION, 2, false);
            }
            else
            {
                InitializeScreenPlayer();
            }
            if (m_directorMode || (m_totalNumberPlayers == 1))
            {
                PlayerSoundWithNewLevel();
            }                
            bool previousStateIsFirstTimeRun = m_isFirstTimeRun;
            m_isFirstTimeRun = false;
            BasicSystemEventController.Instance.DispatchBasicSystemEvent(EVENT_GAMECONTROLLER_RESPONSE_IS_GAME_RUNNING, IsGameFakeRunning());
            return previousStateIsFirstTimeRun;
        }

        // -------------------------------------------
        /* 
		 * Change the state of the object		
		 */
        protected void SetState(int _newState)
        {
            if (_newState == m_stateManager.State) return;
            /*
            if (_newState == STATE_RUNNING)
            {
                Debug.LogError("CHANGING TO STATE_RUNNING+++++++++++++++++++++++++++++++++++++++++++++++");
                return;
            }
            */

            m_stateManager.ChangeState(_newState);
            
            switch (m_stateManager.State)
            {
                ///////////////////////////////////////
                case STATE_CONNECTING:
                    SetUpStateConnecting();
                    if (DEBUG) Debug.LogError("STATE_CONNECTING+++++++++++++++++++++++++++++++++++++++++++++++++");
                    break;
                    
                ///////////////////////////////////////
                case STATE_LOADING:
                    if (DEBUG) Debug.LogError("STATE_LOADING+++++++++++++++++++++++++++++++++++++++++++++++++");
                    SetUpStateLoading();
                    break;

                ///////////////////////////////////////
                case STATE_REPOSITION:
                    if (DEBUG) Debug.LogError("STATE_REPOSITION+++++++++++++++++++++++++++++++++++++++++++++++++");
                    SetUpStateReposition();
                    break;

                ///////////////////////////////////////
                case STATE_RUNNING:
                    if (DEBUG) Debug.LogError("STATE_RUNNING+++++++++++++++++++++++++++++++++++++++++++++++++");
                    SetUpStateRunning();
                    break;

                ///////////////////////////////////////
                case STATE_LEVEL_END:
                    if (DEBUG) Debug.LogError("STATE_LEVEL_END+++++++++++++++++++++++++++++++++++++++++++++++++");
                    UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_OPEN_INFORMATION_SCREEN, ScreenInformationView.SCREEN_INFORMATION, UIScreenTypePreviousAction.DESTROY_ALL_SCREENS, LanguageController.Instance.GetText("message.info"), LanguageController.Instance.GetText("message.level.end.completed"), null, "");
                    break;
            }
        }

        // -------------------------------------------
        /* 
		* IsGameRunning
		*/
        public bool IsGameFakeRunning()
        {
            return (m_stateManager.State == STATE_RUNNING) || (m_stateManager.State == STATE_REPOSITION) || (m_stateManager.State == STATE_LEVEL_END);
        }

        // -------------------------------------------
        /* 
		* IsGameRunning
		*/
        public bool IsGameReallyRunning()
        {
            return (m_stateManager.State == STATE_RUNNING) || (m_stateManager.State == STATE_LEVEL_END);
        }

        // -------------------------------------------
        /* 
		* IsGameLoading
		*/
        public bool IsGameLoading()
        {
            return (m_stateManager.State == STATE_LOADING);
        }

        protected int m_globalCounterEnemies = 0;

        // -------------------------------------------
        /* 
		* CreateNewEnemy
		*/
        public virtual bool CreateNewEnemy(Vector3 _position)
        {
            int indexEnemy = UnityEngine.Random.Range(0, EnemyModelPrefab.Length);
            string initialData = "ENEMY" + m_globalCounterEnemies + "," + "ZOMBIE" + "," + EnemyModelPrefab[indexEnemy] + "," + _position.x + "," + _position.y + "," + _position.z;
            m_globalCounterEnemies++;
#if !ENABLE_CONFUSION
            YourNetworkTools.Instance.CreateLocalNetworkObject(EnemyPrefab[0].name, initialData, true, 10000, 10000, 10000);
#else
            GameObject newZombie = Instantiate(EnemyPrefab[indexEnemy]);
            newZombie.GetComponent<IGameNetworkActor>().Initialize(initialData);
#endif
            
            return true;
        }

        private int m_globalCounterNPCs = 0;

        // -------------------------------------------
        /* 
		* CreateNewNPC
		*/
        public virtual bool CreateNewNPC(Vector3 _position, int _indexNPC)
        {
            string initialData = "NPC" + m_globalCounterNPCs + "," + "NPC" + "," + NPCModelPrefab[_indexNPC] + "," + _position.x + "," + _position.y + "," + _position.z;
            m_globalCounterNPCs++;
            YourNetworkTools.Instance.CreateLocalNetworkObject(NPCPrefab[0].name, initialData, true);
            return true;
        }

        // -------------------------------------------
        /* 
		* CreateNewFX
		*/
        public virtual bool CreateNewFX(int _fx, float _autoDestroy, Vector3 _position, Vector3 _scale)
        {
            GameObject fx = GameObject.Instantiate(FXPrefab[_fx]);
            fx.transform.position = _position;
            fx.transform.localScale = _scale;
            fx.AddComponent<AutoGameDestroy>();
            fx.GetComponent<AutoGameDestroy>().TimeToDestroy = _autoDestroy;
            return true;
        }

        // -------------------------------------------
        /* 
		* CalculateBarycenterPlayers
		*/
        protected virtual Vector3 CalculateBarycenterPlayers()
        {
            /*
            GamePlayer[] players = GameObject.FindObjectsOfType<GamePlayer>();
            Vector3 positionCenter = new Vector3(0, 0, 0);
            for (int i = 0; i < players.Length; i++)
            {
                positionCenter += ((GamePlayer)players[i]).gameObject.transform.position;
            }
            positionCenter = positionCenter / players.Length;
            return positionCenter;
            */
            return Vector3.zero;
        }

        // -------------------------------------------
        /* 
		* GameLogicRunning
		*/
		protected virtual void GameLogicRunning()
		{
            /*
			if (YourNetworkTools.Instance.IsServer && m_enableEnemyAutoGeneration)
			{
				m_timeGenerationEnemies += Time.deltaTime;
				if (m_timeGenerationEnemies > 5)
				{
					m_timeGenerationEnemies = 0;
					Vector3 barycenter = CalculateBarycenterPlayers();
					float xIni = UnityEngine.Random.Range(-10, -5);
					if (UnityEngine.Random.Range(0, 100) > 50)
					{
						xIni = UnityEngine.Random.Range(5, 10);
					}
					float zIni = UnityEngine.Random.Range(-10, -5);
					if (UnityEngine.Random.Range(0, 100) > 50)
					{
						zIni = UnityEngine.Random.Range(5, 10);
					}

					CreateNewEnemy(new Vector3(barycenter.x + xIni, 10, barycenter.z + zIni));
				}
			}
			if (YourNetworkTools.Instance.IsServer)
			{ 
				for (int i = 0; i < m_enemies.Count; i++)
				{
					GameObject goEnemy = m_enemies[i];
					if (goEnemy == null)
					{
						m_enemies.RemoveAt(i);
						i--;
					}
					else
					{
						IGameNetworkActor enemy = goEnemy.GetComponent<IGameNetworkActor>();
						enemy.Logic();
					}
				}
			}

			// RUN SHOOTS FOR ALL
			GameShoot[] shoots = GameObject.FindObjectsOfType<GameShoot>();
			for (int i = 0; i < shoots.Length; i++)
			{
				((GameShoot)shoots[i]).Logic();
			}

			// SHORTCUT TO TEST LEVEL END
			if (YourNetworkTools.Instance.IsServer)
			{
				if (Input.GetKeyDown(KeyCode.Escape))
				{
					ChangeState(STATE_LEVEL_END);
				}
			}
            */
		}

        // -------------------------------------------
        /* 
		* Update
		*/
        public override void Update()
		{
            base.Update();

#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                int nextLevel = m_currentLevel + 1;
                if (nextLevel >= LevelsAssetsNames.Length) nextLevel = LevelsAssetsNames.Length - 1;
                NetworkEventController.Instance.DispatchNetworkEvent(CloudGameAnchorController.EVENT_6DOF_CHANGE_LEVEL, nextLevel.ToString());
            }
            if (Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                int previousLevel = m_currentLevel - 1;
                if (previousLevel < 0) previousLevel = 0;
                NetworkEventController.Instance.DispatchNetworkEvent(CloudGameAnchorController.EVENT_6DOF_CHANGE_LEVEL, previousLevel.ToString());
            }
#endif

            switch (m_stateManager.State)
            {
                ///////////////////////////////////////
                case STATE_CONNECTING:
                    break;

                ///////////////////////////////////////
                case STATE_LOADING:
                    break;

                ///////////////////////////////////////
                case STATE_REPOSITION:
                    break;

                ///////////////////////////////////////
                case STATE_RUNNING:
					GameLogicRunning();
                    break;

                ///////////////////////////////////////
                case STATE_LEVEL_END:
                    break;
            }
		}

	}
}