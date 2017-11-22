﻿using esencialAdmin.Data.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace esencialAdmin.Data
{
    public static class DbInitializer 
    {
        //This example just creates an Administrator role and one Admin users
        public async static Task<bool> Initialize(ApplicationDbContext context, esencialAdminContext esencialAdminContext, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            //create database schema if none exists
            esencialAdminContext.Database.EnsureCreated();
            context.Database.EnsureCreated();

            if(!esencialAdminContext.Templates.Any())
            {
                var template1 = new Templates(){
                    Name = "Wein Etikette"
                };

                var template2 = new Templates()
                {
                    Name = "Olivenöl Etikette"
                };
                esencialAdminContext.Templates.Add(template1);
                esencialAdminContext.Templates.Add(template2);
            }

            if(esencialAdminContext.SubscriptionStatus.Count() != 4)
            {
                esencialAdminContext.SubscriptionStatus.RemoveRange(esencialAdminContext.SubscriptionStatus);
                var status1 = new SubscriptionStatus()
                {
                    Id = 1,
                    Label = "Aktiv"
                };
                var status2 = new SubscriptionStatus()
                {
                    Id = 2,
                    Label = "Läuft aus"
                };
                var status3 = new SubscriptionStatus()
                {
                    Id = 3,
                    Label = "Rechnung noch nicht bezahlt"
                };
                var status4 = new SubscriptionStatus()
                {
                    Id = 4,
                    Label = "Ausgelaufen"
                };

                esencialAdminContext.SubscriptionStatus.Add(status1);
                esencialAdminContext.SubscriptionStatus.Add(status2);
                esencialAdminContext.SubscriptionStatus.Add(status3);
                esencialAdminContext.SubscriptionStatus.Add(status4);
            }

            //If there is already an User with Administrator role, abort
            string roleID = context.Roles.Where(r => r.Name == "Administrator").Select(r => r.Id).FirstOrDefault();
            if(roleID != null)
            {
                if (context.UserRoles.Any(r => r.RoleId == roleID))
                {
                    return true;
                }
            }

            //Create the Administartor Role
            await roleManager.CreateAsync(new IdentityRole("Administrator"));
            await roleManager.CreateAsync(new IdentityRole("Mitarbeiter"));

            //Create the default Admin account and apply the Administrator role
            string password = "1234";
            var user = new ApplicationUser();
            user.UserName = "admin@byom.de";
            user.Email = "admin@byom.de";
            await userManager.CreateAsync(user, password);
            await userManager.AddToRoleAsync(user, "Administrator");
            // userManager.AddToRoleAsync(await userManager.FindByNameAsync(user), "Administrator");
            return true;

        }
    }
}
