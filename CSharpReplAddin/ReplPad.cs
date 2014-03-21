// 
// ReplPad.cs
//  
// Author:
//       Scott Stephens <stephens.js@gmail.com>
// 
// Copyright (c) 2012 Scott Stephens
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using Assembly = System.Reflection.Assembly;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharpRepl.Components;
using MonoDevelop.Projects;
using Gtk;
using Process = System.Diagnostics.Process;
using MonoDevelop.Components.Docking;
using MonoDevelop.Components.DockToolbars;
using MDComponents = MonoDevelop.Components;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Components.Theming;

namespace MonoDevelop.CSharpRepl
{
	public class ReplPad: IPadContent
	{
		public static ReplPad Instance = null;

		public bool Running { get; private set; }

		Pango.FontDescription customFont;

		bool disposed;
		ICSharpRepl shell;

		Process _repl_process;
		StreamOutputter _stdout;
		StreamOutputter _stderr;

		ReplView currentReplView;
		Image emptyImage;
		Notebook notebook;
		Widget content;
		HBox layout;
		Toolbar toolbar;
		ToolButton newReplButton;

		public void Initialize (IPadWindow container)
		{
			emptyImage = new Image ();
			if (IdeApp.Preferences.CustomOutputPadFont != null)
				customFont = IdeApp.Preferences.CustomOutputPadFont;
			else 
				customFont = FontService.DefaultMonospaceFontDescription;
				
			//view.AddMenuCommand("Start Interactive Session", StartInteractiveSessionHandler);
			//view.AddMenuCommand("Connect to Interactive Session", ConnectToInteractiveSessionHandler);

			notebook = new Notebook ();
			notebook.Scrollable = true;

			newReplButton = CreateNewReplButton ();
			newReplButton.Clicked += (object sender, EventArgs e) =>
			{
				currentReplView = AddRepl ();
			};

			notebook.SwitchPage += (object o, SwitchPageArgs args) =>
			{
				currentReplView = (notebook.GetNthPage ((int)(args.PageNum))) as ReplView;
			};

			toolbar = CreateToolbar ();
			toolbar.Add (newReplButton);

			layout = new HBox ();
			layout.PackStart (notebook, true, true, 0);
			layout.PackEnd (toolbar, false, true, 0);
			Control = layout;
			Control.ShowAll ();
			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomOutputPadFontChanged;

			ReplPad.Instance = this;
		}

		private Toolbar CreateToolbar()
		{
			var tb = new Toolbar ();
			tb.IconSize = IconSize.SmallToolbar;
			tb.Orientation = Orientation.Vertical;
			tb.ToolbarStyle = ToolbarStyle.Icons;
			return tb;
		}

		private ToolButton CreateNewReplButton()
		{
			var button = new ToolButton (Gtk.Stock.Add);
			return button;
		}

		private ReplView CurrentRepl()
		{
			if(notebook.CurrentPageWidget != null)
			{
				return notebook.CurrentPageWidget as ReplView;
			}
			return null;
		}

		private ReplView AddRepl(string title = "REPL")
		{
			currentReplView = new ReplView ();
			currentReplView.PromptString = "csharp> ";
			currentReplView.PromptMultiLineString = "+ ";
			currentReplView.ConsoleInput += OnViewConsoleInput;
			currentReplView.SetFont (customFont);
			currentReplView.ShadowType = Gtk.ShadowType.None;
			var tabLabel = new MDComponents.TabLabel (new Label (title), emptyImage);
			notebook.AppendPage (currentReplView, tabLabel);
			if(notebook.NPages < 2) notebook.ShowTabs = false;
			else notebook.ShowTabs = true;
			return currentReplView;
		}

		public void Start(string platform="AnyCPU")
		{
			// Start Repl process
			if (!this.Running)
			{
				this.StartInteractiveSession(platform);
				this.ConnectToInteractiveSession();
			}
		}

		public void Stop()
		{
			if (_stderr != null) {
				_stderr.Stop(); 
				_stderr = null;
			}
			if (_stdout != null) {
				_stdout.Stop();
				_stdout = null;
			}
			if (_repl_process != null)
			{
				try
				{
					_repl_process.Kill();
				}
				catch (InvalidOperationException)
				{
				}
				_repl_process.Close();
				_repl_process.Dispose();
				_repl_process = null;
			}
			this.Running = false;
			currentReplView.WriteOutput("Disconnected.");
		}

