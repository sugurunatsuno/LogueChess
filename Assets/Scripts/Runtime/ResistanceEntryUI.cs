using LogueChess.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace LogueChess.Runtime
{
    public class ResistanceEntryUI: MonoBehaviour
    {
        public Text typeText;
        public Slider valueSlider;
        private ResistanceEntry entry;

        public void Setup(ResistanceEntry e)
        {
            entry = e;
            typeText.text = entry.type;
            valueSlider.minValue = -1f;
            valueSlider.maxValue = 1f;
            valueSlider.value = entry.value;
            valueSlider.onValueChanged.AddListener(v => entry.value = v);
        }
    }
}