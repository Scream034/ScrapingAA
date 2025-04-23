using Core;
using Core.Log;
using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class WindowServiceHandler : Control
{
	public const string Separator = "\uE000";

	// Пути к файлам
	public static class Paths
	{
		public static readonly string StashFolder = System.IO.Path.Combine(Constants.Path.Folder.User, "wTitles");
	}

	[Export] public LineEdit MainFreeLineEdit { get; set; } = null!;
	[Export] public LineEdit SubFreeLineEdit { get; set; } = null!;
	[Export] public VBoxContainer MainVBox { get; set; } = null!;
	[Export] public VBoxContainer SubVBox { get; set; } = null!;
	[Export] public Button CloseButton { get; set; } = null!;

	public Dictionary<string, List<string>> Titles = new();

	private string? lastMainTitle;
	private LineEdit? currentFocusedMainLineEdit;

	public override void _Ready()
	{
		MainFreeLineEdit.TextSubmitted += OnMainLineEditTextSubmitted;
		SubFreeLineEdit.TextSubmitted += OnSubLineEditTextSubmitted;
		CloseButton.Pressed += OnCloseButtonPressed;

		SubFreeLineEdit.Visible = false;

		LoadTitles();
		ParseTitles();
	}

	public List<string>? GetCurrentTitles()
	{
		if (string.IsNullOrWhiteSpace(lastMainTitle)) return null;

		var list = new List<string>() { lastMainTitle };
		list.AddRange(Titles[lastMainTitle]);

		return list;
	}

	/// <summary>
	/// Сохраняет все заголовки в файлы.
	/// </summary>
	public void SaveTitles()
	{
		Log.Print("Saving Titles...");
		if (!System.IO.Directory.Exists(Paths.StashFolder))
			System.IO.Directory.CreateDirectory(Paths.StashFolder);

		foreach (var entry in Titles)
		{
			string filePath = System.IO.Path.Combine(Paths.StashFolder, entry.Key);
			using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
			string str = string.Join(Separator, entry.Value.Where(static x => !string.IsNullOrWhiteSpace(x)));
			file.StoreString(str);
			Log.Print($"Saved {str} to {filePath}");
		}
		Log.Print("Titles saved successfully.");
	}

	/// <summary>
	/// Загружает все заголовки из файлов.
	/// </summary>
	public void LoadTitles()
	{
		Log.Print("Loading Titles...");
		if (!System.IO.Directory.Exists(Paths.StashFolder)) return;

		foreach (var file in System.IO.Directory.GetFiles(Paths.StashFolder))
		{
			string fileName = System.IO.Path.GetFileNameWithoutExtension(file);
			string content = FileAccess.Open(file, FileAccess.ModeFlags.Read).GetAsText();
			Titles[fileName] = content.Split(Separator).ToList();

			Log.Print($"Loaded {Titles[fileName].Count} Titles from {file}");
		}

		Log.Print("Titles loaded successfully.");
	}

	/// <summary>
	/// Форматирует заголовки для редактирования
	/// </summary>
	public void ParseTitles()
	{
		ResetVBox(MainVBox);

		foreach (var entry in Titles)
		{
			if (string.IsNullOrWhiteSpace(entry.Key)) continue;

			LineEdit newLineEdit = CreateLineEdit(MainFreeLineEdit, entry.Key);
			newLineEdit.FocusEntered += () => OnMainLineEditFocused(newLineEdit);
			MainVBox.AddChild(newLineEdit);
		}
	}

	/// <summary>
	/// Обработчик нажатия кнопки закрытия окна.
	/// </summary>
	private void OnCloseButtonPressed()
	{
		lastMainTitle = currentFocusedMainLineEdit?.Text;
		SaveTitles();
		QueueFree();
	}

	/// <summary>
	/// Обработчик отправки текста из основной строки ввода.
	/// </summary>
	private void OnMainLineEditTextSubmitted(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			OS.Alert("Название не может быть пустым", "Ошибка!");
			return;
		}
		else if (Titles.ContainsKey(text))
		{
			OS.Alert("Такое название уже существует", "Ошибка!");
			return;
		}

		// Создаем новую строку ввода для основного списка
		var newLineEdit = CreateLineEdit(MainFreeLineEdit, text);
		newLineEdit.FocusEntered += () => OnMainLineEditFocused(newLineEdit);
		Titles[text] = new List<string>();
		MainVBox.AddChild(newLineEdit);

		MainFreeLineEdit.Text = "";
		HighlightElement(newLineEdit);
		newLineEdit.GrabFocus();
		SubFreeLineEdit.GrabFocus();
	}

	/// <summary>
	/// Обработчик отправки текста из дополнительной строки ввода.
	/// </summary>
	private void OnSubLineEditTextSubmitted(string text)
	{
		if (currentFocusedMainLineEdit == null || string.IsNullOrWhiteSpace(text))
		{
			OS.Alert("Форма слова не может быть пустой", "Ошибка!");
			return;
		}
		else if (Titles.ContainsKey(text))
		{
			OS.Alert("Такая форма слова уже существует", "Ошибка!");
			return;
		}

		// Добавляем новую форму слова
		var mainKey = currentFocusedMainLineEdit.Text;
		Titles[mainKey].Add(text);

		var newLineEdit = CreateLineEdit(SubFreeLineEdit, text);
		SubVBox.AddChild(newLineEdit);

		SubFreeLineEdit.Text = "";
		HighlightElement(newLineEdit);
	}

	/// <summary>
	/// Создает новый элемент LineEdit.
	/// </summary>
	private LineEdit CreateLineEdit(LineEdit template, string text)
	{
		var newLineEdit = (LineEdit)template.Duplicate();
		newLineEdit.Text = text;
		return newLineEdit;
	}

	/// <summary>
	/// Вызывается при фокусировке элемента основного списка.
	/// </summary>
	private void OnMainLineEditFocused(LineEdit lineEdit)
	{
		lineEdit.Modulate = Colors.LightBlue;
		if (currentFocusedMainLineEdit != null) currentFocusedMainLineEdit.Modulate = Colors.White;
		currentFocusedMainLineEdit = lineEdit;
		UpdateSubVBox(lineEdit.Text);
	}

	/// <summary>
	/// Обновляет содержимое дополнительного списка.
	/// </summary>
	private void UpdateSubVBox(string mainKey)
	{
		ResetVBox(SubVBox);
		SubFreeLineEdit.Visible = true;

		if (Titles.TryGetValue(mainKey, out var subItems))
		{
			foreach (var item in subItems)
			{
				if (!string.IsNullOrWhiteSpace(item))
				{
					var lineEdit = CreateLineEdit(SubFreeLineEdit, item);
					SubVBox.AddChild(lineEdit);
				}
			}
		}
	}

	/// <summary>
	/// Очищает контейнер VBox.
	/// </summary>
	private void ResetVBox(VBoxContainer container)
	{
		foreach (Node child in container.GetChildren())
		{
			if (child is LineEdit && child != SubFreeLineEdit && child != MainFreeLineEdit)
				child.QueueFree();
		}
	}

	/// <summary>
	/// Подсвечивает элемент при фокусировке.
	/// </summary>
	private void HighlightElement(Control element)
	{
		element.Modulate = Colors.LightBlue; // Временно изменяем цвет для визуального эффекта
		GetTree().CreateTimer(1f).Timeout += () => element.Modulate = Colors.White; // Возвращаем цвет через время
	}
}