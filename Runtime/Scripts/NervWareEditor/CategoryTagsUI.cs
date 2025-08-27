#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CategoryTagsUI {
	private static Dictionary<string, Vector2> scrollStates = new Dictionary<string, Vector2>();

	/// <summary>
	/// Draws the category tags breadcrumb UI for any SerializedProperty that represents a List<string>
	/// </summary>
	/// <param name="property">SerializedProperty for List<string> categoryTags</param>
	/// <param name="label">Label to display (e.g., "Category Tags")</param>
	public static void DrawCategoryTagsField(SerializedProperty property, string label = "Category Tags") {
		var categoryDB = CategoryDatabase.Instance;
		if (categoryDB == null) {
			EditorGUILayout.LabelField(label, "CategoryDatabase not found in Resources");
			return;
		}

		EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
		
		// Parse current tags to rebuild the hierarchical path
		List<string> currentPath = new List<string>();
		for (int i = 0; i < property.arraySize; i++) {
			string tag = property.GetArrayElementAtIndex(i).stringValue;
			if (!string.IsNullOrEmpty(tag)) {
				currentPath.Add(tag);
			}
		}

		// Display current path
		if (currentPath.Count > 0) {
			string pathDisplay = string.Join(" > ", currentPath);
			EditorGUILayout.LabelField(pathDisplay, EditorStyles.helpBox);
			
			// Back button to remove last level
			if (GUILayout.Button("← Back", GUILayout.Width(60))) {
				if (property.arraySize > 0) {
					property.DeleteArrayElementAtIndex(property.arraySize - 1);
				}
			}
		} else {
			EditorGUILayout.LabelField("No category selected", EditorStyles.helpBox);
		}

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Add Tags:", EditorStyles.boldLabel);

		// Get available categories at current level
		var availableCategories = GetCategoriesAtCurrentLevel(categoryDB, currentPath);
		
		if (availableCategories.Count > 0) {
			// Create unique scroll key for this property
			string scrollKey = property.propertyPath + "_" + property.serializedObject.targetObject.GetInstanceID();
			if (!scrollStates.ContainsKey(scrollKey)) {
				scrollStates[scrollKey] = Vector2.zero;
			}

			// Display category buttons in a grid
			int buttonsPerRow = 3;
			int currentButton = 0;
			
			scrollStates[scrollKey] = EditorGUILayout.BeginScrollView(scrollStates[scrollKey], GUILayout.MaxHeight(75));
			
			while (currentButton < availableCategories.Count) {
				EditorGUILayout.BeginHorizontal();
				
				for (int i = 0; i < buttonsPerRow && currentButton < availableCategories.Count; i++, currentButton++) {
					var category = availableCategories[currentButton];
					
					if (GUILayout.Button(category.name)) {
						// Add this category as a new entry in the list
						property.InsertArrayElementAtIndex(property.arraySize);
						property.GetArrayElementAtIndex(property.arraySize - 1).stringValue = category.name;
					}
				}
				
				EditorGUILayout.EndHorizontal();
			}
			
			EditorGUILayout.EndScrollView();
		} else {
			EditorGUILayout.LabelField("No subcategories available at this level");
		}

		EditorGUILayout.Space();
	}

	private static List<CategoryDatabase.Category> GetCategoriesAtCurrentLevel(CategoryDatabase categoryDB, List<string> breadcrumb) {
		if (breadcrumb.Count == 0) {
			return categoryDB.GetRootCategories();
		}
		
		// Find the category that matches our current breadcrumb path
		var currentCategory = FindCategoryByPath(categoryDB, breadcrumb);
		if (currentCategory != null) {
			return categoryDB.GetChildren(currentCategory.id);
		}
		
		return new List<CategoryDatabase.Category>();
	}
	
	private static CategoryDatabase.Category FindCategoryByPath(CategoryDatabase categoryDB, List<string> path) {
		var currentCategories = categoryDB.GetRootCategories();
		CategoryDatabase.Category currentCategory = null;
		
		foreach (string pathSegment in path) {
			currentCategory = currentCategories.FirstOrDefault(c => c.name == pathSegment);
			if (currentCategory == null) {
				return null;
			}
			currentCategories = categoryDB.GetChildren(currentCategory.id);
		}
		
		return currentCategory;
	}
}
#endif