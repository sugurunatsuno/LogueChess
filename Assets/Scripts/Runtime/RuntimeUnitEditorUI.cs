using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LogueChess.Runtime;
using UnityEngine;
using UnityEngine.UI;
using LogueChess.Runtime;
using TMPro;

namespace RougeChess.Runtime
{
    /// <summary>
/// Runtime editor UI for inspecting and modifying UnitDataSO assets in scene.
/// Attach to a Canvas GameObject with configured UI elements.
/// </summary>
    public class RuntimeUnitEditorUI : MonoBehaviour, IUIService
    {
        [Header("Dropdowns and Buttons")]
        public TMP_Dropdown unitDropdown;
        public Button saveButton;

        [Header("Basic Fields")]
        public TMP_InputField idInput;
        public TMP_InputField nameInput;
        public Image iconPreview;

        [Header("Stats Fields")]
        public TMP_InputField maxHPInput;
        public TMP_InputField atkInput;
        public TMP_InputField defInput;
        public TMP_InputField gaugeSpeedInput;

        [Header("Resistances Container")]
        public Transform resistancesParent;
        public GameObject resistanceEntryPrefab;

        [Header("Skills Container")]
        public Transform skillsParent;
        public GameObject skillSlotPrefab;

        [Header("Perks Container")]
        public Transform perksParent;
        public GameObject perkSlotPrefab;

        [Header("Buffs Container")]
        public Transform buffsParent;
        public GameObject buffSlotPrefab;

        private List<UnitDataSO> allUnits;
        private UnitDataSO currentUnit;

        void Start()
        {
            // Load all UnitDataSO assets from Resources/GameData/Units folder
            allUnits = Resources.LoadAll<UnitDataSO>("GameData/Units").ToList();
            PopulateDropdown();
            unitDropdown.onValueChanged.AddListener(OnUnitSelected);
            saveButton.onClick.AddListener(OnSaveClicked);
            if(allUnits.Count>0) unitDropdown.value = 0;
        }

        void PopulateDropdown()
        {
            unitDropdown.options.Clear();
            foreach(var u in allUnits)
                unitDropdown.options.Add(new TMP_Dropdown.OptionData(u.displayName));
            unitDropdown.RefreshShownValue();
        }

        void OnUnitSelected(int index)
        {
            currentUnit = allUnits[index];
            RefreshUI();
        }

        void RefreshUI()
        {
            // Basic
            idInput.text = currentUnit.unitId;
            nameInput.text = currentUnit.displayName;
            iconPreview.sprite = currentUnit.icon;
            // Stats
            maxHPInput.text = currentUnit.maxHP.ToString();
            atkInput.text = currentUnit.atk.ToString();
            defInput.text = currentUnit.def.ToString();
            gaugeSpeedInput.text = currentUnit.gaugeSpeed.ToString();
            // Resistances
            PopulateList(currentUnit.resistances, resistancesParent, resistanceEntryPrefab, entry=> {
                var comp = entry.GetComponent<ResistanceEntryUI>();
                comp.Setup(currentUnit.resistances[0]);
            });
            // Skills
            PopulateList(currentUnit.skills, skillsParent, skillSlotPrefab, slot=> {
                var comp = slot.GetComponent<SkillSlotUI>();
                comp.Setup((SkillDataSO)slot.GetComponent<ScriptableObject>());
            });
            // Perks
            PopulateList(currentUnit.startingPerks, perksParent, perkSlotPrefab, slot=> {
                var comp = slot.GetComponent<PerkSlotUI>();
                comp.Setup((PerkDataSO)slot.GetComponent<ScriptableObject>());
            });
            // Buffs
            PopulateList(currentUnit.startingBuffs, buffsParent, buffSlotPrefab, slot=> {
                var comp = slot.GetComponent<BuffSlotUI>();
                comp.Setup((BuffDataSO)slot.GetComponent<ScriptableObject>());
            });
        }

        void PopulateList<T>(IEnumerable<T> list, Transform parent, GameObject prefab, Action<GameObject> initializer)
        {
            foreach(Transform child in parent) Destroy(child.gameObject);
            foreach(var item in list)
            {
                var go = Instantiate(prefab, parent);
                // Assuming prefab has component to display T
                var display = go.GetComponent<MonoBehaviour>();
                // Expose item for UI component
                initializer(go);
            }
        }

        void OnSaveClicked()
        {
            // Apply back to currentUnit
            currentUnit.unitId = idInput.text;
            currentUnit.displayName = nameInput.text;
            // icon handled by drag in Inspector only
            int.TryParse(maxHPInput.text, out currentUnit.maxHP);
            int.TryParse(atkInput.text, out currentUnit.atk);
            int.TryParse(defInput.text, out currentUnit.def);
            float.TryParse(gaugeSpeedInput.text, out currentUnit.gaugeSpeed);
            // Resistances, skills, perks, buffs modifications handled inside their UI components

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(currentUnit);
            UnityEditor.AssetDatabase.SaveAssets();
            #endif
            Debug.Log("UnitDataSO saved: " + currentUnit.unitId);
        }

        public UniTask<Skill> PickSkillAsync(Unit actor)
        {
            throw new NotImplementedException();
        }

        public UniTask<Unit> PickTargetAsync(Unit actor, List<Unit> candidates)
        {
            throw new NotImplementedException();
        }

        public UniTask PlaySkillAnimationAsync(Unit actor, Skill skill, Unit target)
        {
            throw new NotImplementedException();
        }

        public void UpdateGaugeDisplay(Unit unit)
        {
            throw new NotImplementedException();
        }

        public void InitializeUnits(IEnumerable<Unit> allUnits, IEnumerable<Unit> allies, IEnumerable<Unit> enemies)
        {
            throw new NotImplementedException();
        }
    }

}

