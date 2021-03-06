﻿namespace UnitTest.Aliases
{
    using System;
    using System.Collections.Generic;
    using Bond;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Linq;

    public static class BondTypeAliasConverter
    {
        public static decimal Convert(ArraySegment<byte> value, decimal unused)
        {
            var bits = new int[value.Count / sizeof(int)];
            Buffer.BlockCopy(value.Array, value.Offset, bits, 0, bits.Length * sizeof(int));
            return new decimal(bits);
        }

        public static ArraySegment<byte> Convert(decimal value, ArraySegment<byte> unused)
        {
            var bits = decimal.GetBits(value);
            var data = new byte[bits.Length * sizeof(int)];
            Buffer.BlockCopy(bits, 0, data, 0, data.Length);
            return new ArraySegment<byte>(data);
        }
    }

    [TestClass]
    public class TypeAliasTests
    {
        [TestMethod]
        public void GenericTypeAlias()
        {
            var from = UnitTest.Random.Init<GenericAlias>();
            TestTypeAliases(from);

            from = new GenericAlias
            {
                bar = new Alias.EnumString<Bar>(Bar.One)
            };

            TestTypeAliases(from);
        }
        
        [TestMethod]
        public void AliasContainer()
        {
            var from = UnitTest.Random.Init<ContainerAlias>();
            TestTypeAliases<ContainerAlias, ContainerNotAliased>(from);
        }

        [TestMethod]
        public void ArrayResize()
        {
            // Generate an array so that it has to be resized twice when we
            // deserialize it with the SimpleXmlParser.
            //
            // More particularly, we generate an array that's beyond the hard
            // limit of 64 items. The SimpleXmlParser is streaming, so it will
            // tell the DeserializerTransform that the size is 0. So, when we
            // attempt to add an element to the array, it will immediately
            // resize the array to have 64 spots. Then, when we reach 64
            // elements, we resize the array to be 128. NB, the SimpleXmlParser
            // is called in the extensive TestTypeAlias suite, which will
            // attempt to transcode.

            var seed = (int)DateTime.Now.ToBinary();
            var r = new System.Random(seed);
            System.Diagnostics.Trace.TraceInformation("Random seed {0}", seed);

            var target = new bool[66];
            foreach (var i in Enumerable.Range(0, 66))
            {
                target[i] = r.Next(0, 2) == 1;
            }

            var data = UnitTest.Random.Init<ContainerAlias>();
            data.arrayContainer = target;

            TestTypeAliases<ContainerAlias, ContainerNotAliased>(data);
        }

        [TestMethod]
        public void AliasBlob()
        {
            var from = InitBlobAlias();

            TestTypeAliases<BlobAlias, BlobNotAliased>(from);
        }

        [TestMethod]
        public void AliasesInField()
        {
            var from = new FieldOfStructWithAliases
            {
                b = InitBlobAlias()
            };

            TestTypeAliases(from);
        }

        [TestMethod]
        public void AliasesInContainer()
        {
            var from = new ContainerOfStructWithAliases
            {
                m = {{"foo", new List<BlobAlias> {InitBlobAlias()}}}
            };
            TestTypeAliases(from);
        }

        [TestMethod]
        public void AliasesInBase()
        {
            var from = new BaseWithAliases
            {
                x = decimal.One / 3m,
                y = { 100000000000000000m, -9999999999999999999999m },
                z = 79228162514264337593543950335m
            };
            TestTypeAliases(from);
        }

        [TestMethod]
        public void AliasesInNested()
        {
            var from = new NestedWithAliases
            {
                f = new FieldOfStructWithAliases
                {
                    b = InitBlobAlias()
                }
            };
            TestTypeAliases(from);
        }

        static BlobAlias InitBlobAlias()
        {
            return new BlobAlias
            {
                x = decimal.One / 3m,
                y = { 100000000000000000m, -9999999999999999999999m },
                z = 79228162514264337593543950335m
            };
        }

        static void TestTypeAliases<T>(T from) where T : class
        {
            TestTypeAliases<T, T>(from);
        }

        static void TestTypeAliases<T, U>(T from) where T : class
        {
            Util.AllSerializeDeserialize<T, T>(from);

            var to = Clone<T>.From(Clone<U>.From(from));
            Assert.IsTrue(from.IsEqual<T>(to));
        }
    }
}
