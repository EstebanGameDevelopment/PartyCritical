using UnityEngine;
using UnityEngine.UI;
using YourCommonTools;
using YourNetworkingTools;

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
        private bool m_isRightHand;

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
			if (_list != null)
			{
				if (_list.Length > 0)
				{
					if (_list[0] != null)
					{
						if (_list[0] is string)
						{
							string[] initialData = ((string)_list[0]).Split(',');
							Vector3 initialPosition = new Vector3(float.Parse(initialData[2]), float.Parse(initialData[3]), float.Parse(initialData[4]));
							transform.position = initialPosition;
                            Name = initialData[0];
                            m_isRightHand = bool.Parse(initialData[1]);
                            if (IsMine())
							{
								NetworkEventController.Instance.DispatchNetworkEvent(NetworkEventController.EVENT_WORLDOBJECTCONTROLLER_INITIAL_DATA, NetworkID.GetID(), (string)_list[0]);
							}
                            InitializeCommon();
#if ENABLE_OCULUS
                            BasicSystemEventController.Instance.DispatchBasicSystemEvent(OculusHandsManager.EVENT_OCULUSHANDMANAGER_LINK_WITH_NETWORK_GAMEHAND, m_isRightHand, this.gameObject);
#endif
                        }
					}
				}
			}
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
	}
}