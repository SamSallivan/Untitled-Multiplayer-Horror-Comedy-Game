using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/ItemsList", order = 2)]
public class ItemList : ScriptableObject
{
	public List<ItemData> itemDataList = new List<ItemData>();
}
