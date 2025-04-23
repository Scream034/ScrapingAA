namespace Core.Sites;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Core;
using Core.Components.Characteristics;
using Core.Sites.Istk;
using Core.Sites.Solyarka;
using Core.Sites.SpecAvto;
using Core.Components;

using Godot;

public class BaseSiteParser : IBaseSiteParser
{
	public string? URL { get; set; }

	private string? _domain;
	public string? Domain
	{
		get
		{
			if (string.IsNullOrWhiteSpace(_domain) && !string.IsNullOrWhiteSpace(URL))
			{
				Uri uri = new(URL);
				_domain = uri.Scheme + "://" + uri.Host;
			}

			return _domain;
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public ChromiumBrowser Browser { get; set; } = null!;


	public static IBaseSiteParser? Create(string siteName)
	{
		switch (siteName)
		{
			case IstkParser.Name:
				return new IstkParser(Global.Instance.Spider);
			case SolyarkaParser.Name:
				return new SolyarkaParser(Global.Instance.Spider);
			case SpecAvtoParser.Name:
				return new SpecAvtoParser(Global.Instance.Spider);
			case LeasingParser.Name:
				return new LeasingParser(Global.Instance.Spider);

			default:
				return new BaseSiteParser();
		}
	}


	public virtual async Task Main(string[] args)
	{
		string? url = args[0];
		if (string.IsNullOrEmpty(url))
		{
			OS.Alert("Первым аргументом должна быть ссылка, а не пустая строка", "Ошибка");
			return;
		}
		else if (!url.StartsWith("http://") && !url.StartsWith("https://"))
		{
			OS.Alert("Первым аргументом должна быть ссылка", "Ошибка");
			return;
		}

		URL = url;

		Global.Instance.Products.AddRange(await GetProductsInPageAsync());
	}

	public virtual Task<IProductBase<IBaseCharacteristicsParser>[]> GetProductsInPageAsync() => throw new System.NotImplementedException();

	public virtual async Task<IProductBase<IBaseCharacteristicsParser>[]> GetIOProductsAsync()
	{
		List<Task<IProductBase<IBaseCharacteristicsParser>>> products = new();

		foreach (DirectoryInfo directory in new DirectoryInfo(Constants.Path.Folder.Temp).GetDirectories().OrderByDescending(static d => d.LastWriteTime))
		{
			products.Add(GetIOProductAsync(directory.Name));
		}

		await Task.WhenAll(products);

		return products.Select(t => t.Result).ToArray();
	}

	protected virtual async Task<IProductBase<IBaseCharacteristicsParser>> GetIOProductAsync(string directoryName)
	{
		IProductBase<IBaseCharacteristicsParser> product = new ProductBase<IBaseCharacteristicsParser>(new BaseCharacteristicsParser());
		await product.SaveManager.LoadAsync(directoryName);
		return product;
	}

	protected virtual Task<IEnumerable<IProductBase<IBaseCharacteristicsParser>>> GetProductsFromArgsAsync(string[] args)
	{
		throw new NotImplementedException();
	}
}
