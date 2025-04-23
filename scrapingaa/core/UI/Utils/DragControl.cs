namespace Core.UI.Utils;

using Godot;

[GlobalClass, Tool]
public partial class DragControl : Control
{
	[Export] public Node Target { get; set; } = null!;

	private bool _isDragging = false;
	private Vector2 _offset;
	private Vector2 _targetPosition; // Предполагаемая позиция
	private Window? _highlight;

	public override void _EnterTree()
	{
		base._EnterTree();

		if (Constants.IsEditorHint)
		{
			foreach (var child in GetChildren())
			{
				if (child is not Control control) continue;
				else if (control.MouseFilter == MouseFilterEnum.Pass) continue;
				else if (control is Button || control is Range) continue;

				control.TreeEntered += () =>
				{
					if (control.GetParent() is Control parentControl)
					{
						GD.PushWarning($"Control {parentControl.Name}/{control.Name} has MouseFilter set to {control.MouseFilter}, which will prevent the drag control from working properly.\rIMPORTANT: Please set it to Pass.");
					}
				};
			}

			return; // Выход из кода редактора
		};

		MouseDefaultCursorShape = CursorShape.Move;
		MouseFilter = MouseFilterEnum.Pass;
		MouseForcePassScrollEvents = true;
		MouseRecursiveBehavior = RecursiveBehavior.Inherited;

		ProcessPriority = 2;
		ProcessPhysicsPriority = 2;

		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		SetProcessInput(false);
		SetProcessUnhandledInput(false);
		SetProcess(false);
		SetPhysicsProcess(false);
		SetPhysicsProcessInternal(false);
	}

	public override void _Ready()
	{
		base._Ready();

		if (Target.Get(Control.PropertyName.Position).VariantType == Variant.Type.Nil)
		{
			throw new System.InvalidOperationException("Target must have a Position property");
		}
		else if (Target.Get(Control.PropertyName.Size).VariantType == Variant.Type.Nil)
		{
			throw new System.InvalidOperationException("Target must have a Size property");
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (Constants.IsEditorHint) return;

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					// Начало перетаскивания
					_isDragging = true;
					_offset = mouseEvent.GlobalPosition - (Vector2)Target.Get(Control.PropertyName.Position);
					_highlight = CreateHighlight(Target);
					GetTree().Root.AddChild(_highlight);
					_highlight.Popup();
				}
				else
				{
					// Окончание перетаскивания
					_isDragging = false;
					if (!Node.IsInstanceValid(_highlight)) return;

					Target.Set(Control.PropertyName.Position, _targetPosition);
					_highlight.QueueFree();
				}
			}
		}
		else if (_isDragging && @event is InputEventMouseMotion motionEvent)
		{
			// Вычисляем предполагаемую позицию
			_targetPosition = motionEvent.GlobalPosition - _offset;
			_highlight!.Position = (Vector2I)_targetPosition;
		}
	}

	public Window CreateHighlight(Node target)
	{
		// TODO: Подумать об использование паттерна instance
		var window = new Window()
		{
			Name = "Highlight",
			Position = (Vector2I)target.Get(Control.PropertyName.Position),
			Size = (Vector2I)target.Get(Control.PropertyName.Size),
			Borderless = true,
			Unfocusable = true,
			SharpCorners = true,
			ProcessMode = ProcessModeEnum.Disabled,
			ProcessPriority = -1,
			ProcessPhysicsPriority = -1,
			GuiDisableInput = true,
			Disable3D = true,
			HandleInputLocally = false,
			Unresizable = true
		};

		return window;
	}

	private void OnMouseEntered()
	{
		if (_isDragging) return;
		SetProcessUnhandledInput(true);
	}

	private void OnMouseExited()
	{
		if (_isDragging) return;
		SetProcessUnhandledInput(false);
	}
}