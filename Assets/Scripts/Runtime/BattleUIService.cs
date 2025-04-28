using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
using LogueChess.Runtime;

namespace LogueChess.Runtime
{
    public class BattleUIService : MonoBehaviour, IUIService
    {
        //────────────────────────────────────────────
        // ReactiveCommands for input events
        //────────────────────────────────────────────
        private ReactiveCommand<Skill> onSkillSelected = new ReactiveCommand<Skill>();
        private ReactiveCommand<Unit> onTargetSelected = new ReactiveCommand<Unit>();

        //────────────────────────────────────────────
        // Character Info UI
        //────────────────────────────────────────────
        [Header("Character Info UI")]
        [SerializeField] private TMP_Text characterNameText;
        [SerializeField] private Slider hpSlider;
        [SerializeField] private TMP_Text hpValueText;
        [SerializeField] private Slider gaugeSlider;
        [SerializeField] private TMP_Text gaugeValueText;
        [SerializeField] private TMP_Text atkValueText;

        //────────────────────────────────────────────
        // Skill Selection UI
        //────────────────────────────────────────────
        [Header("Skill Selection UI")]
        [SerializeField] private CanvasGroup skillPanelGroup;
        [SerializeField] private Button[] skillButtons;
        [SerializeField] private TMP_Text[] skillLabels;

        //────────────────────────────────────────────
        // Target Selection UI
        //────────────────────────────────────────────
        [Header("Target Selection UI")]
        [SerializeField] private CanvasGroup targetPanelGroup;
        [SerializeField] private Button targetButtonPrefab;
        [SerializeField] private Transform targetButtonContainer;

        //────────────────────────────────────────────
        // Screen Flash Animation
        //────────────────────────────────────────────
        [Header("Screen Flash for Animation")]
        [SerializeField] private Image screenFlash;
        [SerializeField] private float flashDuration = 0.5f;
        
        //────────────────────────────────────────────
        // アクションゲージ表示
        //────────────────────────────────────────────
        [Header("Action Gauge")]
        [SerializeField] private ActionGaugeBar gaugeBar;
        [SerializeField] private UnitListUI allyListUI;
        [SerializeField] private UnitListUI enemyListUI;
        
        void Awake()
        {
            SetCanvasGroup(skillPanelGroup, false);
            SetCanvasGroup(targetPanelGroup, false);
            if (screenFlash != null)
                screenFlash.gameObject.SetActive(false);
            
        }

        public UniTask<Skill> PickSkillAsync(Unit actor)
        {
            // キャラ情報とゲージ表示更新
            ShowCharacterInfo(actor);

            // ReactiveCommand をクリアして再利用可能に
            onSkillSelected = new();

            // スキルパネルを開く
            SetCanvasGroup(skillPanelGroup, true);

            // ボタン設定
            for (int i = 0; i < skillButtons.Length; i++)
            {
                skillButtons[i].onClick.RemoveAllListeners();
                if (i < actor.Skills.Count)
                {
                    var skill = actor.Skills[i];
                    skillLabels[i].text = skill.Name;
                    skillButtons[i].interactable = actor.CurrentGauge.Value >= skill.GaugeCost;
                    skillButtons[i].onClick.AddListener(() =>
                    {
                        SetCanvasGroup(skillPanelGroup, false);
                        onSkillSelected.Execute(skill);
                    });
                }
                else
                {
                    skillLabels[i].text = "";
                    skillButtons[i].interactable = false;
                }
            }

            // ReactiveCommand から UniTask を生成して返す
            return onSkillSelected
                .ToUniTask()                      // 最初の 1 回だけ受け取る
                .ContinueWith(skill =>
                {
                    SetCanvasGroup(skillPanelGroup, false);
                    return skill;
                });
        }

