#if ENABLE_MULTIPLAYER_TIMELINE
using MultiplayerTimeline;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;
using YourVRUI;

namespace PartyCritical
{
    /******************************************
     * 
     * InstructionsBaseController
     * 
     * Instructions collection to be used by the Timeline
     * 
     * @author Esteban Gallardo
     */
    public class InstructionsBaseController : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_INSTRUCTION_CONTROLLER_START = "EVENT_INSTRUCTION_CONTROLLER_START";

        // ----------------------------------------------
        // CONSTANTS
        // ----------------------------------------------	
        public const string GROUP_PLAYERS        = "GROUP_PLAYERS";
        public const string GROUP_DIRECTORS      = "DIRECTORS";
        public const string GROUP_EVERYBODY_HUMAN= "EVERYBODY_HUMAN";
        public const string GROUP_PICKABLE_ITEMS = "PICKABLE_ITEMS";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected bool m_allPlayersConnected = false;

        protected List<string> m_namesPlayers = new List<string>();
        protected Dictionary<string,List<string>> m_classesPlayers = new Dictionary<string, List<string>>();
        protected List<string> m_namesDirectors = new List<string>();
        protected List<string> m_namesSpectators = new List<string>();

        protected List<string> m_teams = new List<string>();

        protected int m_timeMarker;
        protected int m_timeSegment = 1000;

#if ENABLE_MULTIPLAYER_TIMELINE
        protected LayerData m_mainLayer;
        protected TimeLineData m_newTimeLine;
        protected ActionLineData m_newActionLine;
#endif

        protected bool m_listenerInitialized = false;

        protected int m_currentLevel = 0;
        protected string m_currentTimeline = "";

        // GAME CONTROLLER
        public virtual bool USE_PRECALCULATED_PATHFINDING
        {
            get { return false; }
        }
        public virtual bool PATHFINDING_PRECALCULATE_GENERATION
        {
            get { return false; }
        }
        public virtual TextAsset PathfindingData
        {
            get { return null; }
        }
        public virtual int TotalNumberPlayers
        {
            get { return -1; }
            set { }
        }
        public virtual string NamePlayer
        {
            get { return ""; }
        }
        public bool AllPlayersConnected
        {
            get { return m_allPlayersConnected; }
        }
        // GAMEPLAYER
        public virtual string EVENT_GAMEPLAYER_HUMAN_PLAYER_NAME
        {
            get { return ""; }
        }
        public virtual string EVENT_GAMEPLAYER_HUMAN_SPECTATOR_NAME
        {
            get { return ""; }
        }
        public virtual string EVENT_GAMEPLAYER_HUMAN_DIRECTOR_NAME
        {
            get { return ""; }
        }
        public int CurrentLevel
        {
            get { return m_currentLevel; }
            set { m_currentLevel = value; }
        }
        public string CurrentTimeline
        {
            get { return m_currentTimeline; }
            set { m_currentTimeline = value; }
        }
        public int TimeMarker
        {
            get { return m_timeMarker; }
            set { m_timeMarker = value; }
        }
        public int TimeSegment
        {
            get { return m_timeSegment; }
            set { m_timeSegment = value; }
        }

        // -------------------------------------------
        /* 
        * Awake
        */
        void Start()
        {
            InitializeListeners();
        }

        // -------------------------------------------
        /* 
        * InitializeListeners
        */
        protected void InitializeListeners()
        {
            if (!m_listenerInitialized)
            {
                m_listenerInitialized = true;
                BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
#if ENABLE_MULTIPLAYER_TIMELINE
                TimelineEventController.Instance.TimelineEvent += new TimelineEventHandler(OnTimelineEvent);
#endif
                NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
            }
        }

        // -------------------------------------------
        /* 
        * OnDestroy
        */
        void OnDestroy()
        {
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
#if ENABLE_MULTIPLAYER_TIMELINE
            TimelineEventController.Instance.TimelineEvent -= OnTimelineEvent;
#endif
            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
        }

        // -------------------------------------------
        /* 
		 * InitialitzationDirectors
		 */
        protected void InitialitzationDirectors()
        {
#if ENABLE_MULTIPLAYER_TIMELINE
            GroupActorsData directorsGroup = GameLevelData.Instance.GetGroupByName(GROUP_DIRECTORS);
            if (directorsGroup == null)
            {
                directorsGroup = GameLevelData.Instance.AddNewGroup(GROUP_DIRECTORS);
            }
            for (int i = 0; i < m_namesDirectors.Count; i++)
            {
                if (directorsGroup.Members.IndexOf(m_namesDirectors[i]) == -1)
                {
                    directorsGroup.AddActor(m_namesDirectors[i]);
                }
            }
#endif
        }

