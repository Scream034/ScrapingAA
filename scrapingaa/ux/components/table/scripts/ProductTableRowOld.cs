#if false
namespace UX.Components.Table;

using Godot;

using System;
using System.Collections.Generic;
using System.Linq;

using Core;
using Core.Log;
using Core.Components;
using Core.Components.Characteristics;
using System.Text.Json;

public partial class ProductTableRow : TableRow
{
	public static class Meta
	{
		public static class CharacteristicRow
		{
			public static readonly string CanMove = "need";
			public static readonly string LastColor = "lastColor";
			public static readonly string OldKey = "oldKey";
			public static readonly string CategoryType = "categoryType";
		}
	}

	public IProductBase<IBaseCharacteristicsParser> Product;
	public new string? Title { get => Product.Title; set => Product.SaveManager.SetTitle(value); }
	public new string? URL { get => Product.URL; set => Product.URL = value; }
	public HashSet<HBoxContainer> SelectedRows = new();
	public ProductCharacteristics Characteristics { get => Product.CharacteristicsParser.Characteristics.Storage; set => Product.CharacteristicsParser.Characteristics.Storage = value; }
	public VBoxContainer? TableContainer;
	public HBoxContainer? CurrentRow;
	public HBoxContainer? LastRow;

	public bool IsEmptyCharacteristics
	{
		get => Characteristics.Count == 0;
		set
		{
			
			if (value)
			{
				Button button = new() { Name = "EmptyCharacteristicsButton", Text = "◬" };
				button.Pressed += OnEmptyCharacteristicsButtonPressed;
				button.Modulate = new Color(0, 0, 1, 1f);
				button.TooltipText = "Пустой список характеристик";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("EmptyCharacteristicsButton")?.QueueFree();
			}
		}
	}

	public bool IsEmptyImages
	{
		get => Product.SaveManager.ImagePaths.Count == 0;
		set
		{
			if (value)
			{
				Button button = new() { Name = "EmptyImagesButton", Text = "◬" };
				button.Pressed += OnEmptyImagesButtonPressed;
				button.Modulate = new Color(1, 1, 0, 1f);
				button.TooltipText = "Пустой список изображений";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("EmptyImagesButton")?.QueueFree();
			}
		}
	}

	public bool IsIncorrectTitle
	{
		get => Product.SaveManager.ImagePaths.Count == 0;
		set
		{
			if (value)
			{
				Button button = new() { Name = "IncorrectTitleButton", Text = "◬" };
				button.Pressed += OnIncorrectTitleButtonPressed;
				button.Modulate = new Color(0, 1, 1, 1f);
				button.TooltipText = "Некорректное название";
				AddChild(button);
			}
			else
			{
				GetNodeOrNull<Button>("IncorrectTitleButton")?.QueueFree();
			}
		}
	}


	public new string Name
	{
		get => GetName();
		set
		{
			if (Product.SaveManager.SetTitle(value) is string title)
			{
				TitleButton.Text = title;
				SetName(title);
			}
			else
			{
				Log.Error($"Failed to set title for product: {product.SaveManager.DirectoryPath}");
			}
		}
	}

	private Panel? _panel;
	private ProductCharacteristics? _originalCharacteristics; // Для хранения оригинальных характеристик


	public ProductTableRow(IProductBase<IBaseCharacteristicsParser> product) : base(product.Title!, product.URL!)
	{
		Product = product;

		Modulate = product.IsBroken ? new Color(1, 0, 0, 1f) : new Color(1, 1, 1, 1f);

		IsEmptyCharacteristics = Characteristics.Count == 0;
		IsEmptyImages = Product.SaveManager.ImagePaths.Count == 0;
		IsIncorrectTitle = Title == null || Title.Contains(Constants.IncorrectTitleString);
	}


	public override void Init()
	{
		if (Product.IsBroken)
		{
			Log.Error($"Product is broken: {product.SaveManager.DirectoryPath}");
			OS.Alert("Невозможно открыть сломанный продукт", "Ошибка");
			return;
		}

		Clear();
		CreatePanel();
		CreateCharacteristicsTable();
		GetTree().Root.GetChild(1).AddChild(_panel);
	}

