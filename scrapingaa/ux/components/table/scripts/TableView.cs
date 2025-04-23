namespace UX.Components.Table;

using System;
using System.Collections.Generic;

using Godot;

using Core;
using Core.Log;

[GlobalClass]
public partial class TableView : Control
{
	public CRows Rows;
	public TableRow? CurrentRow;
	public TableRow? LastRow;

	[Export] public ScrollContainer ScrollContainer = null!;
	[Export] public VBoxContainer Container = null!;


	public TableView()
	{
		Name = nameof(TableView);
		SizeFlagsVertical = SizeFlags.ExpandFill;

		Rows = new(this);
	}

	public override void _EnterTree()
	{
		if (ScrollContainer == null)
		{
			throw new NullReferenceException("You must add a ScrollContainer to the TableView");
		}

		if (Container == null)
		{
			throw new NullReferenceException("You must add a VBoxContainer to the TableView/ScrollContainer");
		}
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		if (@event.IsEcho()) return;

		if (Input.IsActionJustReleased(Constants.Keys.DeleteRow))
		{
			RemoveSelectedRows();
		}
	}

	/// <summary>
	/// Внутренний метод добавления таил
	/// </summary>
	public void AddRowInternal(TableRow row)
	{
		Rows.Add(row);
	}

	public void ClearSelections()
	{
		foreach (TableRow row in Rows)
		{
			row.IsChecked(); // Убираем выделение каждого ряда
		}
	}

	public void AddSelectedRows()
	{
		foreach (TableRow row in Rows.Selected)
		{
			Rows.Add(row);
		}
	}

	public void RemoveSelectedRows()
	{
		foreach (TableRow row in Rows.Selected)
		{
			Rows.Remove(row);
		}
	}

	protected void OnRowFocusEntered(TableRow row)
	{
		CurrentRow = row;

		if (LastRow != null)
		{
			if (Input.IsActionPressed(Constants.Keys.MultiSelectLineRow))
			{
				int startIndex = Math.Min(CurrentRow.GetIndex(), LastRow.GetIndex()) + 1;
				int endIndex = Math.Max(CurrentRow.GetIndex(), LastRow.GetIndex()) - 1;

				bool toggle = false;
				for (int i = startIndex; i <= endIndex; i++)
				{
					TableRow _row = Container.GetChild<TableRow>(i);
					toggle = Rows.Select(_row);
				}

				if (toggle)
				{
					Rows.Selected.Remove(CurrentRow);
					Rows.Selected.Add(LastRow);
				}
				else
				{
					Rows.Selected.Add(CurrentRow);
					Rows.Selected.Remove(LastRow);
				}
			}
		}
	}

	protected void OnRowFocusExited(TableRow row)
	{
		LastRow = row;
		CurrentRow = null;
	}

	protected void OnRowToggle(TableRow row, bool toggle)
	{
		Log.Print($"Toggle {row.Title} (Total: {Rows.Selected.Count}/{Rows.Count})");
		if (toggle)
		{
			Log.Print("Added");
			Rows.Selected.Add(row);
		}
		else
		{
			Log.Print("Removed");
			Rows.Selected.Remove(row);
		}
	}


	public class CRows(TableView tableView) : List<TableRow>
	{
		public CSelectedRows Selected = new(tableView);
		public event Action<TableRow>? OnRemoved;


		public bool Select(TableRow row)
		{
			if (Selected.Contains(row))
			{
				Selected.Remove(row);
				return false;
			}

			Selected.Add(row);
			return true;
		}

		public new bool Remove(TableRow row)
		{
			if (!base.Remove(row)) return false;

			Selected.Remove(row);
			OnRemoved?.Invoke(row);
			return true;
		}

		public new void Clear()
		{
			base.Clear();
			Selected.Clear();
		}
	}

	public class CSelectedRows : HashSet<TableRow>
	{
		public readonly TableView TableView;
		public event Action? OnChanged; // Изменяем это на событие


		public CSelectedRows(TableView tableView)
		{
			TableView = tableView;
		}


		public new bool Add(TableRow row)
		{
			if (!base.Add(row)) return false;

			row.CheckBox.SetPressedNoSignal(true);
			OnChanged?.Invoke();
			return true;
		}

		public new bool Remove(TableRow row)
		{
			if (!base.Remove(row)) return false;

			row.CheckBox.SetPressedNoSignal(false);
			OnChanged?.Invoke();
			return true;
		}

		public new void Clear()
		{
			base.Clear();
			OnChanged?.Invoke();
		}

		public List<T> ToList<T>() where T : TableRow
		{
			List<T> list = new();

			foreach (TableRow row in this)
			{
				list.Add((T)row);
			}

			return list;
		}
	}
}