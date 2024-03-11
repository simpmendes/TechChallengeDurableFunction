# Azure Function Durable para Aprovação de Pedidos

Esta é uma Azure Function Durable desenvolvida para simular o processo de aprovação de pedidos. A função utiliza o modelo Durable Functions do Azure para orquestrar o processo de aprovação, incluindo a validação do pedido e a adição do pedido em uma base de dados.

## Funcionalidades

- **Validação do Pedido**: A função verifica se o pedido está presente na base de dados antes de ser aprovado.
- **Adição do Pedido na Base de Dados**: Se o pedido for válido, ele é adicionado à base de dados.
- **Aprovação do Pedido**: O pedido é aprovado se a validação for bem-sucedida e adicionado à base de dados.

## Configuração

Antes de usar esta função, é necessário configurar adequadamente as conexões com a base de dados e quaisquer outras configurações necessárias. Certifique-se de ajustar o arquivo `local.settings.json` com as suas configurações locais e as variáveis de ambiente no ambiente de produção.

## Pré-requisitos

- Conta Azure ativa
- Azure Functions Core Tools instalado localmente (para desenvolvimento)
- Configuração adequada do Azure Storage Account para armazenamento de estados e eventos das funções Durable

## Uso

1. Clone este repositório:

```bash
git clone https://github.com/seu-usuario/azure-function-durable-pedido-aprovacao.git
```
2. Execute a função localmente:
```bash
func start
```
3. Teste a função com os dados de pedido apropriados. Exemplo de JSON de pedido:
```json
{
  "Id": 123,
  "NomeComprador": "João",
  "Valor": 100.00,
  "Quantidade": 2
}
```

Observe a saída para verificar se o pedido foi aprovado e adicionado à base de dados com sucesso.
