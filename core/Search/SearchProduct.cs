using System.Threading.Tasks;
using Microsoft.Playwright;

namespace Core.Search;

public partial class SearchProduct : IProductBaseIdentifier
{
	public readonly IElementHandle Element;
	public string? Title { get; set; }
	public string? URL { get; set; }

	public SearchProduct(IElementHandle element)
	{
		Element = element;
	}

	public async Task InitAsync(string? domain)
	{
		Title = await Element.InnerTextAsync();
		if (!string.IsNullOrEmpty(domain))
		{
			URL = domain + await Element.GetAttributeAsync("href");
		}
	}
}