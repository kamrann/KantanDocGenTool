/* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

// Copyright (C) 2016 Cameron Angus. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.Diagnostics;

namespace KantanDocGen
{
	class Program
	{
		static string ParseArgumentValue(List<string> ArgumentList, string Prefix, string DefaultValue)
		{
			for (int Idx = 0; Idx < ArgumentList.Count; Idx++)
			{
				if (ArgumentList[Idx].StartsWith(Prefix))
				{
					string Value = ArgumentList[Idx].Substring(Prefix.Length);
					ArgumentList.RemoveAt(Idx);
					return Value;
				}
			}
			return DefaultValue;
		}

		static string ParseArgumentPath(List<string> ArgumentList, string Prefix, string DefaultValue)
		{
			string Value = ParseArgumentValue(ArgumentList, Prefix, DefaultValue);
			if (Value != null)
			{
				Value = Path.GetFullPath(Value);
			}
			return Value;
		}

		static string ParseArgumentDirectory(List<string> ArgumentList, string Prefix, string DefaultValue)
		{
			string Value = ParseArgumentPath(ArgumentList, Prefix, DefaultValue);
			if (Value != null && !Directory.Exists(Value))
			{
				Directory.CreateDirectory(Value);
			}
			return Value;
		}

		static private void ProcessOutputReceived(Object Sender, DataReceivedEventArgs Line)
		{
			if (Line.Data != null && Line.Data.Length > 0)
			{
				Console.WriteLine(Line.Data);
			}
		}

		public static void SafeCreateDirectory(string Path)
		{
			if (!Directory.Exists(Path))
			{
				Directory.CreateDirectory(Path);
			}
		}

		private static void CopyWholeDirectory(string SourceDir, string DestDir)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(SourceDir);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ SourceDir);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(DestDir))
			{
				Directory.CreateDirectory(DestDir);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(DestDir, file.Name);
				file.CopyTo(temppath, true);
			}

			foreach (DirectoryInfo subdir in dirs)
			{
				string temppath = Path.Combine(DestDir, subdir.Name);
				CopyWholeDirectory(subdir.FullName, temppath);
			}
		}

		// @NOTE: Currently unused, seemingly no way to use Slate for the node rendering when running commandlet.
		// Instead this tool is now invoked by a plugin.
		static bool RunXmlDocGenCommandlet(string EngineDir, string EditorPath, string OutputDir)
		{
			// Create the output directory
			SafeCreateDirectory(OutputDir);

			string Arguments = "-run=KantanDocs -path=" + OutputDir + " -name=BlueprintAPI -stdout -FORCELOGFLUSH -CrashForUAT -unattended -AllowStdOutLogVerbosity";
			Console.WriteLine("Running: {0} {1}", EditorPath, Arguments);

			using (Process NewProcess = new Process())
			{
				NewProcess.StartInfo.WorkingDirectory = EngineDir;
				NewProcess.StartInfo.FileName = EditorPath;
				NewProcess.StartInfo.Arguments = Arguments;
				NewProcess.StartInfo.UseShellExecute = false;
				NewProcess.StartInfo.RedirectStandardOutput = true;
				NewProcess.StartInfo.RedirectStandardError = true;

				NewProcess.OutputDataReceived += new DataReceivedEventHandler(ProcessOutputReceived);
				NewProcess.ErrorDataReceived += new DataReceivedEventHandler(ProcessOutputReceived);

				try
				{
					NewProcess.Start();
					NewProcess.BeginOutputReadLine();
					NewProcess.BeginErrorReadLine();
					NewProcess.WaitForExit();
					if (NewProcess.ExitCode != 0)
					{
						Console.WriteLine("Error: Xml doc generation commandlet failed, aborting.\nIs the plugin installed?");
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

		static void Main(string[] args)
		{
			List<string> ArgumentList = new List<string>(args);

			Console.WriteLine("KantanDocGen invoked with arguments:");
			foreach (string Arg in ArgumentList)
			{
				Console.WriteLine(Arg);
			}

			string DocsTitle = ParseArgumentValue(ArgumentList, "-name=", null);
			if(DocsTitle == null)
			{
				Console.WriteLine("KantanDocGen: Error: Documentation title (-name=) required. Aborting.");
				return;
			}

			// Get the default paths

			// If unspecified, assume the directory containing our binary is one level below the base directory
			string DocGenBaseDir = ParseArgumentDirectory(ArgumentList, "-basedir=", Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), ".."));
			string OutputRoot = ParseArgumentDirectory(ArgumentList, "-outputdir=", Directory.GetCurrentDirectory());
			string OutputDir = Path.Combine(OutputRoot, DocsTitle);

			//string MsxslPath = ParseArgumentPath(ArgumentList, "-xslproc=", Path.Combine(EngineDir, "Binaries/ThirdParty/Msxsl/msxsl.exe"));

			// Xsl transform files - if not specified explicitly, look for defaults relative to base directory
			string IndexTransformPath = ParseArgumentPath(ArgumentList, "-indexxsl=", Path.Combine(DocGenBaseDir, "xslt/index_xform.xsl"));
			string ClassTransformPath = ParseArgumentPath(ArgumentList, "-classxsl=", Path.Combine(DocGenBaseDir, "xslt/class_docs_xform.xsl"));
			string NodeTransformPath = ParseArgumentPath(ArgumentList, "-nodexsl=", Path.Combine(DocGenBaseDir, "xslt/node_docs_xform.xsl"));

			bool bFromIntermediate = ArgumentList.Contains("-fromintermediate");
			string IntermediateDir;
			if (bFromIntermediate)
			{
				// Intermediate docs already created, we need to have been passed an intermediate directory to locate them
				IntermediateDir = ParseArgumentDirectory(ArgumentList, "-intermediatedir=", null);
				if (IntermediateDir == null)
				{
					Console.WriteLine("KantanDocGen: Error: -fromintermediate requires -intermediatedir to be set. Aborting.");
					return;
				}

				if(!Directory.Exists(IntermediateDir))
				{
					Console.WriteLine("KantanDocGen: Error: Specified intermediate directory not found. Aborting.");
					return;
				}
			}
			else
			{
				// @TODO: This doesn't work, since commandlet cannot create Slate windows!
				// Can reenable this path if manage to get a Program target type to build against the engine.
				Console.WriteLine("KantanDocGen: Error: Calling without -fromintermediate currently not supported. Use the KantanDocGen engine plugin to generate documentation.");
				return;

/*				IntermediateDir = ParseArgumentDirectory(ArgumentList, "-intermediatedir=", Path.Combine(EngineDir, "Intermediate\\KantanDocGen"));

				// Need to generate intermediate docs first
				// Run editor commandlet to generate XML and image files
				string EditorPath = Path.Combine(EngineDir, "Binaries\\Win64\\UE4Editor-Cmd.exe");
				if (!RunXmlDocGenCommandlet(EngineDir, EditorPath, IntermediateDir))
				{
					return;
				}
*/			}

			const bool bCleanOutput = true;
			bool bHardClean = ArgumentList.Contains("-cleanoutput");
			if (bCleanOutput)
			{
				// If the output directory exists, attempt to delete it (this will fail if bHardClean is false and the directory contains files/subfolders)
				if (Directory.Exists(OutputDir))
				{
					try
					{
						Directory.Delete(OutputDir, bHardClean);
					}
					catch(Exception)
					{
						Console.WriteLine("KantanDocGen: Error: Output directory '{0}' exists and not empty/couldn't delete. Remove and rerun, or specify -cleanoutput (If running from plugin console, add 'clean' parameter).", OutputDir);
						return;
					}
				}
			}

			//var XslXform = new MsxslXform(MsxslPath);
			var IndexXform = new SaxonXform();
			var ClassXform = new SaxonXform();
			var NodeXform = new SaxonXform();

			// Initialize the transformations
			if (!IndexXform.Initialize(IndexTransformPath, ProcessOutputReceived))
			{
				Console.WriteLine("Error: Failed to initialize xslt processor.");
				return;
			}
			if (!ClassXform.Initialize(ClassTransformPath, ProcessOutputReceived))
			{
				Console.WriteLine("Error: Failed to initialize xslt processor.");
				return;
			}
			if (!NodeXform.Initialize(NodeTransformPath, ProcessOutputReceived))
			{
				Console.WriteLine("Error: Failed to initialize xslt processor.");
				return;
			}

			// Loop over all generated xml files and apply the transformation
			int Success = 0;
			int Failed = 0;
			// @TODO: Should iterate over index/class xml entries rather than enumerate files and directories
			var SubFolders = Directory.EnumerateDirectories(IntermediateDir);
			foreach (string Sub in SubFolders)
			{
				string ClassTitle = Path.GetFileName(Sub);
				string OutputClassDir = Path.Combine(OutputDir, ClassTitle);
				SafeCreateDirectory(OutputClassDir);
				string NodeDir = Path.Combine(Sub, "nodes");
				if (Directory.Exists(NodeDir))
				{
					string OutputNodesDir = Path.Combine(OutputClassDir, "nodes");
					SafeCreateDirectory(OutputNodesDir);

					var InputFiles = Directory.EnumerateFiles(NodeDir, "*.xml", SearchOption.TopDirectoryOnly);
					foreach (string FilePath in InputFiles)
					{
						string FileTitle = Path.GetFileNameWithoutExtension(FilePath);
						string OutputPath = Path.Combine(OutputNodesDir, FileTitle + ".html");

						string InputPath = FilePath;
						if (!NodeXform.TransformXml(InputPath, OutputPath))
						{
							Console.WriteLine("Error: Xsl transform failed for file {0} - skipping.", InputPath);
							++Failed;
							continue;
						}

						++Success;
					}

					string OutputClassPath = Path.Combine(OutputClassDir, ClassTitle + ".html");
					ClassXform.TransformXml(Path.Combine(Sub, ClassTitle + ".xml"), OutputClassPath);
				}

				// Copy the images for this class to the output directory
				CopyWholeDirectory(Path.Combine(Sub, "img"), Path.Combine(OutputClassDir, "img"));
			}

			string OutputIndexPath = Path.Combine(OutputDir, "index.html");
			IndexXform.TransformXml(Path.Combine(IntermediateDir, "index.xml"), OutputIndexPath);

			CopyWholeDirectory(Path.Combine(DocGenBaseDir, "css"), Path.Combine(OutputDir, "css"));

			Console.WriteLine("KantanDocGen completed:");
			Console.WriteLine("{0} node docs successfully transformed.", Success);
			Console.WriteLine("{0} failed.", Failed);
		}
	}
}

