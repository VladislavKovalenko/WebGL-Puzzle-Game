using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;
using UnityEditor.AnimatedValues;

namespace UnityEditor.UI
{
    public class ProceduralImageEditorUtility
    {
        [MenuItem("GameObject/UI/Procedural Image")]
        public static void AddProceduralImage()
        {
            GameObject o = new GameObject();
            o.AddComponent<ProceduralImage>();
            o.layer = LayerMask.NameToLayer("UI");
            o.name = "Procedural Image";
            if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponentInParent<Canvas>() != null)
            {
                o.transform.SetParent(Selection.activeGameObject.transform, false);
                Selection.activeGameObject = o;
            }
            else
            {
                if (GameObject.FindFirstObjectByType<Canvas>() == null)
                    EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");
                Canvas c = GameObject.FindFirstObjectByType<Canvas>();

                c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;

                o.transform.SetParent(c.transform, false);
                Selection.activeGameObject = o;
            }
        }

        [MenuItem("CONTEXT/Image/Replace with Procedural Image")]
        public static void ReplaceWithProceduralImage(MenuCommand command)
        {
            Image image = (Image)command.context;
            GameObject obj = image.gameObject;
            GameObject.DestroyImmediate(image);
            obj.AddComponent<ProceduralImage>();
        }
    }

    [CustomEditor(typeof(FreeModifier), true)]
    [CanEditMultipleObjects]
    public class FreeModifierEditor : Editor
    {
        protected SerializedProperty radiusX, radiusY, radiusZ, radiusW;
        protected SerializedProperty uniformRadius;
        protected SerializedProperty uniformValue;
        
        protected void OnEnable()
        {
            radiusX = serializedObject.FindProperty("radius.x");
            radiusY = serializedObject.FindProperty("radius.y");
            radiusZ = serializedObject.FindProperty("radius.z");
            radiusW = serializedObject.FindProperty("radius.w");
            uniformRadius = serializedObject.FindProperty("uniformRadius");
            uniformValue = serializedObject.FindProperty("uniformValue");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            GUILayout.Space(8);
            
            EditorGUILayout.PropertyField(uniformRadius, new GUIContent("Uniform Radius"));
            
            if (uniformRadius.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(uniformValue, new GUIContent("Radius"));
                if (EditorGUI.EndChangeCheck())
                {
                    float val = uniformValue.floatValue;
                    radiusX.floatValue = val;
                    radiusY.floatValue = val;
                    radiusZ.floatValue = val;
                    radiusW.floatValue = val;
                }
            }
            else
            {
                RadiusGUI();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        protected void RadiusGUI()
        {
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(radiusX, new GUIContent("Upper Left"));
                EditorGUILayout.PropertyField(radiusY, new GUIContent("Upper Right"));
                GUILayout.Space(8);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(8);
            GUILayout.BeginHorizontal();
            {
                EditorGUILayout.PropertyField(radiusW, new GUIContent("Lower Left"));
                EditorGUILayout.PropertyField(radiusZ, new GUIContent("Lower Right"));
                GUILayout.Space(8);
            }
            GUILayout.EndHorizontal();
        }
    }

    [CustomEditor(typeof(ProceduralImage), true)]
    [CanEditMultipleObjects]
    public class ProceduralImageEditor : ImageEditor
    {
        SerializedProperty m_borderWidth, m_falloffDist;
        SerializedProperty m_FillMethod, m_FillOrigin, m_FillAmount, m_FillClockwise, m_Type, m_Sprite;
        
        // Gradient Properties
        SerializedProperty m_UseGradient, m_ThreeColors, m_Color1, m_Color2, m_Color3, m_Angle, m_MiddlePoint;
        
        AnimBool showFilled;
        AnimBool showGradient;
        
        GUIContent spriteTypeContent = new GUIContent("Image Type");
        GUIContent clockwiseContent = new GUIContent("Clockwise");

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Type = serializedObject.FindProperty("m_Type");
            m_FillMethod = serializedObject.FindProperty("m_FillMethod");
            m_FillOrigin = serializedObject.FindProperty("m_FillOrigin");
            m_FillClockwise = serializedObject.FindProperty("m_FillClockwise");
            m_FillAmount = serializedObject.FindProperty("m_FillAmount");
            m_Sprite = serializedObject.FindProperty("m_Sprite");

            var typeEnum = (Image.Type)m_Type.enumValueIndex;
            showFilled = new AnimBool(!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);
            showFilled.valueChanged.AddListener(Repaint);

            m_borderWidth = serializedObject.FindProperty("borderWidth");
            m_falloffDist = serializedObject.FindProperty("falloffDistance");

            // Gradient Initialization
            m_UseGradient = serializedObject.FindProperty("useGradient");
            m_ThreeColors = serializedObject.FindProperty("threeColors");
            m_Color1 = serializedObject.FindProperty("color1");
            m_Color2 = serializedObject.FindProperty("color2");
            m_Color3 = serializedObject.FindProperty("color3");
            m_Angle = serializedObject.FindProperty("angle");
            m_MiddlePoint = serializedObject.FindProperty("middlePoint");

            showGradient = new AnimBool(m_UseGradient.boolValue);
            showGradient.valueChanged.AddListener(Repaint);

            EditorApplication.update -= UpdateProceduralImage;
            EditorApplication.update += UpdateProceduralImage;
        }

        public void UpdateProceduralImage()
        {
            if (target != null)
                (target as ProceduralImage).Update();
            else
                EditorApplication.update -= UpdateProceduralImage;
        }

        public override void OnInspectorGUI()
        {
            CheckForShaderChannelsGUI();
            serializedObject.Update();
            
            ProceduralImageSpriteGUI();
            EditorGUILayout.PropertyField(m_Color);
            RaycastControlsGUI();
            ProceduralImageTypeGUI();
            
            EditorGUILayout.Space();

            // Gradient GUI Section
            EditorGUILayout.PropertyField(m_UseGradient, new GUIContent("Use Gradient"));
            showGradient.target = m_UseGradient.boolValue;
            if (EditorGUILayout.BeginFadeGroup(showGradient.faded))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_ThreeColors, new GUIContent("Three Colors"));
                EditorGUILayout.PropertyField(m_Color1, new GUIContent("Color Start"));
                
                // Если включено 3 цвета, меняем подпись для среднего цвета
                string color2Label = m_ThreeColors.boolValue ? "Color Middle" : "Color End";
                EditorGUILayout.PropertyField(m_Color2, new GUIContent(color2Label));

                if (m_ThreeColors.boolValue)
                {
                    EditorGUILayout.PropertyField(m_Color3, new GUIContent("Color End"));
                    EditorGUILayout.PropertyField(m_MiddlePoint, new GUIContent("Middle Point"));
                }

                EditorGUILayout.PropertyField(m_Angle, new GUIContent("Angle"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFadeGroup();

            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(m_borderWidth);
            EditorGUILayout.PropertyField(m_falloffDist);
            
            serializedObject.ApplyModifiedProperties();
        }

        protected void ProceduralImageSpriteGUI()
        {
            if (m_Sprite.hasMultipleDifferentValues)
            {
                EditorGUILayout.PropertyField(m_Sprite);
            }
            else
            {
                Sprite s = (Sprite)EditorGUILayout.ObjectField("Sprite", EmptySprite.IsEmptySprite((Sprite)m_Sprite.objectReferenceValue) ? null : m_Sprite.objectReferenceValue, typeof(Sprite), false, GUILayout.Height(16));
                if (s != null)
                    m_Sprite.objectReferenceValue = s;
                else
                    m_Sprite.objectReferenceValue = EmptySprite.Get();
            }
        }

        protected void ProceduralImageTypeGUI()
        {
            if (m_Type.hasMultipleDifferentValues)
            {
                int idx = Convert.ToInt32(EditorGUILayout.EnumPopup(spriteTypeContent, (ProceduralImageType)(-1)));
                if (idx != -1)
                    m_Type.enumValueIndex = idx;
            }
            else
            {
                m_Type.enumValueIndex = Convert.ToInt32(EditorGUILayout.EnumPopup(spriteTypeContent, (ProceduralImageType)m_Type.enumValueIndex));
            }

            ++EditorGUI.indentLevel;
            {
                Image.Type typeEnum = (Image.Type)m_Type.enumValueIndex;
                showFilled.target = (!m_Type.hasMultipleDifferentValues && typeEnum == Image.Type.Filled);
                if (EditorGUILayout.BeginFadeGroup(showFilled.faded))
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(m_FillMethod);
                    if (EditorGUI.EndChangeCheck())
                        m_FillOrigin.intValue = 0;
                    switch ((Image.FillMethod)m_FillMethod.enumValueIndex)
                    {
                        case Image.FillMethod.Horizontal:
                            m_FillOrigin.intValue = (int)(Image.OriginHorizontal)EditorGUILayout.EnumPopup("Fill Origin", (Image.OriginHorizontal)m_FillOrigin.intValue);
                            break;
                        case Image.FillMethod.Vertical:
                            m_FillOrigin.intValue = (int)(Image.OriginVertical)EditorGUILayout.EnumPopup("Fill Origin", (Image.OriginVertical)m_FillOrigin.intValue);
                            break;
                        case Image.FillMethod.Radial90:
                            m_FillOrigin.intValue = (int)(Image.Origin90)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin90)m_FillOrigin.intValue);
                            break;
                        case Image.FillMethod.Radial180:
                            m_FillOrigin.intValue = (int)(Image.Origin180)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin180)m_FillOrigin.intValue);
                            break;
                        case Image.FillMethod.Radial360:
                            m_FillOrigin.intValue = (int)(Image.Origin360)EditorGUILayout.EnumPopup("Fill Origin", (Image.Origin360)m_FillOrigin.intValue);
                            break;
                    }
                    EditorGUILayout.PropertyField(m_FillAmount);
                    if ((Image.FillMethod)m_FillMethod.enumValueIndex > Image.FillMethod.Vertical)
                        EditorGUILayout.PropertyField(m_FillClockwise, clockwiseContent);
                }
                EditorGUILayout.EndFadeGroup();
            }
            --EditorGUI.indentLevel;
        }

        void CheckForShaderChannelsGUI()
        {
            Canvas c = (target as Component).GetComponentInParent<Canvas>();
            if (c != null && (c.additionalShaderChannels | AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3) != c.additionalShaderChannels)
            {
                EditorGUILayout.HelpBox("TexCoord1,2,3 are not enabled as an additional shader channel in parent canvas. Procedural Image will not work properly", MessageType.Error);
                if (GUILayout.Button("Fix: Enable TexCoord1,2,3 in Canvas: " + c.name))
                {
                    Undo.RecordObject(c, "enable TexCoord1,2,3 as additional shader channels");
                    c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
                }
            }
        }

        public override string GetInfoString()
        {
            ProceduralImage image = target as ProceduralImage;
            return string.Format("Line-Weight: {0}", image.BorderWidth);
        }

        protected enum ProceduralImageType
        {
            Simple = 0,
            Filled = 3
        }
    }
}