﻿using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
#if UNITY_2022_2_OR_NEWER
using EffectSettingsEditor = UnityEditor.CustomEditor;
#else
using EffectSettingsEditor = UnityEditor.Rendering.VolumeComponentEditorAttribute;
#endif

namespace SCPE
{
    [EffectSettingsEditor(typeof(Sketch))]
    sealed class SketchEditor : VolumeComponentEditor
    {
        SerializedDataParameter strokeTex;
        SerializedDataParameter projectionMode;
        SerializedDataParameter blendMode;
        SerializedDataParameter intensity;
        SerializedDataParameter brightness;
        SerializedDataParameter tiling;
        
        private bool isSetup;

        float minBrightness;
        float maxBrightness;

        public override void OnEnable()
        {
            base.OnEnable();

            var o = new PropertyFetcher<Sketch>(serializedObject);
            isSetup = AutoSetup.ValidEffectSetup<SketchRenderer>();

            strokeTex = Unpack(o.Find(x => x.strokeTex));
            projectionMode = Unpack(o.Find(x => x.projectionMode));
            blendMode = Unpack(o.Find(x => x.blendMode));
            intensity = Unpack(o.Find(x => x.intensity));
            brightness = Unpack(o.Find(x => x.brightness));
            tiling = Unpack(o.Find(x => x.tiling));
        }

        public override void OnInspectorGUI()
        {
            SCPE_GUI.DisplayDocumentationButton("Sketch");

            SCPE_GUI.DisplaySetupWarning<SketchRenderer>(ref isSetup);

            SCPE_GUI.ShowDepthTextureWarning();

            PropertyField(intensity);
            SCPE_GUI.DisplayIntensityWarning(intensity);
            
            EditorGUILayout.Space();
            
            PropertyField(strokeTex);
            PropertyField(projectionMode);
            PropertyField(blendMode);

            minBrightness = brightness.value.vector2Value.x;
            maxBrightness = brightness.value.vector2Value.y;

            using (new EditorGUILayout.HorizontalScope())
            {
                // Override checkbox
                var overrideRect = GUILayoutUtility.GetRect(17f, 17f, GUILayout.ExpandWidth(false));
                overrideRect.yMin += 4f;
                brightness.overrideState.boolValue = GUI.Toggle(overrideRect, brightness.overrideState.boolValue, EditorGUIUtility.TrTextContent("", "Override this setting for this volume."), CoreEditorStyles.smallTickbox);

                // Property
                using (new EditorGUI.DisabledScope(!brightness.overrideState.boolValue))
                {
                    EditorGUILayout.LabelField(brightness.displayName + " (Min/Max)", GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField(minBrightness.ToString(), GUILayout.Width(50f));
                    EditorGUILayout.MinMaxSlider(ref minBrightness, ref maxBrightness, 0f, 2f);
                    EditorGUILayout.LabelField(maxBrightness.ToString(), GUILayout.Width(50f));
                }
            }

            brightness.value.vector2Value = new Vector2(minBrightness, maxBrightness);
            PropertyField(tiling);
        }
    }
}