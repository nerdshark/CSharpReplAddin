using System;

namespace MonoDevelop.CSharpRepl
{
	public partial class ConnectToReplDialog : Gtk.Dialog
	{
		private ReplPad replPad;

		public ConnectToReplDialog (ReplPad replPad)
		{
			this.Build ();
			this.replPad = replPad;
		}

		protected void OnButtonOkClicked (object sender, EventArgs e)
		{
			replPad.ConnectToInteractiveSession (replAddressEntry.Text);
			Destroy ();
		}

		protected void OnButtonCancelClicked (object sender, EventArgs e)
		{
			Destroy ();
		}
	}
}

