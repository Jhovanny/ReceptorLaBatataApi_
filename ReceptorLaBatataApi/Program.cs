using Dapper;
using Npgsql;
using ReceptorLaBatataApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregamos los servicios para generar la interfaz visual de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Activamos la página web de Swagger. 
// NOTA: Lo ponemos "suelto" (sin el if de Development) para que lo puedas ver en tu VPS
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Esto hace que la página principal al entrar al servidor sea directamente Swagger
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API La Batata v1");
    c.RoutePrefix = string.Empty;
});

app.MapPost("/api/sincronizar", async (List<ArticuloCompra> compras, IConfiguration config) =>
{
    string connectionString = config.GetConnectionString("PostgresConnection");

    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    using var transaction = await connection.BeginTransactionAsync();

    try
    {
        string queryUpsert = @"
            INSERT INTO public.reporte_compras 
            (codigo_barras, descripcion, departamento, stock_actual, punto_reorden, cantidad_comprar, costo_unitario, costo_total_estimado, ultima_actualizacion) 
            VALUES 
            (@CodigoBarras, @Descripcion, @Departamento, @StockActual, @PuntoReorden, @CantidadComprar, @CostoUnitario, @CostoTotalEstimado, CURRENT_TIMESTAMP)
            ON CONFLICT (codigo_barras) 
            DO UPDATE SET 
                descripcion = EXCLUDED.descripcion,
                departamento = EXCLUDED.departamento,
                stock_actual = EXCLUDED.stock_actual,
                punto_reorden = EXCLUDED.punto_reorden,
                cantidad_comprar = EXCLUDED.cantidad_comprar,
                costo_unitario = EXCLUDED.costo_unitario,
                costo_total_estimado = EXCLUDED.costo_total_estimado,
                ultima_actualizacion = CURRENT_TIMESTAMP;";

        await connection.ExecuteAsync(queryUpsert, compras, transaction: transaction);
        await transaction.CommitAsync();

        return Results.Ok(new { mensaje = $"Se procesaron {compras.Count} artículos exitosamente." });
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        return Results.Problem("Error al guardar en BD: " + ex.Message);
    }
})
.WithName("RecibirDatosEleventa")
.WithOpenApi(); // Estas dos líneas le dan formato en la pantalla de Swagger

app.Run();