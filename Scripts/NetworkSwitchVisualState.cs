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

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        private int m_currentState;
        private Animator m_animator;

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
            if (_nameEvent == KeysEventInputController.ACTION_BUTTON_DOWN)
            {
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(CameraBaseController.EVENT_CAMERACONTROLLER_REQUEST_SELECTOR_DATA, this.gameObject.name);
            }
        }

        // -------------------------------------------
        /* 
		 * OnBasicSystemEvent
		 */
        private void OnBasicSystemEvent(string _nameEvent, object[] _list)
        {
            if (_nameEvent == CameraBaseController.EVENT_CAMERACONTROLLER_RESPONSE_SELECTOR_DATA)
            {
                if (this.gameObject.name == (string)_list[0])
                {
                    Vector3 pos = (Vector3)_list[1];
                    Vector3 fwd = (Vector3)_list[2];
                    RaycastHit raycastHit = new RaycastHit();
                    if (Utilities.GetRaycastHitInfoByRay(pos, fwd, ref raycastHit, ActorTimeline.LAYER_PLAYERS))
                    {
                        Vector3 pc = Utilities.Clone(raycastHit.point);
                        for (int i = 0; i < States.Length; i++)
                        {
                            if (States[i] == raycastHit.collider.gameObject)
                            {
                                NetworkEventController.Instance.DispatchNetworkEvent(EVENT_NETWORKSWITCHSTATE_INCREASE_STATE, this.gameObject.name);
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
            if (_nameEvent ==  EVENT_NETWORKSWITCHSTATE_CHANGE_STATE)
            {
                string nameSelected = (string)_list[0];
                if (this.gameObject.name == nameSelected)
                {
                    SelectVisualState(int.Parse((string)_list[1]));
                }
            }
            if (_nameEvent == EVENT_NETWORKSWITCHSTATE_INCREASE_STATE)
            {
                string nameSelected = (string)_list[0];
                if (this.gameObject.name == nameSelected)
                {
                    m_currentState++;
                    SelectVisualState(m_currentState);
                }
            }
            if (_nameEvent == EVENT_NETWORKSWITCHSTATE_DECREASE_STATE)
            {
                string nameSelected = (string)_list[0];
                if (this.gameObject.name == nameSelected)
                {
                    m_currentState--;
                    SelectVisualState(m_currentState);
                }
            }
        }
    }
}