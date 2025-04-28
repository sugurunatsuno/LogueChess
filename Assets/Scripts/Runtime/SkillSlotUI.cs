using LogueChess.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LogueChess.Runtime
{
    public class SkillSlotUI: MonoBehaviour
    {
        public Text skillNameText;
        private SkillDataSO skill;

        public void Setup(SkillDataSO s)
        {
            skill = s;
            skillNameText.text = skill.skillName;
        }
    }
}