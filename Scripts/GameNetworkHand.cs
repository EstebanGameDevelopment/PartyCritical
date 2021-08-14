using System;
using UnityEngine;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;
#if ENABLE_YOURVRUI
using YourVRUI;
#endif

namespace PartyCritical
{
    /******************************************
     * 
     * GameNetworkHand
     * 
     * @author Esteban Gallardo
     */
    public class GameNetworkHand : ActorTimeline, IGameNetworkActor
	{
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_GAMENETWORKHAND_CREATED_NEW     = "EVENT_GAMENETWORKHAND_CREATED_NEW";

        // ----------------------------------------------
        // CONSTANTS
        // ----------------------------------------------	

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected bool m_isRightHand;

        // ----------------------------------------------
        // GETTERS/SETTERS
        // ----------------------------------------------	
        public bool IsRightHand
        {
            get { return m_isRightHand; }
        }

        // -------------------------------------------
        /* 
		* Awake
		*/
        public void Awake()
		{
			EventNameObjectCreated = EVENT_GAMENETWORKHAND_CREATED_NEW;
		}

        // -------------------------------------------
        /* 
		* Start
		*/
        public override void Start()
		{
            base.Start();
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
        public override bool Destroy()
		{
            if (base.Destroy()) return true;

			NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
			GameObject.Destroy(this.gameObject);

            return false;
		}

        // -------------------------------------------
        /* 
		* Initialize
		*/
        public override void Initialize(params object[] _list)
		{
            try
            {
                if (_list != null)
                {
                    if (_list.Length > 0)
                    {
                        if (_list[0] != null)
                        {
                            if (_list[0] is string)
                            {
                                if (m_initialData == null)
                                {
                                    InitializeWithData((string)_list[0]);
                                }                                
                            }
                        }
                    }
                }
            }
            catch (Exception err) { }
        }

        // -------------------------------------------
        /* 
		* InitializeWithData
		*/
        protected override void InitializeWithData(string _initialData)
        {
            m_initialData = _initialData;
            string[] initialData = m_initialData.Split(',');
            Vector3 initialPosition = new Vector3(float.Parse(initialData[2]), float.Parse(initialData[3]), float.Parse(initialData[4]));
            transform.position = initialPosition;
            Name = initialData[0];
            m_isRightHand = bool.Parse(initialData[1]);
            bool linkToHands = false;
            if (!GameBaseController.InstanceBase.IsSinglePlayer)                
            {
                linkToHands = IsMine();
            }
            else
            {
                linkToHands = true;
            }
            if (linkToHands)
            {
                if (!GameBaseController.InstanceBase.IsSinglePlayer) NetworkEventController.Instance.PriorityDelayNetworkEvent(NetworkEventController.EVENT_WORLDOBJECTCONTROLLER_INITIAL_DATA, 0.1f, NetworkID.GetID(), m_initialData);
#if ENABLE_OCULUS 
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(OculusHandsManager.EVENT_OCULUSHANDMANAGER_LINK_WITH_NETWORK_GAMEHAND, m_isRightHand, this.gameObject);
#elif ENABLE_PICONEO
                BasicSystemEventController.Instance.DispatchBasicSystemEvent(PicoNeoHandController.EVENT_PICONEOHANDCONTROLLER_LINK_WITH_NETWORK_GAMEHAND, m_isRightHand, this.gameObject);
#endif
            }
            InitializeCommon();
        }


        // -------------------------------------------
        /* 
		* InitializeCommon
		*/
        public override void InitializeCommon()
		{
            if (GetModel() != null)
            {
                NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
            }
        }

        // -------------------------------------------
        /* 
		* HideModelOwnHand
		*/
        public void HideModelOwnHand()
        {
            if (GetModel() != null)
            {
                if (this.gameObject.GetComponent<BoxCollider>() != null)
                {
                    this.gameObject.GetComponent<BoxCollider>().enabled = false;
                }
                m_model.SetActive(false);
            }
        }

        // -------------------------------------------
        /* 
		* Manager of global events
		*/
        protected override void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, params object[] _list)
		{
            base.OnNetworkEvent(_nameEvent, _isLocalEvent, _networkOriginID, _networkTargetID, _list);
        }

        // -------------------------------------------
        /* 
		 * LateUpdate
		 */
        protected override void LateUpdate()
        {
        }
    }
}