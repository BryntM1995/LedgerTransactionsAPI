using System.Reflection;
using LedgerTransactionsAPI.Swagger.LedgerTransactions.Attributes;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LedgerTransactionsAPI.Swagger
{
    public class IdempotencyKeyHeaderFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            // ¿El endpoint tiene el atributo [Idempotent]?
            var hasAttr = context.MethodInfo.GetCustomAttribute<IdempotentAttribute>() != null;
            if (!hasAttr) return;

            operation.Parameters ??= new List<OpenApiParameter>();
            if (operation.Parameters.Any(p => p.Name.Equals("Idempotency-Key", StringComparison.OrdinalIgnoreCase)))
                return;

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = true, // ponlo true si quieres “obligatorio” en Swagger
                Description = "Unique key to make the operation idempotent.",
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Example = new OpenApiString(Guid.NewGuid().ToString())
                }
            });
        }
    }
}
