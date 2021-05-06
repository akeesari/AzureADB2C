using Microsoft.Extensions.DependencyInjection;
using NLog;
using System;

namespace AzureADB2C
{
	class Program
	{
		private static Logger logger = null;

		static void Main(string[] args)
		{
			int exitCode = 0;
			logger = LogManager.GetCurrentClassLogger();
			logger.Info($"AzureADB2C process  - started");


			IServiceCollection services = new ServiceCollection();
			Startup startup = new Startup();
			startup.ConfigureServices(services);

			IServiceProvider serviceProvider = services.BuildServiceProvider();

			try
			{
				// entry to run app
				serviceProvider.GetService<GraphRepository>().Run();
			}
			catch (Exception ex)
			{
				exitCode = -1;
				logger.Error(ex, ex.Message);
				logger.Info($"AzureADB2C process Failed with Exit Code = {exitCode}");
			}

			logger.Info($"AzureADB2C process executed successfully with Exit Code = {exitCode}");
			Environment.Exit(exitCode);

			Console.ReadLine();
		}
	}
}
