using System;
using SQLite.Net.DateTimeOffset.Attributes;

namespace SQLite.Net.DateTimeOffset.Test.Models
{
    [Table("TestItems")]
    public class TestItem
    {
        private static readonly System.DateTimeOffset _testDateTimeOffset = new System.DateTimeOffset(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified), TimeSpan.FromHours(5));

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [DateTimeOffsetSerialize]
        public System.DateTimeOffset Test { get; set; } = _testDateTimeOffset;

        [DateTimeOffsetSerialize("yyyy-MM-dd, HH:mm:ss", true)]
        public System.DateTimeOffset Test_KeepOriginal_CustomFormat { get; set; } = _testDateTimeOffset;

        [DateTimeOffsetSerialize]
        [Ignore]
        public System.DateTimeOffset Test_Ignore { get; set; } = _testDateTimeOffset;

        [DateTimeOffsetSerialize]
        [Column("Specialname1")]
        public System.DateTimeOffset Test_Columnname { get; set; } = _testDateTimeOffset;

        [DateTimeOffsetSerialize(true)]
        [Column("Specialname2")]
        public System.DateTimeOffset Test_KeepOriginal_Columnname { get; set; } = _testDateTimeOffset;
    }
}
