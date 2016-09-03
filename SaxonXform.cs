/* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Copyright (C) 2016 Cameron Angus. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Saxon.Api;

namespace KantanDocGen
{
	public class SaxonXform : DocXform
	{
		XsltTransformer CompiledXform;

		public override bool Initialize(string XsltPath, DataReceivedEventHandler OutputHandler)
		{
			var xslt = new FileInfo(XsltPath);

			// Compile stylesheet
			var processor = new Processor();
			var compiler = processor.NewXsltCompiler();
			var executable = compiler.Compile(new Uri(xslt.FullName));
			CompiledXform = executable.Load();

			return true;
		}

		public override bool TransformXml(string SourceXmlPath, string OutputPath)
		{
			var input = new FileInfo(SourceXmlPath);
			var output = new FileInfo(OutputPath);

			DomDestination destination = new DomDestination();
			try
			{
				// Do transformation to a destination				
				using (var inputStream = input.OpenRead())
				{
					CompiledXform.SetInputStream(inputStream, new Uri(input.DirectoryName));
					CompiledXform.Run(destination);
				}
			}
			catch(Exception Ex)
			{
				Console.WriteLine(Ex.ToString() + "\n" + Ex.StackTrace);
				return false;
			}

			// Save result to file
			destination.XmlDocument.Save(output.FullName);
			return true;
		}
	}
}
