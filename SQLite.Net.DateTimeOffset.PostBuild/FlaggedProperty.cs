using Mono.Cecil;

namespace SQLite.Net.DateTimeOffset.PostBuild
{
	/// <summary>
	/// Represents a property that must be rebuilt to fulfil the requirements for being serialized to
	/// an SQLite database.
	/// </summary>
	internal struct FlaggedProperty
	{
		/// <summary>
		/// The property itself
		/// </summary>
		internal PropertyDefinition Property { get; set; }

		/// <summary>
		/// The DateTimeOffset format to be used (default is yyyy-MM-dd HH:mm:ss zzzz)
		/// </summary>
		internal string Format { get; set; }

		/// <summary>
		/// Flag that indicates whether the original DateTimeOffset property shall be stored as numeric
		/// value (representing ticks in UTC timezone) in addition to the string representation
		/// </summary>
		internal bool KeepOriginal { get; set; }
	}
}
