namespace UX.ProductOption.Internal;

using System;
using System.Linq;
using System.Collections.Generic;
using Godot;
using Core;
using Core.Log;
using Core.Components;

public sealed partial class ProductCharacteristicsBody : HBoxContainer, IProductBaseBodyValue
{
	public static class Meta
	{
		public static class CharacteristicRow
		{
			public const string CanMove = "need";
			public const string LastColor = "lastColor";
			public const string OldKey = "oldKey";
			public const string CategoryType = "categoryType";
		}
	}

	[Export] public int ThemeKeyFontSize = 18;
	[Export] public int ThemeValueFontSize = 18;

	public ProductOptionPanel Parent = null!;
	public HashSet<HBoxContainer> SelectedRows = new();
	public HBoxContainer? CurrentRow;
	public HBoxContainer? LastRow;

	public override void _EnterTree()
	{
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		UpdateCategories();
	}

	public void Initialize(ProductOptionPanel parent)
	{
		Parent = parent;
	}

	public void Destroy()
	{
		CurrentRow = null;
		LastRow = null;
		SelectedRows.Clear();
	}

	public override void _UnhandledKeyInput(InputEvent _)
	{
		if (Input.IsActionJustReleased(Constants.Keys.DeleteRow))
		{
			if (SelectedRows.Count > 0)
			{
				foreach (HBoxContainer row in SelectedRows)
				{
					RemoveSelectedRow(row);
					RemoveRow(row);
				}
			}
			else
			{
				if (CurrentRow != null)
				{
					RemoveRow(CurrentRow);
				}
			}
		}
	}

	public void UpdateCategories()
	{
		if (HasNode(ProductCharacteristics.Type.Unknown.ToDisplayString()))
		{
			foreach (var category in ProductCharacteristics.DisplayTypeNames)
			{
				RemoveCategory(category.Key);
			}
		}

		foreach (var category in ProductCharacteristics.DisplayTypeNames)
		{
			AddChild(CreateCategory(category.Key));
		}
	}

