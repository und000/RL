using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(WeaponFamily))]
public class WeaponFamilyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        var values = new List<int> { 1, 4, 2, 32, 64, 128 };
        var names = new List<string> { "한손검", "대검", "단검", "도", "쌍검", "총" };
        int current = property.intValue;
        bool single = property.name == "weaponFamily";
        label = new GUIContent(single ? "무기군" : "사용 가능 무기군", property.tooltip);
        if (single)
        {
            if (!values.Contains(current))
            {
                values.Add(current);
                names.Add($"{WeaponFamilyUtility.Label((WeaponFamily)current)} (기존 값)");
            }
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.IntPopup(position, label.text, current, names.ToArray(), values.ToArray());
            if (EditorGUI.EndChangeCheck()) property.intValue = next;
        }
        else
        {
            // Preserve old asset masks without offering legacy families on newly authored skills.
            if (current != (int)WeaponFamily.All)
            {
                if ((current & 8) != 0) { values.Add(8); names.Add("창(기존)"); }
                if ((current & 16) != 0) { values.Add(16); names.Add("기타(기존)"); }
            }
            int selection = 0;
            for (int i = 0; i < values.Count; i++)
                if ((current & values[i]) != 0) selection |= 1 << i;
            EditorGUI.BeginChangeCheck();
            int selected = EditorGUI.MaskField(position, label, selection, names.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                int mask = 0;
                if (selected == -1) mask = (int)WeaponFamily.All;
                else for (int i = 0; i < values.Count; i++)
                    if ((selected & (1 << i)) != 0) mask |= values[i];
                property.intValue = mask;
            }
        }
        EditorGUI.EndProperty();
    }
}

[CustomEditor(typeof(WeaponStatsProfile))]
public class WeaponStatsProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeaponStatsProfile profile = (WeaponStatsProfile)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
        if (WeaponFamilyUtility.NumberBase(profile.Family) > 0 && !profile.HasValidWeaponId)
            EditorGUILayout.HelpBox($"무기 번호는 {WeaponFamilyUtility.NumberBase(profile.Family) + 1}~{WeaponFamilyUtility.NumberBase(profile.Family) + 999} 범위여야 합니다.", MessageType.Warning);
        EditorGUILayout.LabelField("Basic Attack Combo Count", profile.BasicAttackCount.ToString());
        EditorGUILayout.LabelField("Weapon Skill Count", profile.SkillCount.ToString());
        EditorGUILayout.LabelField("Heavy Attack", profile.HasHeavyAttack ? "Enabled" : "Disabled");
        EditorGUILayout.LabelField("Special Attack", profile.DefaultSpecialAttack != null ? profile.DefaultSpecialAttack.DisplayName : "Missing");
        EditorGUILayout.LabelField("Special Attack Slot", profile.LockSpecialAttack ? "Unique / Locked" : "Replaceable");
        if (!WeaponFamilyUtility.IsSingle(profile.Family))
            EditorGUILayout.HelpBox("무기군은 하나만 선택해야 합니다.", MessageType.Error);
        if (profile.DefaultSpecialAttack == null)
            EditorGUILayout.HelpBox("Assign a Default Special Attack for newly acquired weapons.", MessageType.Warning);
        else if (!profile.DefaultSpecialAttack.CanUseOn(profile))
            EditorGUILayout.HelpBox("Default Special Attack is not compatible with this weapon.", MessageType.Error);
        if (profile.SpecialAttackSlotClip == null)
            EditorGUILayout.HelpBox("Assign the original clip used by the SpecialAttack Animator state.", MessageType.Error);
        if (profile.LockSpecialAttack && profile.DefaultSpecialAttack != null && profile.DefaultSpecialAttack.ExclusiveWeapon != profile)
            EditorGUILayout.HelpBox("A unique weapon's default skill must set Exclusive Weapon to this weapon.", MessageType.Error);

        if (profile.WeaponPrefab == null)
        {
            EditorGUILayout.HelpBox("Weapon Prefab is required.", MessageType.Error);
        }
        if (string.IsNullOrWhiteSpace(profile.IdleStateName))
        {
            EditorGUILayout.HelpBox(
                "Idle State Name is required for every weapon.",
                MessageType.Error);
        }
        if (profile.BasicAttackCount == 0)
        {
            EditorGUILayout.HelpBox(
                "Add at least one Basic Attack Step.",
                MessageType.Warning);
        }
    }
}

