using InspectionProcessor.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddOptions<InspectionApiOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection("InspectionApi").Bind(options);
            });

        services.AddHttpClient<IInspectionApiClient, InspectionApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InspectionApiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            }
        });
    })
    .Build();

host.Run();