	public override void _UnhandledKeyInput(InputEvent _)
	{
		if (Input.IsActionJustReleased(Constants.Keys.DeleteRow))
		{
			if (SelectedRows.Count() > 0)
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

	public bool Clear()
	{
		if (_panel == null) return false;

		SelectedRows.Clear();
		_panel.QueueFree();
		_panel = null;

		return true;
	}

	public void ToggleAllButtons()
	{
		var closeButton = _panel!.GetNode<Button>("MainContainer/CloseButton");
		var restoreButton = _panel!.GetNode<Button>("MainContainer/RestoreButton");
		var aiButton = _panel!.GetNode<Button>("MainContainer/AIButton");
		closeButton.Disabled = !closeButton.Disabled;
		restoreButton.Disabled = !restoreButton.Disabled;
		aiButton.Disabled = !aiButton.Disabled;
	}

	private void CreatePanel()
	{
		_panel = new Panel() {
			MouseFilter = MouseFilterEnum.Pass
		};
		_panel.SetAnchorsPreset(LayoutPreset.FullRect);

		VBoxContainer vBox = new VBoxContainer() { Name = "MainContainer" };
		vBox.SetAnchorsPreset(LayoutPreset.FullRect);

		// Добавляем кнопки
		vBox.AddChild(CreateLabelWithLineEdit("Название: ", Title!, OnTitleChanged));
		vBox.AddChild(CreateLabelWithLineEdit("Ссылка: ", URL!, OnUrlChanged));
		vBox.AddChild(CreateAIButton());
		vBox.AddChild(CreateRestoreButton()); // Добавляем кнопку восстановления
		vBox.AddChild(CreateCloseButton());

		vBox.AddChild(CreateCharacteristicsContainer());

		_panel.AddChild(vBox);
	}

	private void UpdateCharacteristicsContainer()
	{
		VBoxContainer vBox = _panel!.GetNode<VBoxContainer>("MainContainer");
		vBox.GetNode<ScrollContainer>("CharacteristicsContainer").Free();
		vBox.AddChild(CreateCharacteristicsContainer());
	}

	private ScrollContainer CreateCharacteristicsContainer()
	{
		ScrollContainer scrollContainer = new ScrollContainer
		{
			Name = "CharacteristicsContainer",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		scrollContainer.AddChild(CreateCharacteristicsTable());
		return scrollContainer;
	}

	private void OnTitleChanged(LineEdit lineEdit, string newText)
	{
		Name = newText; // Изменяем имя

		if (Name != newText)
		{
			Log.Error($"Directory {newText} already exists");
			OS.Alert($"Папка с именем \"{newText}\" уже существует");
			lineEdit.Text = Name; // Имя не изменилось
		}

		IsIncorrectTitle = Title == null || Title.Contains(Constants.IncorrectTitleString);
	}

	private void OnUrlChanged(LineEdit _, string newText)
	{
		URL = newText;
	}

	private async void OnAIButtonPressed()
	{
		if (!Global.Instance.ChatBot.IsServerStarted)
		{
			OS.Alert("Сервер чат-бота не запущен", "Ошибка!");
			Log.Error("Chat bot server is not started");
			return;
		}

		// Сохраняем оригинальные характеристики перед изменением
		_originalCharacteristics = Characteristics.Clone();

		string characteristicsText = "";
		foreach (var category in Characteristics)
		{
			characteristicsText += $"{category.Key}:\n";
			foreach (var characteristic in category.Value)
			{
				characteristicsText += $"{characteristic.Key}: {characteristic.Value}\n";
			}
		}

		ToggleAllButtons();

		string response = (await Global.Instance.ChatBot.SendMessageAsync($"<Content>{characteristicsText}</Content>")).Replace("```json", "").Replace("```", "");
		GD.Print(response);

		// Парсинг JSON
		try
		{
			var document = JsonDocument.Parse(response);
			ProductCharacteristics characteristics = new();

			foreach (var property in document.RootElement.EnumerateObject())
			{
				characteristics.Add(ProductCharacteristicsType.FromDisplayString(property.Name), property.Value.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString()));
			}

			if (characteristics.Count > 0)
			{
				Characteristics = characteristics;
				UpdateCharacteristicsContainer();
			}
		}
		catch (JsonException ex)
		{
			Log.Error($"Ошибка при парсинге JSON: {ex.Message}");
			OS.Alert("Ошибка при парсинге JSON", "Ошибка!");
		}
		finally
		{
			ToggleAllButtons();
			OS.Alert("Сортировка завершена", "Уведомление!");
		}
	}

	// Новый метод для восстановления характеристик
	private void OnRestoreButtonPressed()
	{
		if (_originalCharacteristics == null)
		{
			OS.Alert("Нечего восстанавливать", "Уведомление!");
			return;
		}

		Characteristics = _originalCharacteristics.Clone(); // Восстанавливаем оригинальные характеристики
		UpdateCharacteristicsContainer(); // Обновляем UI
		_originalCharacteristics = null; // Очищаем ссылку на оригинальные характеристики
		OS.Alert("Успешно восстановлено", "Уведомление!");
	}

	private void OnCloseButtonPressed()
	{
		product.SaveManager.SaveAsync();
		Clear();
	}

	private Button CreateAIButton()
	{
		Button aiButton = new Button { Name = "AIButton", Text = "Сортировать с помощью ИИ" };
		aiButton.Pressed += OnAIButtonPressed;
		return aiButton;
	}

	private Button CreateRestoreButton()
	{
		Button restoreButton = new Button { Name = "RestoreButton", Text = "Восстановить оригинальные характеристики" };
		restoreButton.Pressed += OnRestoreButtonPressed;
		return restoreButton;
	}

	private Button CreateCloseButton()
	{
		Button closeButton = new Button { Name = "CloseButton", Text = "Закрыть подробную информацию" };
		closeButton.Pressed += OnCloseButtonPressed;
		return closeButton;
	}

	private HBoxContainer CreateLabelWithLineEdit(string labelText, string initialValue, Action<LineEdit, string> onTextChanged)
	{
		HBoxContainer container = new HBoxContainer();
		Label label = new Label { Text = labelText };
		LineEdit lineEdit = new LineEdit { Text = initialValue };

		lineEdit.TextChanged += (string newText) =>
		{
			onTextChanged(lineEdit, newText);
		};

		label.SizeFlagsHorizontal = SizeFlags.Fill;
		lineEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		container.SizeFlagsHorizontal = SizeFlags.ExpandFill;

		container.AddChild(label);
		container.AddChild(lineEdit);
		return container;
	}

	private VBoxContainer CreateCharacteristicsTable()
	{
		if (_panel == null)
		{
			Log.Error("panel is not found");
			throw new InvalidOperationException("panel is not found");
		}

		TableContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.Fill,
			Name = "VBoxCharacteristicsTable"
		};

		// Сначала добавляем все категории, кроме Unknown
		foreach (var category in ProductCharacteristics.DisplayTypeNames.Where(static c => c.Key != ProductCharacteristics.Type.Unknown))
		{
			string keyName = category.Key.ToDisplayString();
			VBoxContainer categoryContainer = new VBoxContainer { Name = keyName };
			Label categoryLabel = CreateCategoryLabel(keyName);

			VBoxContainer characteristics = CreateCharacteristicsRow(category.Key);

			categoryContainer.AddChild(categoryLabel);
			categoryContainer.AddChild(characteristics);

			TableContainer.AddChild(categoryContainer);
		}

		// Добавляем Unknown категорию в конец
		string? unknownCategory = ProductCharacteristics.DisplayTypeNames[ProductCharacteristics.Type.Unknown];
		VBoxContainer unknownCategoryContainer = new VBoxContainer { Name = unknownCategory };
		Label unknownCategoryLabel = CreateCategoryLabel(unknownCategory);

		VBoxContainer _characteristics = CreateCharacteristicsRow(ProductCharacteristics.Type.Unknown);

		unknownCategoryContainer.AddChild(unknownCategoryLabel);
		unknownCategoryContainer.AddChild(_characteristics);

		TableContainer.AddChild(unknownCategoryContainer);

		return TableContainer;
	}

	// Метод для создания метки категории с голубым цветом
	private Label CreateCategoryLabel(string text)
	{
		Label categoryLabel = new Label
		{
			Name = typeof(Label).Name,
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			Modulate = new Color(0.0f, 0.5f, 1.0f) // Устанавливаем цвет текста на голубой
		};
		return categoryLabel;
	}

	private VBoxContainer CreateCharacteristicsRow(ProductCharacteristics.Type category)
	{
		VBoxContainer rowContainer = new VBoxContainer();

		rowContainer.AddChild(CreateCharacteristicRow(ProductCharacteristics.Type.Unknown, "", "", false));
		foreach (var pair in Characteristics.Get(category))
		{
			HBoxContainer row = CreateCharacteristicRow(category, pair.Key, pair.Value, true);

			rowContainer.AddChild(row);
		}

		return rowContainer;
	}

	private HBoxContainer CreateCharacteristicRow(ProductCharacteristics.Type type, string key, string value, bool canMove)
	{
		HBoxContainer row = new HBoxContainer();

		LineEdit keyEdit = new LineEdit { Name = "KeyEdit", Text = key, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		LineEdit valueEdit = new LineEdit { Name = "ValueEdit", Text = value, SizeFlagsHorizontal = SizeFlags.ExpandFill };

		row.SetMeta(Meta.CharacteristicRow.OldKey, key);
		row.SetMeta(Meta.CharacteristicRow.CategoryType, type.ToString());

		keyEdit.FocusEntered += () => OnKeyFocusEntered(row);
		keyEdit.FocusExited += () => OnKeyFocusExited(row, keyEdit);
		keyEdit.TextChanged += _ => OnKeyChanged(keyEdit);
		valueEdit.TextChanged += _ => OnValueChanged(valueEdit);

		row.SetMeta(Meta.CharacteristicRow.CanMove, canMove);

		row.AddChild(keyEdit);
		row.AddChild(valueEdit);
		return row;
	}

	private void RemoveRow(HBoxContainer row)
	{
		LineEdit lineEdit = row.GetNode<LineEdit>("KeyEdit");
		Characteristics.RemoveKey(lineEdit.Text);
		row.QueueFree();
	}

	private void AddSelectedRow(HBoxContainer row)
	{
		Color color = new(0.2f, 0.2f, 0.8f);
		row.SetMeta(Meta.CharacteristicRow.LastColor, row.Modulate);
		row.Modulate = color;
		SelectedRows.Add(row);
		IsEmptyCharacteristics = false;
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
		IsEmptyCharacteristics = SelectedRows.Count == 0;
	}

	private void SelectRow(HBoxContainer row)
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

	private void OnKeyFocusEntered(HBoxContainer row)
	{
		SetProcessUnhandledKeyInput(true);

		VBoxContainer vBoxContainer = row.GetParent<VBoxContainer>();
		CurrentRow = row;

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
			if (Node.IsInstanceValid(LastRow) && SelectedRows.Count == 0 && LastRow.GetParent() != vBoxContainer)
			{
				SelectRow(LastRow);
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

					string? value = Characteristics[oldType, oldKeyText];
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

					Characteristics.Remove(oldType, oldKeyText);
					Characteristics.Add(newType, oldKeyText, value);

					oldVBoxContainer.RemoveChild(selectedRow);
					vBoxContainer.AddChild(selectedRow);
					RemoveSelectedRow(selectedRow);
				}
			}
		}
		else if (Input.IsActionPressed(Constants.Keys.MultiSelectOneRow))
		{
			SelectRow(row);
		}
	}

