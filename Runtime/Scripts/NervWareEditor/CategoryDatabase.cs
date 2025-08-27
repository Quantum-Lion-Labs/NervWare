using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
	fileName = "CategoryDatabase",
	menuName = "ScriptableObject/Category Database"
)]
public class CategoryDatabase : ScriptableObject {
	private static CategoryDatabase _instance;
	public static CategoryDatabase Instance {
		get {
			if (_instance == null) {
				_instance = Resources.Load<CategoryDatabase>("CategoryDatabase");
				if (_instance == null) {
					Debug.LogError("CategoryDatabase not found in Resources folder. Please create one and place it in a Resources folder.");
				}
			}
			return _instance;
		}
	}
	// We use a flat list and parent IDs to represent the hierarchy.
	// This is much more robust for serialization than nested lists.
	[HideInInspector]
	public List<Category> categories = new List<Category>();

	// A helper class to hold the data for a single category.
	[Serializable]
	public class Category {
		public string name;
		public int id;
		public int parentId;

		public Category(string name, int id, int parentId) {
			this.name = name;
			this.id = id;
			this.parentId = parentId;
		}
	}

	public Category GetCategoryById(int id) {
		return categories.Find(c => c.id == id);
	}

	public List<Category> GetChildren(int parentId) {
		return categories.FindAll(c => c.parentId == parentId);
	}

	public List<Category> GetRootCategories() {
		return categories.FindAll(c => c.parentId == -1);
	}

	public string GetCategoryPath(int categoryId) {
		var category = GetCategoryById(categoryId);
		if (category == null) return "";

		var path = new List<string>();
		var current = category;
		
		while (current != null) {
			path.Insert(0, current.name);
			current = current.parentId == -1 ? null : GetCategoryById(current.parentId);
		}
		
		return string.Join(" > ", path);
	}

	public List<string> GetCategoryBreadcrumbs(int categoryId) {
		var breadcrumbs = new List<string>();
		var current = GetCategoryById(categoryId);
		
		while (current != null) {
			breadcrumbs.Insert(0, current.name);
			current = current.parentId == -1 ? null : GetCategoryById(current.parentId);
		}
		
		return breadcrumbs;
	}
}