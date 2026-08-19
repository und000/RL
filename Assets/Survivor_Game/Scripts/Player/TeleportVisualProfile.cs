using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TeleportVisualProfile_",
    menuName = "Player/Teleport Visual Profile")]
public class TeleportVisualProfile : ScriptableObject
{
    [Serializable]
    public class VisualPrefabs
    {
        public GameObject aimLine;
        public GameObject targetRange;
        public GameObject arrivalShockwave;
    }

    [SerializeField] private VisualPrefabs visuals = new VisualPrefabs();
    public VisualPrefabs Visuals => visuals;
}
