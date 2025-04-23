namespace Core.Components.Smart.AEXU;

using Log;

using System;
using System.Linq;

/// <summary>
/// Algorithm for Extraction of Units (Single)
/// </summary>
public sealed class AEXUnit
{
	public enum Type
	{
		Unknown,        // Неизвестный тип
		Length,         // Длина, расстояние
		Area,           // Площадь
		Volume,         // Объем
		Weight,         // Вес, масса
		Time,           // Время
		Speed,          // Скорость
		Power,          // Мощность
		Energy,         // Энергия
		Pressure,       // Давление
		Temperature,    // Температура
		Frequency,      // Частота
		Density,        // Плотность
		Force,          // Сила
		Torque,         // Крутящий момент
		Voltage,        // Напряжение
		Current,        // Ток
		Resistance,     // Сопротивление
		Capacitance,    // Емкость
		Inductance,     // Индуктивность
		Luminosity,     // Яркость, световой поток
		Angle,          // Угол
		Ratio          // Отношение, коэффициент (%, ppm и т.д.)
	}

	public readonly string Raw;
	public bool IsSingle = true;
	public readonly string Name;
	public readonly Type Result = Type.Unknown;

	public AEXUnit(string name, Type type, string raw = "")
	{
		Name = name;
		Result = type;
		Raw = raw;
	}

	public AEXUnit Clone()
	{
		return new(Name, Result) { IsSingle = IsSingle };
	}

	public AEXUnit Clone(string raw)
	{
		return new(Name, Result, raw) { IsSingle = IsSingle };
	}

	public static AEXUnit? Parse(string? line)
	{
		if (string.IsNullOrWhiteSpace(line)) return null;

		string[] parts = line.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 2) return null;

		string name = string.Join(' ', parts[0..(parts.Length - 1)]);
		string unit = parts.Last();

		if (Enum.TryParse(unit, out Type type))
		{
			return new(name, type);
		}
		else
		{
			Log.Error($"Unknown unit type ({name}): {unit}");
		}

		return null;
	}

	public override string ToString()
	{
		return $"{Name} ({Raw})";
	}
}