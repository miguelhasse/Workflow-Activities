using System;
using System.Activities;
using System.IO;
using System.Security.Cryptography;

namespace ZON.Activities.Statements
{
	public sealed class FileChecksum : CodeActivity<string>
	{
		[RequiredArgument]
		public InArgument<string> Source { get; set; }

		protected override string Execute(CodeActivityContext context)
		{
			string filename = context.GetValue(this.Source);
			return GetChecksumBuffered(filename);
		}

		private static string GetChecksumBuffered(string filePath)
		{
			using (var fileStm = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				using (var stream = new BufferedStream(fileStm, 1024 * 64))
				{
					using (var ha = MD5CryptoServiceProvider.Create())
					{
						byte[] checksum = ha.ComputeHash(stream);
						return BitConverter.ToString(checksum).Replace("-", String.Empty);
					}
				}
			}
		}
	}
}
