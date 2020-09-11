using System;
using System.Collections.Generic;
using System.Text;

using Glacie.Data.Dbr;

using Xunit;
using Xunit.Abstractions;

namespace Glacie
{
    [Trait("Category", "DBR")]
    public sealed class DbrFieldReaderTests
    {
        private readonly ITestOutputHelper Output;

        public DbrFieldReaderTests(ITestOutputHelper output)
        {
            Output = output;
        }

        [Fact]
        public void EmptyText()
        {
            Assert.Equal(0, CountFields(""));
            Assert.Equal(0, CountFields(" "));
            Assert.Equal(0, CountFields("\r\t\n "));
        }

        [Fact]
        public void FieldNameParsing()
        {
            var text = @"templateName,someValue,
  templateName,someValue,
templateName  ,someValue,
  templateName  ,someValue,";

            using var reader = new DbrFieldReader(text);

            for (var i = 0; i < 4; i++)
            {
                Assert.True(reader.Read());
                Assert.Equal("templateName", reader.Name);
            }

            Assert.False(reader.Read());
        }

        [Fact]
        public void FieldNameWithSpacesParsing()
        {
            var text = @" Field  Name With   Spaces ,someValue,";

            using var reader = new DbrFieldReader(text);

            Assert.True(reader.Read());
            Assert.Equal("Field  Name With   Spaces", reader.Name);

            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData(",someValue,")]
        [InlineData("  ,someValue,")]
        public void EmptyFieldName(string text)
        {
            using var reader = new DbrFieldReader(text);
            Assert.Throws<DbrParseErrorException>(() => reader.Read());
        }

        [Theory]
        [InlineData(@" Field  Name With   Spaces ")]
        [InlineData(@" Field  Name With   Spaces")]
        public void UnexpectedEofInFieldName(string text)
        {
            using var reader = new DbrFieldReader(text);
            Assert.Throws<DbrParseErrorException>(() => reader.Read());
        }

        [Theory]
        [InlineData("someField,value0,", 1, "value")]
        [InlineData("someField, value0,", 1, "value")]
        [InlineData("someField,value0 ,", 1, "value")]
        [InlineData("someField, value0 ,", 1, "value")]
        [InlineData("someField, \r\nvalue0,", 1, "value")]
        [InlineData("someField,value0\r\n ,", 1, "value")]
        [InlineData("someField, \r\nvalue0 ,", 1, "value")]
        [InlineData("someField,val ue0,", 1, "val ue")]
        [InlineData("someField, val ue0,", 1, "val ue")]
        [InlineData("someField,val ue0 ,", 1, "val ue")]
        [InlineData("someField, val ue0 ,", 1, "val ue")]
        [InlineData("someField, value0;value1,", 2, "value")]
        [InlineData("someField,value0 ;value1,", 2, "value")]
        [InlineData("someField, value0 ;value1,", 2, "value")]
        [InlineData("someField, value0;\r\nvalue1,", 2, "value")]
        public void ValueParsing(string text, int valueCount, string valuePrefix)
        {
            using var reader = new DbrFieldReader(text);
            Assert.True(reader.Read());
            Assert.Equal("someField", reader.Name);
            Assert.Equal(valueCount, reader.ValueCount);
            for (var i = 0; i < valueCount; i++)
            {
                Assert.Equal(valuePrefix + i, reader.GetStringValue(i));
            }

            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("someField,,", 1)]
        [InlineData("someField, ,", 1)]
        [InlineData("someField,;;,", 3)]
        [InlineData("someField,  ; ;  ,", 3)]
        public void EmptyValueParsing(string text, int valueCount)
        {
            using var reader = new DbrFieldReader(text);
            Assert.True(reader.Read());
            Assert.Equal("someField", reader.Name);
            Assert.Equal(valueCount, reader.ValueCount);
            for (var i = 0; i < valueCount; i++)
            {
                Assert.Equal("", reader.GetStringValue(i));
            }
            Assert.False(reader.Read());
        }

        [Theory]
        [InlineData("someField,someValue\r\n")]
        [InlineData("someField,someValue\r\n;")]
        public void UnexpectedEofInValue(string text)
        {
            using var reader = new DbrFieldReader(text);
            var e = Assert.Throws<DbrParseErrorException>(() => reader.Read());
            Output.WriteLine(e.ToString());
        }

        [Fact]
        public void MultipleFields()
        {
            var text = @"templateName,someTemplate,
someField1,someValue,";

            using var reader = new DbrFieldReader(text);
            Assert.True(reader.Read());
            Assert.Equal("templateName", reader.Name);
            Assert.Equal(1, reader.ValueCount);
            Assert.Equal("someTemplate", reader.GetStringValue(0));

            Assert.True(reader.Read());
            Assert.Equal("someField1", reader.Name);
            Assert.Equal(1, reader.ValueCount);
            Assert.Equal("someValue", reader.GetStringValue(0));

            Assert.False(reader.Read());
            Assert.Throws<InvalidOperationException>(() => reader.Name);
            Assert.Equal(0, reader.ValueCount);
            Assert.Throws<InvalidOperationException>(() => reader.GetStringValue(0));
        }

        [Theory]
        [InlineData("someField1,someValue,\r\ntemplateName,someTemplate,\r\nsomeField2,someValue,")]
        [InlineData("someField1,someValue,\r\nsomeField2,someValue,\r\ntemplateName,someTemplate,")]
        public void TemplateNameAlwaysReportedFirst(string text)
        {
            using var reader = new DbrFieldReader(text);
            Assert.True(reader.Read());
            Assert.Equal("templateName", reader.Name);
            Assert.Equal(1, reader.ValueCount);
            Assert.Equal("someTemplate", reader.GetStringValue(0));

            Assert.True(reader.Read());
            Assert.Equal("someField1", reader.Name);
            Assert.Equal(1, reader.ValueCount);
            Assert.Equal("someValue", reader.GetStringValue(0));

            Assert.True(reader.Read());
            Assert.Equal("someField2", reader.Name);
            Assert.Equal(1, reader.ValueCount);
            Assert.Equal("someValue", reader.GetStringValue(0));

            Assert.False(reader.Read());
        }

        private int CountFields(string text)
        {
            var fieldCount = 0;

            using var reader = new DbrFieldReader(text);
            while (reader.Read())
            {
                fieldCount++;
            }

            return fieldCount;
        }
    }
}