	private void OnKeyFocusExited(HBoxContainer row, LineEdit keyEdit)
	{
		SetProcessUnhandledKeyInput(false);
		LastRow = row;
	}

	private void OnKeyChanged(LineEdit keyEdit)
	{
		HBoxContainer row = keyEdit.GetParent<HBoxContainer>();
		ProductCharacteristics.Type categoryType = ProductCharacteristicsType.FromString(row.GetMeta(Meta.CharacteristicRow.CategoryType).As<string>())!;
		string oldKey = row.GetMeta(Meta.CharacteristicRow.OldKey).As<string>();

		if (Characteristics.Contains(categoryType, oldKey))
		{
			string newKey = keyEdit.Text;
			string? value = Characteristics[categoryType, key: oldKey];
			if (value == null)
			{
				Log.Error($"Value is null {categoryType}, {oldKey}");
				throw new InvalidOperationException("Value is null");
			}
			Characteristics.Remove(categoryType, oldKey);
			Characteristics[categoryType, newKey] = value;

			row.SetMeta(Meta.CharacteristicRow.OldKey, newKey);
		}
	}

	private void OnValueChanged(LineEdit valueEdit)
	{
		HBoxContainer row = valueEdit.GetParent<HBoxContainer>();
		ProductCharacteristics.Type categoryType = ProductCharacteristicsType.FromString(row.GetMeta(Meta.CharacteristicRow.CategoryType).As<string>())!;
		string oldKey = row.GetMeta(Meta.CharacteristicRow.OldKey).As<string>();

		if (Characteristics.Contains(categoryType, oldKey))
		{
			Characteristics[categoryType, oldKey] = valueEdit.Text;
		}
	}

	private void OnEmptyCharacteristicsButtonPressed()
	{
		OS.Alert("Пустой список характеристик");
	}

	private void OnEmptyImagesButtonPressed()
	{
		OS.Alert("Пустой список изображений");
	}

	private void OnIncorrectTitleButtonPressed()
	{
		OS.Alert("Неверный заголовок");
	}
}
#endif