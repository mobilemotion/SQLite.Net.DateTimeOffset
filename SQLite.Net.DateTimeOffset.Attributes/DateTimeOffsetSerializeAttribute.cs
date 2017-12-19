using System;

namespace SQLite.Net.DateTimeOffset.Attributes
{
	/// <summary>
	/// Attribute to be used on <see cref="System.DateTimeOffset"/> properties within SQLite data model classes.
	/// When applying the <code>SQLite.Net.DateTimeOffset.PostBuild.PostBuildTask</code> to an assembly, all
	/// properties flagged with this attribute within the assembly will be rebuilt to allow serialization to the
	/// SQLite database.
	/// May only be applied to properties of type <see cref="System.DateTimeOffset"/>, otherwise the PostBuildTask
	/// will fail!
	/// </summary>
	[AttributeUsage(AttributeTargets.Property)]
	public class DateTimeOffsetSerializeAttribute : Attribute
    {
	    public string Format { get; private set; }
	    public bool KeepOriginal { get; private set; }

	    public DateTimeOffsetSerializeAttribute() { }

	    public DateTimeOffsetSerializeAttribute(string format)
	    {
		    Format = format;
	    }

	    public DateTimeOffsetSerializeAttribute(bool keepOriginal)
	    {
		    KeepOriginal = keepOriginal;
	    }

	    public DateTimeOffsetSerializeAttribute(string format, bool keepOriginal)
	    {
			Format = format;
			KeepOriginal = keepOriginal;
	    }
    }
}
