using System;
using UnityEngine;
using YourCommonTools;
using YourNetworkingTools;

namespace PartyCritical
{
	public class NetworkLayerSetup : MonoBehaviour
	{
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_NETWORKLAYERSETUP_REFRESH   = "EVENT_NETWORKLAYERSETUP_REFRESH";

        // ----------------------------------------------
        // PUBLIC MEMBERS
        // ----------------------------------------------	
        public string LayerName = "";

        // -------------------------------------------
        /* 
		 * Start
		 */
        public void Start()
        {
            NetworkEventController.Instance.NetworkEvent += new NetworkEventHandler(OnNetworkEvent);
        }

        // -------------------------------------------
        /* 
		 * OnDestroy
		 */
        public void OnDestroy()
        {
            NetworkEventController.Instance.NetworkEvent -= OnNetworkEvent;
        }

        // -------------------------------------------
        /* 
		 * OnNetworkEvent
		 */
        private void OnNetworkEvent(string _nameEvent, bool _isLocalEvent, int _networkOriginID, int _networkTargetID, object[] _list)
        {
            if (_nameEvent == EVENT_NETWORKLAYERSETUP_REFRESH)
            {
                Utilities.ApplyLayerOnGameObject(this.gameObject, LayerMask.NameToLayer(LayerName));
            }
        }
    }
}