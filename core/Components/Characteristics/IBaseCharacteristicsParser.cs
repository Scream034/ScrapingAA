namespace Core.Components.Characteristics;

public interface IBaseCharacteristicsParser
{
	public WebCharacteristics Characteristics { get; set; }

	public string? Model { get; set; }

	public string? Price { get; set; }

	/// <summary>
	/// Двигатель
	/// </summary>
	public EngineType Engine { get; set; }

	/// <summary>
	/// КПП
	/// </summary>
	public TransmissionType Transmission { get; set; }

	/// <summary>
	/// Мощность в л.с.
	/// </summary>
	public string? Power { get; set; }

	/// <summary>
	/// Объём двигателя
	/// </summary>
	public string? EngineVolume { get; set; }

	/// <summary>
	/// Привод
	/// </summary>
	public DriveType Drive { get; set; }

	/// <summary>
	/// Кол-во мест
	/// </summary>
	public string? Seats { get; set; }

	public string ToIOString();

	public bool FromIOString(in string ioString);
}

public enum EngineType
{
	/// <summary>
	/// Неизвестный
	/// </summary>
	None,

	/// <summary>
	/// Бензин
	/// </summary>
	Benzine,

	/// <summary>
	/// Газ
	/// </summary>
	Gas,

	/// <summary>
	/// Гибрид
	/// </summary>
	Hybrid,

	/// <summary>
	/// Дизель
	/// </summary>
	Diesel,

	/// <summary>
	/// Электрический
	/// </summary>
	Electric
}

public enum TransmissionType
{
	/// <summary>
	/// Неизвестный
	/// </summary>
	None,

	/// <summary>
	/// Автоматическая
	/// </summary>
	Automatic,

	/// <summary>
	/// Вариатор
	/// </summary>
	Varicor,

	/// <summary>
	/// Механическая
	/// </summary>
	Manual,

	/// <summary>
	/// Робот
	/// </summary>
	Robot,
}

public enum DriveType
{
	/// <summary>
	/// Неизвестный
	/// </summary>
	None,

	/// <summary>
	/// Задний
	/// </summary>
	Back,

	/// <summary>
	/// Передний
	/// </summary>
	Front,

	/// <summary>
	/// Полный
	/// </summary>
	Full
}