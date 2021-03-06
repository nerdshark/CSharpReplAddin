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
using Gtk;

namespace MonoDevelop.CSharpRepl
{

	class StreamOutputter
	{
		string Name { get; set; }
		StreamReader Source { get; set; }
		ReplView Destination { get; set; }
		Thread _background_thread;

		public StreamOutputter(StreamReader source, ReplView destination, string name="")
		{
			this.Source = source;
			this.Destination = destination;
			this.Name = name;
		}

		public void Start()
		{
			_background_thread = new Thread(new ThreadStart(this.Run));
			_background_thread.Start();
		}

		public void Stop()
		{
			_background_thread.Abort();
		}

		private void Run()
		{
			while (true)
			{
				string tmp;
				try {
					tmp = this.Source.ReadLine();
				} catch (ThreadAbortException) {
					break;
				} catch (Exception e) {
					this.WriteOutput("Error reading " + this.Name + ", shutting down: " + e.Message);
					break;
				}

				if (tmp == null)
					break;

				if (tmp != "")
				{
					this.WriteOutput(tmp + Environment.NewLine);
				}
			}
		}

		private void WriteOutput(string content)
		{
			Gtk.Application.Invoke((x,y) => this.Destination.WriteOutput(content));
		}
	}

}