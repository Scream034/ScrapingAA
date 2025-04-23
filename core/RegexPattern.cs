namespace Core;

public static class RegexPattern
{
	public const string ControlChars = @"[\x00-\x1F\x7F-\x9F]";
	public const string HTMLEntities = @"&[a-zA-Z0-9#]+;";
	public const string HTMLTags = @"<[^>]+>";
	public const string HTMLStringTags = @"<[^>]*(?:br|p|div)[^>]*>";
	public const string ArithmeticOperations = @"[\+\-\*/]";
	public const string FinalPunctuationMarks = @"(\s*[.!?]+\s*|\s*[?]+|\s*[!]+)+";
	public const string InPerhaps = @"\((?:[^\(\)]+|\(.*?\))*?\)";
	public const string Words = @"\b[А-Яа-яЁёA-Za-z0-9'-]+\b";
	public const string WordsInMyStem = @"\b[А-Яа-яЁёA-Za-z0-9'-]+\b";
	public const string EnderSymbols = @"(:|;|\.|,|—|–)$";
	public const string SubEnderSymbols = @"(:|;|\.|,)$";
}