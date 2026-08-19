using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EnemySpawnTable))]
public class EnemySpawnTableEditor : Editor
{
    private SerializedProperty spawnMode;
    private SerializedProperty batchInterval;
    private SerializedProperty entries;
    private bool showEntries = true;

    private void OnEnable()
    {
        spawnMode = serializedObject.FindProperty("spawnMode");
        batchInterval = serializedObject.FindProperty("batchInterval");
        entries = serializedObject.FindProperty("entries");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(spawnMode, new GUIContent("Spawn Mode", "소환 방식을 선택합니다."));
        EditorGUILayout.PropertyField(batchInterval,
            new GUIContent("Batch Interval", "이 테이블이 한 번 실행된 뒤 다음 실행까지의 시간(초)입니다."));
        EditorGUILayout.Space();

        showEntries = EditorGUILayout.Foldout(showEntries,
            $"Entries ({entries.arraySize})", true);
        if (showEntries)
        {
            EditorGUI.indentLevel++;
            for (int index = 0; index < entries.arraySize; index++)
            {
                DrawEntry(entries.GetArrayElementAtIndex(index), index);
            }
            EditorGUI.indentLevel--;

            if (GUILayout.Button("Add Entry"))
            {
                entries.InsertArrayElementAtIndex(entries.arraySize);
                ResetNewEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1));
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEntry(SerializedProperty entry, int index)
    {
        entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded,
            $"Element {index}", true);
        if (!entry.isExpanded) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("prefab"));
        EnemySpawnTable.SpawnMode mode =
            (EnemySpawnTable.SpawnMode)spawnMode.enumValueIndex;

        if (mode == EnemySpawnTable.SpawnMode.WeightedRandom)
        {
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("weight"),
                new GUIContent("Weight", "조건을 만족한 항목 사이의 상대적인 선택 가중치입니다."));
            DrawGroupSize(entry);
        }
        else if (mode == EnemySpawnTable.SpawnMode.FixedBatch)
        {
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("fixedSpawnCount"),
                new GUIContent("Spawn Count", "배치마다 확정적으로 생성할 수입니다."));
        }
        else
        {
            DrawGroupSize(entry);
        }

        EditorGUILayout.PropertyField(entry.FindPropertyRelative("startTime"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("endTime"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("minimumPlayerLevel"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("maximumPlayerLevel"));

        if (GUILayout.Button("Remove Entry"))
        {
            entries.DeleteArrayElementAtIndex(index);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2f);
    }

    private static void DrawGroupSize(SerializedProperty entry)
    {
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("minimumGroupSize"));
        EditorGUILayout.PropertyField(entry.FindPropertyRelative("maximumGroupSize"));
    }

    private static void ResetNewEntry(SerializedProperty entry)
    {
        entry.FindPropertyRelative("prefab").objectReferenceValue = null;
        entry.FindPropertyRelative("weight").floatValue = 1f;
        entry.FindPropertyRelative("fixedSpawnCount").intValue = 1;
        entry.FindPropertyRelative("startTime").floatValue = 0f;
        entry.FindPropertyRelative("endTime").floatValue = 0f;
        entry.FindPropertyRelative("minimumPlayerLevel").intValue = 1;
        entry.FindPropertyRelative("maximumPlayerLevel").intValue = 999;
        entry.FindPropertyRelative("minimumGroupSize").intValue = 1;
        entry.FindPropertyRelative("maximumGroupSize").intValue = 1;
    }
}
