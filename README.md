# BrickDex

A personal LEGO set collection manager. Track your LEGO sets, manage wishlists, and explore the Rebrickable database.

## Features

- **Collection Management**: Add LEGO sets to your collection with quantity and status tracking
- **Wishlist**: Keep track of sets you want to acquire
- **Search**: Search the Rebrickable database with advanced filters (year, parts, theme)
- **Set Details**: View detailed information about any LEGO set
- **Authentication**: Sign in with Google or GitHub

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Node.js](https://nodejs.org/) (LTS recommended)
- [Yarn](https://yarnpkg.com/) (v4 - included via Corepack)

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/your-username/brick-dex.git
cd brick-dex
```

### 2. Configure API keys

The application requires a Rebrickable API key. Set it using user secrets:

```bash
cd src/BrickDex.Web
dotnet user-secrets set "Rebrickable:ApiKey" "your-api-key-here"
```

Get your free API key at [Rebrickable API](https://rebrickable.com/api/).

For OAuth authentication (optional), configure:

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:Google:ClientSecret" "your-client-secret"
dotnet user-secrets set "Authentication:GitHub:ClientId" "your-client-id"
dotnet user-secrets set "Authentication:GitHub:ClientSecret" "your-client-secret"
```

### 3. Install frontend dependencies

```bash
cd src/BrickDex.Frontend
corepack enable
yarn install
```

### 4. Apply database migrations

```bash
cd src/BrickDex.Web
dotnet ef database update
```

## Running the Application

You need to run both the backend (.NET) and frontend (Vite) projects simultaneously.

### Terminal 1 - Backend

```bash
cd src/BrickDex.Web
dotnet run
```

The backend runs at `https://localhost:5001` by default.

### Terminal 2 - Frontend

```bash
cd src/BrickDex.Frontend
yarn dev
```

The Vite dev server runs at `https://localhost:5173` and proxies requests to the backend.

## Building for Production

### Frontend

```bash
cd src/BrickDex.Frontend
yarn build
```

This outputs optimized assets to `wwwroot` in the web project.

### Backend

```bash
dotnet build -c Release
dotnet publish src/BrickDex.Web -c Release -o ./publish
```

## Project Structure

```
brick-dex/
├── src/
│   ├── BrickDex.Core/          # Domain models, services, interfaces
│   ├── BrickDex.Web/           # ASP.NET Core Razor Pages application
│   └── BrickDex.Frontend/      # Vite + Lit frontend assets
├── test/
│   ├── BrickDex.Core.Tests/    # Unit tests
│   └── BrickDex.TestHelpers/   # Test utilities
└── benchmarks/
    └── BrickDex.Benchmarks/    # Performance benchmarks
```

## Technology Stack

- **Backend**: ASP.NET Core 10, Razor Pages, Entity Framework Core, SQLite
- **Frontend**: Vite, Lit (Web Components), Sass
- **Authentication**: ASP.NET Core Identity with Google/GitHub OAuth
- **API**: Rebrickable API for LEGO set data
- **Testing**: xUnit, FakeItEasy, Vitest

## Running Tests

### .NET Tests

```bash
dotnet test
```

### Frontend Tests

```bash
cd src/BrickDex.Frontend
yarn test
```

## IDE Support

- **Visual Studio**: 17.13 or later (for .slnx format)
- **Rider**: 2024.3.6 or later
- **VS Code**: Latest C# / C# Dev Kit extension

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