        public UniTask<Unit> PickTargetAsync(Unit actor, List<Unit> candidates)
        {
            onTargetSelected = new();
            SetCanvasGroup(targetPanelGroup, true);

            // 既存ボタンをクリア
            foreach (Transform child in targetButtonContainer)
                Destroy(child.gameObject);

            // 新規ボタン生成
            foreach (var unit in candidates)
            {
                if (unit.IsDead) continue;
                var btn = Instantiate(targetButtonPrefab, targetButtonContainer);
                var label = btn.GetComponentInChildren<TMP_Text>();
                label.text = unit.DisplayName; // displayName を使いましょう
                btn.onClick.AddListener(() =>
                {
                    SetCanvasGroup(targetPanelGroup, false);
                    onTargetSelected.Execute(unit);
                });
                
                // ボタンの位置をずらす
            }

            // ReactiveCommand → UniTask
            return onTargetSelected
                .ToUniTask()
                .ContinueWith(u =>
                {
                    SetCanvasGroup(targetPanelGroup, false);
                    return u;
                });
        }

        public async UniTask PlaySkillAnimationAsync(Unit actor, Skill skill, Unit target)
        {
            if (screenFlash != null)
            {
                screenFlash.gameObject.SetActive(true);
                screenFlash.color = new Color(1, 1, 1, 0);
                float half = flashDuration / 2f;
                float t = 0f;
                // フェードイン
                while (t < half)
                {
                    t += Time.deltaTime;
                    screenFlash.color = screenFlash.color.WithAlpha(Mathf.Clamp01(t / half));
                    await UniTask.Yield();
                }
                t = 0f;
                // フェードアウト
                while (t < half)
                {
                    t += Time.deltaTime;
                    screenFlash.color = screenFlash.color.WithAlpha(1 - Mathf.Clamp01(t / half));
                    await UniTask.Yield();
                }
                screenFlash.gameObject.SetActive(false);
            }
            else
            {
                await UniTask.Delay(TimeSpan.FromSeconds(flashDuration));
            }
        }

        public void UpdateGaugeDisplay(Unit unit)
        {
            // HUDSlot が ReactiveProperty で自動更新する想定なので不要
        }

        public void InitializeUnits(IEnumerable<Unit> allUnits, IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            // ゲージバー
            gaugeBar.Initialize(allUnits);
            // 味方リスト
            allyListUI.Populate(allies.ToList());
            // 敵リスト
            enemyListUI.Populate(enemies.ToList());
        }


        // ヘルパー: キャラ情報を一括表示
        private void ShowCharacterInfo(Unit actor)
        {
            characterNameText.text = actor.DisplayName;
            hpSlider.maxValue = actor.Stats.MaxHP;
            hpSlider.value = actor.Stats.CurrentHP;
            hpValueText.text = $"{actor.Stats.CurrentHP}/{actor.Stats.MaxHP}";
            atkValueText.text = actor.Stats.ATK.ToString();
            gaugeSlider.maxValue = 100f;
            gaugeSlider.value = actor.CurrentGauge.Value;
            gaugeValueText.text = $"{actor.CurrentGauge.Value:0}/{100:0}";

            actor.CurrentGauge
                .Subscribe(g =>
                {
                    gaugeSlider.value = g;
                    gaugeValueText.text = $"{g:0}/{100:0}";
                })
                .AddTo(this);
        }

        private void SetCanvasGroup(CanvasGroup cg, bool visible)
        {
            cg.alpha = visible ? 1f : 0f;
            cg.blocksRaycasts = visible;
            cg.interactable = visible;
        }
    }

    // 拡張メソッド
    public static class ImageExtensions
    {
        public static Color WithAlpha(this Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
    
    public static class ReactiveCommandExtensions
    {
        public static UniTask<T> ToUniTask<T>(this ReactiveCommand<T> command)
        {
            var tcs = new UniTaskCompletionSource<T>();
            IDisposable sub = null;
            sub = command.Subscribe(value =>
            {
                tcs.TrySetResult(value);
                sub.Dispose();      // １回だけ受け取ったら購読解除
            });
            return tcs.Task;
        }
    }
}
