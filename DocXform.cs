using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace KantanDocGen
{
	public abstract class DocXform
	{
		public abstract bool Initialize(string XsltPath, DataReceivedEventHandler OutputHandler);
		public abstract bool TransformXml(string SourceXmlPath, string OutputPath);
	}
}
