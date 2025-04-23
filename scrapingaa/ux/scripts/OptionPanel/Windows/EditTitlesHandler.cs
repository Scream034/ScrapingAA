using System;
using System.Linq;
using System.Collections.Generic;
using Core;
using Core.Log;
using Core.Components;
using Core.UI.Window;
using Core.Extensions;
using Core.Components.Characteristics;
using Godot;

namespace UX.Option.First.Windows.EditTitles;

public partial class EditTitlesHandler : Control
{
	[Export] public LineEdit LineEdit = null!;
	[Export] public HBoxContainer HBoxButtons = null!;
	[Export] public Button PreviousButton = null!;
	[Export] public Button NextButton = null!;
	[Export] public Button ApplyButton = null!;
	[Export] public Label Label = null!;

	public List<Element> Elements = null!;
	public Element? CurrentElement;
	public int CurrentIndex = 0;
	public bool UnsavedChange { get => _unsavedChanged; set => SetUnsavedChanged(value); }
	private bool _unsavedChanged = false;

	public void Initialize(IEnumerable<IProductBase<IBaseCharacteristicsParser>> products)
	{
		Elements = products.Select(p => new Element(p)).ToList();
		SetupUI(Elements[0]);
		Next();
	}

	public void Apply(string word)
	{
		foreach (var element in Elements)
		{
			element.Apply(word);
		}
	}

	protected void SetupUI(Element element)
	{
		NextButton.Pressed += OnNextButtonPressed;
		PreviousButton.Pressed += OnPreviousButtonPressed;
		ApplyButton.Pressed += OnApplyButtonPressed;
		LineEdit.TextChanged += OnLineEditTextChanged;
		LineEdit.TextSubmitted += OnLineEditTextSubmitted;
	}

	protected void Load(Element element)
	{
		element.Load(HBoxButtons, LineEdit);
		element.SpaceSelected += OnSpaceSelected;
		CurrentElement = element;
		UpdateSelectedElement();
	}

	protected void UpdateSelectedElement()
	{
		Label.Text = CurrentElement == null ? $"Выбран: нет/{Elements.Count}" : $"Выбран: {Elements.FindIndex(e => e == CurrentElement) + 1}/{Elements.Count}";
	}

	protected void Next()
	{
		CurrentIndex++;
		if (CurrentIndex >= Elements.Count) CurrentIndex = 0;

		try
		{
			Load(element: Elements[CurrentIndex]);
		}
		catch (ArgumentOutOfRangeException)
		{
			Log.Error($"Index ({CurrentIndex}) out of range");
		}
	}

	protected void Previous()
	{
		CurrentIndex--;
		if (CurrentIndex < 0) CurrentIndex = 0;

		try
		{
			Load(element: Elements[CurrentIndex]);
		}
		catch (ArgumentOutOfRangeException)
		{
			Log.Error($"Index ({CurrentIndex}) out of range");
		}
	}

	private void SetUnsavedChanged(bool value)
	{
		Log.Print($"Unsaved change: {value}");

		if (value)
		{
			LineEdit.ShowTooltip($"Нажмите \"{Key.Enter}\" для сохранения изменений!", 0);
		}
		else
		{
			LineEdit.HideTooltip();
		}

		_unsavedChanged = value;
	}

	private void OnNextButtonPressed()
	{
		Next();
	}

	private void OnPreviousButtonPressed()
	{
		Previous();
	}

	private async void OnApplyButtonPressed()
	{
		string? word = await InputWindow.ShowAndAwaitAsync(GetTree(), "Введите слово для вставки: ", true, false);
		if (string.IsNullOrEmpty(word))
		{
			OS.Alert("Вы не ввели слово для применения!", "Ошибка!");
			return;
		};

		Apply(word);
	}

	private void OnLineEditTextChanged(string text)
	{
		UnsavedChange = true;
	}

	private void OnLineEditTextSubmitted(string text)
	{
		if (!UnsavedChange) return;

		Element element = Elements[CurrentIndex];
		element.Title = text;
		element.Load(HBoxButtons, LineEdit);
		UnsavedChange = false;
	}

	private void OnSpaceSelected(ushort number)
	{
		CurrentElement?.HighlightSelectedButton(HBoxButtons, number);
	}

	public class Element
	{
		public static Shortcut[] Shortcuts = [
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key1, Unicode = '1', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key2, Unicode = '2', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key3, Unicode = '3', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key4, Unicode = '4', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key5, Unicode = '5', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key6, Unicode = '6', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key7, Unicode = '7', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key8, Unicode = '8', ShiftPressed = true, Device = -1 } } },
			new() { Events = new() { new InputEventKey() { Keycode = Key.Key9, Unicode = '9', ShiftPressed = true, Device = -1 } } }
		];

		public event Action<ushort>? SpaceSelected; // Событие, вызываемое при нажатии на кнопку пробела

		public readonly IProductBase<IBaseCharacteristicsParser> Product;
		public string Title { get; set; }
		public ushort Space { get; private set; }
		public List<int> Spaces { get; private set; }

		public Element(IProductBase<IBaseCharacteristicsParser> product)
		{
			Product = product;
			Title = Product.Title!;
			Spaces = Title.FindAllPositions(' ');
		}

		public bool Apply(string word)
		{
			if (Space == 0) return true; // Если пробел не выбран, то ничего не делаем

			Title = Title.Insert(Spaces[Space - 1], word);

			if (Product.SaveManager.SetTitle(Title) == null)
			{
				return false;
			}

			return true;
		}

		public bool Load(HBoxContainer container, LineEdit lineEdit)
		{
			container.FreeChildren();

			container.AddChildren(CreateSpaceButtons((ushort)Spaces.Count, lineEdit));

			lineEdit.Text = Title;

			HighlightSelectedButton(container, Space);

			return true;
		}

		public Button[] CreateSpaceButtons(ushort count, LineEdit lineEdit)
		{
			return Enumerable.Range(1, count).Select(i => CreateSpaceButton((ushort)i, lineEdit)).ToArray();
		}

		public void HighlightSelectedButton(HBoxContainer container, ushort space)
		{
			foreach (Node node in container.GetChildren())
			{
				if (node is Button button)
				{
					if (button.Name == $"Space{space}")
					{
						button.Disabled = true;
					}
					else
					{
						button.Disabled = false;
					}
				}
			}
		}

		protected Button CreateSpaceButton(ushort number, LineEdit lineEdit)
		{
			Button button = new() { Name = $"Space{number}", Text = number.ToString() };
			button.Pressed += () =>
			{
				Title = lineEdit.Text;
				Space = number;
				SpaceSelected?.Invoke(number);
			};

			if (number >= 1 && number <= 9)
			{
				Shortcut shortcut = (Shortcut)Shortcuts[number - 1].Duplicate();
				button.Shortcut = shortcut;
				button.TooltipText = $"Нажмите Shift+{number} для выбора пробела ({number}), где вставить название";
			}
			else
			{
				button.TooltipText = $"Нажмите для выбора пробела ({number}), где вставить название";
			}


			return button;
		}
	}
}