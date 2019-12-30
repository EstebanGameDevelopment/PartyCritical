using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using YourCommonTools;

namespace PartyCritical
{
    /******************************************
     * 
     * ScreenPartyOver
     * 
     * Shows the final score of the player
     * 
     * @author Esteban Gallardo
     */
    public class ScreenPartyOver : ScreenBaseView, IBasicView
    {
        public const string SCREEN_NAME = "SCREEN_PARTY_OVER";

        // ----------------------------------------------
        // PUBLIC MEMBERS
        // ----------------------------------------------	
        public AudioClip GameOverCVS2 = null;

        // ----------------------------------------------
        // PRIVATE MEMBERS
        // ----------------------------------------------	
        private Transform m_container;

        // -------------------------------------------
        /* 
         * Initialize
         */
        public override void Initialize(params object[] _list)
        {
            m_container = this.gameObject.transform.Find("Content");

            m_container.Find("Text").GetComponent<Text>().text = LanguageController.Instance.GetText("screen.the.party.is.over");

            UIEventController.Instance.UIEvent += new UIEventHandler(OnUIEvent);

            if (GameOverCVS2 != null)
            {
                Invoke("PlaySoundGameOver", 0.1f);
            }
        }

        // -------------------------------------------
        /* 
         * PlaySoundGameOver
         */
        public void PlaySoundGameOver()
        {
            SoundsController.Instance.PlaySingleSound(GameOverCVS2);
        }

        // -------------------------------------------
        /* 
         * Destroy
         */
        public override bool Destroy()
        {
            if (base.Destroy()) return true;

            UIEventController.Instance.UIEvent -= OnUIEvent;
            GameObject.Destroy(this.gameObject);

            return false;
        }

        // -------------------------------------------
        /* 
         * OnUIEvent
         */
        private void OnUIEvent(string _nameEvent, params object[] _list)
        {

        }
    }
}