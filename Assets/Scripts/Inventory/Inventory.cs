﻿using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInventory", menuName = "Items/Inventory")]
public class Inventory : ScriptableObject
{
    [SerializeField] private VoidEvent onInventoryItemsUpdated = null;
    [SerializeField] private ItemSlot testItemSlot = new ItemSlot();
    public ItemContainer ItemContainer { get; } = new ItemContainer(20);


    public void OnEnable()
    {
        ItemContainer.OnItemsUpdated += onInventoryItemsUpdated.Raise;

    }

    public void OnDisable()
    {
        ItemContainer.OnItemsUpdated -= onInventoryItemsUpdated.Raise;
    }
    [ContextMenu("Test Add")]
    public void TestAdd()
    {
        ItemContainer.AddItem(testItemSlot);
    }

}
