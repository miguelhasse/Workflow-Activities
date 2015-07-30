using System;
using System.Activities;
using System.IO;

namespace Hasseware.Activities.Statements
{
	public sealed class FileWriter : AsyncCodeActivity
	{
		[RequiredArgument]
		public InArgument<string> Source { get; set; }

		[RequiredArgument]
		public InArgument<string> Target { get; set; }

		protected override void CacheMetadata(CodeActivityMetadata metadata)
		{
			metadata.AddArgument(new RuntimeArgument("Source", typeof(string), ArgumentDirection.In, true));
			metadata.AddArgument(new RuntimeArgument("Target", typeof(string), ArgumentDirection.In, true));
		}

		protected override IAsyncResult BeginExecute(AsyncCodeActivityContext context, AsyncCallback callback, object state)
		{
			string filepath = this.Target.Get(context);

			if (!System.IO.Path.IsPathRooted(filepath))
				throw new ArgumentException("Supplied file path must be rooted.");

			if (String.IsNullOrWhiteSpace(System.IO.Path.GetExtension(filepath)))
				filepath = System.IO.Path.Combine(filepath, System.IO.Path.GetTempFileName());

			string folder = System.IO.Path.GetDirectoryName(filepath);
			if (!System.IO.Directory.Exists(folder)) System.IO.Directory.CreateDirectory(folder);


			FileStream file = File.Open(filepath, FileMode.Create);
			context.UserState = file;

			byte[] bytes = System.Text.Encoding.Default.GetBytes(this.Source.Get(context));
			return file.BeginWrite(bytes, 0, bytes.Length, callback, state);
		}

		protected override void EndExecute(AsyncCodeActivityContext context, IAsyncResult result)
		{
			FileStream file = (FileStream)context.UserState;
			try
			{
				file.EndWrite(result);
				file.Flush();
			}
			finally
			{
				file.Close();
			}
		}
	}
}
