using System.IO;

namespace CSHTML5.Tools.StubMerger
{
	public class ClassPart
	{
		public Namespace Namespace { get; }
		public string FullPath { get; }
		public string FileName { get; }
		public string Name { get; }
		public bool IsStub { get; }

		public ClassPart(Namespace @namespace, string path, bool isStub)
		{
			Namespace = @namespace;
			FullPath = path;
			FileName = Path.GetFileName(FullPath);
			Name = Path.GetFileNameWithoutExtension(FileName);
			IsStub = isStub;
		}
	}
}