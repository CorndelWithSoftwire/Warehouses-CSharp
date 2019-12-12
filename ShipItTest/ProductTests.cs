﻿using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShipIt.Controllers;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Models.DataModels;
using ShipIt.Parsers;
using ShipIt.Repositories;

namespace ShipItTest
{
    [TestClass]
    public class ProductControllerTests : AbstractBaseTest
    {
        ProductController productController = new ProductController(new ProductRepository());
        ProductRepository productRepository = new ProductRepository();

        private const int WAREHOUSE_ID = 1;

        //private const Employee EMPLOYEE = new EmployeeBuilder().setWarehouseId(WAREHOUSE_ID).createEmployee();
        private const string GTIN = "0000346374230";

        [TestMethod]
        public void TestRoundtripProductRepository()
        {
            onSetUp();
            var product = new ProductBuilder().CreateProductDatabaseModel();
            productRepository.AddProducts(new List<ProductDataModel>() {product});
            Assert.AreEqual(productRepository.GetProductByGtin(product.Gtin).Name, product.Name);
            Assert.AreEqual(productRepository.GetProductByGtin(product.Gtin).Gtin, product.Gtin);
        }

        [TestMethod]
        public void TestGetProduct()
        {
            onSetUp();
            var productBuilder = new ProductBuilder().setGtin(GTIN);
            productRepository.AddProducts(new List<ProductDataModel>() {productBuilder.CreateProductDatabaseModel()});
            var result = productController.Get(GTIN);

            var correctProduct = productBuilder.CreateProduct();
            Assert.IsTrue(ProductsAreEqual(correctProduct, result.Product));
            Assert.IsTrue(result.Success);
        }

        private bool ProductsAreEqual(Product A, Product B)
        {
            const double floatingPointTolerance = 10 * float.Epsilon;
            return A.Discontinued == B.Discontinued
                   && A.Gcp == B.Gcp
                   && A.Gtin == B.Gtin
                   && A.LowerThreshold == B.LowerThreshold
                   && A.MinimumOrderQuantity == B.MinimumOrderQuantity
                   && A.Name == B.Name
                   && Math.Abs(A.Weight - B.Weight) < floatingPointTolerance;
        }

        [TestMethod]
        public void TestGetNonexistentProduct()
        {
            onSetUp();
            try
            {
                productController.Get(GTIN);
            }
            catch (NoSuchEntityException e)
            {
                Assert.IsTrue(e.Message.Contains(GTIN));
            }
        }

        [TestMethod]
        public void TestAddProducts()
        {
            onSetUp();
            var productBuilder = new ProductBuilder().setGtin(GTIN);
            var productRequest = productBuilder.CreateProductRequest();

            var response = productController.Post(productRequest);
            var databaseProduct = productRepository.GetProductByGtin(GTIN);
            var correctDatabaseProduct = productBuilder.CreateProductDatabaseModel();

            Assert.IsTrue(response.Success);
            ProductsAreEqual(new Product(databaseProduct), new Product(correctDatabaseProduct));
        }
    }
}
