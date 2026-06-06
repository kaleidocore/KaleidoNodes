using System;
using System.Linq;
using Godot;

namespace KaleidoNodes;

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
	Rect2 _fullBounds;
	Rect2 _glyphBounds;
	Color _color = Colors.White;
	HorizontalOrigin _originX = HorizontalOrigin.Center;
	VerticalOrigin _originY = VerticalOrigin.Baseline;
	Vector2 _offset;
	Vector2 _lastScale = Vector2.Zero;

	ColorRect? _boundsGizmo;
	ColorRect BoundsGizmo => _boundsGizmo ?? throw new InvalidOperationException("BoundsGizmo node not found");

	Area2D? _hitbox;
	Area2D Hitbox => _hitbox ?? throw new InvalidOperationException("Hitbox node not found");

	bool CanRebuild => IsNodeReady() && _hitbox != null;

	public Rect2 FullBounds => _fullBounds;
	public Rect2 GlyphBounds => _glyphBounds;
	public bool IsGrabbed { get; private set; }
	public bool IsHovered { get; private set; }

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

	Color _outlineColor = Colors.Black;
	[Export]
	public Color OutlineColor
	{
		get => _outlineColor;
		set
		{
			_outlineColor = value;
			QueueRedraw();
		}
	}

	int _outlineWidth = 1;
	[Export]
	public int OutlineWidth
	{
		get => _outlineWidth;
		set
		{
			_outlineWidth = value;
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

	uint _collisionLayer = 1;
	[Export(hint: PropertyHint.Layers2DPhysics)]
	public uint CollisionLayer
	{
		get => _collisionLayer;
		set
		{
			_collisionLayer = value;

			if (_hitbox != null)
				_hitbox.CollisionLayer = value;
		}
	}

	uint _collisionMask = 1;
	[Export(hint: PropertyHint.Layers2DPhysics)]
	public uint CollisionMask
	{
		get => _collisionMask;
		set
		{
			_collisionMask = value;

			if (_hitbox != null)
				_hitbox.CollisionMask = value;
		}
	}

	bool _mouseEnabled = true;
	[Export]
	public bool MouseEnabled
	{
		get => _mouseEnabled;
		set
		{
			_mouseEnabled = value;

			if (_hitbox != null)
				_hitbox.InputPickable = value;
		}
	}

	bool _hitboxEnabled = true;
	[Export]
	public bool MonitorEnabled
	{
		get => _hitboxEnabled;
		set
		{
			_hitboxEnabled = value;

			if (_hitbox != null)
			{
				_hitbox.Monitoring = value;
				_hitbox.Monitorable = value;
			}
		}
	}

	[Signal] public delegate void GrabbedEventHandler();
	[Signal] public delegate void ReleasedEventHandler();
	[Signal] public delegate void HoveredEventHandler();
	[Signal] public delegate void UnhoveredEventHandler();
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
			InputPickable = MouseEnabled,
			Monitoring = MonitorEnabled,
			Monitorable = MonitorEnabled,
			CollisionLayer = _collisionLayer,
			CollisionMask = _collisionMask,
		};

		AddChild(_hitbox);
		_hitbox.ShowBehindParent = true;

		base._EnterTree();
	}

	public override void _Ready()
	{
		if (_hitbox != null)
		{
			_hitbox.InputPickable = MouseEnabled;
			_hitbox.Monitoring = MonitorEnabled;
			_hitbox.Monitorable = MonitorEnabled;
		}

		if (!Engine.IsEditorHint())
		{
			Hitbox.MouseEntered += () =>
			{
				if (!IsHovered)
				{
					IsHovered = true;
					EmitSignal(SignalName.Hovered);
					//GD.Print($"Mouse entered {Text}");
				}
			};

			Hitbox.MouseExited += () =>
			{
				if (IsHovered)
				{
					IsHovered = false;
					EmitSignal(SignalName.Unhovered);
					//GD.Print($"Mouse exited {Text}");
				}
			};

			Hitbox.InputEvent += (n, e, idx) =>
			{
				if (e is InputEventMouseButton mb)
				{
					if (mb.Pressed && mb.ButtonIndex == MouseButton.Left && !IsGrabbed)
					{
						IsGrabbed = true;
						EmitSignal(SignalName.Grabbed);
						//GD.Print("Grabbed GlyphText");
					}
					else if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && IsGrabbed)
					{
						IsGrabbed = false;
						EmitSignal(SignalName.Released);
						//GD.Print("Not Grabbed GlyphText");
					}
				}
			};

			/*
			Hitbox.MouseShapeEntered += shapeIdx =>
			{
				EmitSignal(SignalName.MouseEntered, (int)shapeIdx);
			};

			Hitbox.MouseShapeExited += shapeIdx =>
			{
				EmitSignal(SignalName.MouseExited, (int)shapeIdx);
			};
			*/

			Hitbox.AreaEntered += area => EmitSignal(SignalName.AreaEntered, area);
			Hitbox.AreaExited += area => EmitSignal(SignalName.AreaExited, area);
		}

		SetNotifyTransform(true);
		Rebuild();
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if (what == NotificationTransformChanged)
		{
			var scale = GlobalScale;
			if (!scale.IsEqualApprox(_lastScale))
			{
				_lastScale = scale;
				QueueRedraw();
			}
		}
	}

	public override void _UnhandledInput(InputEvent e)
	{
		base._UnhandledInput(e);

		if (e is InputEventMouseButton mb)
		{
			if (!mb.Pressed && mb.ButtonIndex == MouseButton.Left && IsGrabbed)
			{
				IsGrabbed = false;
				EmitSignal(SignalName.Released);
				//GD.Print("Not Grabbed GlyphText");
			}
		}
	}

	public override void _Draw()
	{
		if (Engine.IsEditorHint())
		{
			DrawRect(_fullBounds, new Color(0f, 0f, 1f, 0.1f), filled: true);

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

		var ci = GetCanvasItem();
		_textLine?.Draw(ci, _offset, Color);

		var scaledOutline = Mathf.RoundToInt(OutlineWidth * GlobalScale.X);

		if (scaledOutline > 0)
			_textLine?.DrawOutline(ci, _offset, scaledOutline, OutlineColor);
		else
			GD.Print("Outline width is zero or negative after scaling, skipping outline draw.");
	}

	void Rebuild()
	{
		//GD.Print($"Rebuilding GlyphText hitboxes for '{_text}'...");

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

		Rect2? glyphBounds = null;

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
				var position = new Vector2(left + (size.X * 0.5f), top + (size.Y * 0.5f));

				if (glyphBounds == null)
					glyphBounds = new Rect2(position - (size * 0.5f), size);
				else
					glyphBounds = glyphBounds.Value.Merge(new Rect2(position - (size * 0.5f), size));

				Hitbox.AddChild(new CollisionShape2D
				{
					Shape = new RectangleShape2D { Size = size },
					Position = position,
					Visible = false,
				});
			}

			cursorX += advance;
		}

		glyphBounds ??= new Rect2(Vector2.Zero, Vector2.Zero);

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
		_fullBounds = new Rect2(_offset, fullSize);
		_glyphBounds = new Rect2((_offset + glyphBounds.Value.Position), glyphBounds.Value.Size);

		Hitbox.Position = new Vector2(offsetX, offsetY);

		ts.FreeRid(shaped);
		QueueRedraw();
	}

	public Rect2 GetGlobalFullRect() => GlobalTransform * _fullBounds;
	public Rect2 GetGlobalGlyphRect() => GlobalTransform * _glyphBounds;
}