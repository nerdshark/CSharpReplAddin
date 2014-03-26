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
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Analysis;

//using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MonoDevelop.CSharpRepl
{
	public class ReplPad: IPadContent
	{
		private class ReplSession : IDisposable
		{
			private readonly IAsyncCSharpRepl repl;

			public IAsyncCSharpRepl Repl { get { return repl; } }

			private readonly Process process;

			public Process Process { get { return process; } }

			private readonly StreamOutputter stdout;

			public StreamOutputter Stdout { get { return stdout; } }

			private readonly StreamOutputter stderr;

			public StreamOutputter Stderr { get { return stderr; } }

			private readonly ReplView replView;
			private readonly int port;

			public int Port { get { return port; } }

			public ReplSession (ReplView view, Process proc, int port)
			{
				replView = view;
				process = proc;
				this.port = port;

				stderr = new StreamOutputter (proc.StandardError, replView);
				stdout = new StreamOutputter (proc.StandardOutput, replView);
				Stderr.Start ();
				Stdout.Start ();

				var tmprepl = new CSharpReplServerProxy (String.Format ("tcp://127.0.0.1:{0}", port));
				tmprepl.Start ();
				repl = tmprepl;
			}

			private bool disposed;

			public void Dispose ()
			{
				if (!disposed) return;
				if (repl != null) ((CSharpReplServerProxy)repl).Dispose ();
				if (stderr != null) stderr.Stop ();
				if (stdout != null) stdout.Stop ();
				if (process != null) process.Close ();
			}
		}

		public static ReplPad Instance = null;

		public bool Running { get; private set; }

		private int nextPort = 33333;
		Pango.FontDescription customFont;
		bool disposed;
		//		ICSharpRepl shell;
		//Process _repl_process;
		//StreamOutputter _stdout;
		//StreamOutputter _stderr;
		//ReplView currentReplView;
		Image emptyImage;
		Notebook notebook;
		Widget content;
		HBox layout;
		Toolbar toolbar;
		ToolButton newReplButton;
		Dictionary<ReplView, ReplSession> replSessions;

		public void Initialize (IPadWindow window)
		{
			replSessions = new Dictionary<ReplView, ReplSession> ();
			window.Icon = MonoDevelop.Ide.Gui.Stock.Console;
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

			/*notebook.Added += (object o, AddedArgs args) => {
				currentReplView = (args.Widget as ReplView);
			};*/
			/*notebook.SwitchPage += (object o, SwitchPageArgs args) => {
				currentReplView = (notebook.GetNthPage ((int)(args.PageNum))) as ReplView;
			};
			*/
			toolbar = CreateToolbar ();
			//toolbar.Add (newReplButton);

			window.GetToolbar (PositionType.Right).Add (newReplButton);
			window.GetToolbar (PositionType.Right).Visible = true;
			window.GetToolbar (PositionType.Right).ShowAll ();

			layout = new HBox ();
			layout.PackStart (notebook, true, true, 0);
			layout.PackEnd (toolbar, false, true, 0);
			Control = layout;
			Control.ShowAll ();
			IdeApp.Preferences.CustomOutputPadFontChanged += HandleCustomOutputPadFontChanged;

			ReplPad.Instance = this;
		}

		private Toolbar CreateToolbar ()
		{
			var tb = new Toolbar ();
			tb.IconSize = IconSize.SmallToolbar;
			tb.Orientation = Orientation.Vertical;
			tb.ToolbarStyle = ToolbarStyle.Icons;
			return tb;
		}

		private ToolButton CreateNewReplButton ()
		{
			var button = new ToolButton (Gtk.Stock.Add);
			button.Clicked += (object sender, EventArgs e) =>
			{
				var view = AddRepl ();
				StartInteractiveSession (view);
			};
			return button;
		}

		private ReplView CurrentRepl ()
		{
			if (notebook.CurrentPageWidget != null)
			{
				return notebook.CurrentPageWidget as ReplView;
			}
			return null;
		}

		private ReplView AddRepl (string title = "REPL")
		{
			var repl = new ReplView ();
			repl.PromptString = "csharp> ";
			repl.PromptMultiLineString = "+ ";
			repl.ConsoleInput += OnViewConsoleInput;
			repl.SetFont (customFont);
			repl.ShadowType = Gtk.ShadowType.None;

			var tabLabel = new MDComponents.TabLabel (new Label (title), emptyImage);

			notebook.AppendPage (repl, tabLabel);
			if (notebook.NPages < 2)
				notebook.ShowTabs = false;
			else
				notebook.ShowTabs = true;
			tabLabel.CloseClicked += (object sender, EventArgs e) =>
			{
				Stop (repl);
			};
			notebook.ShowAll ();
			return repl;
		}

		public void Start (string platform = "AnyCPU")
		{
			var view = AddRepl ();
			view.ShowAll ();
			this.StartInteractiveSession (view, platform);
		}

		public void Stop (ReplView view)
		{
			if (view != null)
			{
				if (replSessions.ContainsKey (view))
				{
					var session = replSessions [view];
					if (session != null)
					{
						session.Stdout.Stop ();
						session.Stderr.Stop ();
						try
						{
							session.Process.Kill ();
						}
						catch (InvalidOperationException)
						{
						}
						session.Process.Close ();
						session.Process.Dispose ();
					}
				}
				if (notebook.Children.Contains (view))
					notebook.Remove (view);
			}
		}

		public void Stop ()
		{
			Stop (CurrentRepl ());
		}

		public void StopAllRepls ()
		{
			foreach (var widget in notebook.Children)
			{
				if (widget is ReplView)
				{
					Stop ((ReplView)widget);
				}
			}
		}

		void StartInteractiveSessionHandler (object sender, EventArgs e)
		{
			var view = notebook.CurrentPageWidget as ReplView;
			if (view == null)
				view = AddRepl ();
			this.StartInteractiveSession (view);
		}

		void ConnectToInteractiveSessionHandler (object sender, EventArgs e)
		{
			//ConnectToInteractiveSession ();
		}

		void StartInteractiveSession (ReplView view, string platform = "AnyCPU")
		{
			if (view == null)
			{
				throw new InvalidProgramException ("ReplView is null");
			}
			string exe_name;
			switch (platform.ToLower ())
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
					view.WriteOutput (String.Format ("Cannot start interactive session for platform {0}. Platform not supported.",
					                                 platform));
					return;
			}

			string bin_dir = Path.GetDirectoryName (Assembly.GetAssembly (typeof(ReplPad)).Location);
			string repl_exe = Path.Combine (bin_dir, exe_name);

			var port = nextPort++;
			var config = MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.Project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
			var start_info = new ProcessStartInfo (repl_exe, port.ToString ());
			start_info.UseShellExecute = false;
			start_info.CreateNoWindow = true;
			start_info.RedirectStandardError = true;
			start_info.RedirectStandardOutput = true;

			var proc = config.TargetRuntime.ExecuteAssembly (start_info);
			//_repl_process = Process.Start (start_info);

			var session = new ReplSession (view, proc, port);
			replSessions.Add (view, session);
			//Running = true;
			Thread.Sleep (1000); // Give _repl_process time to start up before we let anybody do anything with it
		}

		void HandleCustomOutputPadFontChanged (object sender, EventArgs e)
		{
			if (customFont != null)
			{
				customFont.Dispose ();
				customFont = null;
			}
			
			customFont = IdeApp.Preferences.CustomOutputPadFont;

			foreach (var page in notebook.Children)
			{
				var view = page as ReplView;
				if (view != null)
					view.SetFont (customFont);
			}

		}

		public void InputBlock (string block, string prefix_to_strip = "")
		{
			var view = notebook.CurrentPageWidget as ReplView;
			if (view != null)
				view.WriteInput (block, prefix_to_strip);
		}

		public void LoadReferences (DotNetProject project)
		{
			var view = ((ReplView)notebook.CurrentPageWidget);
			var session = replSessions [view];
			foreach (var x in project.References)
			{
				if (x.ReferenceType == ReferenceType.Assembly)
				{
					// Just a path to the reference, can be passed in no problem
					session.Repl.loadAssembly (x.Reference);
				}
				else
				if (x.ReferenceType == ReferenceType.Gac || x.ReferenceType == ReferenceType.Package)
				{
					// The fully-qualified name of the assembly, can be passed in no problem
					session.Repl.loadAssembly (x.Reference);
				}
				else
				if (x.ReferenceType == ReferenceType.Project)
				{
					DotNetProject inner_project = project.ParentSolution.FindProjectByName (x.Reference) as DotNetProject;
					if (inner_project != null)
					{
						var config = inner_project.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as DotNetProjectConfiguration;
						string file_name = config.CompiledOutputName.FullPath.ToString ();
						session.Repl.loadAssembly (file_name);
					}
					else
						view.WriteOutput (String.Format ("Cannot load non .NET project reference: {0}/{1}", project.Name, x.Reference));
				}
			}
		}

		void OnViewConsoleInput (object sender, ConsoleInputEventArgs e)
		{
			var view = sender as ReplView;
			if (view == null)
			{
				throw new InvalidCastException ("View doesn't seem to be a ReplView");
			}
			var session = replSessions [view];


			if (session == null)
			{
				view.WriteOutput ("Not connected.");
				view.Prompt (true);
				return;
			}
			else
			{
				session.Repl.evaluate (e.Text).ContinueWith (task =>
				{
					var result = task.Result;
					switch (result.Type)
					{
						case ResultType.FAILED:
							Gtk.Application.Invoke (delegate
							{
								view.WriteOutput (result.ResultMessage);
								view.Prompt (false);
							});
							break;
						case ResultType.NEED_MORE_INPUT:
							Gtk.Application.Invoke (delegate
							{
								view.Prompt (false, true);
							});
							break;
						case ResultType.SUCCESS_NO_OUTPUT:
							Gtk.Application.Invoke (delegate
							{
								view.Prompt (false);
							});
							break;
						case ResultType.SUCCESS_WITH_OUTPUT:
							Gtk.Application.Invoke (delegate
							{
								view.WriteOutput (result.ResultMessage);
								view.Prompt (true);
							});
							
							break;
						default:
							throw new Exception ("Unexpected state! Contact developers.");
							break;
					}
				});
			}
		}

		public void RedrawContent ()
		{
		}

		public Gtk.Widget Control
		{
			get
			{
				return content;
			}
			private set
			{
				content = value;
			}
		}

		public void Dispose ()
		{
			if (!disposed)
			{
				StopAllRepls ();

				IdeApp.Preferences.CustomOutputPadFontChanged -= HandleCustomOutputPadFontChanged;
				if (customFont != null)
					customFont.Dispose ();

				disposed = true;
			}
		}
	}
}