namespace UX.Components.Table;

using System;
using Godot;

public partial class TableRow : HBoxContainer
{
	public readonly Button TitleButton;
	public readonly CheckBox CheckBox;
	public string Title { get; set; }
	public string URL { get; set; }

	public TableRow(string title, string url)
	{
		Title = title;
		Name = Title;
		URL = url;

		CheckBox = new() { Name = nameof(CheckBox) };
		TitleButton = new() { Text = Title };

		TitleButton.Pressed += OnPressed; ;

		AddChild(CheckBox);
		AddChild(TitleButton);
	}

	public virtual void Init() => throw new NotImplementedException();

	protected virtual void OnPressed() => Init();

	public bool IsChecked() => CheckBox.ButtonPressed;
}