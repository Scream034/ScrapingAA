using System.Threading.Tasks;

namespace UX.Option.First.Services;

// Базовый интерфейс без параметров
public interface IFService
{
	public virtual void Init(object? args) => args = null;
	public virtual Task Start(object? args) => Task.CompletedTask;
}

// Обобщенный интерфейс
public interface IFService<TInitializeArgs, TStartArgs> : IFService
		where TInitializeArgs : IFInitializeArguments
		where TStartArgs : IFStartArguments
{
	public void Init(TInitializeArgs args);
	public Task Start(TStartArgs args);
}
public interface IFInitializeArguments { }
public interface IFStartArguments { }