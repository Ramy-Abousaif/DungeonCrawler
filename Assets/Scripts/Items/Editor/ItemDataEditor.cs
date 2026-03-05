using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]
public class ItemDataEditor : Editor
{
    private SerializedProperty effectsProperty;
    private Type[] effectTypes = Array.Empty<Type>();
    private string[] effectTypeNames = Array.Empty<string>();
    private int selectedEffectTypeIndex;

    private void OnEnable()
    {
        effectsProperty = serializedObject.FindProperty("effects");

        effectTypes = TypeCache.GetTypesDerivedFrom<ItemEffect>()
            .Where(type => !type.IsAbstract && !type.IsGenericType && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.Name)
            .ToArray();

        effectTypeNames = effectTypes.Select(type => ObjectNames.NicifyVariableName(type.Name)).ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "m_Script", "effects");
        DrawEffectsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffectsSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select an effect type and click Add Effect. Edit values in the Settings section for each entry.", MessageType.None);

        if (effectTypes.Length == 0)
        {
            EditorGUILayout.HelpBox("No concrete ItemEffect types found.", MessageType.Warning);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            selectedEffectTypeIndex = EditorGUILayout.Popup(
                selectedEffectTypeIndex,
                effectTypeNames
            );

            if (GUILayout.Button("Add Effect", GUILayout.Width(100f)))
            {
                AddEffect(effectTypes[selectedEffectTypeIndex]);
            }
        }

        if (effectsProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("No effects added yet.", MessageType.Info);
            return;
        }

        for (int index = 0; index < effectsProperty.arraySize; index++)
        {
            SerializedProperty effectElement = effectsProperty.GetArrayElementAtIndex(index);
            object effectValue = effectElement.managedReferenceValue;
            string title = effectValue != null
                ? ObjectNames.NicifyVariableName(effectValue.GetType().Name)
                : "Unassigned Effect";
            bool removeRequested = false;

            EditorGUILayout.BeginVertical("box");
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"{index + 1}. {title}", EditorStyles.boldLabel);

                if (GUILayout.Button("Change", GUILayout.Width(70f)))
                {
                    ShowTypeMenu(index);
                }

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    removeRequested = true;
                }
            }

            if (removeRequested)
            {
                RemoveEffect(index);
                EditorGUILayout.EndVertical();
                return;
            }

            if (effectElement.managedReferenceValue != null)
            {
                effectElement.isExpanded = true;
                EditorGUILayout.PropertyField(effectElement, new GUIContent("Settings"), true);
            }
            else
            {
                EditorGUILayout.HelpBox("Select Change to choose an effect type.", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void AddEffect(Type effectType)
    {
        int newIndex = effectsProperty.arraySize;
        effectsProperty.arraySize++;

        SerializedProperty newElement = effectsProperty.GetArrayElementAtIndex(newIndex);
        newElement.managedReferenceValue = Activator.CreateInstance(effectType);

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void RemoveEffect(int index)
    {
        effectsProperty.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);
    }

    private void ShowTypeMenu(int index)
    {
        GenericMenu menu = new GenericMenu();

        foreach (Type effectType in effectTypes)
        {
            Type capturedType = effectType;
            string displayName = ObjectNames.NicifyVariableName(capturedType.Name);

            menu.AddItem(new GUIContent(displayName), false, () =>
            {
                SerializedProperty element = effectsProperty.GetArrayElementAtIndex(index);
                element.managedReferenceValue = Activator.CreateInstance(capturedType);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            });
        }

        menu.ShowAsContext();
    }
}
