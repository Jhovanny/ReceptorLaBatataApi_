using Dapper;
using Npgsql;
using ReceptorLaBatataApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregamos los servicios para generar la interfaz visual de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ---> NUEVO: Habilitar CORS para que Neubox pueda leer la API <---
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirNeubox", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---> NUEVO: Activar CORS en la aplicación <---
app.UseCors("PermitirNeubox");

// 2. Activamos la página web de Swagger. 
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API La Batata v1");
    c.RoutePrefix = string.Empty;
});

// =====================================================================
// MÉTODO 1: RECIBIR DATOS DEL WORKER (Este es tu código original POST)
// =====================================================================
app.MapPost("/api/sincronizar", async (List<ArticuloCompra> compras, IConfiguration config) =>
{
    string connectionString = config.GetConnectionString("PostgresConnection");

    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    using var transaction = await connection.BeginTransactionAsync();

    try
    {
        await connection.ExecuteAsync("DELETE FROM public.reporte_compras;", transaction: transaction);
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
.WithOpenApi();


// =====================================================================
// MÉTODO 2: MANDAR DATOS A NEUBOX (ESTE ES EL NUEVO GET)
// =====================================================================
app.MapGet("/api/surtido", async (IConfiguration config) =>
{
    string connectionString = config.GetConnectionString("PostgresConnection");

    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    try
    {
        // Consultamos todo lo de la tabla ordenado por departamento
        string querySelect = "SELECT * FROM public.reporte_compras ORDER BY departamento, descripcion;";

        // Dapper lee la base de datos y arma la lista de forma automática
        var listaSurtido = await connection.QueryAsync(querySelect);

        return Results.Ok(listaSurtido);
    }
    catch (Exception ex)
    {
        return Results.Problem("Error al consultar en BD: " + ex.Message);
    }
})
.WithName("ObtenerSurtidoPendiente")
.WithOpenApi();

app.Run();