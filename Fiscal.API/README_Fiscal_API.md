# Fiscal.API

API fiscal para emissão de **NFC-e (modelo 65)**, com backend em **C# /
.NET 8**, frontend em **React + Vite**, banco **PostgreSQL** e
integração com a **SEFAZ GO** usando **Zeus NFe/NFCe**.

> **Status atual:** homologação funcionando em nuvem. Login validado
> pelo frontend hospedado e emissão de NFC-e autorizada pela SEFAZ a
> partir da API hospedada no Render.

## 1. Arquitetura atual

``` text
React/Vite -> Vercel -> HTTPS -> Render / Fiscal.API (.NET 8 + Docker)
                                      |
                                      +-> Aiven PostgreSQL
                                      |
                                      +-> Certificado A1/PFX -> SEFAZ GO
```

  Componente     Tecnologia              Hospedagem
  -------------- ----------------------- -------------
  Frontend       React + Vite            Vercel
  Backend        ASP.NET Core / .NET 8   Render
  Container      Docker                  Render
  Banco          PostgreSQL              Aiven
  Autenticação   JWT Bearer              Fiscal.API
  Fiscal         Zeus NFe/NFCe           Fiscal.API
  SEFAZ          Goiás                   Homologação

## 2. Estrutura do repositório

``` text
Fiscal.API/
├── Fiscal.API/             # Backend .NET
│   ├── Controllers/
│   ├── Data/
│   ├── DTOs/
│   ├── Migrations/
│   ├── Models/
│   ├── Schemas/
│   ├── Services/
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Fiscal.API.csproj
│   ├── Dockerfile
│   └── .dockerignore
├── FrontEnd/               # React/Vite
│   ├── public/
│   ├── src/
│   ├── package.json
│   └── vite.config.js
├── .gitignore
└── Fiscal.API.sln
```

Certificados (`*.pfx`, `*.p12`, `*.pem`, `*.key`) **não devem ser
versionados no Git**.

## 3. Backend

Stack: .NET 8, ASP.NET Core Web API, EF Core, Npgsql/PostgreSQL, JWT
Bearer, Swagger, Zeus.Net.NFe.NFCe e Docker.

Configuração fiscal atual:

``` text
UF: GO
Ambiente: Homologação
Modelo: NFC-e 65
Série: 1
```

## 4. Banco de dados

O PostgreSQL está hospedado no **Aiven**. A connection string não fica
no `appsettings.json` do repositório.

``` json
{
  "ConnectionStrings": {
    "FiscalDb": ""
  }
}
```

No Render:

``` text
ConnectionStrings__FiscalDb
```

O valor contém host, porta, banco, usuário, senha e SSL e deve
permanecer secreto.

## 5. JWT

Configuração não secreta:

``` json
"Jwt": {
  "Key": "",
  "Issuer": "Fiscal.API",
  "Audience": "Fiscal.FrontEnd",
  "ExpirationMinutes": 60
}
```

No Render:

``` text
Jwt__Key
```

Endpoint validado:

``` http
POST /api/auth/login
```

Fluxo validado: `Vercel -> Render -> Aiven -> JWT`.

## 6. Certificado digital

O PFX **não é incluído na imagem Docker nem armazenado no GitHub**. Ele
é convertido para Base64 e fornecido ao container por configuração do
ambiente.

Variáveis:

``` text
CERT_PFX_BASE64
Fiscal__SenhaCertificado
```

Na inicialização, a API lê o Base64, reconstrói os bytes, cria
temporariamente `/tmp/certificado.pfx` e configura o caminho usado pela
biblioteca fiscal.

``` csharp
var certificadoBase64 = Environment.GetEnvironmentVariable("CERT_PFX_BASE64");

if (!string.IsNullOrWhiteSpace(certificadoBase64))
{
    var certificadoBytes = Convert.FromBase64String(certificadoBase64);
    var caminhoCertificado = Path.Combine(Path.GetTempPath(), "certificado.pfx");

    File.WriteAllBytes(caminhoCertificado, certificadoBytes);
    builder.Configuration["Fiscal:Certificado"] = caminhoCertificado;
}
```

**Base64 não é criptografia.** `CERT_PFX_BASE64` e
`Fiscal__SenhaCertificado` devem ser tratados como segredos e nunca
aparecer em commits, prints ou logs.

## 7. Render

Variáveis necessárias:

``` text
ConnectionStrings__FiscalDb
Jwt__Key
Fiscal__SenhaCertificado
CERT_PFX_BASE64
```

Configuração de build:

``` text
Language: Docker
Branch: main
Root Directory: Fiscal.API
Dockerfile Path: ./Dockerfile
Docker Build Context Directory: .
Porta: 8080
```

