using System;
using Eto.Forms;
using MonoMac.AppKit;
using MonoMac.Foundation;
using Eto.Drawing;
using Eto.Platform.Mac.Drawing;

namespace Eto.Platform.Mac.Forms.Controls
{
	public class TextAreaHandler : MacView<NSTextView, TextArea>, ITextArea
	{
		int? lastCaretIndex;
		Range? lastSelection;

		public class EtoTextView : NSTextView, IMacControl
		{
			public WeakReference WeakHandler { get; set; }

			public object Handler
			{ 
				get { return (object)WeakHandler.Target; }
				set { WeakHandler = new WeakReference(value); } 
			}
		}

		public NSScrollView Scroll { get; private set; }

		public override NSView ContainerControl
		{
			get { return Scroll; }
		}
		// Remove use of delegate when events work correctly in MonoMac
		public class EtoDelegate : NSTextViewDelegate
		{
			WeakReference handler;

			public TextAreaHandler Handler { get { return (TextAreaHandler)handler.Target; } set { handler = new WeakReference(value); } }

			public override void TextDidChange(NSNotification notification)
			{
				Handler.Widget.OnTextChanged(EventArgs.Empty);
			}

			public override void DidChangeSelection(NSNotification notification)
			{
				var selection = Handler.Selection;
				if (selection != Handler.lastSelection)
				{
					Handler.Widget.OnSelectionChanged(EventArgs.Empty);
					Handler.lastSelection = selection;
				}
				var caretIndex = Handler.CaretIndex;
				if (caretIndex != Handler.lastCaretIndex)
				{
					Handler.Widget.OnCaretIndexChanged(EventArgs.Empty);
					Handler.lastCaretIndex = caretIndex;
				}
			}
		}

		public TextAreaHandler()
		{
			Control = new EtoTextView
			{
				Handler = this,
				Delegate = new EtoDelegate { Handler = this },
				AutoresizingMask = NSViewResizingMask.WidthSizable | NSViewResizingMask.HeightSizable,
				HorizontallyResizable = true,
				VerticallyResizable = true,
				Editable = true,
				Selectable = true,
				AllowsUndo = true,
				MinSize = new System.Drawing.SizeF(0, 0),
				MaxSize = new System.Drawing.SizeF(float.MaxValue, float.MaxValue)
			};
			Control.TextContainer.WidthTracksTextView = true;

			Scroll = new EtoScrollView
			{
				Handler = this,
				AutoresizesSubviews = true,
				HasVerticalScroller = true,
				HasHorizontalScroller = true,
				AutohidesScrollers = true,
				BorderType = NSBorderType.BezelBorder,
				DocumentView = Control
			};
		}

		protected override SizeF GetNaturalSize(SizeF availableSize)
		{
			return TextArea.DefaultSize;
		}

		public override void AttachEvent(string handler)
		{
			switch (handler)
			{
				case TextArea.TextChangedEvent:
				/*Control.TextDidChange += (sender, e) => {
					Widget.OnTextChanged (EventArgs.Empty);
				};*/
					break;
				case TextArea.SelectionChangedEvent:
				/*Control.DidChangeSelection += (sender, e) => {
					var selection = this.Selection;
					if (selection != lastSelection) {
						Widget.OnSelectionChanged (EventArgs.Empty);
						lastSelection = selection;
					}
				};*/
					break;
				case TextArea.CaretIndexChangedEvent:
				/*Control.DidChangeSelection += (sender, e) => {
					var caretIndex = Handler.CaretIndex;
					if (caretIndex != lastCaretIndex) {
						Handler.Widget.OnCaretIndexChanged (EventArgs.Empty);
						lastCaretIndex = caretIndex;
					}
				};*/
					break;
				default:
					base.AttachEvent(handler);
					break;
			}
		}

		public bool ReadOnly
		{
			get { return !Control.Editable; }
			set { Control.Editable = !value; }
		}

		public override bool Enabled
		{
			get { return Control.Selectable; }
			set
			{
				Control.Selectable = value;
				if (!value)
				{
					Control.TextColor = NSColor.DisabledControlText;
					Control.BackgroundColor = NSColor.ControlBackground;
				}
				else
				{
					Control.TextColor = NSColor.ControlText;
					Control.BackgroundColor = NSColor.TextBackground;
				}
			}
		}

		public string Text
		{
			get
			{
				return Control.Value;
			}
			set
			{
				Control.TextStorage.SetString(font.AttributedString(value ?? string.Empty));
				Control.DisplayIfNeeded();
			}
		}

		Font font;
		public Font Font
		{
			get
			{
				if (font == null)
					font = new Font(Widget.Generator, new FontHandler(Control.Font));
				return font;
			}
			set
			{
				font = value;
				Control.TextStorage.SetString(font.AttributedString(Control.Value));
				LayoutIfNeeded();
			}
		}

		public bool Wrap
		{
			get
			{
				return Control.TextContainer.WidthTracksTextView;
			}
			set
			{
				if (value)
				{
					Control.TextContainer.WidthTracksTextView = true;
				}
				else
				{
					Control.TextContainer.WidthTracksTextView = false;
					Control.TextContainer.ContainerSize = new System.Drawing.SizeF(float.MaxValue, float.MaxValue);
				}
			}
		}

		public string SelectedText
		{
			get
			{
				var range = Control.SelectedRange;
				if (range.Location >= 0 && range.Length > 0)
					return Control.Value.Substring(range.Location, range.Length);
				else
					return null;
			}
			set
			{
				var range = Control.SelectedRange;
				Control.TextStorage.DeleteRange(range);
				if (value != null)
				{
					range.Length = value.Length;
					Control.TextStorage.Insert(new NSAttributedString(value), range.Location);
					Control.SelectedRange = range;
				}
			}
		}

		public Range Selection
		{
			get { return Control.SelectedRange.ToEto(); }
			set { Control.SelectedRange = value.ToNS(); }
		}

		public void SelectAll()
		{
			Control.SelectAll(Control);
		}

		public int CaretIndex
		{
			get { return Control.SelectedRange.Location; }
			set { Control.SelectedRange = new NSRange(value, 0); }
		}

		public void Append(string text, bool scrollToCursor)
		{
			var range = new NSRange(this.Control.Value.Length, 0);
			this.Control.Replace(range, text);
			range = new NSRange(this.Control.Value.Length, 0);
			this.Control.SelectedRange = range;
			if (scrollToCursor)
				this.Control.ScrollRangeToVisible(range);
		}
	}
}
