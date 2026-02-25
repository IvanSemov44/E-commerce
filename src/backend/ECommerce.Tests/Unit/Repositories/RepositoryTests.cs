using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.Entities;
using ECommerce.Infrastructure.Data;
using ECommerce.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ECommerce.Tests.Unit.Repositories
{
    [TestClass]
    public class RepositoryTests
    {
        private AppDbContext _context = null!;
        private Repository<Product> _sut = null!; // System Under Test using Product as the entity

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
                .Options;

            _context = new AppDbContext(options);
            _sut = new Repository<Product>(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [TestMethod]
        public async Task GetByIdAsync_ExistingId_ReturnsEntity()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "Test Product", Price = 10.0m, IsActive = true };
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.GetByIdAsync(product.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
            result.Name.Should().Be("Test Product");
        }

        [TestMethod]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Act
            var result = await _sut.GetByIdAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [TestMethod]
        public async Task AddAsync_ValidEntity_AddsToDatabase()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "New Product", Price = 20.0m, IsActive = true };

            // Act
            await _sut.AddAsync(product);
            await _context.SaveChangesAsync();

            // Assert
            var dbProduct = await _context.Products.FindAsync(product.Id);
            dbProduct.Should().NotBeNull();
            dbProduct!.Name.Should().Be("New Product");
        }

        [TestMethod]
        public async Task UpdateAsync_ExistingEntity_UpdatesDatabase()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "Original Name", Price = 10.0m, IsActive = true };
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            product.Name = "Updated Name";
            await _sut.UpdateAsync(product);
            await _context.SaveChangesAsync();

            // Assert
            var dbProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
            dbProduct!.Name.Should().Be("Updated Name");
        }

        [TestMethod]
        public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "To Delete", Price = 10.0m, IsActive = true };
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // Act
            await _sut.DeleteAsync(product);
            await _context.SaveChangesAsync();

            // Assert
            var dbProduct = await _context.Products.FindAsync(product.Id);
            dbProduct.Should().BeNull();
        }

        [TestMethod]
        public async Task GetAllAsync_ReturnsAllEntities()
        {
            // Arrange
            await _context.Products.AddRangeAsync(
                new Product { Id = Guid.NewGuid(), Name = "P1", Price = 10, IsActive = true },
                new Product { Id = Guid.NewGuid(), Name = "P2", Price = 20, IsActive = true }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }
    }
}