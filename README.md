# SQLite.Net.DateTimeOffset

Adds functionality for storing `DateTimeOffset` properties in SQLite databases, using the SQLite.Net-PCL library.

The library consists of a .NET attribute to be used for flagging properties in data model classes, and a post-build task that registers itself automatically to the project when installed as NuGet package. The post-build task decompiles the assembly after build has finished, scans all classes within the assembly to find all properties of type `DateTimeOffset` that are flagged with the `[SQLite.Net.DateTimeOffset.Attributes.DateTimeOffsetSerialize]` attribute, and inserts a duplicate property of type `string` that contains the original property's serialized value, and will be used as SQLite field.

## Usage

### Simplest example

```csharp
[Table("Entities")]
public class Entity
{
	[DateTimeOffsetSerialize]
	public DateTimeOffset Column { get; set; }
}
```

This sample code will produce a table with one column of type text, containing the `Column` property's content in format `yyyy-MM-dd HH:mm:ss zzzz`.

### Specifying column name

```csharp
[Table("Entities")]
public class Entity
{
	[DateTimeOffsetSerialize]
	[Column("DateTimeOffsetColumn")]
	public DateTimeOffset Column { get; set; }
}
```

To specify the target column name, just flag the original `DateTimeOffset` property with the `SQLite.Net.Attributes.Column` attribute as usual, the column name will be applied to the auto-generated text column.

### Specifying serialization format

```csharp
[Table("Entities")]
public class Entity
{
	[DateTimeOffsetSerialize("dd.MM.yyyy HH:mm:ss zzzz")]
	public DateTimeOffset Column { get; set; }
}
```

To store the `DateTimeOffset` value using a certain string format, just pass the desired format as parameter to the `DateTimeOffsetSerialize` attribute.

### Store value as `DateTime` and `string`

```csharp
[Table("Entities")]
public class Entity
{
	[DateTimeOffsetSerialize(keepOriginal: true)]
	public DateTimeOffset Column { get; set; }
}
```

By specifying the `DateTimeOffsetSerialize` attribute's `keepOriginal` constructor parameter, it is possible to ensure that *two* columns will be generated: One containing the `DateTimeOffset` value as text (as described above), and one containing the original `DateTimeOffset` value converted to UTC as number representing ticks. This can be useful, if the `DateTimeOffset` value shall be used for database queries (e.g., sorting by date), which would not be possible with only the text column.

### Combinations

```csharp
[Table("Entities")]
public class Entity
{
	[DateTimeOffsetSerialize("dd.MM.yyyy HH:mm:ss zzzz", true)]
	[Column("DateTimeOffsetColumn")]
	public DateTimeOffset Column { get; set; }
}
```

In general, all attribute options can be combined. If specifying the column name using the `SQLite.Net.Attributes.Column` attribute, *and* using the `keepOriginal` parameter, the given column name will be applied to the UTC `DateTime` column, and the text column will be assigned the same name with the suffix *_Serialized*.