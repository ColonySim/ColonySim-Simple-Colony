using UnityEditor;
using UnityEngine;
using SimpleColony;

[CustomEditor(typeof(SceneModule), true)]
public sealed class SceneModuleInspector : Editor
{
    public override void OnInspectorGUI()
    {
        var module = (SceneModule)target;
        EditorGUILayout.LabelField("Слот симуляции", module.Slot.ToString());
        if (!string.IsNullOrEmpty(module.Description))
            EditorGUILayout.HelpBox(module.Description, MessageType.Info);
        DrawDefaultInspector();
    }
}

[CustomEditor(typeof(SimulationAssembly))]
public sealed class AssemblyInspector : Editor
{
    public override void OnInspectorGUI()
    {
        try
        {
            ((SimulationAssembly)target).Compose();
            EditorGUILayout.HelpBox("Состав готов к запуску.", MessageType.Info);
        }
        catch (System.InvalidOperationException error)
        {
            EditorGUILayout.HelpBox(error.Message, MessageType.Error);
        }
    }
}
