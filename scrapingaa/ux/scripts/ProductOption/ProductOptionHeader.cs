namespace UX.ProductOption;

using System.Threading.Tasks;
using Godot;
using Core;
using Core.Log;
using DocumentFormat.OpenXml.Bibliography;

public sealed partial class ProductOptionHeader : Control
{
	[Export] public LineEdit TitleLineEdit = null!;
	[Export] public LineEdit UrlLineEdit = null!;

	public ProductOptionPanel Parent = null!;

	public override void _EnterTree()
	{
		TitleLineEdit.Text = Parent.Product.Title;
		UrlLineEdit.Text = Parent.Product.URL;

		TitleLineEdit.TextSubmitted += OnTitleLineEditSubmitted;
		UrlLineEdit.TextSubmitted += OnUrlLineEditSubmitted;
	}

	private async void OnTitleLineEditSubmitted(string newText)
	{
		Parent.Row.Name = newText; // Изменяем имя

		if (Parent.Row.Name != newText)
		{
			Log.Error($"Failed set Title: {newText}");
			await HighlightLineEditAsync(TitleLineEdit, "red");
			TitleLineEdit.Text = Parent.Product.Title; // Имя не изменилось
			OS.Alert($"Не удалось поставить Название: \"{newText}\"");
		}
		else
		{
			_ = HighlightLineEditAsync(TitleLineEdit);
		}

		Parent.Row.IsIncorrectTitle = Parent.Product.Title == null || Parent.Product.Title.Contains(Constants.IncorrectTitleString);
	}

	private void OnUrlLineEditSubmitted(string newText)
	{
		Parent.Product.URL = newText;
		_ = HighlightLineEditAsync(UrlLineEdit);
	}

	private async Task HighlightLineEditAsync(LineEdit lineEdit, string colorName = "green", double duration = 0.2)
	{
		var originalColor = lineEdit.Modulate;
		var tree = GetTree();
		await ToSignal(tree.CreateTween().TweenProperty(lineEdit, new NodePath(LineEdit.PropertyName.Modulate), Color.FromString(colorName, originalColor), duration), Tween.SignalName.Finished);
		await Task.Delay((int)duration * 500);
		lineEdit.Modulate = originalColor;
	}
}