Plano usado em homologação: Free, 0.1 CPU, 512 MB RAM. Instâncias
gratuitas podem entrar em inatividade e atrasar a primeira requisição.

## 8. Docker

``` dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Fiscal.API.csproj", "./"]
RUN dotnet restore "Fiscal.API.csproj"

COPY . .
RUN dotnet publish "Fiscal.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY Schemas ./Schemas

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Fiscal.API.dll"]
```

Os `Schemas` precisam estar disponíveis no container.

## 9. Frontend / Vercel

Configuração:

``` text
Application Preset: Vite
Root Directory: FrontEnd
```

O `apiClient.js` usa:

``` javascript
const API_URL =
  import.meta.env.VITE_API_URL || "https://localhost:7211";
```

Na Vercel:

``` text
VITE_API_URL=https://<dominio-da-api-no-render>
```

Assim o desenvolvimento local continua usando `https://localhost:7211`,
enquanto a Vercel usa a API hospedada.

## 10. CORS

O backend aceita o Vite local e o domínio atual da Vercel:

``` csharp
.WithOrigins(
    "http://localhost:5173",
    "https://fiscal-web-iota.vercel.app"
)
.AllowAnyHeader()
.AllowAnyMethod();
```

Se o projeto Vercel for renomeado ou receber domínio próprio, revisar
essa configuração. Como melhoria futura, tornar a origem de produção
configurável por variável de ambiente.

## 11. NFC-e

Endpoint:

``` http
POST /api/nfce/autorizar
Authorization: Bearer <token>
```

Em homologação já foi validado:

``` text
cStatLote: 104 - Lote processado
cStat: 100 - Autorizado o uso da NF-e
```

Também foi validada emissão a partir da API hospedada no Render para a
SEFAZ GO.

## 12. Testes concluídos

-   Build e execução Docker local
-   Conexão Docker -\> Aiven
-   JWT
-   PFX reconstruído a partir de Base64
-   Emissão NFC-e local
-   Deploy Docker no Render
-   Render -\> Aiven
-   Login via Postman -\> Render
-   Emissão NFC-e Render -\> SEFAZ GO
-   Deploy React/Vite na Vercel
-   `VITE_API_URL`
-   CORS Vercel -\> Render
-   Login pelo frontend hospedado

## 13. Segurança

Nunca versionar:

``` text
*.pfx
*.p12
*.pem
*.key
.env
.env.*
appsettings.Development.json
```

Nunca colocar no código ou README os valores de senha Aiven, connection
string de produção, senha do PFX, PFX em Base64, JWT Key ou tokens JWT.

## 14. Desenvolvimento local

Backend:

``` bash
cd Fiscal.API
dotnet run
```

Frontend:

``` bash
cd FrontEnd
npm install
npm run dev
```

Vite local: `http://localhost:5173`.

## 15. Git e deploy

``` bash
git status
git add .
git commit -m "Descricao da alteracao"
git push origin main
```

Antes de qualquer commit, verificar se nenhum certificado ou segredo foi
incluído.

## 16. Próximos passos

-   [ ] Testar emissão NFC-e diretamente pelo frontend da Vercel
-   [ ] Tornar o CORS de produção configurável por variável de ambiente
-   [ ] Implementar DANFE NFC-e
-   [ ] Implementar consulta de NFC-e
-   [ ] Implementar contingência
-   [ ] Evoluir consumidor final
-   [ ] Implementar formas de pagamento
-   [ ] Preparar ambiente SEFAZ de produção
-   [ ] Revisar armazenamento e rotação de secrets antes da produção
-   [ ] Avaliar domínio próprio
-   [ ] Implementar NF-e modelo 55 futuramente
-   [ ] Revisar logs/observabilidade

## 17. Estado atual

``` text
Frontend / Vercel              OK
CORS                           OK
Fiscal.API / Render            OK
JWT                            OK
Aiven PostgreSQL               OK
PFX via Base64                 OK
SEFAZ GO - Homologação         OK
NFC-e cStat 100                OK
```

## 18. Antes de produção

1.  Revisar configurações fiscais por empresa e UF.
2.  Confirmar CSC/ID CSC de produção.
3.  Validar certificado e vencimento.
4.  Gerar novos secrets de produção.
5.  Revisar CORS.
6.  Garantir que logs não exponham dados sensíveis.
7.  Revisar numeração da NFC-e.
8.  Implementar DANFE, consulta e contingência.
9.  Revisar limitações da hospedagem gratuita.
10. Executar testes completos antes de habilitar clientes reais.

------------------------------------------------------------------------

**Projeto:** Fiscal.API\
**Backend:** .NET 8\
**Frontend:** React/Vite\
**Banco:** PostgreSQL\
**Fiscal:** NFC-e / Zeus\
**Ambiente atual:** SEFAZ GO - Homologação
