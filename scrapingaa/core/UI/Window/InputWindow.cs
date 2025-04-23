namespace Core.UI.Window;
using System;
using System.Threading.Tasks;
using Godot;

public partial class InputWindow : BaseWindow
{
	public event Action<string?>? Finished;
	[Export] public LineEdit LineEdit = null!;
	[Export] public Label Label = null!;
	[Export] public Button? SubmitButton;
	[Export] public Button? CancelButton;

	public override void _Ready()
	{
		LineEdit.TextSubmitted += OnTextSubmitted;
		if (Node.IsInstanceValid(SubmitButton))
			SubmitButton.Pressed += OnSubmitButtonPressed;
		if (Node.IsInstanceValid(CancelButton))
			CancelButton.Pressed += OnCancelButtonPressed;
	}

	public void SetButtonsVisible(bool submitButton, bool cancelButton)
	{
		if (Node.IsInstanceValid(SubmitButton))
			SubmitButton.Visible = submitButton;
		if (Node.IsInstanceValid(CancelButton))
			CancelButton.Visible = cancelButton;
		if (Node.IsInstanceValid(CloseButton))
			CloseButton.Visible = cancelButton;
	}

	public string Submit()
	{
		string text = LineEdit.Text;
		Finished?.Invoke(text);

		if (string.IsNullOrEmpty(text))
			return string.Empty;

		return text;
	}

	public void Cancel()
	{
		Finished?.Invoke(null);
	}

	private void OnTextSubmitted(string text)
	{
		Submit();
		QueueFree();
	}

	private void OnSubmitButtonPressed()
	{
		Submit();
		QueueFree();
	}

	private void OnCancelButtonPressed()
	{
		Cancel();
		QueueFree();
	}

	public static InputWindow Create(string label, bool submitButton = true, bool cancelButton = true)
	{
		PackedScene scene = GD.Load<PackedScene>("res://core/resources/UI/Window/input_window.tscn");
		var window = scene.Instantiate<InputWindow>();
		window.Label.Text = label;
		window.SetButtonsVisible(submitButton, cancelButton);
		return window;
	}

	public static async Task<string?> ShowAndAwaitAsync(SceneTree tree, string label, bool submitButton = true, bool cancelButton = true)
	{
		var window = Create(label, submitButton, cancelButton);
		tree.Root.AddChild(window);
		window.Show();

		TaskCompletionSource<string?> tcs = new TaskCompletionSource<string?>();
		window.Finished += (text) =>
		{
			tcs.SetResult(text);
			window.QueueFree();
		};

		DisplayServer.Beep();

		return await tcs.Task;
	}
}