# Meta Credentials Broker (Security Layer)

Este componente é um microsserviço **Lambda-to-Lambda** ou **API-to-Lambda** projetado para isolar as credenciais sensíveis da API Oficial do WhatsApp (Meta).

## 🎯 Objetivo

Centralizar o mapeamento entre o `MetaIdWppBusiness` (ID público da Meta) e os segredos internos do sistema (`metatoken` e bancos de dados dos clientes). Isso permite que outros componentes do sistema obtenham credenciais em tempo de execução sem armazená-las localmente.

## 🛠️ Especificações Técnicas

- **Runtime**: .NET 6/8
- **Banco de Dados**: SQL Server (Base de Integração Master)
- **Segurança**: TrustServerCertificate habilitado para conexões em rede privada (VPC).

## 🔄 Fluxo de Resolução

1. Uma Lambda de processamento recebe um evento da Meta.
2. Ela invoca o **MetaCredentialsBroker** passando o `MetaId`.
3. O Broker consulta a tabela `BOTDESTINOS`.
4. O Broker retorna o Token de acesso e o destino do banco.
5. A Lambda original prossegue com a autorização garantida.

## 📊 Estrutura da Tabela Master (BOTDESTINOS)

| Coluna | Tipo | Descrição |
| --- | --- | --- |
| `MetaIdWppBusiness` | `varchar` | ID Único da Conta Business na Meta |
| `metatoken` | `text` | Permanent Access Token da Meta |
| `CONNECTIONSTRING` | `varchar` | Identificador do Banco de Dados do Cliente |

## 🚀 Deployment

Este serviço deve ser implantado dentro da mesma **VPC** que o banco de dados SQL Server para garantir que o IP privado `172.31.26.245` seja acessível.
