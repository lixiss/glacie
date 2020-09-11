using System;
using Xunit;

namespace Glacie.Data.Arz.Tests
{
    // TODO: (Low) (FieldApi Tests) - rename, and review.

    [Trait("Category", "ARZ")]
    public sealed class FieldApi : IDisposable
    {
        private readonly ArzDatabase _database;
        private ArzRecord Record0 { get; }
        private ArzRecord Record1 { get; }
        private ArzRecord Record2 { get; }

        public FieldApi()
        {
            _database = ArzDatabase.Open(TestData.GtdTqae2);
            Record0 = _database[TestData.GtdTqae2RawRecordNames[0]];
            Record1 = _database[TestData.GtdTqae2RawRecordNames[1]];
            Record2 = _database[TestData.GtdTqae2RawRecordNames[2]];
        }

        public void Dispose()
        {
            _database?.Dispose();
        }

        [Fact]
        public void Name()
        {
            var classField = Record1.Get("Class");
            Assert.Equal("Class", classField.Name);
        }

        [Fact]
        public void ValueType()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);

            var characterLifeField = Record1.Get("characterLife");
            Assert.Equal(ArzValueType.Real, characterLifeField.ValueType);

            var classField = Record1.Get("Class");
            Assert.Equal(ArzValueType.String, classField.ValueType);

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
        }

        [Fact]
        public void ValueCount()
        {
            var classField = Record1.Get("Class");
            Assert.Equal(1, classField.Count);

            var reclamationPointTiersField = Record1.Get("reclamationPointTiers");
            Assert.Equal(25, reclamationPointTiersField.Count);

            var playerTexturesField = Record1.Get("playerTextures");
            Assert.Equal(5, playerTexturesField.Count);

            var monsterAttackSpeedCapMinField = Record0.Get("monsterAttackSpeedCapMin");
            Assert.Equal(3, monsterAttackSpeedCapMinField.Count);
        }

        [Fact]
        public void GetFieldValue()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);
            Assert.Equal(500, defaultGoldField.Get<int>());

            var characterLifeField = Record1.Get("characterLife");
            Assert.Equal(ArzValueType.Real, characterLifeField.ValueType);
            Assert.Equal(300.0f, characterLifeField.Get<float>());

            var classField = Record1.Get("Class");
            Assert.Equal(ArzValueType.String, classField.ValueType);
            Assert.Equal("Player", classField.Get<string>());

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
            Assert.True(distressCallField.Get<bool>());
        }

        [Fact]
        public void GetFieldValueShouldThrowOnArray()
        {
            var reclamationPointTiersField = Record1.Get("reclamationPointTiers");
            var ex = Assert.Throws<ArzException>(() => reclamationPointTiersField.Get<int>());
            Assert.Equal("FieldNotSingleValue", ex.ErrorCode);

            var playerTexturesField = Record1.Get("playerTextures");
            ex = Assert.Throws<ArzException>(() => reclamationPointTiersField.Get<string>());
            Assert.Equal("FieldNotSingleValue", ex.ErrorCode);

            var monsterAttackSpeedCapMinField = Record0.Get("monsterAttackSpeedCapMin");
            ex = Assert.Throws<ArzException>(() => reclamationPointTiersField.Get<float>());
            Assert.Equal("FieldNotSingleValue", ex.ErrorCode);
        }

        [Fact]
        public void GetFieldValueShouldThrowOnTypeMismatch()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);

            var ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<float>());
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<bool>());
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<string>());
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<double>());
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
            ex = Assert.Throws<ArzException>(() => distressCallField.Get<int>());
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);
        }

        [Fact]
        public void GetFieldValueAt()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);
            Assert.Equal(500, defaultGoldField.Get<int>(0));

            var characterLifeField = Record1.Get("characterLife");
            Assert.Equal(ArzValueType.Real, characterLifeField.ValueType);
            Assert.Equal(300.0f, characterLifeField.Get<float>(0));

            var classField = Record1.Get("Class");
            Assert.Equal(ArzValueType.String, classField.ValueType);
            Assert.Equal("Player", classField.Get<string>(0));

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
            Assert.True(distressCallField.Get<bool>(0));
        }

        [Fact]
        public void GetFieldValueAtOutOfRange()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);
            Assert.Throws<ArgumentOutOfRangeException>(() => defaultGoldField.Get<int>(1));

            var characterLifeField = Record1.Get("characterLife");
            Assert.Equal(ArzValueType.Real, characterLifeField.ValueType);
            Assert.Throws<ArgumentOutOfRangeException>(() => characterLifeField.Get<float>(1));

            var classField = Record1.Get("Class");
            Assert.Equal(ArzValueType.String, classField.ValueType);
            Assert.Throws<ArgumentOutOfRangeException>(() => classField.Get<string>(1));

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
            Assert.Throws<ArgumentOutOfRangeException>(() => distressCallField.Get<bool>(1));
        }

        [Fact]
        public void GetFieldValueAtShouldThrowOnTypeMismatch()
        {
            var defaultGoldField = Record1.Get("defaultGold");
            Assert.Equal(ArzValueType.Integer, defaultGoldField.ValueType);

            var ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<float>(0));
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<bool>(0));
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<string>(0));
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            ex = Assert.Throws<ArzException>(() => defaultGoldField.Get<double>(0));
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);

            var distressCallField = Record1.Get("distressCall");
            Assert.Equal(ArzValueType.Boolean, distressCallField.ValueType);
            ex = Assert.Throws<ArzException>(() => distressCallField.Get<int>(0));
            Assert.Equal("FieldTypeMismatch", ex.ErrorCode);
        }

        [Fact]
        public void GetFieldValueAtArrays()
        {
            var reclamationPointTiersField = Record1.Get("reclamationPointTiers");
            for (var i = 0; i < 25; i++)
            {
                Assert.Equal(i, reclamationPointTiersField.Get<int>(i));
            }

            var playerTexturesField = Record1.Get("playerTextures");
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_White.tex", playerTexturesField.Get<string>(0));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Tan.tex", playerTexturesField.Get<string>(1));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_LightBlue.tex", playerTexturesField.Get<string>(2));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Gray.tex", playerTexturesField.Get<string>(3));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Rose.tex", playerTexturesField.Get<string>(4));

            var monsterAttackSpeedCapMinField = Record0.Get("monsterAttackSpeedCapMin");
            Assert.Equal(20.0f, monsterAttackSpeedCapMinField.Get<float>(0));
            Assert.Equal(30.0f, monsterAttackSpeedCapMinField.Get<float>(1));
            Assert.Equal(40.0f, monsterAttackSpeedCapMinField.Get<float>(2));
        }

        [Fact]
        public void GetVariant()
        {
            var defaultGoldValue = Record1["defaultGold"];
            Assert.Equal(VariantType.Integer, defaultGoldValue.Type);
            Assert.Equal(500, defaultGoldValue.Get<int>());

            var characterLifeValue = Record1["characterLife"];
            Assert.Equal(VariantType.Real, characterLifeValue.Type);
            Assert.Equal(300.0f, characterLifeValue.Get<float>());

            var classValue = Record1["Class"];
            Assert.Equal(VariantType.String, classValue.Type);
            Assert.Equal("Player", classValue.Get<string>());

            var distressCallValue = Record1["distressCall"];
            Assert.Equal(VariantType.Boolean, distressCallValue.Type);
            Assert.True(distressCallValue.Get<bool>());

            var reclamationPointTiersValue = Record1["reclamationPointTiers"];
            Assert.Equal(VariantType.IntegerArray, reclamationPointTiersValue.Type);
            Assert.Equal(25, reclamationPointTiersValue.Count);
            for (var i = 0; i < 25; i++)
            {
                Assert.Equal(i, reclamationPointTiersValue.Get<int>(i));
            }

            var playerTexturesValue = Record1["playerTextures"];
            Assert.Equal(VariantType.StringArray, playerTexturesValue.Type);
            Assert.Equal(5, playerTexturesValue.Count);
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_White.tex", playerTexturesValue.Get<string>(0));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Tan.tex", playerTexturesValue.Get<string>(1));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_LightBlue.tex", playerTexturesValue.Get<string>(2));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Gray.tex", playerTexturesValue.Get<string>(3));
            Assert.Equal(@"Creatures\PC\Female\FemalePC01_Rose.tex", playerTexturesValue.Get<string>(4));

            var monsterAttackSpeedCapMinValue = Record0["monsterAttackSpeedCapMin"];
            Assert.Equal(VariantType.RealArray, monsterAttackSpeedCapMinValue.Type);
            Assert.Equal(3, monsterAttackSpeedCapMinValue.Count);
            Assert.Equal(20.0f, monsterAttackSpeedCapMinValue.Get<float>(0));
            Assert.Equal(30.0f, monsterAttackSpeedCapMinValue.Get<float>(1));
            Assert.Equal(40.0f, monsterAttackSpeedCapMinValue.Get<float>(2));
        }
    }
}
