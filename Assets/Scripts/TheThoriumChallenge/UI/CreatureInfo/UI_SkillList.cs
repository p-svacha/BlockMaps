using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework.UI
{
    public class UI_SkillList : MonoBehaviour
    {
        [Header("Elements")]
        public GameObject SkillListContainer;

        [Header("Prefabs")]
        public UI_SkillRow SkillRowPrefab;

        public void Init(Comp_Skills skillComp)
        {
            HelperFunctions.DestroyAllChildredImmediately(SkillListContainer);

            // Skills
            foreach (Skill skill in skillComp.GetAllSkills())
            {
                UI_SkillRow skillRow = Instantiate(SkillRowPrefab, SkillListContainer.transform);
                skillRow.Init(skill);
            }
        }
    }
}
