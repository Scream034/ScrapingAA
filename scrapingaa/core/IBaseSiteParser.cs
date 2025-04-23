namespace Core;

using System.Collections.Generic;
using System.Threading.Tasks;

using Components.Characteristics;
using Components;

public interface IBaseSiteParser
{
	public string? URL { get; protected set; }
	public string? Domain { get; protected set; }

	protected ChromiumBrowser Browser { get; set; }

	public abstract Task Main(string[] args);
	public abstract Task<IProductBase<IBaseCharacteristicsParser>[]> GetIOProductsAsync();
	public abstract Task<IProductBase<IBaseCharacteristicsParser>[]> GetProductsInPageAsync();
}