		void StartInteractiveSessionHandler(object sender, EventArgs e)
        {
			this.StartInteractiveSession();
		}
		void ConnectToInteractiveSessionHandler(object sender, EventArgs e)
		{
			ConnectToInteractiveSession();
		}
		void StartInteractiveSession(string platform="AnyCPU")
		{
            string exe_name;
            switch (platform.ToLower())
            {
                case "anycpu":
                    exe_name = "CSharpReplServer.exe";
                    break;
                case "x86":
                    exe_name = "CSharpReplServer32.exe";
                    break;
                case "x64":
                    exe_name = "CSharpReplServer64.exe";
                    break;
                default:
                    currentReplView.WriteOutput(String.Format("Cannot start interactive session for platform {0}. Platform not supported.", platform));
                    return;
            }

			string bin_dir = Path.GetDirectoryName(Assembly.GetAssembly(typeof(ReplPad)).Location);
			string repl_exe = Path.Combine(bin_dir, exe_name);
			//string  framework_exe = @"C:\Program Files (x86)\Mono-2.10.9\bin\mono.exe";
			//var start_info = new ProcessStartInfo(framework_exe, repl_exe + " 33333");
			var start_info = new ProcessStartInfo(repl_exe,"33333");
			start_info.UseShellExecute = false;
			start_info.CreateNoWindow = true;
			start_info.RedirectStandardError = true;
			start_info.RedirectStandardOutput = true;
			
			_repl_process = Process.Start(start_info);
			_stdout = new StreamOutputter(_repl_process.StandardOutput, currentReplView);
			_stderr = new StreamOutputter(_repl_process.StandardError, currentReplView);
			_stdout.Start();
			_stderr.Start();
			Thread.Sleep(1000); // Give _repl_process time to start up before we let anybody do anything with it
		}
		void ConnectToInteractiveSession()
		{
			var tmpshell = new CSharpReplServerProxy(33333);
			try {
				tmpshell.Start();
				this.shell = tmpshell;
				this.Running = true;
                currentReplView.WriteOutput("Successfully connected to interactive session.");
			} catch (Exception e) {
				this.shell = null;
				this.Running = false;
				currentReplView.WriteOutput("Failed connecting to interactive session: " + e.Message);
			}
		}
		
		void HandleCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			if (customFont != null) {
				customFont.Dispose ();
				customFont = null;
			}
			
			customFont = IdeApp.Preferences.CustomOutputPadFont;
			
			currentReplView.SetFont (customFont);
		}

		public void InputBlock(string block, string prefix_to_strip="")
		{
			this.currentReplView.WriteInput(block, prefix_to_strip);
		}

		public void LoadReferences(DotNetProject project)
		{
			foreach ( var x in project.References)
			{
				if (x.ReferenceType == ReferenceType.Assembly) {
					// Just a path to the reference, can be passed in no problem
					this.shell.loadAssembly(x.Reference);
				} else if (x.ReferenceType == ReferenceType.Gac || x.ReferenceType == ReferenceType.Package) {
					// The fully-qualified name of the assembly, can be passed in no problem
					this.shell.loadAssembly(x.Reference);
				} else if (x.ReferenceType == ReferenceType.Project) {
					DotNetProject inner_project = project.ParentSolution.FindProjectByName(x.Reference) as DotNetProject;
					if (inner_project != null) {
						var config = inner_project.GetConfiguration(IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						string file_name = config.CompiledOutputName.FullPath.ToString();
						this.shell.loadAssembly(file_name);
					} else 
						this.currentReplView.WriteOutput(String.Format ("Cannot load non .NET project reference: {0}/{1}", project.Name, x.Reference));
				}
			}
		}


		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			if (this.shell == null)
			{
				this.currentReplView.WriteOutput("Not connected.");
				this.currentReplView.Prompt(true);
				return;
			}
			
			Result result;
			try {
				result = this.shell.evaluate(e.Text);
			} catch (Exception ex) {
				currentReplView.WriteOutput("Evaluation failed: " + ex.Message);
				currentReplView.Prompt(true);
				return;
			}
			
			switch (result.Type)
			{
			case ResultType.FAILED:
				currentReplView.WriteOutput(result.ResultMessage);
				currentReplView.Prompt(false);
				break;
			case ResultType.NEED_MORE_INPUT:
				currentReplView.Prompt (false,true);
				break;
			case ResultType.SUCCESS_NO_OUTPUT:
				currentReplView.Prompt(false);
				break;
			case ResultType.SUCCESS_WITH_OUTPUT:
				currentReplView.WriteOutput(result.ResultMessage);	
				currentReplView.Prompt(true);
				break;
			default:
				throw new Exception("Unexpected state! Contact developers.");
			}
		}
		
//		void PrintValue (ObjectValue val)
//		{
//			string result = val.Value;
//			if (string.IsNullOrEmpty (result)) {
//				if (val.IsNotSupported)
//					result = GettextCatalog.GetString ("Expression not supported.");
//				else if (val.IsError || val.IsUnknown)
//					result = GettextCatalog.GetString ("Evaluation failed.");
//				else
//					result = string.Empty;
//			}
//			view.WriteOutput (result);
//		}
//		
//		void WaitForCompleted (ObjectValue val)
//		{
//			int iteration = 0;
//			
//			GLib.Timeout.Add (100, delegate {
//				if (!val.IsEvaluating) {
//					if (iteration >= 5)
//						view.WriteOutput ("\n");
//					PrintValue (val);
//					view.Prompt (true);
//					return false;
//				}
//				if (++iteration == 5)
//					view.WriteOutput (GettextCatalog.GetString ("Evaluating") + " ");
//				else if (iteration > 5 && (iteration - 5) % 10 == 0)
//					view.WriteOutput (".");
//				else if (iteration > 300) {
//					view.WriteOutput ("\n" + GettextCatalog.GetString ("Timed out."));
//					view.Prompt (true);
//					return false;
//				}
//				return true;
//			});
//		}
		
		public void RedrawContent ()
		{
		}

		public Gtk.Widget Control {
			get {
				return content;
			}
			private set
			{
				content = value;
			}
		}
		
		public void Dispose ()
		{
			if (!disposed) {
				this.Stop();

				IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomOutputPadFontChanged;
				if (customFont != null)
					customFont.Dispose ();

				disposed = true;
			}
		}
	}


}