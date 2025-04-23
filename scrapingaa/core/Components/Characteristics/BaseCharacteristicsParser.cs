namespace Core.Components.Characteristics;

using System;
using System.Collections.Generic;
using System.Linq;

public class BaseCharacteristicsParser : IBaseCharacteristicsParser
{
	public static readonly Dictionary<EngineType, string> DisplayEngineTypes = new()
	{
		{EngineType.None, "Нет"},
		{EngineType.Benzine, "Бензин"},
		{EngineType.Gas, "Газ"},
		{EngineType.Hybrid, "Гибрид"},
		{EngineType.Diesel, "Дизель"},
		{EngineType.Electric, "Электрический"},
	};

	public static readonly Dictionary<TransmissionType, string> DisplayTransmissionTypes = new()
	{
		{TransmissionType.None, "Нет"},
		{TransmissionType.Automatic, "Автоматическая"},
		{TransmissionType.Varicor, "Вариатор"},
		{TransmissionType.Manual, "Механическая"},
		{TransmissionType.Robot, "Робот"},
	};

	public static readonly Dictionary<DriveType, string> DisplayDriveTypes = new()
	{
		{DriveType.None, "Нет"},
		{DriveType.Back, "Задний"},
		{DriveType.Front, "Передний"},
		{DriveType.Full, "Полный"},
	};

	public const string SeparatorIO = "_|\uE000\n";

	public string? Model { get; set; }
	public string? Price { get; set; }
	public EngineType Engine { get; set; } = EngineType.None;
	public TransmissionType Transmission { get; set; } = TransmissionType.None;
	public string? Power { get; set; }
	public string? EngineVolume { get; set; }
	public DriveType Drive { get; set; } = DriveType.None;
	public string? Seats { get; set; }
	public WebCharacteristics Characteristics { get; set; } = new();

	public string ToIOString()
	{
		return $"{Model}{SeparatorIO}{Price}{SeparatorIO}{Engine}{SeparatorIO}{Transmission}{SeparatorIO}{Power}{SeparatorIO}{EngineVolume}{SeparatorIO}{Drive}{SeparatorIO}{Seats}{SeparatorIO}{Characteristics.Storage.ToIOString()}";
	}

	public bool FromIOString(in string ioString)
	{
		var ioStrings = ioString.Split(SeparatorIO);
		if (ioStrings.Length != 9)
		{
			return false;
		}

		Model = ioStrings[0];
		Price = ioStrings[1];
		Engine = Enum.Parse<EngineType>(ioStrings[2]);
		Transmission = Enum.Parse<TransmissionType>(ioStrings[3]);
		Power = ioStrings[4];
		EngineVolume = ioStrings[5];
		Drive = Enum.Parse<DriveType>(ioStrings[6]);
		Seats = ioStrings[7];
		Characteristics.Storage.FromIOString(ioStrings[8]);
		return true;
	}

	public static string ToDisplayString(in EngineType engineType)
	{
		return DisplayEngineTypes[engineType];
	}

	public static string ToDisplayString(in TransmissionType transmissionType)
	{
		return DisplayTransmissionTypes[transmissionType];
	}

	public static string ToDisplayString(in DriveType driveType)
	{
		return DisplayDriveTypes[driveType];
	}
}