        // -------------------------------------------
        /* 
		* CheckCloseGameWithAllConnectedPlayers
		*/
        protected virtual bool CheckCloseGameWithAllConnectedPlayers()
        {
            if (TotalNumberPlayers == MultiplayerConfiguration.VALUE_FOR_JOINING) return true;

            int totalRegister = m_namesPlayers.Count + m_namesDirectors.Count + m_namesSpectators.Count;
            if ((totalRegister < TotalNumberPlayers) && YourNetworkTools.Instance.IsServer)
            {
                return true;
            }
            else
            {
                if (!m_allPlayersConnected)
                {
                    if (YourNetworkTools.Instance.IsServer)
                    {
                        // THANKS TO THIS MESSAGE WE TRY TO START AT THE SAME TIME
                        NetworkEventController.Instance.PriorityDelayNetworkEvent(EVENT_INSTRUCTION_CONTROLLER_START, 0.1f, TotalNumberPlayers.ToString());
                    }
                    return true;
                }
            }

            return false;
        }

        // -------------------------------------------
        /* 
		* UpdateTimeline
		*/
        protected virtual void UpdateTimeline()
        {
            if (CheckCloseGameWithAllConnectedPlayers()) return;

#if ENABLE_MULTIPLAYER_TIMELINE
            if (GameLevelData.Instance.Init(YourNetworkTools.Instance.IsServer, NamePlayer))
            {
                // Debug.LogError("START MULTIPLAYER INSTRUCTIONS COLLECTION::NAME PLAYER[" + NamePlayer + "]::IS SERVER[" + YourNetworkTools.Instance.IsServer + "]**********************");
                RenderSettings.ambientLight = new Color(1f, 1f, 1f, 1f);
                // PathFindingController.Instance.AllocateMemoryMatrix(25, 25, 1, 4, -53, 0, -50);
                // PathFindingController.Instance.AllocateMemoryMatrix(40, 40, 1, 2, -40, 0, -40);                
                InterpolatorController.Instance.EnableOnUpdate = false;

                // ADD INSTRUCTIONS
                SceneData mainScene = GameLevelData.Instance.CreateNewScene();
                GameLevelData.Instance.SelectCurrentScene(mainScene.IdScene);
                m_mainLayer = mainScene.CreateLayer();

                // PLAYERS
                // Debug.LogError("GROUP_PLAYERS::m_namesPlayers.Count=" + m_namesPlayers.Count);
                GroupActorsData playersGroup = GameLevelData.Instance.AddNewGroup(GROUP_PLAYERS);
                for (int i = 0; i < m_namesPlayers.Count; i++)
                {
                    playersGroup.AddActor(m_namesPlayers[i]);
                    // Debug.LogError("m_namesPlayers["+i+"]=" + m_namesPlayers[i]);
                }

                // CREATE THE TEAM GROUPS
                foreach (KeyValuePair<string, List<string>> item in m_classesPlayers)
                {
                    string teamName = item.Key;
                    m_teams.Add(item.Key);
                    GroupActorsData teamGroup = GameLevelData.Instance.AddNewGroup(teamName);
                    for (int i = 0; i < item.Value.Count; i++)
                    {
                        teamGroup.AddActor(item.Value[i]);
                    }
                }

                // EVERYBODY HUMAN
                GroupActorsData everybodyGroup = GameLevelData.Instance.AddNewGroup(GROUP_EVERYBODY_HUMAN);
                for (int i = 0; i < m_namesPlayers.Count; i++)
                {
                    everybodyGroup.AddActor(m_namesPlayers[i]);
                }
                for (int i = 0; i < m_namesDirectors.Count; i++)
                {
                    everybodyGroup.AddActor(m_namesDirectors[i]);
                }

                // DIRECTORS
                InitialitzationDirectors();

                // TIMELINE FOR LEVEL 0
                CreateTimeLineForLevel0(m_mainLayer, 100, 100 * 1000);
            }

            GameLevelData.Instance.Logic();

            if (!InterpolatorController.Instance.EnableOnUpdate) InterpolatorController.Instance.Logic();
#endif
        }

