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

        services.AddOptions<InspectionEmailOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.ConnectionString =
                    configuration["AcsEmailConnectionString"]
                    ?? configuration["InspectionEmail:ConnectionString"]
                    ?? string.Empty;
                options.FromAddress =
                    configuration["AcsEmailFrom"]
                    ?? configuration["InspectionEmail:FromAddress"]
                    ?? string.Empty;
            });

        services.AddHttpClient<IInspectionApiClient, InspectionApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<InspectionApiOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            }
        });

        services.AddSingleton<IInspectionQueueMessageParser, InspectionQueueMessageParser>();
        services.AddSingleton<IInspectionEmailRenderer, InspectionEmailRenderer>();
        services.AddScoped<IInspectionAttachmentService, InspectionAttachmentService>();
        services.AddScoped<IInspectionEmailSender, InspectionEmailSender>();
        services.AddScoped<IInspectionQueueProcessor, InspectionQueueProcessor>();
    })
    .Build();

host.Run();
