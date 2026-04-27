using Microsoft.AspNetCore.Identity;
using Zenda.Core.Entities;

namespace Zenda.Api.Data;

public static class RoleSeeder
{
    public static async Task SeedSuperAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 1. Crear el rol SuperAdmin si no existe
        if (!await roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
        }

        var adminEmail = "ivanpaoloni2@gmail.com";
        var adminPassword = "SuperAdminZendy2026!@#";

        // 2. Buscar si ya existís en la base
        var user = await userManager.FindByEmailAsync(adminEmail);

        if (user == null)
        {
            // 3. Como no existís (porque no pasaste por el Register de la app), te creamos directamente
            user = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Nombre = "Admin",
                Apellido = "Zendy",
                EmailConfirmed = true, // Te lo autoconfirmamos para que no te pida validar
                NegocioId = null // Clave: No tenés negocio asociado
            };

            var createResult = await userManager.CreateAsync(user, adminPassword);

            if (!createResult.Succeeded)
            {
                // Si falla por reglas de contraseña (ej. falta mayúscula), lo verás en consola
                Console.WriteLine("Error creando SuperAdmin: " + string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }
        }

        // 4. Asignar el rol de Dios
        if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
        {
            await userManager.AddToRoleAsync(user, "SuperAdmin");
        }

        // 5. Si ya existías por pruebas viejas, te desvinculamos de cualquier negocio basura
        if (user.NegocioId != null)
        {
            user.NegocioId = null;
            await userManager.UpdateAsync(user);
        }
    }
}