using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace tcc
{
    /// <summary>
    /// Set round sprite with number of win from each player
    /// </summary>
    public class RoundPanelController : MonoBehaviour
    {
        [SerializeField]
        private GameObject _fightCoreGameObject;

        [SerializeField]
        private Sprite spriteEmpty;

        [SerializeField]
        private Sprite spriteWon;

        [SerializeField]
        private bool isPlayerOne;

        [SerializeField]
        private GameObject[] roundWonImageObjects;

        #region private field

        private FightCore fightCore;
        private Image[] roundWonImages;

        private uint currentRoundWon = 0;

        #endregion
        
        void Awake ()
        {
            if (_fightCoreGameObject != null)
            {
                fightCore = _fightCoreGameObject.GetComponent<FightCore>();
            }

            roundWonImages = new Image[roundWonImageObjects.Length];
            for(int i = 0; i < roundWonImageObjects.Length; i++)
            {
                roundWonImages[i] = roundWonImageObjects[i].GetComponent<Image>();
            }

            currentRoundWon = 0;
            UpdateRoundWonImages();
        }
	
	    // Update is called once per frame
	    void Update ()
        {
		    if(currentRoundWon != getRoundWon())
            {
                currentRoundWon = getRoundWon();
                UpdateRoundWonImages();
            }

	    }

        private uint getRoundWon()
        {
            if (fightCore == null)
                return 0;

            if (isPlayerOne)
                return fightCore.cat1RoundWon;
            else
                return fightCore.cat2RoundWon;
        }

        private void UpdateRoundWonImages()
        {
            for(int i = 0; i < roundWonImages.Length; i++)
            {
                if(i <= (int)currentRoundWon - 1)
                {
                    roundWonImages[i].sprite = spriteWon;
                }
                else
                {
                    roundWonImages[i].sprite = spriteEmpty;
                }
            }
        }
    }

}
