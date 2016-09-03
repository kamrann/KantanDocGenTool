/* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Copyright (C) 2016 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace KantanDocGen
{
	public class MsxslXform : DocXform
	{
		string MsxslExePath;
		string XsltFilePath;
		DataReceivedEventHandler DataOutputHandler;

		public MsxslXform(string ExePath)
		{
			MsxslExePath = ExePath;
		}

		public override bool Initialize(string XsltPath, DataReceivedEventHandler OutputHandler)
		{
			XsltFilePath = XsltPath;
			DataOutputHandler = OutputHandler;
			return true;
		}

		public override bool TransformXml(string SourceXmlPath, string OutputPath)
		{
			// Pass the xml files through a transformation to generate html docs.
			string XslBaseArguments = " \"" + XsltFilePath + "\" -o ";

			using (Process XslProcess = new Process())
			{
				//XslProcess.StartInfo.WorkingDirectory = EngineDir;
				XslProcess.StartInfo.FileName = MsxslExePath;
				XslProcess.StartInfo.Arguments = "\"" + SourceXmlPath + "\"" + XslBaseArguments + "\"" + OutputPath + "\"";
				XslProcess.StartInfo.UseShellExecute = false;
				XslProcess.StartInfo.RedirectStandardOutput = true;
				XslProcess.StartInfo.RedirectStandardError = true;

				Console.WriteLine("XslProcessor path is: " + XslProcess.StartInfo.FileName);
				Console.WriteLine("Arguments: " + XslProcess.StartInfo.Arguments);

				XslProcess.OutputDataReceived += new DataReceivedEventHandler(DataOutputHandler);
				XslProcess.ErrorDataReceived += new DataReceivedEventHandler(DataOutputHandler);

				try
				{
					XslProcess.Start();
					XslProcess.BeginOutputReadLine();
					XslProcess.BeginErrorReadLine();
					XslProcess.WaitForExit();
					if (XslProcess.ExitCode != 0)
					{
						Console.WriteLine("Error: Xsl transformation failed, aborting.");
						return false;
					}
				}
				catch (Exception Ex)
				{
					Console.WriteLine(Ex.ToString() + "\n" + Ex.StackTrace);
					return false;
				}
			}

			return true;
		}
	}
}
