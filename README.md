![.NET](https://github.com/matheusvelloso/fakeorm/workflows/.NET/badge.svg) [![Build Status](https://unboxitrepositorios.visualstudio.com/FakeORM/_apis/build/status/matheusvelloso.fakeorm?branchName=master)](https://unboxitrepositorios.visualstudio.com/FakeORM/_build/latest?definitionId=5&branchName=master)



<!-- PROJECT LOGO -->
<br />
<p align="center">
  <!--<a href="https://github.com/othneildrew/Best-README-Template">
    <img src="images/logo.png" alt="Logo" width="80" height="80">
  </a>-->

  <h3 align="center">FakeORM - Azure Tables</h3>

  <p align="center">
    An awesome ORM to jumpstart your projects!
    <br />
    <a href="https://github.com/matheusvelloso/fakeorm/issues">Report Bug</a>
    ·
    <a href="https://github.com/matheusvelloso/fakeorm/issues">Request Feature</a>
  </p>
</p>

## Install

	PM> Install-Package FakeOrm.AzureTables -Version 1.0.2
## Usage

	services.UseAzureTablesRepository(Configuration);
	//OR
	services.UseAzureTablesRepository(Configuration.GetConnectionString("AzureTableConnection"));