[CustomEditor(typeof(WeaponSkillProfile))]
public class WeaponSkillProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        WeaponSkillProfile profile = (WeaponSkillProfile)target;
        serializedObject.Update();
        if (profile.IsEquippableSpecialAttack)
            DrawPropertiesExcluding(serializedObject, "animationSteps", "cooldown", "recoveryDuration", "useWeaponAttackSpeed", "canInterruptBasicAttack");
        else DrawPropertiesExcluding(serializedObject, "icon", "description", "allowedWeaponFamilies", "exclusiveWeapon", "mpCost", "specialAttackClip", "specialAttack", "specialAttackVfx");
        serializedObject.ApplyModifiedProperties();
        if (profile.IsEquippableSpecialAttack)
        {
            EditorGUILayout.HelpBox("Right-click skill: MP is charged once on activation. StartAttackMovement events control its movement. The SpecialAttack state is used on the equipped weapon.", MessageType.Info);
            EditorGUILayout.LabelField("사용 가능 무기군", profile.AllowedWeaponFamiliesLabel);
            if ((profile.AllowedWeaponFamilies & WeaponFamily.All) == WeaponFamily.None)
                EditorGUILayout.HelpBox("사용 가능한 무기군을 하나 이상 선택하세요. 현재는 어떤 무기도 사용할 수 없습니다.", MessageType.Warning);
            if (profile.ExclusiveWeapon != null && (profile.AllowedWeaponFamilies & profile.ExclusiveWeapon.Family) == 0)
                EditorGUILayout.HelpBox("전용 무기의 무기군도 사용 가능 무기군에 포함해야 합니다.", MessageType.Error);
            if (profile.SpecialAttackClip == null)
                EditorGUILayout.HelpBox("Special Attack Clip is required.", MessageType.Error);
            if (profile.ExclusiveWeapon != null && (!profile.ExclusiveWeapon.LockSpecialAttack || profile.ExclusiveWeapon.DefaultSpecialAttack != profile))
                EditorGUILayout.HelpBox("Exclusive Weapon must lock its slot and use this skill as its default.", MessageType.Error);
            return;
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Count", profile.AnimationCount.ToString());
        if (profile.AnimationCount == 0)
        {
            EditorGUILayout.HelpBox(
                "This skill has no animation steps.",
                MessageType.Warning);
        }
    }
}

[CustomEditor(typeof(MeleeWeaponAttack))]
public class MeleeWeaponAttackEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SerializedProperty profileProperty = serializedObject.FindProperty("weaponStats");
        SerializedProperty animatorProperty = serializedObject.FindProperty("swingAnimator");
        SerializedProperty groupsProperty = serializedObject.FindProperty("hitboxGroups");
        WeaponStatsProfile profile = profileProperty.objectReferenceValue as WeaponStatsProfile;
        Animator animator = animatorProperty.objectReferenceValue as Animator;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefab Validation", EditorStyles.boldLabel);
        if (profile == null)
        {
            EditorGUILayout.HelpBox("Weapon Profile is required.", MessageType.Error);
            return;
        }
        if (animator == null)
        {
            EditorGUILayout.HelpBox("Swing Animator is required.", MessageType.Error);
            return;
        }

        ValidateAnimatorState(animator, profile.IdleStateName, "required Idle");
        for (int skillIndex = 0; skillIndex < profile.SkillCount; skillIndex++)
        {
            WeaponSkillProfile skill = profile.GetSkill(skillIndex);
            if (skill?.AnimationSteps == null) continue;
            for (int motionIndex = 0; motionIndex < skill.AnimationSteps.Length; motionIndex++)
            {
                WeaponSkillMotionStep motion = skill.AnimationSteps[motionIndex];
                if (motion != null)
                {
                    ValidateAnimatorState(
                        animator,
                        motion.animatorStateName,
                        $"Skill '{skill.DisplayName}' Motion {motionIndex + 1}");
                }
            }
        }

        if (profile.SkillCount > 0 &&
            ((MeleeWeaponAttack)target).GetComponent<WeaponSkillController>() == null)
        {
            EditorGUILayout.HelpBox(
                "Weapon Skill Controller is required when the profile has skills.",
                MessageType.Error);
        }

        HashSet<string> groupIds = CollectHitboxGroupIds(groupsProperty);
        for (int index = 0; index < profile.BasicAttackCount; index++)
        {
            ValidateStep(animator, profile.GetBasicAttackStep(index), groupIds, $"Basic Attack {index + 1}");
        }
        if (profile.HasHeavyAttack) ValidateStep(animator, profile.HeavyAttack, groupIds, "Heavy Attack");
        if (profile.HasSpecialAttack) ValidateStep(animator, profile.SpecialAttack, groupIds, "Special Attack");
    }

    private static void ValidateStep(
        Animator animator, WeaponAttackStep step, HashSet<string> groupIds, string label)
    {
        if (step == null)
        {
            EditorGUILayout.HelpBox($"{label} is missing its attack step.", MessageType.Error);
            return;
        }
        ValidateAnimatorState(animator, step.animatorStateName, label);
        if (step.hitWindows == null) return;
        for (int index = 0; index < step.hitWindows.Length; index++)
        {
            WeaponHitWindow hit = step.hitWindows[index];
            if (hit != null && !groupIds.Contains(hit.hitboxGroupId))
            {
                EditorGUILayout.HelpBox(
                    $"{label}, Hit {index + 1}: Hitbox Group '{hit.hitboxGroupId}' is missing from this prefab.",
                    MessageType.Error);
            }
        }
    }

    private static void ValidateAnimatorState(
        Animator animator,
        string stateName,
        string label)
    {
        if (string.IsNullOrWhiteSpace(stateName) ||
            !animator.HasState(0, Animator.StringToHash(stateName)))
        {
            EditorGUILayout.HelpBox(
                $"Animator is missing {label} state '{stateName}'.",
                MessageType.Error);
        }
    }

    private static HashSet<string> CollectHitboxGroupIds(SerializedProperty groups)
    {
        HashSet<string> ids = new HashSet<string>();
        if (groups == null || !groups.isArray) return ids;

        for (int index = 0; index < groups.arraySize; index++)
        {
            SerializedProperty group = groups.GetArrayElementAtIndex(index);
            SerializedProperty id = group.FindPropertyRelative("id");
            if (id != null && !string.IsNullOrWhiteSpace(id.stringValue))
            {
                ids.Add(id.stringValue);
            }
        }
        return ids;
    }
}
