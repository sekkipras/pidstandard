using FluentAssertions;
using PIDStandardization.Services.Caching;
using Xunit;

namespace PIDStandardization.Tests
{
    public class CacheServiceTests
    {
        [Fact]
        public void Set_And_Get_ShouldWorkCorrectly()
        {
            // Arrange
            var cache = new MemoryCacheService();
            var key = "test_key";
            var value = "test_value";

            // Act
            cache.Set(key, value);
            var result = cache.Get<string>(key);

            // Assert
            result.Should().Be(value);
        }

        [Fact]
        public void Get_ShouldReturnNull_WhenKeyDoesNotExist()
        {
            // Arrange
            var cache = new MemoryCacheService();

            // Act
            var result = cache.Get<string>("nonexistent_key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void TryGetValue_ShouldReturnTrue_WhenKeyExists()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("key", "value");

            // Act
            var success = cache.TryGetValue<string>("key", out var result);

            // Assert
            success.Should().BeTrue();
            result.Should().Be("value");
        }

        [Fact]
        public void TryGetValue_ShouldReturnFalse_WhenKeyDoesNotExist()
        {
            // Arrange
            var cache = new MemoryCacheService();

            // Act
            var success = cache.TryGetValue<string>("nonexistent", out var result);

            // Assert
            success.Should().BeFalse();
        }

        [Fact]
        public void Remove_ShouldRemoveKey()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("key", "value");

            // Act
            cache.Remove("key");
            var result = cache.Get<string>("key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void RemoveByPrefix_ShouldRemoveAllMatchingKeys()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("prefix:key1", "value1");
            cache.Set("prefix:key2", "value2");
            cache.Set("other:key3", "value3");

            // Act
            cache.RemoveByPrefix("prefix:");

            // Assert
            cache.Get<string>("prefix:key1").Should().BeNull();
            cache.Get<string>("prefix:key2").Should().BeNull();
            cache.Get<string>("other:key3").Should().Be("value3");
        }

        [Fact]
        public void Clear_ShouldRemoveAllKeys()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");
            cache.Set("key3", "value3");

            // Act
            cache.Clear();

            // Assert
            cache.Get<string>("key1").Should().BeNull();
            cache.Get<string>("key2").Should().BeNull();
            cache.Get<string>("key3").Should().BeNull();
        }

        [Fact]
        public void Set_ShouldOverwriteExistingValue()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("key", "original");

            // Act
            cache.Set("key", "updated");
            var result = cache.Get<string>("key");

            // Assert
            result.Should().Be("updated");
        }

        [Fact]
        public void Set_ShouldSupportComplexTypes()
        {
            // Arrange
            var cache = new MemoryCacheService();
            var obj = new TestObject { Id = 1, Name = "Test" };

            // Act
            cache.Set("object", obj);
            var result = cache.Get<TestObject>("object");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(1);
            result.Name.Should().Be("Test");
        }

        [Fact]
        public void GetStatistics_ShouldReturnCorrectCount()
        {
            // Arrange
            var cache = new MemoryCacheService();
            cache.Set("key1", "value1");
            cache.Set("key2", "value2");

            // Act
            var (count, keys) = cache.GetStatistics();

            // Assert
            count.Should().Be(2);
            keys.Should().Contain("key1");
            keys.Should().Contain("key2");
        }

        [Fact]
        public void CacheKeys_ShouldGenerateCorrectKeys()
        {
            // Arrange
            var projectId = Guid.NewGuid();

            // Assert
            MemoryCacheService.CacheKeys.ProjectById(projectId).Should().Be($"projects:{projectId}");
            MemoryCacheService.CacheKeys.EquipmentByProject(projectId).Should().Be($"equipment:project:{projectId}");
            MemoryCacheService.CacheKeys.EquipmentTagsByProject(projectId).Should().Be($"equipment:tags:{projectId}");
            MemoryCacheService.CacheKeys.LinesByProject(projectId).Should().Be($"lines:project:{projectId}");
        }

        private class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
