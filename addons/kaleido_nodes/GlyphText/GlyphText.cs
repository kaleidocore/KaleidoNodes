using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
namespace Spellbound;

public enum HorizontalOrigin
{
	Left,
	Center,
	Right
}

public enum VerticalOrigin
{
	Top,
	Center,
	Baseline,
	Bottom
}


[Tool]
public partial class GlyphText : Node2D
{
	string _text = "A";
	Font? _font;
	int _fontSize = 100;
	float _padding = 0f;
	TextLine? _textLine;
	Rect2 _bounds;
	Color _color = Colors.White;
	HorizontalOrigin _originX = HorizontalOrigin.Center;
	VerticalOrigin _originY = VerticalOrigin.Baseline;
	Vector2 _offset;
	readonly HashSet<int> _hoveredGlyphs = [];

	ColorRect? _boundsGizmo;
	ColorRect BoundsGizmo => _boundsGizmo ?? throw new InvalidOperationException("BoundsGizmo node not found");

	Area2D? _hitbox;
	Area2D Hitbox => _hitbox ?? throw new InvalidOperationException("Hitbox node not found");

	bool CanRebuild => IsInsideTree();

	public Rect2 Bounds => _bounds;

	[Export]
	public Font? Font
	{
		get => _font;
		set
		{
			_font = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Export]
	public int FontSize
	{
		get => _fontSize;
		set
		{
			_fontSize = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Export]
	public Color Color
	{
		get => _color;
		set
		{
			_color = value;
			QueueRedraw();
		}
	}

	[Export]
	public float Padding
	{
		get => _padding;
		set
		{
			_padding = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Export]
	public string Text
	{
		get => _text;
		set
		{
			_text = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Export]
	public HorizontalOrigin HorizontalOrigin
	{
		get => _originX;
		set
		{
			_originX = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Export]
	public VerticalOrigin VerticalOrigin
	{
		get => _originY;
		set
		{
			_originY = value;

			if (CanRebuild)
				Rebuild();
		}
	}

	[Signal] public delegate void MousePressedEventHandler(int glyphIndex);
	[Signal] public delegate void MouseReleasedEventHandler(int glyphIndex);
	[Signal] public delegate void MouseEnteredEventHandler(int glyphIndex);
	[Signal] public delegate void MouseExitedEventHandler(int glyphIndex);
	[Signal] public delegate void AreaEnteredEventHandler(Area2D area);
	[Signal] public delegate void AreaExitedEventHandler(Area2D area);

	public GlyphText()
	{
		EditorDescription = "A node that renders text as individual glyphs with separate collision shapes for each glyph, allowing for precise mouse interactions.";
	}

	public override void _EnterTree()
	{
		_hitbox = new Area2D
		{
			Name = "Hitbox",
			ZIndex = 1,
			ShowBehindParent = true,
			InputPickable = true,
			Monitoring = true,
			Monitorable = true,
			CollisionLayer = 1,
			CollisionMask = 1,
		};

		AddChild(_hitbox);
		_hitbox.ShowBehindParent = true;

		base._EnterTree();
	}

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			Hitbox.InputEvent += (_, @event, shapeIdx) =>
			{
				if (@event is InputEventMouseButton mb)
				{
					if (mb.Pressed)
						EmitSignal(SignalName.MousePressed, (int)shapeIdx);
					else
						EmitSignal(SignalName.MouseReleased, (int)shapeIdx);
				}
			};

			Hitbox.MouseShapeEntered += shapeIdx =>
			{
				_hoveredGlyphs.Add((int)shapeIdx);

				if (_hoveredGlyphs.Count == 1) // Only emit if this is the first hovered glyph (handles overlapping glyphs)
					EmitSignal(SignalName.MouseEntered, (int)shapeIdx);
			};

			Hitbox.MouseShapeExited += shapeIdx =>
			{
				_hoveredGlyphs.Remove((int)shapeIdx);

				if (_hoveredGlyphs.Count == 0) // Only emit if there are no more hovered glyphs (handles overlapping glyphs)
					EmitSignal(SignalName.MouseExited, (int)shapeIdx);
			};

			Hitbox.AreaEntered += area => EmitSignal(SignalName.AreaEntered, area);
			Hitbox.AreaExited += area => EmitSignal(SignalName.AreaExited, area);
		}

		Rebuild();
	}

	public override void _Draw()
	{
		if (Engine.IsEditorHint())
		{
			DrawRect(_bounds, new Color(0f, 0f, 1f, 0.1f), filled: true);

			foreach (var col in Hitbox.GetChildren().OfType<CollisionShape2D>())
			{
				if (col.IsQueuedForDeletion())
					continue;

				if (col.Shape is not RectangleShape2D rect)
					continue;

				var r = new Rect2(Hitbox.Position + col.Position - (rect.Size * 0.5f), rect.Size);
				DrawRect(r, new Color(.8f, 0f, 0f, 0.3f), filled: true);
			}
		}

		_textLine?.Draw(GetCanvasItem(), _offset, Color);
	}

	void Rebuild()
	{
		foreach (var child in Hitbox.GetChildren())
			child.QueueFree();

		var font = _font ?? ThemeDB.FallbackFont;
		var sizeKey = new Vector2I(_fontSize, 0);
		var ts = TextServerManager.GetPrimaryInterface();

		_textLine = new TextLine();
		_textLine.AddString(_text, font, _fontSize);

		var shaped = ts.CreateShapedText();
		ts.ShapedTextAddString(shaped, _text, font.GetRids(), _fontSize);
		ts.ShapedTextShape(shaped);

		var ascent = (float)ts.ShapedTextGetAscent(shaped);
		float cursorX = 0f;

		foreach (var glyph in ts.ShapedTextGetGlyphs(shaped))
		{
			// Not all keys are guaranteed present — read everything defensively
			glyph.TryGetValue("index", out var indexVar);
			glyph.TryGetValue("advance", out var advanceVar);
			glyph.TryGetValue("offset", out var offsetVar);
			glyph.TryGetValue("font_rid", out var fontRidVar);

			int index = indexVar.VariantType != Variant.Type.Nil ? indexVar.AsInt32() : 0;
			float advance = advanceVar.VariantType != Variant.Type.Nil ? advanceVar.AsSingle() : 0f;
			Vector2 offset = offsetVar.VariantType != Variant.Type.Nil ? offsetVar.AsVector2() : Vector2.Zero;
			Rid fontRid = fontRidVar.VariantType != Variant.Type.Nil ? fontRidVar.AsRid() : default;

			if (index != 0 && fontRid.IsValid)
			{
				var glyphSize = ts.FontGetGlyphSize(fontRid, sizeKey, index);
				var glyphBearing = ts.FontGetGlyphOffset(fontRid, sizeKey, index);

				float left = cursorX + offset.X + glyphBearing.X - _padding;
				float top = ascent + offset.Y + glyphBearing.Y - _padding;
				var size = glyphSize + (Vector2.One * _padding * 2f);

				Hitbox.AddChild(new CollisionShape2D
				{
					Shape = new RectangleShape2D { Size = size },
					Position = new Vector2(left + (size.X * 0.5f), top + (size.Y * 0.5f)),
					Visible = false,
				});
			}

			cursorX += advance;
		}

		float totalWidth = cursorX;
		float fullHeight = (float)ts.ShapedTextGetAscent(shaped) + (float)ts.ShapedTextGetDescent(shaped);
		var fullSize = new Vector2(totalWidth, fullHeight);

		var offsetX = _originX switch
		{
			HorizontalOrigin.Left => 0f,
			HorizontalOrigin.Center => -totalWidth * 0.5f,
			HorizontalOrigin.Right => -totalWidth,
			_ => 0f
		};

		var offsetY = _originY switch
		{
			VerticalOrigin.Top => 0f,
			VerticalOrigin.Center => -fullHeight * 0.5f,
			VerticalOrigin.Bottom => -fullHeight,
			VerticalOrigin.Baseline => -ascent,
			_ => -ascent
		};

		_offset = new Vector2(offsetX, offsetY);

		// origin is baseline (y=0), ascent goes up (negative y), descent goes down
		_bounds = new Rect2(_offset, fullSize);

		Hitbox.Position = new Vector2(offsetX, offsetY);

		ts.FreeRid(shaped);
		QueueRedraw();
	}
}