#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CategoryDatabase))]
public class CategoryDatabaseEditor : Editor {
  private CategoryDatabase db;
  private SerializedProperty categoriesProp;
  
  // Store expanded states across sessions
  private Dictionary<int, bool> expandedStates = new Dictionary<int, bool>();
  
  // Drag and drop state
  private CategoryDatabase.Category draggedCategory;
  private bool isDragging = false;
  private Vector2 dragStartPosition;

  private void OnEnable() {
    db = (CategoryDatabase)target;
    categoriesProp = serializedObject.FindProperty("categories");
  }

  public override void OnInspectorGUI() {
    serializedObject.Update();

    EditorGUILayout.LabelField("Category Database Editor (Drag to Reorder)", EditorStyles.boldLabel);
    EditorGUILayout.Space();

    // Handle drag and drop events
    HandleDragAndDrop();

    // Draw hierarchical categories with reordering
    var rootCategories = db.categories.Where(c => c.parentId == -1).OrderBy(c => GetCategoryIndex(c.id)).ToList();
    
    foreach (var category in rootCategories) {
      DrawReorderableCategory(category, 0);
    }

    EditorGUILayout.Space();

    if (GUILayout.Button("Add Root Category")) {
      AddCategory(-1);
    }

    serializedObject.ApplyModifiedProperties();
  }

  private void DrawReorderableCategory(CategoryDatabase.Category category, int indentLevel) {
    Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 4);
    
    // Handle drag detection
    Event currentEvent = Event.current;
    bool isBeingDragged = draggedCategory == category;
    
    // Visual feedback for drag state
    if (isBeingDragged) {
      EditorGUI.DrawRect(rect, new Color(0.3f, 0.6f, 1f, 0.3f));
    }
    
    // Check for drag start
    if (currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
      if (currentEvent.button == 0) {
        dragStartPosition = currentEvent.mousePosition;
      }
    }
    
    if (currentEvent.type == EventType.MouseDrag && rect.Contains(dragStartPosition)) {
      if (!isDragging && Vector2.Distance(currentEvent.mousePosition, dragStartPosition) > 5f) {
        draggedCategory = category;
        isDragging = true;
        DragAndDrop.PrepareStartDrag();
        DragAndDrop.StartDrag("Category");
      }
    }
    
    // Drop zone highlighting with different colors for different actions
    if (isDragging && draggedCategory != category && rect.Contains(currentEvent.mousePosition)) {
      float relativeY = (currentEvent.mousePosition.y - rect.y) / rect.height;
      
      if (relativeY < 0.3f) {
        // Top third - drop as sibling above
        Rect topRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.3f);
        EditorGUI.DrawRect(topRect, new Color(0f, 0.8f, 1f, 0.3f)); // Blue for sibling
      } else if (relativeY > 0.7f) {
        // Bottom third - drop as sibling below  
        Rect bottomRect = new Rect(rect.x, rect.y + rect.height * 0.7f, rect.width, rect.height * 0.3f);
        EditorGUI.DrawRect(bottomRect, new Color(0f, 0.8f, 1f, 0.3f)); // Blue for sibling
      } else {
        // Middle - drop as child
        Rect middleRect = new Rect(rect.x + indentLevel * 20f + 20f, rect.y + rect.height * 0.3f, rect.width - (indentLevel * 20f + 20f), rect.height * 0.4f);
        EditorGUI.DrawRect(middleRect, new Color(1f, 0.8f, 0f, 0.3f)); // Yellow for child
      }
      
      if (currentEvent.type == EventType.DragUpdated) {
        DragAndDrop.visualMode = DragAndDropVisualMode.Move;
        currentEvent.Use();
      }
      
