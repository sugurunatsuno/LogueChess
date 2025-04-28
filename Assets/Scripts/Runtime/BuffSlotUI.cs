using LogueChess.Runtime;
using UnityEngine.UI;

namespace LogueChess.Runtime
{
    public class BuffSlotUI
    {
        public Text buffNameText;
        private BuffDataSO buff;

        public void Setup(BuffDataSO b)
        {
            buff = b;
            buffNameText.text = buff.buffName;
        }
    }
}