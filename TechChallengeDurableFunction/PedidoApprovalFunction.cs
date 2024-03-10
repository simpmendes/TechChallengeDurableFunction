using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace TechChallengeDurableFunction
{
    public static class PedidoApprovalFunction
    {
        private static string connectionString = "Server=tcp:techchallengefiap.database.windows.net,1433;Initial Catalog=TechChallengeDurableFunc;Persist Security Info=False;User ID=adminAzureSQL;Password=M4nph1$23;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

        [FunctionName("PedidoApprovalFunction_HttpStart")]
        public static async Task<IActionResult> HttpStart(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [DurableClient] IDurableOrchestrationClient starter,
        ILogger log)
        {
            log.LogInformation("Iniciando o processo de aprovação de pedido.");

            // Analisa o corpo da requisição para obter os detalhes do pedido
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            int pedidoId = data?.Id;
            string nomeComprador = data?.nomeComprador;
            decimal valor = data?.valor;
            int quantidade = data?.quantidade;

            // Inicia a instância da orquestração
            string instanceId = await starter.StartNewAsync("PedidoApprovalFunction_Orchestrate", new
            {
                Id = pedidoId,
                NomeComprador = nomeComprador,
                Valor = valor,
                Quantidade = quantidade
            });

            log.LogInformation($"Instância de orquestração iniciada com ID = '{instanceId}'.");

            
            return new OkObjectResult(new { instanceId });
        }

        [FunctionName("PedidoApprovalFunction_Orchestrate")]
        public static async Task RunOrchestrator(
        [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var pedido = context.GetInput<dynamic>();
            int pedidoId = pedido.Id;

            // Realiza a validação do pedido
            bool pedidoAprovado = await context.CallActivityAsync<bool>("ValidarPedido", pedidoId);

            // Se o pedido for aprovado, executa as ações necessárias
            if (pedidoAprovado)
            {
                
                var pedidoDetails = new Pedido
                {
                    Id = pedido.Id,
                    NomeComprador = pedido.NomeComprador,
                    Valor = pedido.Valor,
                    Quantidade = pedido.Quantidade
                };
                // Adiciona o pedido na base de dados
                await context.CallActivityAsync("AdicionarPedidoAoBancoDeDados", pedidoDetails);
                context.SetCustomStatus("Pedido aprovado e processado.");
            }
            else
            {
                context.SetCustomStatus("Pedido rejeitado.");
            }
        }

        [FunctionName("ValidarPedido")]
        public static async Task<bool> ValidarPedido([ActivityTrigger] int pedidoId, ILogger log)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM Pedidos WHERE Id = @PedidoId";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PedidoId", pedidoId);
                    int count = (int)await command.ExecuteScalarAsync();
                    bool resultado = count == 0;
                    log.LogInformation(resultado ? "Pedido Aprovado" : "Pedido Reprovado");
                    return resultado;
                     
                }
            }
        }

        [FunctionName("AdicionarPedidoAoBancoDeDados")]
        public static async Task AdicionarPedidoAoBancoDeDados([ActivityTrigger] Pedido pedido, ILogger log)
        {
            int pedidoId = pedido.Id;
            string nomeComprador = pedido.NomeComprador;
            decimal valor = pedido.Valor;
            int quantidade = pedido.Quantidade;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string sql = "INSERT INTO Pedidos (Id, NomeComprador, Valor, Quantidade) VALUES (@PedidoId, @NomeComprador, CAST(@Valor AS Decimal(18, 0)), @Quantidade)";
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@PedidoId", pedidoId);
                    command.Parameters.AddWithValue("@NomeComprador", nomeComprador);
                    command.Parameters.AddWithValue("@Valor", valor);
                    command.Parameters.AddWithValue("@Quantidade", quantidade);
                    await command.ExecuteNonQueryAsync();

                    log.LogInformation("Pedido Incluído na base");
                }
            }
        }

        public class Pedido
        {
            public int Id { get; set; }
            public string NomeComprador { get; set; }
            public decimal Valor { get; set; }
            public int Quantidade { get; set; }
        }
    }
}
