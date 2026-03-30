using Microsoft.AspNetCore.Mvc;
using System.Data;
using ContractViewer.Models;
using System.Text;
using MySql.Data.MySqlClient;

namespace ContractViewer.Controllers
{
    public class HomeController : Controller
    {
        private readonly IDbConnection _connection;

        public HomeController(IDbConnection connection)
        {
            _connection = connection;
        }

        public IActionResult Index(int page = 1, int pageSize = 50)
        {
            var model = new ContractViewModel
            {
                CurrentPage = page,
                PageSize = pageSize
            };

            try
            {
                // Get total record count
                using (var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=root;Pwd=Ratings@123;"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM contracts";
                        model.TotalRecords = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                // Get column names dynamically
                model.ColumnNames = GetColumnNames();

                // Get paginated data with currency join
                var offset = (page - 1) * pageSize;
                model.Contracts = GetContractsWithCurrency(offset, pageSize);

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading data: {ex.Message}";
                return View(model);
            }
        }

        private List<string> GetColumnNames()
        {
            var columns = new List<string>();
            using (var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=root;Pwd=Ratings@123;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "DESCRIBE contracts";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            columns.Add(reader["Field"].ToString());
                        }
                    }
                }
            }
            return columns;
        }

        private List<Contract> GetContractsWithCurrency(int offset, int pageSize)
        {
            var contracts = new List<Contract>();
            var columns = GetColumnNames();

            using (var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=root;Pwd=Ratings@123;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Join with currencies, sellers, and clients tables
                    command.CommandText = $@"
                        SELECT c.*, cur.symbol as currency_symbol, cur.code as currency_code, cur.name as currency_name,
                               s.name as seller_name, cl.name as client_name
                        FROM contracts c
                        LEFT JOIN currencies cur ON c.currency_id = cur.id
                        LEFT JOIN sellers s ON c.seller_id = s.id
                        LEFT JOIN clients cl ON c.client_id = cl.id
                        ORDER BY c.id LIMIT {pageSize} OFFSET {offset}";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var contract = new Contract
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                S_N = reader["s_n"]?.ToString(),
                                Cycle = reader["cycle"]?.ToString(),
                                Seller = reader["seller_name"]?.ToString(),
                                Client = reader["client_name"]?.ToString(),
                                Buyer = reader["buyer"]?.ToString(),
                                AnnualGeneralContractNumber = reader["annual_general_contract_number"]?.ToString(),
                                AppendixNumber = reader["appendix_number"]?.ToString(),
                                Mill = reader["mill"]?.ToString(),
                                ProductType = reader["product_type"]?.ToString(),
                                Product = reader["product"]?.ToString()
                            };

                            // Add any additional columns dynamically with currency formatting
                            foreach (var columnName in columns)
                            {
                                if (!IsKnownProperty(columnName) && reader[columnName] != null)
                                {
                                    var value = reader[columnName];

                                    // Apply currency formatting for price column using currency_id relationship
                                    if (columnName.ToLower() == "price" && reader["currency_symbol"] != null)
                                    {
                                        var currencySymbol = reader["currency_symbol"].ToString();
                                        var priceValue = Convert.ToDecimal(value);
                                        contract.AdditionalColumns[columnName] = $"{currencySymbol}{priceValue:N2}";
                                    }
                                    else
                                    {
                                        contract.AdditionalColumns[columnName] = value;
                                    }
                                }
                            }

                            contracts.Add(contract);
                        }
                    }
                }
            }

            return contracts;
        }

        private bool IsKnownProperty(string columnName)
        {
            var knownProperties = new[] { "id", "s_n", "cycle", "seller", "client", "buyer",
                "annual_general_contract_number", "appendix_number", "mill", "product_type", "product" };
            return knownProperties.Contains(columnName.ToLower());
        }

        [HttpPost]
        public IActionResult Search(string searchTerm, int page = 1, int pageSize = 50)
        {
            var model = new ContractViewModel
            {
                CurrentPage = page,
                PageSize = pageSize
            };

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index", new { page, pageSize });
            }

            try
            {
                var columns = GetColumnNames();
                var whereClause = string.Join(" OR ", columns.Select(c => $"`{c}` LIKE @searchTerm"));

                // Get total count
                using (var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=root;Pwd=Ratings@123;"))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"SELECT COUNT(*) FROM contracts WHERE {whereClause}";
                        command.Parameters.Add(new MySqlParameter("@searchTerm", "%" + searchTerm + "%"));
                        model.TotalRecords = Convert.ToInt32(command.ExecuteScalar());
                    }
                }

                // Get column names
                model.ColumnNames = columns;

                // Get paginated search results
                var offset = (page - 1) * pageSize;
                model.Contracts = SearchContracts(searchTerm, offset, pageSize);

                ViewBag.SearchTerm = searchTerm;
                return View("Index", model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error searching data: {ex.Message}";
                return View("Index", model);
            }
        }

        private List<Contract> SearchContracts(string searchTerm, int offset, int pageSize)
        {
            var contracts = new List<Contract>();
            var columns = GetColumnNames();
            var whereClause = string.Join(" OR ", columns.Select(c => $"`{c}` LIKE @searchTerm"));

            using (var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=root;Pwd=Ratings@123;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    // Join with currencies, sellers, and clients tables for search results too
                    command.CommandText = $@"
                        SELECT c.*, cur.symbol as currency_symbol, cur.code as currency_code, cur.name as currency_name,
                               s.name as seller_name, cl.name as client_name
                        FROM contracts c
                        LEFT JOIN currencies cur ON c.currency_id = cur.id
                        LEFT JOIN sellers s ON c.seller_id = s.id
                        LEFT JOIN clients cl ON c.client_id = cl.id
                        WHERE {whereClause}
                        ORDER BY c.id LIMIT {pageSize} OFFSET {offset}";
                    command.Parameters.Add(new MySqlParameter("@searchTerm", "%" + searchTerm + "%"));

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var contract = new Contract
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                S_N = reader["s_n"]?.ToString(),
                                Cycle = reader["cycle"]?.ToString(),
                                Seller = reader["seller_name"]?.ToString(),
                                Client = reader["client_name"]?.ToString(),
                                Buyer = reader["buyer"]?.ToString(),
                                AnnualGeneralContractNumber = reader["annual_general_contract_number"]?.ToString(),
                                AppendixNumber = reader["appendix_number"]?.ToString(),
                                Mill = reader["mill"]?.ToString(),
                                ProductType = reader["product_type"]?.ToString(),
                                Product = reader["product"]?.ToString()
                            };

                            // Add additional columns with currency formatting
                            foreach (var columnName in columns)
                            {
                                if (!IsKnownProperty(columnName) && reader[columnName] != null)
                                {
                                    var value = reader[columnName];

                                    // Apply currency formatting for price column
                                    if (columnName.ToLower() == "price" && reader["currency_symbol"] != null)
                                    {
                                        var currencySymbol = reader["currency_symbol"].ToString();
                                        var priceValue = Convert.ToDecimal(value);
                                        contract.AdditionalColumns[columnName] = $"{currencySymbol}{priceValue:N2}";
                                    }
                                    else
                                    {
                                        contract.AdditionalColumns[columnName] = value;
                                    }
                                }
                            }

                            contracts.Add(contract);
                        }
                    }
                }
            }

            return contracts;
        }
    }
}