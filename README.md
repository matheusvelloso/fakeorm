![.NET](https://github.com/matheusvelloso/fakeorm/workflows/.NET/badge.svg) [![Build Status](https://unboxitrepositorios.visualstudio.com/FakeORM/_apis/build/status/matheusvelloso.fakeorm?branchName=master)](https://unboxitrepositorios.visualstudio.com/FakeORM/_build/latest?definitionId=5&branchName=master)

A library based on EF Core, for development using Azure Tables is more productive. [*Click here*](https://github.com/matheusvelloso/fakeorm/issues) to report bug or request feature.

## Install

	PM> Install-Package FakeOrm.AzureTables -Version 1.0.3
## Configuration

```cs
public class Startup
{
   public IConfiguration Configuration { get; }

   public void ConfigureServices(IServiceCollection services)
   {
     services.UseAzureTablesRepository(Configuration);
     //OR
     services.UseAzureTablesRepository(Configuration.GetConnectionString("AzureTableConnection"));
   }
}
```

In appsettings.json:

```json
{
  "ConnectionStrings": {
    "AzureTableConnection": "DefaultEndpointsProtocol=https;AccountName=**************;AccountKey=*********;EndpointSuffix=core.windows.net"
  }
}

```

## Usage

```csharp
using FakeOrm.AzureTables.Domain;

namespace YourWorkspace.YourAwesomeClass
{
    //inherit from 'BaseEntity'
    public class User : BaseEntity
    {
    	//public constructor without parameters is required
        public User() { }
    }
}
```
