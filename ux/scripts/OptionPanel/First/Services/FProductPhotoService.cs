using System;
using System.Threading.Tasks;
using Godot;

using Core.Log;

using UX.Components.Table;
using UX.Option.First.Services.Photo;

namespace UX.Option.First.Services;

public class FProductPhotoService : IFService<FProductTitlePhotoInitializeArguments, FServiceNoneType>
{
	public static string Name => "Показ изображений";

	private readonly PhotoCarouselManager _photoCarouselManager = new();
	private ProductsTableView _tableView = null!;

	public void Init(FProductTitlePhotoInitializeArguments args)
	{
		_tableView = args.TableView ?? throw new ArgumentNullException(nameof(args.TableView));
	}

	public Task Start(FServiceNoneType args = default)
	{
		if (_tableView == null)
		{
			Log.Print("ProductsTableView is not initialized.");
			return Task.CompletedTask;
		}

		_photoCarouselManager.EnsureWindowInitialized();

		_photoCarouselManager.ShowWindow();
		SubscribeToEvents();
		return Task.CompletedTask;
	}

	private void SubscribeToEvents()
	{
		_tableView.RowEntered += OnRowEntered;
		_photoCarouselManager.WindowClosed += UnsubscribeFromEvents;
	}

	private void UnsubscribeFromEvents()
	{
		_tableView.RowEntered -= OnRowEntered;
		_photoCarouselManager.WindowClosed -= UnsubscribeFromEvents;
	}

	private void OnRowEntered(TableRow row)
	{
		if (row is not ProductTableRow productRow)
		{
			Log.Print("Row is not a ProductTableRow.");
			return;
		}

		_photoCarouselManager.LoadImages(productRow);
	}
}

public class FProductTitlePhotoInitializeArguments : IFInitializeArguments
{
	public ProductsTableView TableView { get; }

	public FProductTitlePhotoInitializeArguments(ProductsTableView tableView)
	{
		TableView = tableView ?? throw new ArgumentNullException(nameof(tableView));
	}
}