using Godot;
using System;

namespace Core.Components;

public static class ControlUtils
{
	public static class Meta
	{
		public const string Tooltip = "tooltip";
	}

	/// <summary>
	/// Shows a tooltip for the target control
	/// </summary>
	/// <param name="target">The control to show the tooltip for</param>
	/// <param name="text">The tooltip text to display</param>
	/// <param name="duration">Duration in seconds before auto-hiding (0 for infinite)</param>
	/// <param name="replace">Whether to replace existing tooltip if present</param>
	/// <returns>The created tooltip control</returns>
	public static Control ShowTooltip(this Control target, string text, double duration = 5, bool replace = true)
	{
		// Check for existing tooltip
		var existingTooltip = target.GetTooltipNode();

		// Handle replacement logic
		if (existingTooltip != null)
		{
			if (replace)
			{
				// Replace existing tooltip
				HideTooltipInternal(target, existingTooltip, 0.05f);
			}
			else
			{
				// If not replacing, update existing tooltip's text and duration
				if (Node.IsInstanceValid(existingTooltip))
				{
					var existingLabel = existingTooltip.GetChildOrNull<Label>(0);
					if (existingLabel != null)
					{
						existingLabel.Text = text;
					}

					// Reset existing animations
					var existingTween = existingTooltip.GetTree().CreateTween();
					existingTooltip.Modulate = Colors.White; // Reset visibility

					// Update duration if needed
					if (duration > 0)
					{
						existingTooltip.GetTree().CreateTimer(duration, false, true).Timeout += () =>
						{
							HideTooltipInternal(target, existingTooltip);
						};
					}

					return existingTooltip;
				}
			}
		}

		// Create new tooltip
		Control tooltip = new()
		{
			Name = "Tooltip",
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Modulate = new Color(1, 1, 1, 0) // Start invisible for fade-in
		};
		tooltip.AnchorLeft = 0.2f;
		tooltip.AnchorRight = 0.8f;
		tooltip.AnchorTop = 0.05f;
		tooltip.AnchorBottom = 0.25f;

		Panel background = new()
		{
			Name = "Background",
			ProcessMode = Node.ProcessModeEnum.Disabled,
		};
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		Label label = new()
		{
			Text = text,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ProcessMode = Node.ProcessModeEnum.Disabled
		};
		label.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		tooltip.AddChild(background);
		tooltip.AddChild(label);
		target.AddChild(tooltip);

		// Fade-in animation
		var tween = tooltip.CreateTween();
		tween.TweenProperty(tooltip, Control.PropertyName.Modulate.ToString(), Colors.White, 0.3f);

		// Set up auto-hide if duration is specified
		if (duration > 0)
		{
			target.GetTree().CreateTimer(duration).Timeout += () =>
			{
				HideTooltipInternal(target, tooltip);
			};
		}

		// Store reference in meta
		target.SetMeta(Meta.Tooltip, tooltip);

		return tooltip;
	}

	/// <summary>
	/// Gets the current tooltip node if it exists
	/// </summary>
	/// <param name="target">The control to check for a tooltip</param>
	/// <returns>The tooltip control or null if none exists</returns>
	public static Control? GetTooltipNode(this Control target)
	{
		return target.HasMeta(Meta.Tooltip) ? target.GetMeta(Meta.Tooltip).As<Control?>() : null;
	}

	/// <summary>
	/// Hides the current tooltip if it exists
	/// </summary>
	/// <param name="target">The control whose tooltip to hide</param>
	/// <returns>True if a tooltip was hidden, false otherwise</returns>
	public static bool HideTooltip(this Control target)
	{
		var tooltip = target.GetTooltipNode();
		if (tooltip == null) return false;

		return HideTooltipInternal(target, tooltip);
	}

	/// <summary>
	/// Internal method to handle tooltip hiding with animation
	/// </summary>
	private static bool HideTooltipInternal(this Control target, Control tooltip, float duration = 0.3f)
	{
		if (!Node.IsInstanceValid(tooltip))
			return false;

		// Clear meta reference
		if (target.HasMeta(Meta.Tooltip))
		{
			target.RemoveMeta(Meta.Tooltip);
		}

		// Hide with animation
		var tween = target.CreateTween();
		tween.TweenProperty(tooltip, Control.PropertyName.Modulate.ToString(),
				new Color(1, 1, 1, 0), duration);
		tween.Finished += () =>
		{
			if (Node.IsInstanceValid(tooltip))
				tooltip.QueueFree();
		};

		return true;
	}
}