namespace Core;

using System.Collections.Generic;
using System.Threading.Tasks;

using Core.Components.Characteristics;
using Core.Internal;
using Core.Sites;
using Microsoft.Playwright;

public interface IProductBaseIdentifier
{
	public string? Title { get; protected set; }
	public string? URL { get; set; }
}

public interface IProductBaseCompleted : IProductBaseIdentifier
{
	public string? CompletedURL { get; set; }
}

public interface IProductBase<TCharacteristicsParser> : IProductBaseCompleted where TCharacteristicsParser : IBaseCharacteristicsParser
{
	public TCharacteristicsParser CharacteristicsParser { get; set; }
	public ProductBaseSaveManager<TCharacteristicsParser> SaveManager { get; set; }
	public bool IsBroken { get; protected set; }

	public void CopyTo<TTCharacteristicsParser>(ProductBase<TTCharacteristicsParser> product) where TTCharacteristicsParser : IBaseCharacteristicsParser;
}

public interface IBrowserProduct<TCharacteristicsParser, TBaseSiteParser, TProductType> : IProductBase<TCharacteristicsParser>
		where TCharacteristicsParser : IBaseCharacteristicsParser
		where TBaseSiteParser : IBaseSiteParser
		where TProductType : IBrowserProduct<TCharacteristicsParser, TBaseSiteParser, TProductType>
{
	public IElementHandle? Element { get; protected set; }

	public List<TProductType>? SubProducts { get; set; }

	public Task ParseAsync(TBaseSiteParser parser);
}