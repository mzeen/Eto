using System;
using Eto.Forms;
using System.Collections.Generic;
using System.Linq;
using Eto.Drawing;

namespace Eto.Test
{
	public interface ISection
	{
		string Text { get; }

		Control CreateContent();
	}

	/// <summary>
	/// Sections can nest. Each section item can also host
	/// a control that is displayed in the details view when 
	/// the section is selected.
	/// 
	/// Sections do not have any particular visual representation,
	/// and can be wrapped within a tree item (SectionTreeItem) or
	/// any other visual representation.
	/// </summary>
	public class Section : List<Section>
	{
		public string Text { get; set; }

		public Section()
		{
		}

		public Section(string text, IEnumerable<Section> sections)
			: base (sections.OrderBy (r => r.Text, StringComparer.CurrentCultureIgnoreCase).ToArray())
		{
			this.Text = text;
		}
	}

	/// <summary>
	/// A tree item representation of a section.
	/// </summary>
	public class SectionTreeItem : List<SectionTreeItem>, ITreeGridItem<SectionTreeItem>
	{
		public Section Section { get; private set; }
		public string Text { get { return Section.Text; } }
		public bool Expanded { get; set; }
		public bool Expandable { get { return Count > 0; } }
		public ITreeGridItem Parent { get; set; }

		public SectionTreeItem(Section section)
		{
			this.Section = section;
			this.Expanded = true;
			foreach (var child in section)
			{
				var temp = new SectionTreeItem(child);
				temp.Parent = this;
				this.Add(temp); // recursive
			}
		}
	}

	public abstract class SectionBase : Section, ISection
	{
		public abstract Control CreateContent();
	}

	public class Section<T> : SectionBase
		where T: Control, new()
	{
		public Func<T> Creator { get; set; }

		public override Control CreateContent()
		{
			return Creator != null ? Creator() : new T();
		}
	}

	/// <summary>
	/// Tests for dialogs and forms use this.
	/// </summary>
	public class WindowSectionMethod : Section, ISection
	{
		Func<Window> Func { get; set; }

		public WindowSectionMethod(string text = null)
		{
			Text = text;
		}

		public WindowSectionMethod(string text, Func<Window> f)
		{
			Func = f;
			Text = text;
		}

		protected virtual Window GetWindow()
		{
			return null;
		}

		public Control CreateContent()
		{
			var button = new Button { Text = string.Format("Show the {0} test", Text) };
			var layout = new DynamicLayout();
			layout.AddCentered(button);
			button.Click += (sender, e) => {

				try
				{
					var window = Func != null ? Func() : null ?? GetWindow();

					if (window != null)
					{
						var dialog = window as Dialog;
						if (dialog != null)
						{
							dialog.ShowDialog(null);
							return;
						}
						var form = window as Form;
						if (form != null)
						{
							form.Show();
							return;
						}
					}
				}
				catch (Exception ex)
				{
					Log.Write(this, "Error loading section: {0}", ex.GetBaseException());
				}
			};
			return layout;
		}
	}

	/// <summary>
	/// The base class for views that display the set of tests.
	/// </summary>
	public abstract class SectionList
	{
		public abstract Control Control { get; }
		public abstract ISection SelectedItem { get; }
		public event EventHandler SelectedItemChanged;

		public string SectionTitle
		{
			get
			{
				var section = SelectedItem as Section;
				return section != null ? section.Text : null;
			}
		}

		protected void OnSelectedItemChanged(object sender, EventArgs e)
		{
			if (this.SelectedItemChanged != null)
				this.SelectedItemChanged(sender, e);
		}
	}

	public class SectionListTreeView : SectionList
	{
		TreeGridView treeView;

		public override Control Control { get { return this.treeView; } }

		public override ISection SelectedItem
		{
			get
			{
				var sectionTreeItem = treeView.SelectedItem as SectionTreeItem;
				return sectionTreeItem.Section as ISection;
			}
		}

		public SectionListTreeView(IEnumerable<Section> topNodes)
		{
			this.treeView = new TreeGridView();
			treeView.Style = "sectionList";
			treeView.ShowHeader = false;
			treeView.Columns.Add(new GridColumn { DataCell = new TextBoxCell { Binding = new PropertyBinding("Text") } });
			treeView.DataStore = new SectionTreeItem(new Section("Top", topNodes));
			treeView.SelectedItemChanged += OnSelectedItemChanged;
		}
	}

	/// <summary>
	/// Allows a test case to use a different generator for drawing
	/// graphics than for windowing.
	/// </summary>
	public class DrawingToolkit
	{
		public virtual void Initialize(Drawable drawable)
		{
		}

		public virtual void Render(Graphics graphics, Action<Graphics> render)
		{
			render(graphics);
		}

		public virtual DrawingToolkit Clone()
		{
			return new DrawingToolkit();
		}

		public virtual GeneratorContext GetGeneratorContext()
		{
			return new GeneratorContext(Generator.Current); // don't change the context
		}
	}

	public class D2DToolkit : DrawingToolkit
	{
		Graphics graphics;
		Generator d2d;

		public D2DToolkit()
		{
			this.d2d = Generator.GetGenerator(Generators.Direct2DAssembly);
		}

		public override void Initialize(Drawable drawable)
		{
			base.Initialize(drawable);
			this.graphics = new Graphics(drawable, d2d);
		}

		public override void Render(Graphics g, Action<Graphics> render)
		{
			this.graphics.BeginDrawing();
			//this.graphics.Clear(Brushes.Black() as SolidBrush); // DirectDrawingSection's Drawable seems to automatically clear the background, but that doesn't happen in Direct2d, so we clear it explicitly.
			try
			{
				using (var context = GetGeneratorContext())
					render(this.graphics);
			}
			catch (Exception) { }
			this.graphics.EndDrawing();
		}

		public override GeneratorContext GetGeneratorContext()
		{
			return new GeneratorContext(d2d);
		}

		public override DrawingToolkit Clone()
		{
			return new D2DToolkit();
		}
	}
}

