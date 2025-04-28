using System;
using System.Collections.Generic;
using UnityEngine;

namespace LogueChess.Runtime
{
    public enum SkillType { Basic, Heavy, Ultimate }
    public enum BuffType { Buff, Debuff }
    public enum TargetType { Self, SingleAlly, AllAllies, SingleEnemy, AllEnemies }
    public enum StatType { ATK, DEF, MaxHP, GaugeSpeed, Shield }
    public enum PerkTrigger { OnGaugeGain, BeforeSkill, OnModifyDamage, AfterSkill }

    // =====================
    // Character (Unit) Data
    // =====================
    [CreateAssetMenu(fileName = "UnitData", menuName = "GameData/UnitData")]
    public class UnitDataSO : ScriptableObject
    {
        [Header("Identification")]
        public string unitId;
        public string displayName;
        public Sprite icon;

        [Header("Base Stats")]
        public int maxHP;
        public int atk;
        public int def;
        public float gaugeSpeed;
        
        
        [Header("Resistances")]
        public ResistanceEntry[] resistances; // type and value (-1.0 to 1.0)

        [Header("References")]
        public List<SkillDataSO> skills;       // exactly 3 entries: Basic, Heavy, Ultimate
        public List<PerkDataSO> startingPerks;
        public List<BuffDataSO> startingBuffs;

        [Header("Tags")]
        public string[] tags;
    }
    
    [Serializable]
    public class ResistanceEntry
    {
        public string type;   // 例: "Physical", "Fire"
        [Range(-1f, 1f)]
        public float value;   // -1.0 でダメ2倍、0 で変化なし、1.0 でダメ完全無効
    }

    // =====================
    // Skill Data
    // =====================
    [CreateAssetMenu(fileName = "SkillData", menuName = "GameData/SkillData")]
    public class SkillDataSO : ScriptableObject
    {
        public string skillId;
        public string skillName;
        public SkillType type;
        public float gaugeCost;
        public float basePower;
        public float cooldownTime;
        public string attributeType;
        public BuffDataSO effect;        // optional buff/debuff cast on target
    }

    // =====================
    // Buff/Debuff Data
    // =====================
    [CreateAssetMenu(fileName = "BuffData", menuName = "GameData/BuffData")]
    public class BuffDataSO : ScriptableObject
    {
        public string buffId;
        public string buffName;
        public BuffType buffType;
        public TargetType target;
        public StatType affectedStat;
        public float amount;
        public int durationTurns;
        public bool stackable;
    }

    // =====================
    // Perk Data
    // =====================
    [CreateAssetMenu(fileName = "PerkData", menuName = "GameData/PerkData")]
    public class PerkDataSO : ScriptableObject
    {
        public string perkId;
        public string perkName;
        [TextArea] public string description;
        public PerkTrigger[] triggers;
        public PerkParam[] parameters;
    }

    [Serializable]
    public class PerkParam
    {
        public string paramKey;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
    }
}
