using System;
using System.IO;
using System.Runtime.CompilerServices;
using EFCore.Scaffolding;
using efmssql;
using Microsoft.Data.SqlClient;

var container = await ChinookContainer.StartAsync(Console.WriteLine);
var settings = new ScaffolderSettings(new SqlConnectionStringBuilder(container.ConnectionString))
{
    OutputDirectory = GetOutputDirectory(),
    ContextName = "ChinookContext",
    GetDisplayableConnectionString = builder =>
    {
        ((SqlConnectionStringBuilder)builder).DataSource = "localhost";
        return builder.ConnectionString;
    },
};
Scaffolder.Run(settings);
return;

static DirectoryInfo GetOutputDirectory([CallerFilePath] string path = "") => new(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "src", "Database"));