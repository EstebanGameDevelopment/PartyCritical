using System;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;

namespace PartyCritical
{
	public class NetworkSwitchVisualState : MonoBehaviour
	{
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_NETWORKSWITCHSTATE_CHANGE_STATE   = "EVENT_NETWORKSWITCHSTATE_CHANGE_STATE";
        public const string EVENT_NETWORKSWITCHSTATE_INCREASE_STATE = "EVENT_NETWORKSWITCHSTATE_INCREASE_STATE";
        public const string EVENT_NETWORKSWITCHSTATE_DECREASE_STATE = "EVENT_NETWORKSWITCHSTATE_DECREASE_STATE";

        // ----------------------------------------------
        // PUBLIC MEMBERS
        // ----------------------------------------------	
        public GameObject[] States;
        public string[] Triggers;
        public int InitialState = 0;
        public string LayerName = "";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        private int m_currentState;
        private Animator m_animator;
        private bool m_enableSwitcher = false;

        // -------------------------------------------
        /* 
		 * Start
		 */
        public void Start()
        {
            SelectVisualState(InitialState);
            m_animator = this.gameObject.GetComponent<Animator>();
            NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);
            BasicSystemEventController.Instance.BasicSystemEvent += new BasicSystemEventHandler(OnBasicSystemEvent);
        }

        // -------------------------------------------
        /* 
		 * OnDestroy
		 */
        public void OnDestroy()
        {
            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
            UIEventController.Instance.UIEvent -= OnUIEvent;
            BasicSystemEventController.Instance.BasicSystemEvent -= OnBasicSystemEvent;
        }

        // -------------------------------------------
        /* 
		 * SelectVisualState
		 */
        private void SelectVisualState(int _newState)
        {
            bool hasBeenActivated = false;
            if (m_animator == null)
            {
                hasBeenActivated = SwitchByState(_newState);
            }
            else
            {
                hasBeenActivated = SwitchByTrigger(_newState);
            }
            if (hasBeenActivated)
            {
                NetworkEventController.Instance.ClearNetworkEvents(GameBaseController.EVENT_GAMECONTROLLER_MARKER_BALL, true);
            }
        }

        // -------------------------------------------
        /* 
		 * SwitchByState
		 */
        private bool SwitchByState(int _newState)
        {
            bool hasBeenActivated = false;
            if (States.Length > 0)
            {
                if (_newState < 0)
                {
                    m_currentState = States.Length - 1;
                }
                else
                {
                    m_currentState = _newState % States.Length;
                }
                for (int i = 0; i < States.Length; i++)
                {
                    if (States[i] != null)
                    {
                        States[i].SetActive((i == m_currentState));
                        hasBeenActivated = true;
                    }
                }
            }
            return hasBeenActivated;
        }

        // -------------------------------------------
        /* 
		 * SwitchByTrigger
		 */
        private bool SwitchByTrigger(int _newAnimator)
        {
            bool hasBeenActivated = false;
            if (Triggers.Length > 0)
            {
                if (_newAnimator < 0)
                {
                    m_currentState = Triggers.Length - 1;
                }
                else
                {
                    m_currentState = _newAnimator % Triggers.Length;
                }
                for (int i = 0; i < Triggers.Length; i++)
                {
                    if (Triggers[i] != null)
                    {
                        m_animator.SetTrigger(Triggers[i]);
                        hasBeenActivated = true;
                    }
                }
            }
            return hasBeenActivated;
        }

        // -------------------------------------------
        /* 
		 * OnUIEvent
		 */
        private void OnUIEvent(string _nameEvent, object[] _list)
        {
            if (m_enableSwitcher)
            {
                if (_nameEvent == KeysEventInputController.ACTION_BUTTON_DOWN)
                {
                    BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA, this.gameObject.name);
                }
            }
        }

        // -------------------------------------------
        /* 
		 * OnBasicSystemEvent
		 */
        private void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_LEVEL_LOAD_COMPLETED)
            {
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(GameBaseController.EVENT_GAMECONTROLLER_REQUEST_IS_GAME_RUNNING);
            }
            if (_nameEvent == GameBaseController.EVENT_GAMECONTROLLER_RESPONSE_IS_GAME_RUNNING)
            {
                m_enableSwitcher = (bool)_list[0];
            }
            if (_nameEvent == CameraBaseController.EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA)
            {
                if (this.gameObject.name == (string)_list[0])
                {
                    Vector3 pos = (Vector3)_list[1];
                    Vector3 fwd = (Vector3)_list[2];
                    string maskToConsider = "";
                    if (_list.Length > 3)
                    {
                        maskToConsider = (string)_list[3];
                    }
                    if (LayerName.Length > 0)
                    {
                        for (int i= 0; i < States.Length; i++)
                        {
                            States[i].layer = LayerMask.NameToLayer(LayerName);
                        }
                    }
                    RaycastHit raycastHit = new RaycastHit();
                    bool collided = false;
                    if (maskToConsider == "")
                    {
                        collided = Utilities.GetRaycastHitInfoByRay(pos, fwd, ref raycastHit, ActorTimeline.LAYER_PLAYERS);
                    }
                    else
                    {
                        raycastHit = Utilities.GetRaycastHitInfoByRayWithMask(pos, fwd, maskToConsider);
                        collided = (raycastHit.collider != null);
                    }
                    if (collided)
                    {
                        Vector3 pc = Utilities.Clone(raycastHit.point);
                        for (int i = 0; i < States.Length; i++)
                        {
                            if (Utilities.FindGameObjectInParent(raycastHit.collider.gameObject, States[i]))
                            {
                                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_NETWORKSWITCHSTATE_INCREASE_STATE, Utilities.GetFullPathNameGO(this.gameObject));
                                return;
                            }
                        }
                    }
                }
            }
        }

        // -------------------------------------------
        /* 
		 * OnNetworkEvent
		 */
        private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, object[] _list)
        {
            if (_nameEvent == ClientTCPEventsController.EVENT_CLIENT_TCP_CLOSE_CURRENT_ROOM)
            {
                m_enableSwitcher = true;
            }
            if (_nameEvent ==  EVENT_NETWORKSWITCHSTATE_CHANGE_STATE)
            {
                string nameSelected = (string)_list[0];                
                if (Utilities.GetFullPathNameGO(this.gameObject) == nameSelected)
                {
                    SelectVisualState(int.Parse((string)_list[1]));
                }
            }
            if (_nameEvent == EVENT_NETWORKSWITCHSTATE_INCREASE_STATE)
            {
                string nameSelected = (string)_list[0];
                if (Utilities.GetFullPathNameGO(this.gameObject) == nameSelected)
                {
                    m_currentState++;
                    SelectVisualState(m_currentState);
                }
            }
            if (_nameEvent == EVENT_NETWORKSWITCHSTATE_DECREASE_STATE)
            {
                string nameSelected = (string)_list[0];
                if (Utilities.GetFullPathNameGO(this.gameObject) == nameSelected)
                {
                    m_currentState--;
                    SelectVisualState(m_currentState);
                }
            }
        }
    }
}