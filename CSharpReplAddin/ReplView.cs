// 
// ConsoleView.cs
//  
// Author:
//       Peter Johanson <latexer@gentoo.org>
//       Lluis Sanchez Gual <lluis@novell.com>
//       Scott Stephens <stephens.js@gmail.com>
//
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Scott Stephens (stephens.js@gmail.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using MonoDevelop.Core;
using Gtk;

namespace MonoDevelop.CSharpRepl.Components
{
	public class ReplView: ScrolledWindow
	{
		string scriptLines = "";
		
		Stack<string> commandHistoryPast = new Stack<string> ();
		Stack<string> commandHistoryFuture = new Stack<string> ();
		List<Tuple<string,EventHandler>> menuCommands = new List<Tuple<string,EventHandler>>();

		bool inBlock = false;
		string blockText = "";

		TextView textView;
	
		public ReplView()
		{
			PromptString = "> ";
			PromptMultiLineString = ">> ";
			
			textView = new TextView ();
			Add (textView);
			ShowAll ();
			
			textView.WrapMode = Gtk.WrapMode.Word;
			textView.KeyPressEvent += TextViewKeyPressEvent;
			textView.PopulatePopup += TextViewPopulatePopup;
			
			// The 'Freezer' tag is used to keep everything except
			// the input line from being editable
			TextTag tag = new TextTag ("Freezer");
			tag.Editable = false;
			Buffer.TagTable.Add (tag);
			Prompt (false);
		}
		public void AddMenuCommand(string name, EventHandler handler)
		{
			this.menuCommands.Add(Tuple.Create(name,handler));
		}

		void TextViewPopulatePopup (object o, PopulatePopupArgs args)
		{
			MenuItem item = new MenuItem (GettextCatalog.GetString ("Clear"));
			SeparatorMenuItem sep = new SeparatorMenuItem ();
			
			item.Activated += ClearActivated;
			item.Show ();
			sep.Show ();
			
			args.Menu.Add (sep);
			args.Menu.Add (item);

			foreach (var menu_command in menuCommands)
			{
				var tmp = new MenuItem(menu_command.Item1);
				tmp.Activated += menu_command.Item2;
				tmp.Show();
				args.Menu.Add(tmp);
			}
		}

		void ClearActivated (object sender, EventArgs e)
		{
			Clear ();
		}
		
		public void SetFont (Pango.FontDescription font)
		{
			textView.ModifyFont (font);
		}
		
		public string PromptString { get; set; }
		
		public bool AutoIndent { get; set; }

		public string PromptMultiLineString { get; set; }
		
		[GLib.ConnectBeforeAttribute]
		void TextViewKeyPressEvent (object o, KeyPressEventArgs args)
		{
			args.RetVal = ProcessKeyPressEvent (args.Event);
		}
		
		bool ProcessKeyPressEvent (Gdk.EventKey ev)
		{
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down)
				return false;
			
			// Needed so people can copy and paste, but always end up
			// typing in the prompt.
			if (Cursor.Compare (InputLineBegin) < 0) {
				Buffer.MoveMark (Buffer.SelectionBound, InputLineEnd);
				Buffer.MoveMark (Buffer.InsertMark, InputLineEnd);
			}
			
//			if (ev.State == Gdk.ModifierType.ControlMask && ev.Key == Gdk.Key.space)
//				TriggerCodeCompletion ();
	
			if (ev.Key == Gdk.Key.Return) {
				if (inBlock) {
					if (InputLine == "") {
						ProcessInput (blockText);
						blockText = "";
						inBlock = false;
					} else {
						blockText += "\n" + InputLine;
						string whiteSpace = null;
						if (AutoIndent) {
							System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex (@"^(\s+).*");
							whiteSpace = r.Replace (InputLine, "$1");
							if (InputLine.EndsWith (BlockStart))
								whiteSpace += "\t";
						}
						Prompt (true, true);
						if (AutoIndent)
							InputLine += whiteSpace;
					}
				} else {
					// Special case for start of new code block
					if (!string.IsNullOrEmpty (BlockStart) && InputLine.Trim().EndsWith (BlockStart)) {
						inBlock = true;
						blockText = InputLine;
						Prompt (true, true);
						if (AutoIndent)
							InputLine += "\t";
						return true;
					}
					// Bookkeeping
					if (InputLine != "") {
						// Everything but the last item (which was input),
						//in the future stack needs to get put back into the
						// past stack
						while (commandHistoryFuture.Count > 1)
							commandHistoryPast.Push (commandHistoryFuture.Pop());
						// Clear the pesky junk input line
						commandHistoryFuture.Clear();
	
						// Record our input line
						commandHistoryPast.Push(InputLine);
						if (scriptLines == "")
							scriptLines += InputLine;
						else
							scriptLines += "\n" + InputLine;
					
						ProcessInput (InputLine);
					}
				}
				return true;
			}
	
			// The next two cases handle command history	
			else if (ev.Key == Gdk.Key.Up) {
				if (!inBlock && commandHistoryPast.Count > 0) {
					if (commandHistoryFuture.Count == 0)
						commandHistoryFuture.Push (InputLine);
					else {
						if (commandHistoryPast.Count == 1)
							return true;
						commandHistoryFuture.Push (commandHistoryPast.Pop());
					}
					InputLine = commandHistoryPast.Peek();
				}
				return true;
			}
			else if (ev.Key == Gdk.Key.Down) {
				if (!inBlock && commandHistoryFuture.Count > 0) {
					if (commandHistoryFuture.Count == 1)
						InputLine = commandHistoryFuture.Pop();
					else {
						commandHistoryPast.Push (commandHistoryFuture.Pop ());
						InputLine = commandHistoryPast.Peek ();
					}
				}
				return true;
			}	
			else if (ev.Key == Gdk.Key.Left) {
				// Keep our cursor inside the prompt area
				if (Cursor.Compare (InputLineBegin) <= 0)
					return true;
			}
			else if (ev.Key == Gdk.Key.Home) {
				Buffer.MoveMark (Buffer.InsertMark, InputLineBegin);
				// Move the selection mark too, if shift isn't held
				if ((ev.State & Gdk.ModifierType.ShiftMask) == ev.State)
					Buffer.MoveMark (Buffer.SelectionBound, InputLineBegin);
				return true;
			}
			else if (ev.Key == Gdk.Key.period) {
				return false;
			}
	
			// Short circuit to avoid getting moved back to the input line
			// when paging up and down in the shell output
			else if (ev.Key == Gdk.Key.Page_Up || ev.Key == Gdk.Key.Page_Down) {
				return false;
			}
			
			return false;
		}
		
		TextMark endOfLastProcessing;
		TextMark startOfPrompt;
		PromptState promptState;

		public TextIter InputLineBegin {
			get {
				return Buffer.GetIterAtMark (endOfLastProcessing);
			}
		}

		public TextIter InputPromptBegin {
			get {
				return Buffer.GetIterAtMark(startOfPrompt);
			}
		}

		public TextIter InputLineEnd {
			get { return Buffer.EndIter; }
		}
		
		private TextIter Cursor {
			get { return Buffer.GetIterAtMark (Buffer.InsertMark); }
		}
		
		Gtk.TextBuffer Buffer {
			get { return textView.Buffer; }
		}
		
		// The current input line
		public string InputLine {
			get {
				return Buffer.GetText (InputLineBegin, InputLineEnd, false);
			}
			set {
				TextIter start = InputLineBegin;
				TextIter end = InputLineEnd;
				Buffer.Delete (ref start, ref end);
				start = InputLineBegin;
				Buffer.Insert (ref start, value);
			}
		}

		protected virtual void ProcessInput (string line)
		{
			WriteInput ("\n");
			this.FinishInputLine();
			if (ConsoleInput != null)
				ConsoleInput (this, new ConsoleInputEventArgs (line));
		}

		public void WriteOutput (string line)
		{
			string line_in_progress = this.InputLine;
			TextIter start = this.InputPromptBegin;
			TextIter end = this.InputLineEnd;
			Buffer.Delete(ref start, ref end);
			start = this.InputPromptBegin;
			Buffer.Insert(ref start, line);

			if (promptState != PromptState.None)
				this.Prompt(!line.EndsWith(Environment.NewLine), promptState == PromptState.Multiline );
			start = this.InputLineBegin;
			Buffer.Insert(ref start, line_in_progress);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
		}
	
		public void WriteInput(string line)
		{
			line.Replace(Environment.NewLine,Environment.NewLine+this.PromptMultiLineString);
			TextIter start = Buffer.EndIter;
			Buffer.Insert (ref start , line);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
		}
		public void ProcessInput()
		{

		}

		public void FinishInputLine()
		{
			startOfPrompt = Buffer.CreateMark(null, Buffer.EndIter, true);
			endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true);
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
			promptState = PromptState.None;
		}

		public void Prompt (bool newLine)
		{
			Prompt (newLine, false);
		}
	
		public void Prompt (bool newLine, bool multiline)
		{
			promptState = multiline ? PromptState.Multiline : PromptState.Regular;

			TextIter end = Buffer.EndIter;
			if (newLine)
				Buffer.Insert (ref end, "\n");

			startOfPrompt = Buffer.CreateMark(null, Buffer.EndIter, true);

			if (multiline)
				Buffer.Insert (ref end, PromptMultiLineString);
			else
				Buffer.Insert (ref end, PromptString);
	
			Buffer.PlaceCursor (Buffer.EndIter);
			textView.ScrollMarkOnscreen (Buffer.InsertMark);
	
			// Record the end of where we processed, used to calculate start
			// of next input line
			endOfLastProcessing = Buffer.CreateMark (null, Buffer.EndIter, true);
	
			// Freeze all the text except our input line
			Buffer.ApplyTag(Buffer.TagTable.Lookup("Freezer"), Buffer.StartIter, InputLineBegin);
		}
		
		public void Clear ()
		{
			Buffer.Text = "";
			scriptLines = "";
			Prompt (false);
		}
		
		public void ClearHistory ()
		{
			commandHistoryFuture.Clear ();
			commandHistoryPast.Clear ();
		}
		
		public string BlockStart { get; set; }
		
		public string BlockEnd { get; set; }
		
		public event EventHandler<ConsoleInputEventArgs> ConsoleInput;
	}

	public enum PromptState { None, Regular, Multiline }

	public class ConsoleInputEventArgs: EventArgs
	{
		public ConsoleInputEventArgs (string text)
		{
			Text = text;
		}
		
		public string Text { get; internal set; }
	}
}
