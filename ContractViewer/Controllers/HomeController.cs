using Microsoft.AspNetCore.Mvc;
using System.Data;
using ContractViewer.Models;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using OfficeOpenXml;

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

        public IActionResult ExportToExcel(string searchTerm = "")
        {
            try
            {
                // Get all data (not paginated) for export
                var contracts = string.IsNullOrWhiteSpace(searchTerm)
                    ? GetContractsWithCurrency(0, int.MaxValue)
                    : SearchContracts(searchTerm, 0, int.MaxValue);

                // Set up EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("Contract Data");

                    // Add headers
                    var displayColumns = new List<string>
                    {
                        "s_n", "cycle", "seller", "client", "buyer", "annual_general_contract_number",
                        "appendix_number", "mill", "product_type", "product", "land_sea", "terms_of_delivery",
                        "type_of_transport_unit", "fsc", "destination", "r_station", "r_station_prov",
                        "r_station_region", "volume", "percent_of_quantity", "price", "terms_of_payment",
                        "prepayment_discount", "consignee", "date_of_signature", "last_shipping_date_in_contract",
                        "deferred_last_shipping_date", "actual_deferred_shipping_volume", "planning_date_of_payment",
                        "actual_date_of_payment", "delivery_date", "request_about_delivery_from_client", "status",
                        "initiator", "comments", "frequency_of_extend", "reasons_for", "month", "forwarder",
                        "container_terminal", "packaging", "dap_freight_cars_at_discount", "discount_for_dap_railcars",
                        "twenty_percent_discount_hk", "city_dap_to_warehouse", "one_railway_bill_lading", "bonuses",
                        "buyers_bank", "sellers_bank", "quantity_of_lc", "no_lc", "date_registered_in_sap"
                    };

                    // Add column headers
                    for (int i = 0; i < displayColumns.Count; i++)
                    {
                        worksheet.Cells[1, i + 1].Value = GetDisplayName(displayColumns[i]);
                        worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                        worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    }

                    // Add data rows
                    for (int row = 0; row < contracts.Count; row++)
                    {
                        for (int col = 0; col < displayColumns.Count; col++)
                        {
                            var value = GetColumnValueForExport(contracts[row], displayColumns[col]);
                            worksheet.Cells[row + 2, col + 1].Value = value;
                        }
                    }

                    // Auto-fit columns
                    worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                    // Set response headers
                    var stream = new MemoryStream();
                    package.SaveAs(stream);
                    stream.Position = 0;

                    var fileName = $"Contract_Data_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error exporting data: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private string GetColumnValueForExport(Contract contract, string columnName)
        {
            return columnName.ToLower() switch
            {
                "id" => contract.Id.ToString(),
                "s_n" => contract.S_N ?? "",
                "cycle" => contract.Cycle ?? "",
                "seller" => contract.Seller ?? "",
                "client" => contract.Client ?? "",
                "buyer" => contract.Buyer ?? "",
                "annual_general_contract_number" => contract.AnnualGeneralContractNumber ?? "",
                "appendix_number" => contract.AppendixNumber ?? "",
                "mill" => contract.Mill ?? "",
                "product_type" => contract.ProductType ?? "",
                "product" => contract.Product ?? "",
                "percent_of_quantity" => FormatPercentageForExport(contract.AdditionalColumns.ContainsKey("percent_of_quantity") ? contract.AdditionalColumns["percent_of_quantity"]?.ToString() : ""),
                "terms_of_payment" => FormatTermsOfPaymentForExport(contract.AdditionalColumns.ContainsKey("terms_of_payment") ? contract.AdditionalColumns["terms_of_payment"]?.ToString() : ""),
                _ => contract.AdditionalColumns.ContainsKey(columnName) ? contract.AdditionalColumns[columnName]?.ToString() ?? "" : ""
            };
        }

        private string FormatPercentageForExport(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (decimal.TryParse(value, out decimal percentage))
            {
                return (percentage / 100).ToString("P1"); // Format as percentage (0.05 -> 5.0%)
            }
            return value;
        }

        private string FormatTermsOfPaymentForExport(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value == "1")
            {
                return "100%";
            }
            return value;
        }

        private string GetDisplayName(string columnName)
        {
            // Convert snake_case to readable format
            return System.Text.RegularExpressions.Regex.Replace(
                columnName.Replace("_", " "),
                "([a-z])([A-Z])",
                "$1 $2"
            ).ToUpper();
        }
    }
}