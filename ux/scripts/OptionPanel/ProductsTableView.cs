namespace UX.Option;

using Godot;
using System.Collections.Generic;

using Core;
using Core.Log;
using Core.Components;

using System;
using System.Linq;
using Core.Components.Characteristics;
using UX.Components.Table;

public partial class ProductsTableView : TableView
{
	public event Action<TableRow>? RowEntered;

	public ushort CurrentPage { get; protected set; }
	public ushort PreviousPage { get; protected set; }
	[Export] public ushort RowsPerPage { get; protected set; } = 30;
	public List<IProductBase<IBaseCharacteristicsParser>> BufferedProducts = new();
	public bool DoShowAdded
	{
		get
		{
			return _checkboxDoShowAdded.ButtonPressed;
		}
		set
		{
			_checkboxDoShowAdded.ButtonPressed = value;
		}
	}

	[Export] private Label _labelSelectedCount = null!;
	[Export] private CheckBox _checkboxDoShowAdded = null!;
	[Export] private HBoxContainer _hBoxButtonPages = null!;


	public override void _Ready()
	{
		_checkboxDoShowAdded.Pressed += OnCheckboxDoShowAddedPressed;

		Rows.OnRemoved += OnRowRemoved;
		Rows.Selected.OnChanged += OnSelectedRowsChanged;
	}

	public override void _UnhandledKeyInput(InputEvent @event)
	{
		base._UnhandledKeyInput(@event);

		// Выбираем все продукты, что не добавлены
		if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.F2)
		{
			Rows.ForEach(xRow =>
			{
				if (xRow is ProductTableRow row && !row.Product.SaveManager.IsAdded())
				{
					Rows.Selected.Add(row);
				}
			});
		}
	}

	public static List<ProductTableRow> GetRows(IEnumerable<IProductBase<IBaseCharacteristicsParser>> products)
	{
		var rows = new List<ProductTableRow>();

		foreach (var product in products)
		{
			if (product.Title == null || product.URL == null)
			{
				Log.Error($"Product ({product.SaveManager.DirectoryPath}) title or URL is null");
				continue;
			}

			rows.Add(new ProductTableRow(product));
		}

		return rows;
	}

	protected void PopulateInternal(IEnumerable<ProductTableRow> rows)
	{
		foreach (ProductTableRow row in rows)
		{
			if (DoShowAdded != row.Product.SaveManager.IsAdded())
			{
				continue;
			}

			void onRowFocusEntered()
			{
				OnRowFocusEntered(row);
			}

			void onRowFocusExited()
			{
				OnRowFocusExited(row);
			}

			Button removeButton = new() { Text = "✖" };
			removeButton.Pressed += () =>
			{
				Rows.Remove(row);
				row.CheckBox.FocusEntered -= onRowFocusEntered;
				row.CheckBox.FocusExited -= onRowFocusExited;
				BufferedProducts!.Remove(row.Product);
			};

			row.CheckBox.FocusEntered += onRowFocusEntered;
			row.CheckBox.FocusExited += onRowFocusExited;
			row.AddChild(removeButton);

			AddRowInternal(row);
		}
	}

	public void Clear()
	{
		if (Rows.Count > 0)
		{
			Rows.Clear();
			Container.QueueFreeChildren();
			_hBoxButtonPages.QueueFreeChildren();
		}
	}

	public void GenerateButtonPages()
	{
		for (ushort i = 0; i < CalculateTotalPages(); i++)
		{
			ushort page = i; // Для функции так как ссылка на переменную
			Button button = new() { Name = $"Page_{page + 1}", Text = $"{page + 1}" };
			button.Pressed += () => GotoPage(page);
			_hBoxButtonPages.AddChild(button);
		}
	}

	public void GotoPage(ushort page)
	{
		Log.Print($"Goto: {page} (SP/P: {Rows.Selected.Count}/{Rows.Count})");

		PreviousPage = CurrentPage;
		CurrentPage = page;
		UpdatePreviousPage();
		UpdateCurrentPage();
	}

	public void UpdateCurrentPage()
	{
		ushort startIndex = GetIndexByPage(CurrentPage);
		ushort rowsPerPage = RowsPerPage;
		if (Rows.Count < startIndex)
		{
			startIndex = 0;
			rowsPerPage = (ushort)(RowsPerPage - startIndex);
		}

		var currentButton = _hBoxButtonPages.GetNodeOrNull<Button>($"Page_{CurrentPage + 1}");
		if (currentButton != null)
		{
			currentButton.Disabled = true;
		}

		for (ushort i = startIndex; i < startIndex + rowsPerPage; i++)
		{
			if (Rows.Count <= i) break;

			ProductTableRow row = (ProductTableRow)Rows[i];

			row.TitleButton.Pressed += () => RowEntered?.Invoke(row);
			row.Visible = true;
			row.CheckBox.Toggled += toggle => OnRowToggle(row, toggle);

			Container.AddChild(row);
		}
	}

	public void UpdateRows(in IEnumerable<IProductBase<IBaseCharacteristicsParser>>? products = null)
	{
		Clear();
		List<ProductTableRow> rows = GetRows(products ?? BufferedProducts);
		PopulateInternal(rows);
	}


	public ushort GetIndexByPage(ushort page)
	{
		return (ushort)(page * RowsPerPage);
	}

	protected void UpdatePreviousPage()
	{
		ushort startIndex = GetIndexByPage(PreviousPage);
		ushort rowsPerPage = RowsPerPage;
		if (Rows.Count < startIndex)
		{
			startIndex = 0;
			rowsPerPage = (ushort)(RowsPerPage - startIndex);
		}

		var previousButton = _hBoxButtonPages.GetNodeOrNull<Button>($"Page_{PreviousPage + 1}");
		if (previousButton != null)
		{
			previousButton.Disabled = false;
		}

		for (ushort i = startIndex; i < rowsPerPage + startIndex; i++)
		{
			if (Rows.Count <= i) break;

			TableRow product = Rows[i];
			if (Container.IsAncestorOf(product))
			{
				product.Visible = false;
				Container.RemoveChild(product);
			}
		}
	}

	protected ushort CalculateTotalPages()
	{
		if (Rows.Count <= 0) return 0;

		float total = (float)Math.Ceiling((double)Rows.Count / RowsPerPage);
		return (ushort)(total == 0 ? 1 : total);
	}

	private void OnCheckboxDoShowAddedPressed()
	{
		GD.Print("Click");
		UpdateRows();
		GenerateButtonPages();
		PreviousPage = 0;
		CurrentPage = 0;
	}

	private void OnRowRemoved(TableRow row)
	{
		if (row is not ProductTableRow productRow) return;

		Log.Print($"Remove: {row.Title}");
		productRow.Product.SaveManager.Remove();
		productRow.QueueFree();
	}

	private void OnSelectedRowsChanged()
	{
		_labelSelectedCount.Text = $"Выбрано: {Rows.Selected.Count}";
	}
}
