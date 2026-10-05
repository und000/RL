using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class EnemyContentValidator
{
    private const string EnemyPrefabFolder = "Assets/Survivor_Game/Prefabs/Enemies";
    private const string ProjectilePrefabFolder = "Assets/Survivor_Game/Prefabs/Projectile";
    private const string SpawnDataFolder = "Assets/Survivor_Game/Data/Spawning";
    private const string EnemyDataFolder = "Assets/Survivor_Game/Data/Enemies";

    [MenuItem("Tools/Survivor/Validate Enemy Content")]
    public static void ValidateAll()
    {
        int errors = 0;
        int warnings = 0;
        ValidateEnemyProfiles(ref errors, ref warnings);
        ValidateEnemyPrefabs(ref errors, ref warnings);
        ValidateProjectilePrefabs(ref errors, ref warnings);
        ValidateSpawnTables(ref errors, ref warnings);

        string summary = $"Enemy content validation complete: {errors} error(s), " +
            $"{warnings} warning(s).";
        if (errors > 0) Debug.LogError(summary);
        else if (warnings > 0) Debug.LogWarning(summary);
        else Debug.Log(summary);
    }

    private static void ValidateEnemyProfiles(ref int errors, ref int warnings)
    {
        Dictionary<string, EnemyProfile> ids = new Dictionary<string, EnemyProfile>();
        foreach (string guid in AssetDatabase.FindAssets("t:EnemyProfile", new[] { EnemyDataFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemyProfile profile = AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
            if (profile == null) continue;
            if (string.IsNullOrWhiteSpace(profile.EnemyId))
            {
                ReportError(profile, "Enemy ID is empty.", ref errors);
            }
            else if (ids.TryGetValue(profile.EnemyId, out EnemyProfile duplicate))
            {
                ReportError(profile,
                    $"Enemy ID '{profile.EnemyId}' duplicates {duplicate.name}.", ref errors);
            }
            else ids.Add(profile.EnemyId, profile);

            if (profile.Experience > 0 && profile.ExperienceGemPrefab == null)
            {
                ReportWarning(profile, "Experience is positive but the gem prefab is empty.", ref warnings);
            }
            if (profile.Rank == EnemyRank.Boss &&
                (profile.HealthBarType != HealthBarType.Boss ||
                 profile.MarkerType != EnemyMarkerType.Boss))
            {
                ReportWarning(profile,
                    "Boss profiles should use the boss health bar and boss marker.", ref warnings);
            }
        }
    }

    private static void ValidateEnemyPrefabs(ref int errors, ref int warnings)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        foreach (GameObject prefab in LoadPrefabs(EnemyPrefabFolder))
        {
            EnemyHealth health = prefab.GetComponent<EnemyHealth>();
            if (health == null) continue;
            if (health.Profile == null)
            {
                ReportError(prefab, "EnemyHealth requires an EnemyProfile.", ref errors);
            }
            if (enemyLayer < 0 || prefab.layer != enemyLayer)
            {
                ReportError(prefab, "Root object must use the Enemy layer.", ref errors);
            }
            if (prefab.GetComponent<Rigidbody2D>() == null ||
                prefab.GetComponent<Collider2D>() == null)
            {
                ReportError(prefab, "Enemy root requires Rigidbody2D and Collider2D.", ref errors);
            }
            int movementCount = prefab.GetComponents<EnemyMovement>().Length +
                prefab.GetComponents<EnemyMovementPatternBase>().Length;
            if (movementCount > 1)
            {
                ReportError(prefab, "Multiple movement controllers are attached.", ref errors);
            }
            if (prefab.GetComponent<EnemyOffscreenMarkerTarget>() == null)
            {
                ReportWarning(prefab, "Off-screen marker target is missing.", ref warnings);
            }
            if (prefab.GetComponent<EnemyHealthBar>() == null)
            {
                ReportError(prefab, "Enemy prefab requires EnemyHealthBar.", ref errors);
            }

            EnemyPatternController patternController = prefab.GetComponent<EnemyPatternController>();
            if (patternController != null && prefab.GetComponents<EnemyAttackPattern>().Length == 0)
            {
                ReportWarning(prefab,
                    "EnemyPatternController has no EnemyAttackPattern components.", ref warnings);
            }

            foreach (EnemyProjectileAttackPattern attack in
                prefab.GetComponents<EnemyProjectileAttackPattern>())
            {
                if (attack.ProjectilePrefab == null ||
                    attack.ProjectilePrefab.GetComponent<EnemyProjectile>() == null)
                {
                    ReportError(prefab,
                        "Projectile attack pattern requires an EnemyProjectile prefab.",
                        ref errors);
                }
                if (attack.RequiresAttackGate && !HasAttackGate(prefab))
                {
                    ReportError(prefab,
                        "Projectile attack requires a gate but no IEnemyAttackGate exists.",
                        ref errors);
                }
            }
        }
    }

    private static void ValidateProjectilePrefabs(ref int errors, ref int warnings)
    {
        foreach (GameObject prefab in LoadPrefabs(ProjectilePrefabFolder))
        {
            if (!prefab.name.StartsWith("Projectile_Enemy") ||
                prefab.GetComponent<Projectile>() != null) continue;
            if (prefab.GetComponent<EnemyProjectile>() == null)
            {
                ReportError(prefab, "Enemy projectile requires EnemyProjectile on its root.", ref errors);
            }
            Rigidbody2D body = prefab.GetComponent<Rigidbody2D>();
            Collider2D collider = prefab.GetComponent<Collider2D>();
            if (body == null || collider == null || !collider.isTrigger)
            {
                ReportError(prefab,
                    "Enemy projectile requires Rigidbody2D and a trigger Collider2D.", ref errors);
            }
            Transform render = prefab.transform.Find("Render");
            if (render == null || render.Find("Sprite") == null)
            {
                ReportWarning(prefab,
                    "Recommended hierarchy is Root / Render / Sprite.", ref warnings);
            }

            foreach (EnemyProjectileSpawnOnFinish spawnModule in
                prefab.GetComponents<EnemyProjectileSpawnOnFinish>())
            {
                GameObject child = spawnModule.ChildProjectilePrefab;
                if (child == null || child.GetComponent<EnemyProjectile>() == null)
                {
                    ReportError(prefab,
                        "Spawn On Finish requires an EnemyProjectile child prefab.",
                        ref errors);
                }
                else if (child == prefab)
                {
                    ReportWarning(prefab,
                        "Spawn On Finish references itself and can create an endless chain.",
                        ref warnings);
                }
            }
        }
    }

    private static void ValidateSpawnTables(ref int errors, ref int warnings)
    {
        foreach (string guid in AssetDatabase.FindAssets(
            "t:EnemySpawnTable", new[] { SpawnDataFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EnemySpawnTable table = AssetDatabase.LoadAssetAtPath<EnemySpawnTable>(path);
            if (table == null) continue;
            if (table.Entries == null || table.Entries.Count == 0)
            {
                ReportWarning(table, "Spawn table has no entries.", ref warnings);
                continue;
            }
            for (int index = 0; index < table.Entries.Count; index++)
            {
                EnemySpawnTable.Entry entry = table.Entries[index];
                if (entry == null || entry.prefab == null)
                {
                    ReportError(table, $"Entry {index} has no prefab.", ref errors);
                }
                else if (entry.prefab.GetComponent<EnemyHealth>() == null)
                {
                    ReportError(table,
                        $"Entry {index} prefab '{entry.prefab.name}' is not an enemy.", ref errors);
                }
            }
        }
    }

    private static IEnumerable<GameObject> LoadPrefabs(string folder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (prefab != null) yield return prefab;
        }
    }

    private static bool HasAttackGate(GameObject prefab)
    {
        foreach (MonoBehaviour behaviour in prefab.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IEnemyAttackGate) return true;
        }
        return false;
    }

    private static void ReportError(Object context, string message, ref int count)
    {
        count++;
        Debug.LogError($"[{context.name}] {message}", context);
    }

    private static void ReportWarning(Object context, string message, ref int count)
    {
        count++;
        Debug.LogWarning($"[{context.name}] {message}", context);
    }
}

[CustomEditor(typeof(EnemyHealth))]
public class EnemyHealthEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EnemyHealth health = (EnemyHealth)target;
        if (health.Profile == null)
        {
            EditorGUILayout.HelpBox(
                "Enemy Profile is required. Configure health, defense and experience in that asset.",
                MessageType.Error);
            return;
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Resolved Enemy Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("ID", health.Profile.EnemyId);
        EditorGUILayout.LabelField("Rank", health.Profile.Rank.ToString());
        EditorGUILayout.LabelField("Max Health", health.Profile.MaxHealth.ToString());
        EditorGUILayout.LabelField("Defense", health.Profile.Defense.ToString());
        EditorGUILayout.LabelField("Experience", health.Profile.Experience.ToString());
    }
}
