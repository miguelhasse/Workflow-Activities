using System;
using System.ComponentModel;

namespace Hasseware.Activities.Statements
{
	[Flags]
	public enum FileCopyOptions
	{
		None = 0x0,
		[Description("Fail if destination exists")]
		FailIfDestinationExists = 0x00000001, // Operation fails immediately if the target file already exists.
		[Description("Restartable")]
		Restartable = 0x00000002, //  This can significantly slow down the copy operation as the new file may be flushed multiple times during the copy operation.
		[Description("Allow decrypted destination")]
		AllowDecryptedDestination = 0x00000008, // Attempt to copy an encrypted file will succeed even if the destination copy cannot be encrypted.
		[Description("Keep symbolic link")]
		SymbolicLink = 0x00000800, // If the source file is a symbolic link, the destination file is also a symbolic link.
		[Description("Unbuffered operation")]
		Unbuffered = 0x00001000, // Recommended for very large file transfers.
	}
}
