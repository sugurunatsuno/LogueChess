using System.Collections.Generic;
using UnityEngine;

namespace LogueChess.Runtime
{


    public class UnitListUI : MonoBehaviour
    {
        [SerializeField] private Transform container;   // Vertical LayoutGroup 下の親
        [SerializeField] private GameObject slotPrefab; // UnitHUDSlot ログ出力済みのプレハブ

        /// <summary>
        /// ユニット一覧を（再）表示する
        /// </summary>
        public void Populate(List<Unit> units)
        {
            // 既存をクリア
            foreach (Transform t in container) Destroy(t.gameObject);

            // 生存ユニットだけ表示
            foreach (var u in units)
            {
                if (u.IsDead) continue;
                var go = Instantiate(slotPrefab, container);
                go.name = u.DisplayName;
                // go.GetComponent<UnitHUDSlot>().Setup(u);
            }
        }
    }

}