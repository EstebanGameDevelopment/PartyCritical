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
	 * ScreenBaseCloseRoomView
	 * 
	 * The director has the choice to close the room and start the 
     * game without waiting for all the players to fill up
	 * 
	 * @author Esteban Gallardo
	 */
    public class ScreenBaseCloseRoomView : MonoBehaviour
    {
        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        protected GameObject m_root;
        protected Transform m_container;
        protected bool m_hasBeenTriggered = false;

        // -------------------------------------------
        /* 
		* Constructor
		*/
        void Start()
        {
			m_root = this.gameObject;
			m_container = m_root.transform.Find("Content");

            UIEventController.Instance.UIEvent += new UIEventHandler(OnMenuEvent);

            m_container.Find("StartGame").GetComponent<Button>().onClick.AddListener(OnCloseRoom);
            m_container.Find("StartGame/Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.close.room.description");
        }

        // -------------------------------------------
        /* 
		* OnCloseRoom
		*/
        private void OnCloseRoom()
        {
            if (!m_hasBeenTriggered)
            {
                m_hasBeenTriggered = true;
                NetworkEventController.Instance.PriorityDelayNetworkEvent(GameBaseController.EVENT_GAMECONTROLLER_DIRECTOR_CLOSES_ROOM, 0.01f);
                UIEventController.Instance.DispatchUIEvent(UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN);
            }
        }

        // -------------------------------------------
        /* 
		* OnMenuEvent
		*/
        protected void OnMenuEvent(string _nameEvent, params object[] _list)
		{
            if (_nameEvent == UIEventController.EVENT_SCREENMANAGER_DESTROY_ALL_SCREEN)
            {
                UIEventController.Instance.UIEvent -= OnMenuEvent;
                GameObject.Destroy(this.gameObject);
            }
		}

        // -------------------------------------------
        /* 
		* OnMenuEvent
		*/
        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnCloseRoom();
            }
        }
    }
}