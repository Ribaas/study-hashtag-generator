# HashtagGenerator API

API minimalista desenvolvida em .NET 9 que utiliza modelos de IA (Ollama) para gerar hashtags relevantes a partir de textos fornecidos.

## 🎯 Sobre o Projeto

O HashtagGenerator é uma API REST que recebe um texto e utiliza modelos LLM (via Ollama) para gerar hashtags contextuais e relevantes. A API é ideal para automatizar a criação de hashtags para redes sociais, blogs, marketing digital e outras aplicações que necessitem de marcadores semânticos.

## 👥 Autor

- Desenvolvido por Leonardo Ribas

## ✨ Funcionalidades

- ✅ Geração automática de hashtags baseada em contexto
- ✅ Suporte a múltiplos modelos de IA (Gemma3)
- ✅ Validação robusta de entrada
- ✅ Normalização e filtragem de hashtags
- ✅ Garantia de hashtags únicas
- ✅ Sistema de retry automático
- ✅ Logs detalhados para debugging

## 🚀 Tecnologias

- **.NET 9.0** - Framework principal
- **ASP.NET Core Minimal API** - Framework web minimalista
- **Ollama** - Motor de IA para geração de texto
- **C#** - Linguagem de programação

### Modelos de IA Suportados

- `gemma3:270m` (padrão)
- `gemma3:1b`

## 📦 Pré-requisitos

Antes de começar, certifique-se de ter instalado:

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.ai/) - Para executar os modelos de IA localmente
- [Docker](https://www.docker.com/) (opcional, para execução containerizada)

### Instalando e Configurando o Ollama

1. Instale o Ollama seguindo as instruções em [ollama.ai](https://ollama.ai/)

2. Baixe os modelos necessários:
```bash
ollama pull gemma3:270m
ollama pull gemma3:1b
```

3. Verifique se o Ollama está rodando:
```bash
ollama list
```

O Ollama deve estar acessível em `http://localhost:11434` (porta padrão).

## 🔧 Execução Local

1. Clone o repositório:
```bash
git clone https://github.com/Ribaas/study-hashtag-generator.git
cd study-hashtag-generator
```

2. Execute o projeto na IDE de sua preferência

A API estará disponível em `http://localhost:5126`.

## ⚙️ Configuração

A configuração da aplicação é feita através do arquivo `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "HashtagGenerator": "Information"
    }
  },
  "Ollama": {
    "Url": "http://localhost:11434"
  },
  "HashtagGenerator": {
    "MaxRetries": 10,
    "MaxHashtags": 30
  }
}
```

### Parâmetros de Configuração

| Parâmetro | Descrição | Valor Padrão |
|-----------|-----------|--------------|
| `Ollama:Url` | URL do serviço Ollama | `http://localhost:11434` |
| `HashtagGenerator:MaxRetries` | Número máximo de tentativas para gerar hashtags | `10` |
| `HashtagGenerator:MaxHashtags` | Número máximo de hashtags que podem ser solicitadas | `30` |

## 📖 Uso

### Exemplo de Requisição

**Endpoint:** `POST /hashtags`

**Request Body:**
```json
{
    "text": "Viajando para Angra dos Reis, um paraíso tropical no Rio de Janeiro",
    "count": 10,
    "model": "gemma3:1b"
}
```

**Response (200 OK):**
```json
{
    "count": 10,
    "model": "gemma3:1b",
    "hashtags": [
        "#angradosreis",
        "#rj",
        "#praias",
        "#travelbrazil",
        "#naturelover",
        "#beachlife",
        "#discoverbrazil",
        "#travelphotography",
        "#angraosreis",
        "#turismo"
    ],
    "error": null
}
```

## 📚 API Reference

### POST /hashtags

Gera hashtags baseadas no texto fornecido.

#### Request Body

| Campo | Tipo | Obrigatório | Descrição |
|-------|------|-------------|-----------|
| `text` | string | 🔴 Sim | Texto para geração de hashtags (não pode ser vazio ou apenas espaços) |
| `count` | integer | ⚪ Não | Número de hashtags desejadas (mín: 1, máx: 30, padrão: 10) |
| `model` | string | ⚪ Não | Modelo de IA a ser utilizado (`gemma3:270m` (padrão) ou `gemma3:1b`) |

#### Response Body (Sucesso)

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `count` | integer | Número de hashtags solicitadas |
| `model` | string | Modelo utilizado |
| `hashtags` | array[string] | Lista de hashtags geradas |
| `error` | string? | Mensagem de erro (null se sucesso total) |

#### Códigos de Status

| Código | Descrição |
|--------|-----------|
| `200` | Hashtags geradas com sucesso |
| `400` | Requisição inválida (validação falhou) |
| `500` | Erro interno do servidor |
| `503` | Serviço Ollama indisponível |

## ✅ Validações

A API implementa as seguintes validações:

### Validação de Texto
- ❌ Não pode ser `null`
- ❌ Não pode ser vazio ou conter apenas espaços em branco
- ✅ Deve conter pelo menos um caractere não-espaço

### Validação de Count
- ✅ Mínimo: `1` hashtag
- ✅ Máximo: `30` hashtags
- 🔄 Valores fora do intervalo são normalizados automaticamente

### Validação de Model
- ✅ Modelos aceitos: `gemma3:270m` e `gemma3:1b`
- 🔄 Se não informado, utiliza `gemma3:270m` como padrão
- ❌ Modelos não suportados retornam erro 400

### Normalização de Hashtags

O sistema automaticamente:
- Remove hashtags duplicadas
- Converte para minúsculas
- Remove caracteres especiais inválidos
- Garante que começam com `#`
- Filtra hashtags vazias ou com espaços

## 🏗️ Arquitetura

O projeto segue uma arquitetura em camadas:

```
HashtagGenerator/
├── Program.cs                      # Entry point e configuração da API
├── Models/                         # Modelos de dados
│   ├── HashtagRequest.cs           # DTO de entrada
│   ├── HashtagResponse.cs          # DTO de resposta
│   ├── ErrorResponse.cs            # DTO de erro
│   ├── OllamaRequest.cs            # Request para Ollama
│   └── OllamaResponse.cs           # Response do Ollama
├── Services/                       # Camada de serviços
│   ├── HashtagGeneratorService.cs  # Lógica de geração de hashtags
│   └── OllamaService.cs            # Client HTTP para Ollama
└── Validators/                     # Validadores
    └── HashtagRequestValidator.cs  # Validação de requisições
```

### Fluxo de Funcionamento

1. **Recebimento da Requisição**: API recebe POST em `/hashtags`
2. **Validação**: `HashtagRequestValidator` valida os parâmetros
3. **Geração**: `HashtagGeneratorService` orquestra a geração
4. **Comunicação com IA**: `OllamaService` faz requisições ao Ollama
5. **Processamento**: Hashtags são filtradas, normalizadas e dedupicadas
6. **Retry**: Sistema tenta até 10 vezes se necessário
7. **Resposta**: API retorna JSON com hashtags geradas

## Roadmap Futuro

- [ ] Adicionar Circuit Breaker
- [ ] Criar um docker-compose contendo o Ollama e a API
- [ ] Adicionar OpenAPI/Swagger
- [ ] Criar uma interface web para interação com a API

## 🔗 Links Úteis

- [Ollama](https://ollama.ai/)
