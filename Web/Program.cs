using Web.Configuration;

InvoiceServiceApplication.Run(args, app =>
{
    app.UseForwardedHeaders();
    app.UseHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseRequestLocalization();
    app.UseAuthorization();
    // After authorization, so API requests are limited per authenticated user (see ApiRateLimiting).
    app.UseRateLimiter();

    app.MapStaticAssets();

    app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();
});