      if (currentEvent.type == EventType.DragPerform) {
        PerformCategoryReorder(draggedCategory, category, indentLevel);
        DragAndDrop.AcceptDrag();
        isDragging = false;
        draggedCategory = null;
        currentEvent.Use();
      }
    }

    // Draw the category content
    float indentWidth = indentLevel * 20f;
    Rect contentRect = new Rect(rect.x + indentWidth, rect.y + 2, rect.width - indentWidth, EditorGUIUtility.singleLineHeight);
    
    // Foldout for expansion
    bool isExpanded = expandedStates.GetValueOrDefault(category.id, false);
    Rect foldoutRect = new Rect(contentRect.x, contentRect.y, 12, contentRect.height);
    expandedStates[category.id] = EditorGUI.Foldout(foldoutRect, isExpanded, "");
    
    // Category name field with subcategory counts
    int directCount = GetDirectChildCount(category.id);
    int totalCount = GetTotalDescendantCount(category.id);
    
    Rect nameRect = new Rect(contentRect.x + 15, contentRect.y, contentRect.width * 0.6f, contentRect.height);
    string newName = EditorGUI.TextField(nameRect, category.name);
    if (newName != category.name) {
      category.name = newName;
      EditorUtility.SetDirty(db);
    }
    
    // Display subcategory counts (read-only)
    if (directCount > 0) {
      string countText = totalCount > directCount ? $"({directCount}/{totalCount})" : $"({directCount})";
      Rect countRect = new Rect(nameRect.xMax - 40, contentRect.y, 35, contentRect.height);
      EditorGUI.LabelField(countRect, countText, EditorStyles.miniLabel);
    }
    
    // Add child button
    Rect addRect = new Rect(nameRect.xMax + 5, contentRect.y, 25, contentRect.height);
    if (GUI.Button(addRect, "+")) {
      AddCategory(category.id);
      expandedStates[category.id] = true;
    }
    
    // Remove button
    Rect removeRect = new Rect(addRect.xMax + 5, contentRect.y, 25, contentRect.height);
    if (GUI.Button(removeRect, "-")) {
      if (EditorUtility.DisplayDialog(
        "Delete Category?",
        $"Are you sure you want to delete '{category.name}' and all its subcategories?",
        "Delete",
        "Cancel")) {
        RemoveCategoryById(category.id);
        GUIUtility.ExitGUI();
      }
    }

    // Draw children if expanded
    if (expandedStates.GetValueOrDefault(category.id, false)) {
      var children = db.categories.Where(c => c.parentId == category.id)
        .OrderBy(c => GetCategoryIndex(c.id)).ToList();
      foreach (var child in children) {
        DrawReorderableCategory(child, indentLevel + 1);
      }
    }
  }
  
  private void HandleDragAndDrop() {
    if (Event.current.type == EventType.DragExited || Event.current.type == EventType.MouseUp) {
      isDragging = false;
      draggedCategory = null;
    }
  }
  
  private void PerformCategoryReorder(CategoryDatabase.Category draggedCat, CategoryDatabase.Category targetCat, int targetIndentLevel) {
    // Prevent dropping on self or children
    if (draggedCat == targetCat || IsChildOf(targetCat.id, draggedCat.id)) {
      return;
    }
    
    // Get the current drop rect to calculate relative position
    Rect currentRect = GUILayoutUtility.GetLastRect();
    Vector2 mousePos = Event.current.mousePosition;
    float relativeY = (mousePos.y - currentRect.y) / currentRect.height;
    
    // Determine new parent based on drop position
    if (relativeY < 0.3f || relativeY > 0.7f) {
      // Drop as sibling with same parent
      draggedCat.parentId = targetCat.parentId;
    } else {
      // Drop as child of target
      draggedCat.parentId = targetCat.id;
      expandedStates[targetCat.id] = true; // Auto-expand parent
    }
    
    // Reorder in the flat list for proper display order
    int draggedIndex = db.categories.FindIndex(c => c.id == draggedCat.id);
    int targetIndex = db.categories.FindIndex(c => c.id == targetCat.id);
    
    if (draggedIndex >= 0 && targetIndex >= 0) {
      // Remove from old position
      db.categories.RemoveAt(draggedIndex);
      
      // Adjust target index if needed
      if (draggedIndex < targetIndex) {
        targetIndex--;
      }
      
      // Insert at new position
      if (draggedCat.parentId == targetCat.id) {
        // Insert as first child (after target)
        db.categories.Insert(targetIndex + 1, draggedCat);
      } else if (relativeY < 0.3f) {
        // Insert before target (as sibling above)
        db.categories.Insert(targetIndex, draggedCat);
      } else {
        // Insert after target (as sibling below)
        db.categories.Insert(targetIndex + 1, draggedCat);
      }
      
      EditorUtility.SetDirty(db);
    }
  }
  
  private bool IsChildOf(int potentialChildId, int potentialParentId) {
    var category = db.categories.FirstOrDefault(c => c.id == potentialChildId);
    while (category != null && category.parentId != -1) {
      if (category.parentId == potentialParentId) {
        return true;
      }
      category = db.categories.FirstOrDefault(c => c.id == category.parentId);
    }
    return false;
  }
  
  private int GetCategoryIndex(int categoryId) {
    return db.categories.FindIndex(c => c.id == categoryId);
  }
  
  private int GetDirectChildCount(int parentId) {
    return db.categories.Count(c => c.parentId == parentId);
  }
  
  private int GetTotalDescendantCount(int parentId) {
    int count = 0;
    var directChildren = db.categories.Where(c => c.parentId == parentId);
    
    foreach (var child in directChildren) {
      count++; // Count the direct child
      count += GetTotalDescendantCount(child.id); // Recursively count grandchildren
    }
    
    return count;
  }
  
  private void AddCategory(int parentId) {
    int newId = db.categories.Count > 0 ? db.categories.Max(c => c.id) + 1 : 0;

    categoriesProp.InsertArrayElementAtIndex(categoriesProp.arraySize);
    SerializedProperty newCategoryProp = categoriesProp.GetArrayElementAtIndex(
      categoriesProp.arraySize - 1
    );
    newCategoryProp.FindPropertyRelative("name").stringValue = "New Category";
    newCategoryProp.FindPropertyRelative("id").intValue = newId;
    newCategoryProp.FindPropertyRelative("parentId").intValue = parentId;

    if (parentId != -1) {
      expandedStates[parentId] = true;
    }
  }

  private void RemoveCategoryById(int categoryId) {
    var idsToRemove = new HashSet<int> { categoryId };
    var queue = new Queue<int>(new[] { categoryId });

    while (queue.Count > 0) {
      int currentId = queue.Dequeue();
      var children = db.categories.Where(c => c.parentId == currentId);
      foreach (var child in children) {
        idsToRemove.Add(child.id);
        queue.Enqueue(child.id);
      }
    }

    for (int i = categoriesProp.arraySize - 1; i >= 0; i--) {
      int id = categoriesProp.GetArrayElementAtIndex(i)
        .FindPropertyRelative("id")
        .intValue;
      if (idsToRemove.Contains(id)) {
        categoriesProp.DeleteArrayElementAtIndex(i);
      }
    }

    serializedObject.ApplyModifiedProperties();
  }
}
#endif