        // -------------------------------------------
        /* 
		 * CreateTimeLineForLevel0 (**WARNING** THIS IS A BASIC SAMPLE DATA)
		 */
#if ENABLE_MULTIPLAYER_TIMELINE
        protected virtual void CreateTimeLineForLevel0(LayerData _layer, int _secondsEndPreviousLevel, int _sencondsEndNextLevel)
        {
            m_currentTimeline = "TIMELINE_LEVEL_0";
            m_currentLevel = 0;
            TimeLineData timeline = _layer.CreateTimeline(m_currentTimeline, _secondsEndPreviousLevel, _sencondsEndNextLevel);
            ActionLayerData actionLayerData = timeline.CreateActionLayer();

            // PLAYERS
            GroupActorsData playersGroup = GameLevelData.Instance.GetGroupByName(GROUP_PLAYERS);
            // Debug.LogError("CREATING BASE OBJECT DATA FOR HUMANS++++++++++++++++");
            // Debug.LogError("playersGroup.Members.Count = "+ playersGroup.Members.Count);            
            for (int i = 0; i < playersGroup.Members.Count; i++)
            {
                // Debug.LogError("playersGroup.Members["+i+"]="+ playersGroup.Members[i]);
                timeline.CreateObject("Player1_1", playersGroup.Members[i], Vector3.zero, Quaternion.identity, true, (playersGroup.Members[i] == NamePlayer));
            }

            m_timeMarker = 100;
            m_timeSegment = 1000;

            // COLLECTIBLES
            timeline.CreateObject("AMMO", "AMMO_BOX_1", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_2", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_3", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_4", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);

            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // +++++++++++ PRESENTATION MESSAGE
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // WELCOME MESSAGE
            ActionLineData helloWorldActionLineData = actionLayerData.CreateActionLine("HELLO_WORLD_CONFIRMATION",
                                                new List<String>(new String[] { GROUP_PLAYERS }), m_timeMarker, m_timeMarker + m_timeSegment, true, true, false, true,
                                                "[ACTION_BUTTON:OWN_ALL:PARAMS]",
                                                "SWITCH_TRIGGERED_SUCESSFULLY");
            CMDConfirmation hcInstructionInformation = new CMDConfirmation("helloworld_confirm_1", NamePlayer, LanguageController.Instance.GetText("in-game.msg.information"), LanguageController.Instance.GetText("in-game.welcome.message"));
            helloWorldActionLineData.Commands.Add(hcInstructionInformation);

            // **** TIME MARKER UPDATE ****
            m_timeMarker += (m_timeSegment + 500);

            // SUCCESS SWITCH MESSAGE
            ActionLineData switchSuccessActionLineData = actionLayerData.CreateActionLine("SWITCH_TRIGGERED_SUCESSFULLY",
                                                new List<String>(new String[] { GROUP_PLAYERS }), m_timeMarker, m_timeMarker + m_timeSegment, true, true, false, true,
                                                "[ACTION_BUTTON:OWN_ALL:PARAMS]",
                                                "JUMP_TO_NEXT_LEVEL_1");
            CMDConfirmation switchSuccessInstructionInformation = new CMDConfirmation("switchsuccess_confirm_1", NamePlayer, LanguageController.Instance.GetText("in-game.switch.success"), LanguageController.Instance.GetText("in-game.switch.success"));
            switchSuccessActionLineData.Commands.Add(switchSuccessInstructionInformation);

            // **** TIME MARKER UPDATE ****
            m_timeMarker += (m_timeSegment + 500);

            // YourVRUIScreenController.Instance.DestroyScreens();
            UIEventController.Instance.DelayUIEvent(ScreenController.EVENT_FORCE_DESTRUCTION_POPUP, 0.01f);
        }
#endif

        // -------------------------------------------
        /* 
         * CreateTimeLineForLevel1 (**WARNING** THIS IS A BASIC SAMPLE DATA)
         */
#if ENABLE_MULTIPLAYER_TIMELINE
        protected virtual void CreateTimeLineForLevel1(LayerData _layer, int _secondsEndPreviousLevel, int _sencondsEndNextLevel)
        {
            m_currentTimeline = "TIMELINE_LEVEL_1";
            TimeLineData timeline = _layer.CreateTimeline(m_currentTimeline, _secondsEndPreviousLevel, _sencondsEndNextLevel);
            ActionLayerData actionLayerData = timeline.CreateActionLayer();

            // **** TIME MARKER UPDATE ****
            m_timeMarker = 500;

            // PLAYERS
            GroupActorsData playersGroup = GameLevelData.Instance.GetGroupByName(GROUP_PLAYERS);
            for (int i = 0; i < playersGroup.Members.Count; i++)
            {
                timeline.CreateObject("Player1_1", playersGroup.Members[i], Vector3.zero, Quaternion.identity, true, (playersGroup.Members[i] == NamePlayer));
            }

            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++
            // +++++++++++ PRESENTATION MESSAGE
            // +++++++++++++++++++++++++++++++++++++++++++++++++++++++

            // WELCOME LEVEL 1 MESSAGE
            ActionLineData showNextLevelInfoActionLineData = actionLayerData.CreateActionLine("HELLO_NEW_LEVEL_CONFIRMATION",
                                                new List<String>(new String[] { GROUP_PLAYERS }), m_timeMarker, m_timeMarker + m_timeSegment, true, true, false, true,
                                                "[ACTION_BUTTON:OWN_ALL:PARAMS]",
                                                "ACTIVATE_ZOMBIE_GENERATION_1");
            CMDConfirmation showNLInstructionInformation = new CMDConfirmation("hello_new_level_confirm", NamePlayer, LanguageController.Instance.GetText("in-game.msg.information"), LanguageController.Instance.GetText("in-game.level.1.loaded"));
            showNextLevelInfoActionLineData.Commands.Add(showNLInstructionInformation);

            // **** TIME MARKER UPDATE ****
            m_timeMarker += (m_timeSegment + 500);

            // COLLECTIBLES
            timeline.CreateObject("AMMO", "AMMO_BOX_1", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_2", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_3", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);
            timeline.CreateObject("AMMO", "AMMO_BOX_4", Vector3.zero, Quaternion.identity, false, false, false, false, false, 0, true);

            // WAIT UNTIL THE DAY TO COME
            ActionLineData waitDayToComeActionLineData = actionLayerData.CreateActionLine("BLOCKER_WAIT_UNTIL_DAY",
                                                    new List<String>(new String[] { "SYSTEM" }), m_timeMarker, m_timeMarker + m_timeSegment, true, true, false, true,
                                                    "[EVENT_DAY_REACHED:ANYBODY:PARAMS]",
                                                    "INFORM_SUCCESS_ZOMBIE_DEAD");
            CMDWait waitDayToComeCommand = new CMDWait("just_wait_for_day_to_come", "EVENT_DAY_REACHED", 120);
            waitDayToComeActionLineData.Commands.Add(waitDayToComeCommand);

            // **** TIME MARKER UPDATE ****
            m_timeMarker += (m_timeSegment + 100);

            // END LEVEL 1 MESSAGE
            ActionLineData showEndLevelInfoActionLineData = actionLayerData.CreateActionLine("INFORM_SUCCESS_ZOMBIE_DEAD",
                                                new List<String>(new String[] { GROUP_PLAYERS }), m_timeMarker, m_timeMarker + m_timeSegment, true, true, false, true,
                                                "[ACTION_BUTTON:OWN_ALL:PARAMS]",
                                                "FINAL_RESULTS_GAME");
            CMDConfirmation showEndLevelInformation = new CMDConfirmation("show_end_level_confirm", NamePlayer, LanguageController.Instance.GetText("in-game.msg.information"), LanguageController.Instance.GetText("in-game.level.1.end.zombies.completed"));
            showEndLevelInfoActionLineData.Commands.Add(showEndLevelInformation);

            // **** TIME MARKER UPDATE ****
            m_timeMarker += (m_timeSegment + 500);

            // SHOW RESULT FINAL EVALUATION
            ActionLineData showResultGameInLifeValueLineData = actionLayerData.CreateActionLine("FINAL_RESULTS_GAME",
                                                 new List<String>(new String[] { BaseObjectData.NAME_ACTOR_SYSTEM }), m_timeMarker, m_timeMarker + 200, false, false, false, false);
            CMDThrowBasicSystemEvent showFinalScoreScreenCMD = new CMDThrowBasicSystemEvent("command_show_final_score_screen", "EVENT_GAMECONTROLLER_SHOW_EVALUATION_END_GAME", 1, false);
            CMDSoundPlay soundFinalScoreFXCMD = new CMDSoundPlay("sound_play_final_score_fx_cmd", "FINAL_SCORE_FX", -1, CMDSoundPlay.CHANNEL_TWO, false, Vector3.zero, false, true, 1);
            showResultGameInLifeValueLineData.Commands.Add(showFinalScoreScreenCMD);
            showResultGameInLifeValueLineData.Commands.Add(soundFinalScoreFXCMD);
        }
#endif

        // -------------------------------------------
        /* 
        * OnTimelineEvent
        */
#if ENABLE_MULTIPLAYER_TIMELINE
        protected virtual void OnTimelineEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == BaseObjectData.EVENT_BASEOBJECTDATA_REQUEST_ACTIVATION)
            {
                BaseObjectData baseObjectData = (BaseObjectData)_list[0];
            }
            if (_nameEvent == ActionLineData.EVENT_ACTIONLINEDATA_PLAYING_GAME_START)
            {
                m_newActionLine = (ActionLineData)_list[0];
            }
            if (_nameEvent == TimeLineData.EVENT_TIMELINEDATA_ACTIVATION_PROCCESS_COMPLETED)
            {
                m_newTimeLine = (TimeLineData)_list[0];
            }
        }
