namespace Core.Extensions;

using System.Collections.Generic;
using System.Linq;
using Core.Components.Characteristics;
using Core.Sites;

public static class ProductBaseExtensions
{
	public static IProductBase<IBaseCharacteristicsParser> ToAbstract<TCharacteristicsParser>(this IProductBase<TCharacteristicsParser> productBase) where TCharacteristicsParser : BaseCharacteristicsParser
	{
		ProductBase<IBaseCharacteristicsParser> product = new ProductBase<IBaseCharacteristicsParser>(productBase.CharacteristicsParser);
		productBase.CopyTo(product);
		return product;
	}

	public static IEnumerable<IProductBase<IBaseCharacteristicsParser>> ToAbstract<TCharacteristicsParser>(this IEnumerable<IProductBase<TCharacteristicsParser>> productBaseList) where TCharacteristicsParser : BaseCharacteristicsParser
	{
		return productBaseList.Select(productBase => productBase.ToAbstract());
	}
}