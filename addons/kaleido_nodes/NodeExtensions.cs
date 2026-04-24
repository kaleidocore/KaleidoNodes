using System;
using System.Linq.Expressions;
using System.Reflection;
using Godot;

namespace KaleidoNodes;

public static class NodeExtensions
{
	public static Vector2 GlobalCenter(this Control control)
		=> control.GlobalPosition + control.GetGlobalTransform().BasisXform(control.Size * 0.5f);

	public static Vector2 GlobalPivot(this Control control)
		=> control.GlobalPosition + control.GetGlobalTransform().BasisXform(control.PivotOffset + (control.PivotOffsetRatio * control.Size));

	public static Tween TweenTo(this Control control, Node2D target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
		=> control.TweenTo(target.GlobalPosition, duration, transition, ease, delay);

	public static Tween TweenTo(this Control control, Control target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
		=> control.TweenTo(target.GlobalPivot(), duration, transition, ease, delay);

	public static Tween TweenTo(this Control control, Vector2 target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
	{
		var offset = control.GlobalPosition - control.GlobalPivot();
		var tween = control.TweenProperty(c => c.GlobalPosition, target + offset, duration, transition, ease, delay);
		return tween;
	}

	public static Tween TweenTo(this Node2D node, Node2D target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
		=> node.TweenTo(target.GlobalPosition, duration, transition, ease, delay);

	public static Tween TweenTo(this Node2D node, Control target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
		=> node.TweenTo(target.GlobalPivot(), duration, transition, ease, delay);

	public static Tween TweenTo(this Node2D node, Vector2 target, float duration = 1f, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f)
	{
		var tween = node.TweenProperty(n => n.GlobalPosition, target, duration, transition, ease, delay);
		return tween;
	}

	public static void MoveTo(this Control control, Node2D target)
	{
		var offset = control.GlobalPosition - control.GlobalPivot();
		control.GlobalPosition = target.GlobalPosition + offset;
	}

	public static void MoveTo(this Control control, Control target)
	{
		var offset = control.GlobalPosition - control.GlobalPivot();
		control.GlobalPosition = target.GlobalPivot() + offset;
	}

	public static void MoveTo(this Node2D node, Node2D target)
	{
		node.GlobalPosition = target.GlobalPosition;
	}

	public static void MoveTo(this Node2D node, Control target)
	{
		node.GlobalPosition = target.GlobalPivot();
	}

	public static Tween TweenProperty<TNode, T>(this TNode node, Expression<Func<TNode, T>> property, T target, float duration, Tween.TransitionType transition = Tween.TransitionType.Cubic, Tween.EaseType ease = Tween.EaseType.InOut, float delay = 0f) where TNode : Node
	{
		var getter = property.Compile();
		var body = property.Body is UnaryExpression unary ? unary.Operand : property.Body;
		var prop = (PropertyInfo)((MemberExpression)body).Member;
		var name = $"tween_{prop.Name}";

		if (node.HasMeta(name))
		{
			var oldTween = node.GetMeta(name).As<Tween>();
			oldTween.Kill();
		}

		var tween = node.CreateTween();
		var mt = tween.TweenMethod(Callable.From<Variant>(v => prop.SetValue(node, Convert.ChangeType(v.Obj, typeof(T)))), Variant.From(getter(node)), Variant.From(target), duration);

		// Godot architecture is absolute shit
		mt.SetDelay(delay);
		mt.SetTrans(transition);
		mt.SetEase(ease);

		node.SetMeta(name, tween);
		tween.Finished += () => node.RemoveMeta(name);

		return tween;
	}

	public static Tween Delay(this Node node, float delay, Action action)
	{
		var tween = node.CreateTween();
		tween.TweenInterval(delay);
		tween.Finished += action;
		return tween;
	}
}