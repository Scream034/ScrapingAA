namespace UX.Head;

using Godot;
using System.IO;
using Core;
using Core.Admin;
using Core.UI;

public sealed partial class InputAdminPanel : AlternativePanel
{
	[Export] private LineEdit _loginEdit = null!;
	[Export] private LineEdit _passwordEdit = null!;
	[Export] private LineEdit _urlEdit = null!;

	private static string FilePath = Path.Combine(Constants.Path.Folder.User, Constants.PathName.File.Admin); // Путь к файлу для сохранения данных

	public override void _Ready()
	{
		base._Ready();

		// Загрузить сохраненные данные
		LoadAdminInfo();

		// Подключить события изменения текста
		_loginEdit.TextChanged += OnLineEditChanged;
		_passwordEdit.TextChanged += OnLineEditChanged;
		_urlEdit.TextChanged += OnLineEditChanged;
	}

	private void OnLineEditChanged(string newText)
	{
		// Сохранить информацию каждый раз, когда изменяется текст
		SaveAdminInfo();
	}

	public AdminInfo GetAdminInfo()
	{
		return new(_loginEdit.Text, _passwordEdit.Text, _urlEdit.Text);
	}

	private void SaveAdminInfo()
	{
		// Сохранить логин, пароль и URL в файл
		string data = $"{_loginEdit.Text}\n{_passwordEdit.Text}\n{_urlEdit.Text}";
		File.WriteAllText(FilePath, data);
	}

	private void LoadAdminInfo()
	{
		// Проверка, существует ли файл
		if (File.Exists(FilePath))
		{
			// Загрузить данные из файла
			string[] data = File.ReadAllLines(FilePath);
			if (data.Length >= 3)
			{
				_loginEdit.Text = data[0];
				_passwordEdit.Text = data[1];
				_urlEdit.Text = data[2];
			}
		}
	}
}