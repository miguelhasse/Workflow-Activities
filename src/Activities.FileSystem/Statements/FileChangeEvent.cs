using System;
using System.Runtime.Serialization;

namespace Hasseware.Activities.Statements
{
	[DataContract]
	public class FileChangeEvent
	{
		/// <summary>
		/// Gets the type of directory event that occurred.
		/// </summary>
		[DataMember(EmitDefaultValue = false)]
		public string ChangeType { get; internal set; }

		/// <summary>
		/// Gets the fully qualifed path of the affected file or directory.
		/// </summary>
		[DataMember(EmitDefaultValue = false)]
		public string FullPath { get; internal set; }

		/// <summary>
		/// Gets the name of the affected file or directory.
		/// </summary>
		[DataMember(EmitDefaultValue = false)]
		public string Name { get; internal set; }

		/// <summary>
		/// Gets the previous fully qualified path of the affected file or directory.
		/// </summary>
		[DataMember(EmitDefaultValue = false)]
		public string OldPath { get; internal set; }

		/// <summary>
		/// Gets the time when the event that occurred.
		/// </summary>
		[DataMember(EmitDefaultValue = false)]
		public DateTime ChangeTime { get; internal set; }
	}
}
