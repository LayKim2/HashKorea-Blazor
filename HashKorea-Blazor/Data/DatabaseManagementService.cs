﻿using HashKorea.Models;
using Microsoft.EntityFrameworkCore;

namespace HashKorea.Data;

public class DatabaseManagementService
{
    public static void MigrationInitialisation(IApplicationBuilder app)
    {
        using (var serviceScope = app.ApplicationServices.CreateScope())
        {
            serviceScope.ServiceProvider.GetService<DataContext>().Database.Migrate();
        }
    }
}