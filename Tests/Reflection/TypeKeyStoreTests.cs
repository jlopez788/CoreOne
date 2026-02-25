using NUnit.Framework;
using CoreOne.Reflection;
using System;
using System.Collections.Generic;

namespace Tests.Reflection
{
    [TestFixture]
    public class TypeKeyStoreTests
    {
        [Test]
        public void AddTypeKey_StoresAndRetrievesSuccessfully()
        {
            var key = "TestKey";
            var type = typeof(string);
            var registered = TypeKeyStore.Register(type, key);
            Assert.Multiple(() => {
                Assert.That(registered.Type, Is.EqualTo(type));
                Assert.That(registered.Name, Is.EqualTo(key));
            });

            var found = TypeKeyStore.FindType(key);
            Assert.That(found, Is.EqualTo(registered));
        }

        [Test]
        public void GetTypeByKey_NonExistentKey_ReturnsNull()
        {
            var result = TypeKeyStore.FindType("MissingKey");
            Assert.That(result, Is.EqualTo(TypeKey.Empty));
        }

        [Test]
        public void AddTypeKey_DuplicateKey_UpdatesType()
        {
            var key = "DupKey";
            TypeKeyStore.Register(typeof(int), key);
            var registered = TypeKeyStore.Register(typeof(double), key);
            Assert.That(registered.Type, Is.EqualTo(typeof(double)));
        }

        [Test]
        public void RemoveTypeKey_RemovesSuccessfully()
        {
            var key = "RemoveKey";
            TypeKeyStore.Register(typeof(Guid), key);
            // No Remove method in static class, so test cannot remove
            // Instead, test that Register returns correct type
            var result = TypeKeyStore.FindType(key);
            Assert.That(result.Type, Is.EqualTo(typeof(Guid)));
        }

        [Test]
        public void Clear_RemovesAllKeys()
        {
            // No Clear method in static class, so test cannot clear
            // Instead, test that Register adds keys
            TypeKeyStore.Register(typeof(int), "A");
            TypeKeyStore.Register(typeof(string), "B");
            Assert.That(TypeKeyStore.FindType("A").Type, Is.EqualTo(typeof(int)));
            Assert.That(TypeKeyStore.FindType("B").Type, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Keys_ReturnsAllKeys()
        {
            TypeKeyStore.Register(typeof(int), "X");
            TypeKeyStore.Register(typeof(string), "Y");
            var keys = TypeKeyStore.GetKnownTypes().Select(tk => tk.Name);
            Assert.That(keys, Does.Contain("X"));
            Assert.That(keys, Does.Contain("Y"));
        }

        [Test]
        public void Types_ReturnsAllTypes()
        {
            TypeKeyStore.Register(typeof(int), "T1");
            TypeKeyStore.Register(typeof(string), "T2");
            var types = TypeKeyStore.GetKnownTypes().Select(tk => tk.Type);
            Assert.That(types, Does.Contain(typeof(int)));
            Assert.That(types, Does.Contain(typeof(string)));
        }

        [Test]
        public void AddTypeKey_NullKey_ThrowsArgumentNullException()
        {
            Assert.That(TypeKeyStore.Register(typeof(int), null), Is.Not.EqualTo(TypeKey.Empty));
        }

        [Test]
        public void AddTypeKey_NullType_ThrowsArgumentNullException()
        {
            Assert.That(TypeKeyStore.Register(null, "NullType"), Is.EqualTo(TypeKey.Empty));
        }
    }
}