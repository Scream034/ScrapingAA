using Godot;

namespace UX.Head;

using Core.UI;
using Core.Log;

public sealed partial class EditSitesPanel : AlternativePanel
{
	[Export] private LineEdit _inputUrl = null!;
	[Export] private Button _addButton = null!;
	[Export] private Button _removeButton = null!;
	[Export] private OptionButton _selectButton = null!;

	private const string SitesFileName = "user://websites.txt"; // Путь к файлу

	public override void _Ready()
	{
		base._Ready();

		_addButton.Pressed += AddSite;
		_removeButton.Pressed += RemoveSite;

		LoadWebsites(); // Загружаем сайты из файла
	}

	public string[] GetWebsites()
	{
		string[] websites = new string[_selectButton.ItemCount];

		for (int i = 0; i < _selectButton.ItemCount; i++)
		{
			websites[i] = _selectButton.GetItemText(i);
		}

		return websites;
	}

	public void AddSite()
	{
		string? url = _inputUrl.Text;
		if (string.IsNullOrEmpty(url))
		{
			Log.Error("Invalid URL");
			return;
		}

		if (url.StartsWith("https://"))
		{
			_selectButton.AddItem(url);
			Global.Instance.Spider.WebsitesToVisit.Add(url);
			SaveWebsites(); // Сохраняем изменения
			_inputUrl.Clear();
		}
		else
		{
			Log.Error("URL must start with https://");
			OS.Alert("URL должен начинаться с https://", "Ошибка добавления сайта");
		}
	}

	public void RemoveSite()
	{
		int index = _selectButton.Selected;
		if (index == -1)
		{
			Log.Error("No site selected");
			OS.Alert("Не выбран сайт", "Ошибка удаления сайта");
			return;
		}

		_selectButton.RemoveItem(index);
		Global.Instance.Spider.WebsitesToVisit.RemoveAt(index);
		SaveWebsites(); // Сохраняем изменения
	}

	public void LoadWebsites()
	{
		_selectButton.Clear();

		if (FileAccess.Open(SitesFileName, FileAccess.ModeFlags.Read) is FileAccess file)
		{
			while (!file.EofReached())
			{
				var line = file.GetLine().StripEdges();
				if (!string.IsNullOrEmpty(line))
				{
					_selectButton.AddItem(line);
					Global.Instance.Spider.WebsitesToVisit.Add(line);
				}
			}
			file.Close();
		}
		else
		{
			// По умолчанию если файла нет, добавляем сайты из Global.Instance
			foreach (string item in Global.Instance.Spider.WebsitesToVisit)
			{
				_selectButton.AddItem(item);
			}
		}
	}

	public void SaveWebsites()
	{
		if (FileAccess.Open(SitesFileName, FileAccess.ModeFlags.Write) is FileAccess file)
		{
			foreach (string url in Global.Instance.Spider.WebsitesToVisit)
			{
				file.StoreLine(url); // Сохраняем каждую строку
			}
			file.Close();
		}
	}
}