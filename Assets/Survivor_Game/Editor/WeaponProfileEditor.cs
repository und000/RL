using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponStatsProfile))]
public class WeaponStatsProfileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeaponStatsProfile profile = (WeaponStatsProfile)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Configuration Summary", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Basic Attack Combo Count", profile.BasicAttackCount.ToString());
        EditorGUILayout.LabelField(
            "Weapon Skill Count",
            profile.WeaponSkills != null ? profile.WeaponSkills.Length.ToString() : "0");

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
        DrawDefaultInspector();

        WeaponSkillProfile profile = (WeaponSkillProfile)target;
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
        for (int index = 0; index < profile.BasicAttackCount; index++)
        {
            WeaponAttackStep step = profile.GetBasicAttackStep(index);
            if (step != null)
            {
                ValidateAnimatorState(
                    animator,
                    step.animatorStateName,
                    $"Basic Attack Step {index + 1}");
            }
        }

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
        for (int stepIndex = 0; stepIndex < profile.BasicAttackCount; stepIndex++)
        {
            WeaponAttackStep step = profile.GetBasicAttackStep(stepIndex);
            if (step?.hitWindows == null) continue;
            for (int hitIndex = 0; hitIndex < step.hitWindows.Length; hitIndex++)
            {
                WeaponHitWindow hit = step.hitWindows[hitIndex];
                if (hit != null && !groupIds.Contains(hit.hitboxGroupId))
                {
                    EditorGUILayout.HelpBox(
                        $"Attack {stepIndex + 1}, Hit {hitIndex + 1}: " +
                        $"Hitbox Group '{hit.hitboxGroupId}' is missing from this prefab.",
                        MessageType.Error);
                }
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
