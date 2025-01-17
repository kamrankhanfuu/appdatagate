﻿namespace DataGate.Data.Seeding
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using DataGate.Data.Models;

    internal class SettingsSeeder : ISeeder
    {
        public async Task SeedAsync(UsersDbContext dbContext, IServiceProvider serviceProvider)
        {
            if (dbContext.Settings.Any())
            {
                return;
            }

            await dbContext.Settings.AddAsync(new Setting { Name = "Setting1", Value = "value1" });
        }
    }
}
