﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace BuildingAIChanger
{
    public class EditorController : MonoBehaviour
    {
        private readonly UIView m_view;
        private SelectAIPanel m_selectAIPanel;
        private ToolController m_toolController;
        private UIPanel m_propPanel;
        private UIComponent m_uiContainer;
        
        private EditorController()
        {
            m_view = UIView.GetAView();
            InsertUI();
            m_toolController = ToolsModifierControl.toolController;
            m_toolController.eventEditPrefabChanged += OnEditPrefabChanged;
        }

        private void OnEditPrefabChanged(PrefabInfo info)
        {
            Debug.Log("prefab changed");
            m_selectAIPanel.value = info.GetAI().GetType().FullName;
            RefreshUIPosition();
        }

        private void InsertUI()
        {
            m_uiContainer = m_view.FindUIComponent("FullScreenContainer");
            m_propPanel = m_uiContainer.Find<UIPanel>("DecorationProperties");
            m_selectAIPanel = m_uiContainer.AddUIComponent<SelectAIPanel>();
            m_selectAIPanel.eventValueChanged += OnAIFieldChanged;
            RefreshUIPosition();
        }

        private void OnAIFieldChanged(UIComponent component, string value)
        {
            var buildingInfo = (BuildingInfo) m_toolController.m_editPrefabInfo;
            if (m_selectAIPanel.IsValueValid())
            {
                // remove old ai
                var oldAI = buildingInfo.gameObject.GetComponent<PrefabAI>();
                DestroyImmediate(oldAI);

                // add new ai
                var type = m_selectAIPanel.TryGetAIType();
                var newAI = (PrefabAI) buildingInfo.gameObject.AddComponent(type);

                TryCopyAttributes(oldAI, newAI);

                buildingInfo.DestroyPrefabInstance();
                buildingInfo.InitializePrefabInstance();
                RefreshPropertiesPanel(buildingInfo);
                RefreshUIPosition();
            }
        }

        private void TryCopyAttributes(PrefabAI oldAI, PrefabAI newAI)
        {
            var oldAIFields =
                oldAI.GetType()
                    .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                               BindingFlags.FlattenHierarchy);
            var newAIFields = newAI.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                           BindingFlags.FlattenHierarchy);

            var newAIFieldDic = new Dictionary<String, FieldInfo>(newAIFields.Length);
            foreach (FieldInfo field in newAIFields)
            {
                newAIFieldDic.Add(field.Name, field);
            }

            foreach (FieldInfo fieldInfo in oldAIFields)
            {
                if (fieldInfo.IsDefined(typeof (CustomizablePropertyAttribute), true))
                {
                    FieldInfo newAIField;
                    newAIFieldDic.TryGetValue(fieldInfo.Name, out newAIField);

                    try
                    {
                        if (newAIField.GetType().Equals(fieldInfo.GetType()))
                        {
                            newAIField.SetValue(newAI, fieldInfo.GetValue(oldAI));
                        }
                    }
                    catch (NullReferenceException) {}
                }
            }
        }

        private void RefreshPropertiesPanel(BuildingInfo prefabInfo)
        {
            var decorationPropertiesPanel = m_propPanel.GetComponent<DecorationPropertiesPanel>();
            decorationPropertiesPanel.GetType()
                .InvokeMember("Refresh", BindingFlags.InvokeMethod | BindingFlags.NonPublic, null,
                    decorationPropertiesPanel, new object[] {prefabInfo});
        }

        private void RefreshUIPosition()
        {
            m_selectAIPanel.transformPosition = m_propPanel.transformPosition;
            m_selectAIPanel.transform.Translate(0, 0.3f, 0);
            m_selectAIPanel.PerformLayout();
        }

        public static EditorController Create()
        {
            return new EditorController();
        }
    }
}
