using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace WorldSpaceUI.Editor
{
	[CustomEditor(typeof(WorldSpaceUIDocument))]
	public class WorldSpaceUIDocumentEditor : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			WorldSpaceUIDocument worldSpaceUI = (WorldSpaceUIDocument)target;
			var iterator = serializedObject.GetIterator();
			VisualElement container = new VisualElement();
			
			
			var panelSettingsPropPath = nameof(WorldSpaceUIDocument.panelSettingsAsset);
	
			
			if(iterator.NextVisible(true))
			{
				do
				{
					if (iterator.propertyPath == panelSettingsPropPath)
					{
						var panelSettingsInspector = new Foldout();
						panelSettingsInspector.contentContainer.style.backgroundColor = new StyleColor(new Color(0,0,0,.25f));
						//panelSettingsInspector.value = false;
						panelSettingsInspector.Q<Toggle>()[0].Add(new PropertyField(iterator)
						{
							style = { flexGrow = 1}
						});
						panelSettingsInspector.TrackPropertyValue(iterator, x=>RefreshPanelSettingsInspector(panelSettingsInspector,x));
						RefreshPanelSettingsInspector(panelSettingsInspector, iterator);
						container.Add(panelSettingsInspector);
					}
					else
					{
						container.Add(new PropertyField(iterator));
					}
				} while (iterator.NextVisible(false));
			}


			Button forceRefresh = new Button(worldSpaceUI.ForceRefresh)
			{
				text = "Force Refresh"
			};
			
			
			container.Add(forceRefresh);
			
			return container;
		}

		private void RefreshPanelSettingsInspector(Foldout foldout, SerializedProperty obj)
		{
			foldout.contentContainer.Clear();

			var toggle = foldout.Q<Toggle>();
			if (obj.objectReferenceValue != null)
			{
				var serializedSubAsset = new SerializedObject(obj.objectReferenceValue);
				var referenceResolution = serializedSubAsset.FindProperty("m_ReferenceResolution");
				Debug.Log(referenceResolution==null);
				var propField = new PropertyField(referenceResolution);
				propField.BindProperty(referenceResolution);
				foldout.contentContainer.Add(propField);
			}
			else
			{
				toggle.value = false;
				toggle.Q<VisualElement>("unity-checkmark").visible = false;
			}

		
		}

		private void OnSceneGUI()
		{
			WorldSpaceUIDocument worldSpaceUI = (WorldSpaceUIDocument)target;

			//var mesh = worldSpaceUI.GetMesh();

		}
	}
}