using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace tcc
{
    /// <summary>
    /// Set guard sprite numbers with guard health from each player
    /// </summary>
    public class GuardPanelController : MonoBehaviour
    {

        [SerializeField]
        private GameObject _fightCoreGameObject;
        
        [SerializeField]
        private bool isPlayerOne;

        [SerializeField]
        private GameObject[] guardImageObjects;

        #region private field

        private FightCore fightCore;

        private int currentGuardHealth = 0;

        #endregion

        void Awake()
        {
            if (_fightCoreGameObject != null)
            {
                fightCore = _fightCoreGameObject.GetComponent<FightCore>();
            }
            
            currentGuardHealth = 0;
            UpdateGuardHealthImages();
        }

        // Update is called once per frame
        void Update()
        {
            if (currentGuardHealth != getGuardHealth())
            {
                currentGuardHealth = getGuardHealth();
                UpdateGuardHealthImages();
            }

        }

        private int getGuardHealth()
        {
            if (fightCore == null)
                return 0;

            if (isPlayerOne)
                return fightCore.cat1.guardHealth;
            else
                return fightCore.cat2.guardHealth;
        }

        private void UpdateGuardHealthImages()
        {
            for (int i = 0; i < guardImageObjects.Length; i++)
            {
                if (i <= (int)currentGuardHealth - 1)
                {
                    guardImageObjects[i].SetActive(true);
                }
                else
                {
                    guardImageObjects[i].SetActive(false);
                }
            }
        }
    }
}