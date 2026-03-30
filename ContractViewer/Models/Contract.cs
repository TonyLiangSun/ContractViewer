namespace ContractViewer.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public string? S_N { get; set; }
        public string? Cycle { get; set; }
        public string? Seller { get; set; }
        public string? Client { get; set; }
        public string? Buyer { get; set; }
        public string? AnnualGeneralContractNumber { get; set; }
        public string? AppendixNumber { get; set; }
        public string? Mill { get; set; }
        public string? ProductType { get; set; }
        public string? Product { get; set; }

        // Dictionary to hold additional columns dynamically
        public Dictionary<string, object> AdditionalColumns { get; set; } = new Dictionary<string, object>();
    }

    public class ContractViewModel
    {
        public List<Contract> Contracts { get; set; } = new List<Contract>();
        public List<string> ColumnNames { get; set; } = new List<string>();
        public int TotalRecords { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
    }
}