	private VBoxContainer CreateCategory(in ProductCharacteristics.Type type)
	{
		string categoryName = type.ToDisplayString();

		VBoxContainer vBox = new()
		{
			Name = categoryName,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		Label categoryLabel = new()
		{
			Name = nameof(Label),
			Text = categoryName,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Modulate = new Color(0.0f, 0.5f, 1.0f) // Устанавливаем цвет текста на голубой
		};

		VBoxContainer rows = new();

		rows.AddChild(CreateCharacteristicRow(ProductCharacteristics.Type.Unknown, "", "", false));
		foreach (var pair in Parent.Product.CharacteristicsParser.Characteristics.Storage.Get(type))
		{
			HBoxContainer row = CreateCharacteristicRow(type, pair.Key, pair.Value, true);

			rows.AddChild(row);
		}

		vBox.AddChild(categoryLabel);
		vBox.AddChild(rows);
		return vBox;
	}

	private bool RemoveCategory(in ProductCharacteristics.Type type)
	{
		string categoryName = type.ToDisplayString();

		var node = GetNodeOrNull(categoryName);
		if (node == null)
		{
			return false;
		}

		RemoveChild(node);
		return true;
	}

	private void RemoveRow(HBoxContainer row)
	{
		LineEdit lineEdit = row.GetNode<LineEdit>("KeyEdit");
		Parent.Product.CharacteristicsParser.Characteristics.Storage.RemoveKey(lineEdit.Text);
		row.QueueFree();
	}

	private void AddSelectedRow(HBoxContainer row)
	{
		Color color = new(0.2f, 0.2f, 0.8f);
		row.SetMeta(Meta.CharacteristicRow.LastColor, row.Modulate);
		row.Modulate = color;
		SelectedRows.Add(row);
	}

	private void RemoveSelectedRow(HBoxContainer row)
	{
		Color? _color = (Color?)row.GetMeta(Meta.CharacteristicRow.LastColor, @default: default);
		if (_color is not Color color)
		{
			return;
		}

		row.Modulate = color;
		row.SetMeta(Meta.CharacteristicRow.LastColor, default);
		SelectedRows.Remove(row);
	}

	private void SelectRow(in HBoxContainer row)
	{
		bool need = (bool)row.GetMeta(Meta.CharacteristicRow.CanMove, false);
		if (!need) return; // Значит он не нужен в работе

		if (SelectedRows.Contains(row))
		{
			RemoveSelectedRow(row);
		}
		else
		{
			AddSelectedRow(row);
		}
	}

	private HBoxContainer CreateCharacteristicRow(in ProductCharacteristics.Type type, in string key, in string value, in bool canMove)
	{
		HBoxContainer row = new HBoxContainer();

		LineEdit keyEdit = new LineEdit
		{
			Name = "KeyEdit",
			Text = key,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		LineEdit valueEdit = new LineEdit
		{
			Name = "ValueEdit",
			Text = value,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};

		keyEdit.AddThemeConstantOverride("font_size", ThemeKeyFontSize);
		valueEdit.AddThemeConstantOverride("font_size", ThemeValueFontSize);

		row.SetMeta(Meta.CharacteristicRow.OldKey, key);
		row.SetMeta(Meta.CharacteristicRow.CategoryType, type.ToString());

		keyEdit.FocusEntered += () => OnFocusEntered(row, keyEdit);
		keyEdit.FocusExited += () => OnFocusExited(row);
		keyEdit.TextChanged += _ => OnKeyChanged(keyEdit);
		valueEdit.FocusEntered += () => OnFocusEntered(row, valueEdit);
		valueEdit.FocusExited += () => OnFocusExited(row);
		valueEdit.TextChanged += _ => OnValueChanged(valueEdit);

		row.SetMeta(Meta.CharacteristicRow.CanMove, canMove);

		row.AddChild(keyEdit);
		row.AddChild(valueEdit);
		return row;
	}

	private void OnFocusEntered(HBoxContainer row, LineEdit lineEdit)
	{
		SetProcessUnhandledKeyInput(true);

		int index = lineEdit.GetIndex() + 1 > 1 ? 0 : 1;
		if (row.GetChild(index) is LineEdit secondLineEdit)
		{
			lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			secondLineEdit.SizeFlagsHorizontal = SizeFlags.Fill;
		}

		VBoxContainer vBoxContainer = row.GetParent<VBoxContainer>();
		CurrentRow = row;

		// Обработка множественного фокуса
		if (Node.IsInstanceValid(LastRow))
		{
			// Логика для одиночного фокуса, если нет выделенных строк
			if (LastRow.GetParent() == vBoxContainer)
			{
				if (Input.IsActionPressed(Constants.Keys.MultiSelectLineRow))
				{
					int startIndex = Math.Min(row.GetIndex(), LastRow.GetIndex());
					int endIndex = Math.Max(row.GetIndex(), LastRow.GetIndex());
					if (startIndex != endIndex)
					{
						HBoxContainer startContainer = vBoxContainer.GetChild<HBoxContainer>(startIndex);
						HBoxContainer endContainer = vBoxContainer.GetChild<HBoxContainer>(endIndex);
						if (!SelectedRows.Contains(startContainer))
						{
							startIndex += 1;
							SelectRow(startContainer);
						}
						if (!SelectedRows.Contains(endContainer))
						{
							endIndex -= 1;
							SelectRow(endContainer);
						}

						for (int i = startIndex; i <= endIndex; i++)
						{
							HBoxContainer container = vBoxContainer.GetChild<HBoxContainer>(i);
							SelectRow(container);
						}
					}
				}
			}
		}

		if (Input.IsActionPressed(Constants.Keys.MoveRow))
		{
			MoveRow(LastRow, vBoxContainer);
		}
		else if (Input.IsActionPressed(Constants.Keys.MultiSelectOneRow))
		{
			SelectRow(row);
		}
	}

	private void OnFocusExited(in HBoxContainer row)
	{
		SetProcessUnhandledKeyInput(false);
		LastRow = row;

		foreach (Control control in row.GetChildren())
		{
			control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		}
	}

	private void OnKeyChanged(in LineEdit keyEdit)
	{
		HBoxContainer row = keyEdit.GetParent<HBoxContainer>();
		ProductCharacteristics.Type categoryType = ProductCharacteristicsType.FromString(row.GetMeta(Meta.CharacteristicRow.CategoryType).As<string>())!;
		string oldKey = row.GetMeta(Meta.CharacteristicRow.OldKey).As<string>();

		if (Parent.Product.CharacteristicsParser.Characteristics.Storage.Contains(categoryType, oldKey))
		{
			string newKey = keyEdit.Text;
			string? value = Parent.Product.CharacteristicsParser.Characteristics.Storage[categoryType, key: oldKey];
			if (value == null)
			{
				Log.Error($"Value is null {categoryType}, {oldKey}");
				throw new InvalidOperationException("Value is null");
			}

			Parent.Product.CharacteristicsParser.Characteristics.Storage.Remove(categoryType, oldKey);
			Parent.Product.CharacteristicsParser.Characteristics.Storage[categoryType, newKey] = value;

			row.SetMeta(Meta.CharacteristicRow.OldKey, newKey);
		}
	}

	private void OnValueChanged(in LineEdit valueEdit)
	{
		HBoxContainer row = valueEdit.GetParent<HBoxContainer>();
		ProductCharacteristics.Type categoryType = ProductCharacteristicsType.FromString(row.GetMeta(Meta.CharacteristicRow.CategoryType).As<string>())!;
		string oldKey = row.GetMeta(Meta.CharacteristicRow.OldKey).As<string>();

		if (Parent.Product.CharacteristicsParser.Characteristics.Storage.Contains(categoryType, oldKey))
		{
			Parent.Product.CharacteristicsParser.Characteristics.Storage[categoryType, oldKey] = valueEdit.Text;
		}
	}

	private void MoveRow(in HBoxContainer? row, in VBoxContainer vBoxContainer)
	{
		if (Node.IsInstanceValid(row) && SelectedRows.Count == 0 && row.GetParent() != vBoxContainer)
		{
			SelectRow(row);
		}

		foreach (var selectedRow in SelectedRows)
		{
			// Проверяем, можем ли перемещать строки
			bool canMove = (bool)selectedRow.GetMeta(Meta.CharacteristicRow.CanMove, false);
			VBoxContainer oldVBoxContainer = selectedRow.GetParent<VBoxContainer>();
			Label title = oldVBoxContainer.GetParent().GetNode<Label>(typeof(Label).Name);

			// Получаем старый ключ
			LineEdit oldKeyEdit = selectedRow.GetNode<LineEdit>("KeyEdit");
			string oldKeyText = oldKeyEdit.Text;

			// Перемещаем строку только если это разрешено
			if (canMove)
			{
				ProductCharacteristics.Type? _type = ProductCharacteristicsType.FromDisplayString(title.Text);
				if (_type is not ProductCharacteristics.Type oldType)
				{
					Log.Error($"Unknown type {title.Text}");
					return;
				}

				string? value = Parent.Product.CharacteristicsParser.Characteristics.Storage[oldType, oldKeyText];
				if (value == null)
				{
					Log.Error($"Value is null for type {oldType}, text is {oldKeyText}");
					throw new InvalidOperationException("Value is null");
				}

				title = vBoxContainer.GetParent().GetNode<Label>(typeof(Label).Name);

				_type = ProductCharacteristicsType.FromDisplayString(title.Text);
				if (_type is not ProductCharacteristics.Type newType)
				{
					Log.Error($"Unknown type {title.Text}");
					return;
				}

				Parent.Product.CharacteristicsParser.Characteristics.Storage.Remove(oldType, oldKeyText);
				Parent.Product.CharacteristicsParser.Characteristics.Storage.Add(newType, oldKeyText, value);

				oldVBoxContainer.RemoveChild(selectedRow);
				vBoxContainer.AddChild(selectedRow);
				RemoveSelectedRow(selectedRow);
			}
		}
	}
}
