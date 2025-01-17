﻿using DataGate.Data.Models.Users;
using MyTested.AspNetCore.Mvc;
using System;

namespace DataGate.Web.Tests.TestData
{
    public static class TestApplicationUser
    {
        public const string Id = TestUser.Identifier;

        public const string Username = TestUser.Username;

        public const string Email = "test@email.com";

        public const string Name = "test_name";

        public const string Surname = "test_surname";

        public const string Password = "test";

        public const string PasswordHash = "AO7kszlVq1gUsEN6eEwH9WcbppmJlG0qtZpmG65xdklCa89AalTbiA+uXXCOVjzDXw==";

        public static ApplicationUser GetDefaultUser() => new ApplicationUser
        {
            Id = Id,
            UserName = Username,
            Email = Email,           
            PasswordHash = PasswordHash,
            EmailConfirmed = true,
            NormalizedUserName = Username.ToUpper(),
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
