using LogueChess.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LogueChess.Runtime
{
    public class PerkSlotUI: MonoBehaviour
    {
        public Text perkNameText;
        private PerkDataSO perk;

        public void Setup(PerkDataSO p)
        {
            perk = p;
            perkNameText.text = perk.perkName;
        }
    }
}