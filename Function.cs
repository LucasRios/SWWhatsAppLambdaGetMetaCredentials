using System;
using System.Threading.Tasks;
using System.Text.Json;
using Amazon.Lambda.Core;
using Microsoft.Data.SqlClient;

// Serializador para garantir que a entrada e saída JSON sejam processadas corretamente pela AWS
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace MetaCredentialsBroker
{
    /// <summary>
    /// Função que atua como centralizador de credenciais para a API Oficial da Meta.
    /// </summary>
    public class Function
    {
        // String de conexão com o Banco Master. 
        // Nota: O IP 172.31.26.245 indica um servidor interno na VPC da AWS.
        private const string MASTER_CONN_STRING = "<MASTER_CONN_STRING>";

        /// <summary>
        /// Handler que recebe um ID da Meta e retorna as credenciais associadas.
        /// </summary>
        public async Task<CredentialResponse> FunctionHandler(CredentialRequest input, ILambdaContext context)
        {
            // Validação básica de entrada
            if (string.IsNullOrEmpty(input.MetaId))
            {
                context.Logger.LogLine("Erro: MetaId não fornecido.");
                return null;
            }

            try
            {
                using var connection = new SqlConnection(MASTER_CONN_STRING);
                await connection.OpenAsync();

                // Busca o Token de autenticação da Meta e a String de Conexão do cliente
                // O filtro é feito pelo MetaIdWppBusiness, que é a chave única da conta na Meta
                var query = "SELECT ...E MetaIdWppBusiness = @id";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@id", input.MetaId);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    // Retorna as credenciais encontradas com sucesso
                    return new CredentialResponse
                    {
                        Token = reader["metatoken"]?.ToString(),
                        DbName = reader["CONNECTIONSTRING"]?.ToString(),
                        Sucesso = true
                    };
                }

                // Caso o ID não exista na tabela de mapeamento
                context.Logger.LogLine($"Aviso: MetaIdWppBusiness {input.MetaId} não encontrado no Master.");
                return new CredentialResponse
                {
                    Sucesso = false,
                    Erro = $"Aviso: MetaIdWppBusiness {input.MetaId} não encontrado no Master."
                };
            }
            catch (Exception ex)
            {
                // Tratamento de erros de conexão ou SQL
                context.Logger.LogLine($"ERRO CRÍTICO no Broker: {ex.Message}");
                return new CredentialResponse { Sucesso = false, Erro = ex.Message };
            }
        }
    }

    // Modelos de Dados (DTOs)
    public class CredentialRequest
    {
        public string MetaId { get; set; } // O ID que vem do Webhook da Meta
    }

    public class CredentialResponse
    {
        public string Token { get; set; }        // O Bearer Token para chamadas de API
        public string DbName { get; set; }       // Nome do banco ou connection string do cliente
        public bool Sucesso { get; set; }        // Flag de controle
        public string Erro { get; set; }         // Detalhes em caso de falha
    }
}