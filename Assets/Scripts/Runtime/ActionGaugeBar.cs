using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace LogueChess.Runtime
{
    public class ActionGaugeBar : MonoBehaviour
    {
        [Header("ゲージライン")]
        [SerializeField] private RectTransform lineRect;             // ゲージ背景の RectTransform
        [SerializeField] private RectTransform markerContainer;      // マーカーを置く親
        [SerializeField] private GameObject markerPrefab;            // アイコン付きマーカープレハブ

        private Dictionary<Unit, RectTransform> markers = new();

        /// <summary>バトル開始時に呼び出し</summary>
        public void Initialize(IEnumerable<Unit> allUnits)
        {
            // 既存マーカークリア
            foreach (Transform c in markerContainer) Destroy(c.gameObject);
            markers.Clear();

            // ユニットごとにマーカーを生成
            foreach (var u in allUnits)
            {
                var go = Instantiate(markerPrefab, markerContainer);
                go.name = u.DisplayName;
                var rt = go.GetComponent<RectTransform>();
                // プレハブに Image や TextMeshPro でユニットアイコン/名前をセットしておく
                // go.GetComponentInChildren<Image>().sprite = /* u のアイコン */;
                markers[u] = rt;

                // リアクティブに位置を更新
                u.CurrentGauge
                    .Subscribe(_ => UpdateMarkerPosition(u))
                    .AddTo(this);
            }
        }

        private void UpdateMarkerPosition(Unit u)
        {
            if (!markers.TryGetValue(u, out var rt)) return;
            float normalized = Mathf.Clamp01(u.CurrentGauge.Value / 100f);
            float width = lineRect.rect.width;
            float height = lineRect.rect.height;
            // 左端を0、右端をwidthにマッピング
            
            if(rt == null) return;
            // rt.anchoredPosition = new Vector2(normalized * width, rt.anchoredPosition.y);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, normalized * height);
        }
    }

}