
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using LogueChess.Runtime; // Unit, Skill, Stats, BuffInstance, IPerk

namespace LogueChess.Runtime
{
    /// <summary>
    /// ScriptableObject ベースの PartyService。Inspector から UnitDataSO を設定できます。
    /// </summary>
    [CreateAssetMenu(fileName = "PartyServiceSO", menuName = "LogueChess/Party Service")]
    public class PartyServiceSO : ScriptableObject, IPartyService
    {
        [Header("Player Units Configuration")]
        public List<UnitDataSO> playerUnits; // インスペクタで設定

        public UniTask<List<Unit>> GetPartyUnitsAsync()
        {
            // UnitDataSO を Runtime モデルに変換
            var units = playerUnits
                .Where(cfg => cfg != null)
                .Select(cfg => cfg.ToRuntimeUnit())
                .ToList();
            return UniTask.FromResult(units);
        }
    }

    /// <summary>
    /// ScriptableObject ベースの EnemyService。Inspector から UnitDataSO を設定できます。
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyServiceSO", menuName = "LogueChess/Enemy Service")]
    public class EnemyServiceSO : ScriptableObject, IEnemyService
    {
        [Header("Enemy Units Configuration")]
        public List<UnitDataSO> enemyUnits;

        public UniTask<List<Unit>> GetEnemiesAsync()
        {
            var units = enemyUnits
                .Where(cfg => cfg != null)
                .Select(cfg => cfg.ToRuntimeUnit())
                .ToList();
            return UniTask.FromResult(units);
        }
    }

    /// <summary>
    /// UnitDataSO (SO) から実際のランタイム用 Unit を生成する拡張メソッド
    /// </summary>
    public static class UnitDataExtensions
    {
        public static Unit ToRuntimeUnit(this UnitDataSO cfg)
        {
            // Stats
            var stats = new Stats(cfg.atk, cfg.gaugeSpeed, cfg.maxHP);
            // Skills
            var skills = cfg.skills != null
                ? cfg.skills
                    .Where(s => s != null)
                    .Select(s => new Skill(
                        s.skillId,
                        s.skillName,
                        s.type,
                        s.gaugeCost,
                        s.basePower,
                        s.attributeType,
                        s.effect != null
                            ? new BuffInstance(
                                s.effect.buffId,
                                s.effect.durationTurns,
                                s.effect.amount,
                                damage => damage * (1f - (s.effect.affectedStat == StatType.GaugeSpeed ? 0f : 0f)) // 実際は StatType に応じた処理
                              )
                            : null
                    ))
                    .ToList()
                : new List<Skill>();
            
            // Perks：IPerk 実装は別途 ScriptablePerk などでラップ
            var perks = cfg.startingPerks != null
                ? cfg.startingPerks
                    .Where(p => p != null)
                    .Select(p => new ScriptablePerk(p) as IPerk)
                    .ToList()
                : new List<IPerk>();

            // Unit 生成
            var unit = new Unit(cfg.unitId, stats, skills, perks);
            // 初期バフ
            if (cfg.startingBuffs != null)
            {
                foreach (var b in cfg.startingBuffs.Where(bf => bf != null))
                {
                    unit.ApplyBuff(new BuffInstance(
                        b.buffId,
                        b.durationTurns,
                        b.amount,
                        dmg =>
                        {
                            // 効果量の乗算
                            if (b.affectedStat == StatType.ATK)
                                return dmg * (1f + b.amount);
                            return dmg;
                        }
                    ));
                }
            }
            return unit;
        }
    }
}

// ※ ScriptablePerk クラスのサンプル実装
namespace LogueChess.Runtime
{
    /// <summary>
    /// PerkDataSO をラップして IPerk 実装を提供
    /// </summary>
    public class ScriptablePerk : IPerk
    {
        private readonly PerkDataSO data;
        public ScriptablePerk(PerkDataSO so) { data = so; }
        public float OnGaugeGain(Unit unit, float gain) => gain;
        public void BeforeSkill(Unit unit, Skill skill) {}
        public float OnModifyDamage(Unit attacker, Unit defender, Skill skill, float damage) => damage;
        public void AfterSkill(Unit unit, Skill skill) {}
    }
}
    