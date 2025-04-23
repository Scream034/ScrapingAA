namespace Core.UI.Window;

using Godot;

[GlobalClass]
public partial class BaseWindow : Window
{
	[Export] public Button? CloseButton;

	public override void _EnterTree()
	{
		base._EnterTree();

		if (CloseButton != null)
		{
			CloseButton.Pressed += OnCloseButtonPressed;
			CloseButton.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
			CloseButton.MouseFilter = Control.MouseFilterEnum.Stop;
			CloseButton.MouseRecursiveBehavior = Control.RecursiveBehavior.Enabled;
		}
	}

	protected virtual void OnCloseButtonPressed()
	{
		QueueFree();
	}
}