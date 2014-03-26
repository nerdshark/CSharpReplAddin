
// This file has been generated by the GUI designer. Do not modify.
namespace MonoDevelop.CSharpRepl
{
	public partial class ConnectToReplDialog
	{
		private global::Gtk.HBox hbox1;
		private global::Gtk.Label label2;
		private global::Gtk.Entry replAddressEntry;
		private global::Gtk.Button buttonCancel;
		private global::Gtk.Button buttonOk;

		protected virtual void Build ()
		{
			global::Stetic.Gui.Initialize (this);
			// Widget MonoDevelop.CSharpRepl.ConnectToReplDialog
			this.Name = "MonoDevelop.CSharpRepl.ConnectToReplDialog";
			this.Title = global::Mono.Unix.Catalog.GetString ("Connect To REPL");
			this.WindowPosition = ((global::Gtk.WindowPosition)(4));
			this.Modal = true;
			this.Resizable = false;
			this.DestroyWithParent = true;
			// Internal child MonoDevelop.CSharpRepl.ConnectToReplDialog.VBox
			global::Gtk.VBox w1 = this.VBox;
			w1.Name = "dialog1_VBox";
			w1.BorderWidth = ((uint)(2));
			// Container child dialog1_VBox.Gtk.Box+BoxChild
			this.hbox1 = new global::Gtk.HBox ();
			this.hbox1.Name = "hbox1";
			this.hbox1.Spacing = 6;
			// Container child hbox1.Gtk.Box+BoxChild
			this.label2 = new global::Gtk.Label ();
			this.label2.Name = "label2";
			this.label2.LabelProp = global::Mono.Unix.Catalog.GetString ("Address");
			this.label2.SingleLineMode = true;
			this.hbox1.Add (this.label2);
			global::Gtk.Box.BoxChild w2 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.label2]));
			w2.Position = 0;
			w2.Expand = false;
			w2.Fill = false;
			// Container child hbox1.Gtk.Box+BoxChild
			this.replAddressEntry = new global::Gtk.Entry ();
			this.replAddressEntry.CanFocus = true;
			this.replAddressEntry.Name = "replAddressEntry";
			this.replAddressEntry.IsEditable = true;
			this.replAddressEntry.InvisibleChar = '●';
			this.hbox1.Add (this.replAddressEntry);
			global::Gtk.Box.BoxChild w3 = ((global::Gtk.Box.BoxChild)(this.hbox1 [this.replAddressEntry]));
			w3.Position = 1;
			w1.Add (this.hbox1);
			global::Gtk.Box.BoxChild w4 = ((global::Gtk.Box.BoxChild)(w1 [this.hbox1]));
			w4.Position = 0;
			w4.Expand = false;
			w4.Fill = false;
			// Internal child MonoDevelop.CSharpRepl.ConnectToReplDialog.ActionArea
			global::Gtk.HButtonBox w5 = this.ActionArea;
			w5.Name = "dialog1_ActionArea";
			w5.Spacing = 10;
			w5.BorderWidth = ((uint)(5));
			w5.LayoutStyle = ((global::Gtk.ButtonBoxStyle)(4));
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonCancel = new global::Gtk.Button ();
			this.buttonCancel.CanDefault = true;
			this.buttonCancel.CanFocus = true;
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.UseStock = true;
			this.buttonCancel.UseUnderline = true;
			this.buttonCancel.Label = "gtk-cancel";
			this.AddActionWidget (this.buttonCancel, -6);
			global::Gtk.ButtonBox.ButtonBoxChild w6 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5 [this.buttonCancel]));
			w6.Expand = false;
			w6.Fill = false;
			// Container child dialog1_ActionArea.Gtk.ButtonBox+ButtonBoxChild
			this.buttonOk = new global::Gtk.Button ();
			this.buttonOk.CanDefault = true;
			this.buttonOk.CanFocus = true;
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.UseStock = true;
			this.buttonOk.UseUnderline = true;
			this.buttonOk.Label = "gtk-ok";
			this.AddActionWidget (this.buttonOk, -5);
			global::Gtk.ButtonBox.ButtonBoxChild w7 = ((global::Gtk.ButtonBox.ButtonBoxChild)(w5 [this.buttonOk]));
			w7.Position = 1;
			w7.Expand = false;
			w7.Fill = false;
			if ((this.Child != null)) {
				this.Child.ShowAll ();
			}
			this.DefaultWidth = 425;
			this.DefaultHeight = 119;
			this.Show ();
		}
	}
}
