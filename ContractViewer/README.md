# Contract Records Viewer - .NET 8 Web Application

A modern web application to view and manage your contract records with support for 102+ columns and horizontal scrolling.

## Features

- ✅ **Responsive Design**: Works on desktop and mobile devices
- ✅ **Horizontal Scrolling**: View all 102 columns with smooth scrolling
- ✅ **Search Functionality**: Search across all columns simultaneously
- ✅ **Pagination**: Efficient handling of large datasets
- ✅ **Dynamic Columns**: Automatically detects and displays all table columns
- ✅ **Modern UI**: Clean Bootstrap 5 interface with Font Awesome icons
- ✅ **Hover Effects**: Enhanced readability with row highlighting
- ✅ **Tooltips**: Full content preview on hover

## Prerequisites

- .NET 8.0 SDK
- MySQL Server 8.0+
- Your contract data imported into MySQL

## Quick Setup

### 1. Update Database Connection

Edit `Program.cs` and update the connection string:

```csharp
var connection = new MySqlConnection("Server=localhost;Database=contract_database;Uid=your_username;Pwd=your_password;");
```

Or use `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=contract_database;Uid=your_username;Pwd=your_password;"
  }
}
```

### 2. Install Dependencies

```bash
cd ContractViewer
dotnet restore
```

### 3. Run the Application

```bash
dotnet run
```

The application will start at `http://localhost:5000`

## Project Structure

```
ContractViewer/
├── Program.cs              # Application entry point
├── ContractViewer.csproj   # Project configuration
├── appsettings.json       # Configuration file
├── Controllers/
│   └── HomeController.cs   # Main controller
├── Models/
│   └── Contract.cs         # Data models
└── Views/
    ├── _ViewImports.cshtml # View imports
    ├── _ViewStart.cshtml  # View startup
    └── Home/
        └── Index.cshtml   # Main view
```

## Configuration Options

### Pagination Settings
- Default page size: 50 records
- Options: 25, 50, 100, 200 records per page
- Configurable via dropdown in the UI

### Search Functionality
- Searches across ALL columns automatically
- Case-insensitive search
- Real-time results with pagination

### Column Display
- Automatically detects all table columns
- Handles dynamic column addition
- Responsive column widths
- Horizontal scrolling for overflow

## Database Schema Requirements

Your MySQL table should have the following structure:

```sql
CREATE TABLE contracts (
    id INT AUTO_INCREMENT PRIMARY KEY,
    s_n VARCHAR(100),
    cycle VARCHAR(100),
    seller VARCHAR(255),
    client VARCHAR(255),
    buyer VARCHAR(255),
    annual_general_contract_number VARCHAR(255),
    appendix_number VARCHAR(255),
    mill VARCHAR(255),
    product_type VARCHAR(255),
    product TEXT,
    -- Additional columns will be detected automatically
    ...
);
```

## Usage Guide

### Basic Navigation

1. **View Records**: Open the application to see all contract records
2. **Horizontal Scrolling**: Use mouse wheel or scrollbar to view all columns
3. **Pagination**: Navigate through pages using the pagination controls
4. **Search**: Enter search terms to filter across all columns

### Advanced Features

#### Column Management
- The application automatically detects all columns in your table
- No manual configuration needed for new columns
- All columns are searchable

#### Data Export
To export data, you can:
1. Use MySQL Workbench export features
2. Add export functionality to the controller
3. Select and copy data directly from the table

#### Mobile Responsiveness
- The table adapts to different screen sizes
- Horizontal scrolling works on touch devices
- Touch-friendly controls and navigation

## Customization

### Styling
Modify the CSS in `Index.cshtml` to customize:
- Colors and themes
- Font sizes and families
- Table styling and spacing
- Button and form styles

### Adding Features

#### Export to CSV
Add this method to `HomeController.cs`:

```csharp
public IActionResult ExportToCsv()
{
    // Implementation for CSV export
}
```

#### Advanced Filtering
Add filter controls to the view and implement filtering logic in the controller.

#### Column Visibility Toggle
Add checkboxes to show/hide specific columns.

## Troubleshooting

### Common Issues

#### Database Connection Error
```
Error: Unable to connect to MySQL database
Solution: Check connection string in Program.cs or appsettings.json
```

#### Missing Data
```
Error: No records displayed
Solution: Verify data exists in the contracts table
```

#### Performance Issues
```
Error: Slow loading with large datasets
Solution: Reduce page size or add database indexes
```

### Performance Optimization

1. **Add Database Indexes**:
```sql
CREATE INDEX idx_contracts_id ON contracts(id);
CREATE INDEX idx_contracts_cycle ON contracts(cycle);
CREATE INDEX idx_contracts_seller ON contracts(seller);
```

2. **Optimize Queries**: The application uses efficient LIMIT/OFFSET pagination

3. **Connection Pooling**: MySQL connection pooling is enabled by default

## Development

### Adding New Features

1. **New Controller Actions**: Add methods to `HomeController.cs`
2. **New Views**: Create `.cshtml` files in the `Views` folder
3. **New Models**: Add classes to the `Models` folder

### Testing

Run the application in development mode:
```bash
dotnet watch run
```

This enables hot reload for development.

## Deployment

### Production Deployment

1. **Publish the Application**:
```bash
dotnet publish -c Release -o ./publish
```

2. **Deploy to Server**:
- Copy the publish folder to your server
- Configure the web server (IIS, Apache, Nginx)
- Update connection strings for production

### Docker Deployment

Create a `Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ContractViewer.dll"]
```

## License

This project is provided as-is for your use.

## Support

For issues or questions:
1. Check the troubleshooting section
2. Verify database connectivity
3. Review the application logs
4. Check browser console for client-side errors