#endif

        protected bool m_pathfindingInitialized = false;

        // -------------------------------------------
        /* 
		* CalculateCollisionsPathfinding
		*/
        protected virtual void CalculateCollisionsPathfinding(float _timeToDisplayCollisions = 0)
        {
            // CALCULATE THE COLLISIONS OF THE LEVEL
            PathFindingController.Instance.CalculateCollisions(0, new string[3] { ActorTimeline.LAYER_ITEMS, ActorTimeline.LAYER_PLAYERS, ActorTimeline.LAYER_NPCS });
            PathFindingController.Instance.ClearDotPaths();
            PathFindingController.Instance.RenderDebugMatrixConstruction(-1, _timeToDisplayCollisions);
        }

        // -------------------------------------------
        /* 
		* InitPathfindingMatrix
		*/
        public virtual void InitPathfindingMatrix()
        {
            if (m_pathfindingInitialized) return;
            m_pathfindingInitialized = true;

            PathFindingController.Instance.AllocateMemoryMatrix(25, 25, 1, 4, -53, 0, -50);
            PathFindingController.Instance.SetWaypointHeight(10);

            CalculateCollisionsPathfinding();

            if (USE_PRECALCULATED_PATHFINDING)
            {
                if (!PATHFINDING_PRECALCULATE_GENERATION)
                {
                    if (YourNetworkTools.Instance.IsServer)
                    {
                        // PathFindingController.Instance.LoadFile("Assets/pathfinding.dat");
                        PathFindingController.Instance.LoadAsset(PathfindingData);
                        Debug.LogError("************************************** PATHFINDING DATA LOADED **************************************");
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		* Manager of global events
		*/
        protected virtual void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
        {
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_PLAYER_IS_READY)
            {
                Invoke("InitPathfindingMatrix", 0.5f);
            }
            if (_nameEvent == EVENT_INSTRUCTION_CONTROLLER_START)
            {
                m_allPlayersConnected = true;
                TotalNumberPlayers = int.Parse((string)_list[0]);
                // Debug.LogError("EVENT_INSTRUCTION_CONTROLLER_START::TotalNumberPlayers="+ TotalNumberPlayers);
            }
            if (_nameEvent == EVENT_GAMEPLAYER_HUMAN_SPECTATOR_NAME)
            {
                string nameNewSpectator = (string)_list[0];
                BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMEPLAYER_HUMAN_SPECTATOR_NAME, 3, nameNewSpectator);
            }
            if (_nameEvent == EVENT_GAMEPLAYER_HUMAN_DIRECTOR_NAME)
            {
                string nameNewDirector = (string)_list[0];
                BasicSystemEventController.Instance.DelayBasicSystemEvent(EVENT_GAMEPLAYER_HUMAN_DIRECTOR_NAME, 3, nameNewDirector);
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_NUMBER_LEVEL_TO_LOAD)
            {
                int currentLevel = int.Parse((string)_list[0]);
                if (m_currentLevel == currentLevel)
                {
                    Debug.LogError("RESETTING TIMLINE ++START++");
#if ENABLE_MULTIPLAYER_TIMELINE
                    int totalDestroyed = GameLevelData.Instance.CurrentScene.DestroyTimelines(true, m_currentTimeline);
                    Debug.LogError("RESETTING TIMLINE ++FINISH++ TOTAL DESTROYED=" + totalDestroyed);
#endif
                }
                m_currentLevel = currentLevel;
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_RECALCULATE_COLLISIONS)
            {
#if ENABLE_MULTIPLAYER_TIMELINE
                m_pathfindingInitialized = false;
                float totalTimeToRender = 0;
                if (_list.Length > 0)
                {
                    totalTimeToRender = float.Parse((string)_list[0]);
                }
                CalculateCollisionsPathfinding(totalTimeToRender);
#endif
            }
        }

        // -------------------------------------------
        /* 
        * OnBasicSystemEvent
        */
        protected virtual void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == EVENT_GAMEPLAYER_HUMAN_PLAYER_NAME)
            {
                string nameNewPlayer = (string)_list[0];
                string classNewPlayer = (string)_list[1];
                if (!m_namesPlayers.Contains(nameNewPlayer))
                {
                    m_namesPlayers.Add(nameNewPlayer);
                }

                // CLASS MEMBER
                List<string> memberOfClass;
                if (!m_classesPlayers.TryGetValue(classNewPlayer, out memberOfClass))
                {
                    memberOfClass = new List<string>();
                    m_classesPlayers.Add(classNewPlayer, memberOfClass);
                }
                memberOfClass.Add(nameNewPlayer);
            }
            if (_nameEvent == EVENT_GAMEPLAYER_HUMAN_DIRECTOR_NAME)
            {
                string nameNewDirector = (string)_list[0];
                if (!m_namesDirectors.Contains(nameNewDirector))
                {
                    m_namesDirectors.Add(nameNewDirector);
                }
            }
            if (_nameEvent == EVENT_GAMEPLAYER_HUMAN_SPECTATOR_NAME)
            {
                string nameNewSpectator = (string)_list[0];
                // m_allPlayersConnected = true;
                if (!m_namesSpectators.Contains(nameNewSpectator))
                {
                    m_namesSpectators.Add(nameNewSpectator);
                }
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED)
            {
                // Debug.LogError("InstructionsController::EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED!!!!!!!!!!!!!!!!!!!!!!!!");
                CreateNewTimelineForLevel((int)_list[0]);
            }
        }

        // -------------------------------------------
        /* 
		 * CreateNewTimelineForLevel
		 */
        protected virtual void CreateNewTimelineForLevel(int _layer)
        {
#if ENABLE_MULTIPLAYER_TIMELINE
            int SECONDS_END_LEVEL_0 = 100 * 1000;
            int SECONDS_END_LEVEL_1 = 200 * 1000;
            int SECONDS_END_LEVEL_2 = 300 * 1000;
            int SECONDS_END_LEVEL_3 = 400 * 1000;

            switch (_layer)
                {
                    case 0:
                        if (m_mainLayer != null)
                        {
                            CreateTimeLineForLevel0(m_mainLayer, 100, SECONDS_END_LEVEL_0);
                            GameLevelData.Instance.SetCurrentTimePlaying(0, "");
                        }
                        break;

                    case 1:
                        CreateTimeLineForLevel1(m_mainLayer, SECONDS_END_LEVEL_0, SECONDS_END_LEVEL_1);
                        GameLevelData.Instance.SetCurrentTimePlaying(SECONDS_END_LEVEL_0, "");
                        break;
                }
#endif
        }


        // -------------------------------------------
        /* 
		 * IsGameReallyRunning
		 */
        public virtual bool IsGameReallyRunning()
        {
            return true;
        }

        // -------------------------------------------
        /* 
		 * Update
		 */
        public virtual void Update()
        {
            if (IsGameReallyRunning())
            {
                UpdateTimeline();

                // RESET CURRENT TIMELINE
                if (Input.GetKeyDown(KeyCode.R))
                {
                    NetworkEventController.Instance.DispatchNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_CONFIRMATION_RELOAD_LEVEL);
                }
            }
        }

        // -------------------------------------------
        /* 
		 * OnGUI
		 */
        protected virtual void OnGUI()
        {
#if ENABLE_MULTIPLAYER_TIMELINE && UNITY_EDITOR
            string data = "TIME=" + GameLevelData.Instance.CurrentTimePlaying;
            if (m_newTimeLine != null) data += ":T:" + m_newTimeLine.Name;
            if (m_newActionLine != null) data += ":A:" + m_newActionLine.Name;
            GUI.Box(new Rect(0, 30, Screen.width, 30), data);
#endif
        }
    }
}