﻿using System.Collections.Generic;
using System.Data;
using System.Linq;
using Npgsql;
using ShipIt.Models.DataModels;

namespace ShipIt.Repositories
{
    public interface IProductRepository
    {
        int GetCount();
        ProductDataModel GetProductByGtin(string gtin);
        ProductDataModel GetProductById(int id);
        void AddProducts(IEnumerable<ProductDataModel> products);
        void DiscontinueProductByGtin(string gtin);
    }

    public class ProductRepository : RepositoryBase, IProductRepository
    {
        public int GetCount()
        {
            string EmployeeCountSQL = "SELECT COUNT(*) FROM gcp";
            return (int) QueryForLong(EmployeeCountSQL);
        }

        public ProductDataModel GetProductByGtin(string gtin)
        {

            string sql = "SELECT p_id, gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt FROM gtin WHERE gtin_cd = @gtin_cd";
            var parameter = new NpgsqlParameter("@gtin_cd", gtin);
            return base.RunSingleGetQuery(sql, reader => new ProductDataModel(reader), $"No products found with gtin of value {gtin}", parameter);
        }

        public ProductDataModel GetProductById(int id)
        {

            string sql = "SELECT p_id, gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt FROM gtin WHERE p_id = @p_id";
            var parameter = new NpgsqlParameter("@p_id", id);
            string noProductWithIdErrorMessage = $"No products found with id of value {id.ToString()}";
            return RunSingleGetQuery(sql, reader => new ProductDataModel(reader), noProductWithIdErrorMessage, parameter);
        }

        public void DiscontinueProductByGtin(string gtin)
        {
            string sql = "UPDATE gtin SET ds = 1 WHERE gtin_cd = @gtin_cd";
            var parameter = new NpgsqlParameter("@gtin_cd", gtin);
            string noProductWithGtinErrorMessage = $"No products found with gtin of value {gtin.ToString()}";

            RunSingleQuery(sql, noProductWithGtinErrorMessage, parameter);
        }

        public void AddProducts(IEnumerable<ProductDataModel> products)
        {
            string sql = "INSERT INTO gtin (gtin_cd, gcp_cd, gtin_nm, m_g, l_th, ds, min_qt) VALUES (@gtin_cd, @gcp_cd, @gtin_nm, @m_g, @l_th, @ds, @min_qt)";

            foreach (var product in products)
            {
                var parameters = product.GetNpgsqlParameters().ToArray();
                RunQuery(sql, parameters);
            }
        }
    }
}