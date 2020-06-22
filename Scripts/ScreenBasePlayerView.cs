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
	 * ScreenBasePlayerView
	 * 
	 * Default player HUD on Gyroscope mode
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenBasePlayerView : MonoBehaviour
    {
        // ----------------------------------------------
        // EVENTS
        // ----------------------------------------------	
        public const string EVENT_SCREENPLAYER_OPEN_INVENTORY = "EVENT_SCREENPLAYER_OPEN_INVENTORY";

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected GameObject m_root;
        protected Transform m_container;

        // -------------------------------------------
        /* 
		* Constructor
		*/
        public virtual void Start()
        {
			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

            if (m_container.Find("Inventory") != null)
            {
                GameObject openInventoryScreen = m_container.Find("Inventory").gameObject;
                openInventoryScreen.GetComponent<Button>().onClick.AddListener(OpenInventory);
#if !ENABLE_MULTIPLAYER_TIMELINE
                openInventoryScreen.SetActive(false);
#endif
            }
        }

        // -------------------------------------------
        /* 
		* OpenInventory
		*/
        protected virtual void OpenInventory()
		{
            UIEventController.Instance.DispatchUIEvent(EVENT_SCREENPLAYER_OPEN_INVENTORY);
        }

	}
}