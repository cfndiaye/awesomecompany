using AwesomeCompany;
using AwesomeCompany.Entities;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<DatabaseContext>(
    o => o.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();


app.UseHttpsRedirection();


app.MapPut("increase-salary", async (int companyId, DatabaseContext context) => {
    var company = await context
                            .Set<Company>()
                            .Include(c => c.Employees)
                            .FirstOrDefaultAsync(c => c.Id == companyId);
    if(company is null)
    {
        return Results.NotFound($"The company with id:{companyId} is not found.");
    }

    //EF CORE
    foreach (var employee in company.Employees)
    {
        employee.Salary *= 1.1m;

    }

    company.LastUpdateSalaryUtc = DateTime.UtcNow;

    await context.SaveChangesAsync();

    return Results.NoContent();

});

app.MapPut("increase-salary-sql", async (int companyId, DatabaseContext context) => {
    var company = await context
                            .Set<Company>()
                            .Include(c => c.Employees)
                            .FirstOrDefaultAsync(c => c.Id == companyId);
    if (company is null)
    {
        return Results.NotFound($"The company with id:{companyId} is not found.");
    }

    await context.Database.BeginTransactionAsync();
        //SQL
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = {company.Id}");

        company.LastUpdateSalaryUtc = DateTime.UtcNow;

        await context.SaveChangesAsync();

    await context.Database.CommitTransactionAsync();

    return Results.NoContent();

});

app.MapPut("increase-salary-sql-dapper", async (int companyId, DatabaseContext context) => {
    var company = await context
                            .Set<Company>()
                            .Include(c => c.Employees)
                            .FirstOrDefaultAsync(c => c.Id == companyId);
    if (company is null)
    {
        return Results.NotFound($"The company with id:{companyId} is not found.");
    }

    var transaction = await context.Database.BeginTransactionAsync();
    //SQL
    await context.Database.GetDbConnection().ExecuteAsync(
        "UPDATE Employees SET Salary = Salary * 1.1 WHERE CompanyId = @company.Id",
        new {CompanyId = company.Id}, transaction.GetDbTransaction());

    company.LastUpdateSalaryUtc = DateTime.UtcNow;

    await context.SaveChangesAsync();

    await context.Database.CommitTransactionAsync();

    return Results.NoContent();

});

app.Run();


