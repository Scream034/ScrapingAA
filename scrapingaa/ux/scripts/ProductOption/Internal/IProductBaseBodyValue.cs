namespace UX.ProductOption.Internal;

using Godot;

public interface IProductBaseBodyValue
{
	public abstract void Initialize(ProductOptionPanel parent);
	public abstract void Destroy();
}