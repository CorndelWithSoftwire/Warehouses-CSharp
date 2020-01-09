﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.UI.WebControls.WebParts;
using ShipIt.Exceptions;
using ShipIt.Models.ApiModels;
using ShipIt.Repositories;

namespace ShipIt.Controllers
{
    public class OutboundOrderController : ApiController
    {
        private readonly IStockRepository stockRepository;
        private readonly IProductRepository productRepository;

        public OutboundOrderController(IStockRepository stockRepository, IProductRepository productRepository)
        {
            this.stockRepository = stockRepository;
            this.productRepository = productRepository;
        }

        public void Post([FromBody]OutboundOrderRequestModel request)
        {
            var gtins = new List<string>();

            foreach (var orderLine in request.OrderLines)
            {
                if (gtins.Contains(orderLine.gtin))
                {
                    throw new ValidationException(String.Format("Outbound order request contains duplicate product gtin: {0}", orderLine.gtin));
                }
                gtins.Add(orderLine.gtin);
            }

            var productDataModels = productRepository.GetProductsByGtin(gtins);
            var products = productDataModels.ToDictionary(p => p.Gtin, p => new Product(p));

            var lineItems = new List<StockAlteration>();
            var productIds = new List<int>();
            var errors = new List<string>();

            foreach (var orderLine in request.OrderLines)
            {
                if (!products.ContainsKey(orderLine.gtin))
                {
                    errors.Add($"Unknown product gtin: {orderLine.gtin}");
                }
                else
                {
                    var product = products[orderLine.gtin];
                    lineItems.Add(new StockAlteration(product.Id, orderLine.quantity));
                    productIds.Add(product.Id);
                }
            }

            if (errors.Count > 0)
            {
                throw new NoSuchEntityException(string.Join("; ", errors));
            }

            var stock = stockRepository.GetStockByWarehouseAndProductIds(request.WarehouseId, productIds);

            var orderLines = request.OrderLines.ToList();
            errors = new List<string>();

            for (int i = 0; i < lineItems.Count; i++)
            {
                var lineItem = lineItems[i];
                var orderLine = orderLines[i];

                if (!stock.ContainsKey(lineItem.ProductId))
                {
                    errors.Add($"Product: {orderLine.gtin}, no stock held");
                    continue;
                }

                var item = stock[lineItem.ProductId];
                if (lineItem.Quantity > item.held)
                {
                    errors.Add(
                        $"Product: {orderLine.gtin}, stock held: {item.held}, stock to remove: {lineItem.Quantity}");
                }
            }

            if (errors.Count > 0)
            {
                throw new InsufficientStockException(string.Join("; ", errors));
            }

            stockRepository.RemoveStock(request.WarehouseId, lineItems);
        }
    }
}